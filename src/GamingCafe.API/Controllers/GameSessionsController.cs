using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using GamingCafe.Core.Models;
using GamingCafe.Core.Interfaces;
using GamingCafe.Data.Repositories;
using System.ComponentModel.DataAnnotations;

namespace GamingCafe.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class GameSessionsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GameSessionsController> _logger;

    public GameSessionsController(IUnitOfWork unitOfWork, ILogger<GameSessionsController> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Get paginated list of game sessions with filtering
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<ActionResult<PagedResponse<GameSessionDto>>> GetGameSessions([FromQuery] GetGameSessionsRequest request)
    {
        try
        {
            var sessions = await _unitOfWork.Repository<GameSession>().GetAllAsync();
            var filteredSessions = sessions.AsQueryable();

            // Apply filters
            if (request.UserId.HasValue)
            {
                filteredSessions = filteredSessions.Where(s => s.UserId == request.UserId.Value);
            }

            if (request.StationId.HasValue)
            {
                filteredSessions = filteredSessions.Where(s => s.StationId == request.StationId.Value);
            }

            if (request.Status.HasValue)
            {
                filteredSessions = filteredSessions.Where(s => s.Status == request.Status.Value);
            }

            if (request.StartDate.HasValue)
            {
                filteredSessions = filteredSessions.Where(s => s.StartTime >= request.StartDate.Value);
            }

            if (request.EndDate.HasValue)
            {
                filteredSessions = filteredSessions.Where(s => s.StartTime <= request.EndDate.Value);
            }

            if (request.IsActive.HasValue)
            {
                if (request.IsActive.Value)
                {
                    filteredSessions = filteredSessions.Where(s => s.Status == SessionStatus.Active);
                }
                else
                {
                    filteredSessions = filteredSessions.Where(s => s.Status != SessionStatus.Active);
                }
            }

            // Apply sorting
            filteredSessions = request.SortBy?.ToLower() switch
            {
                "starttime" => request.SortDescending ? 
                    filteredSessions.OrderByDescending(s => s.StartTime) : 
                    filteredSessions.OrderBy(s => s.StartTime),
                "duration" => request.SortDescending ? 
                    filteredSessions.OrderByDescending(s => s.Duration) : 
                    filteredSessions.OrderBy(s => s.Duration),
                "totalcost" => request.SortDescending ? 
                    filteredSessions.OrderByDescending(s => s.TotalCost) : 
                    filteredSessions.OrderBy(s => s.TotalCost),
                "status" => request.SortDescending ? 
                    filteredSessions.OrderByDescending(s => s.Status) : 
                    filteredSessions.OrderBy(s => s.Status),
                _ => filteredSessions.OrderByDescending(s => s.StartTime)
            };

            var totalCount = filteredSessions.Count();
            var pagedSessions = filteredSessions
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(s => new GameSessionDto
                {
                    SessionId = s.SessionId,
                    UserId = s.UserId,
                    StationId = s.StationId,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    Duration = s.Duration ?? TimeSpan.Zero,
                    Status = s.Status.ToString(),
                    TotalCost = s.TotalCost,
                    Notes = s.Notes,
                    CreatedAt = s.CreatedAt,
                    Username = s.User.Username,
                    StationName = s.Station.StationName
                })
                .ToList();

            var response = new PagedResponse<GameSessionDto>
            {
                Data = pagedSessions,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving game sessions");
            return StatusCode(500, "An error occurred while retrieving game sessions");
        }
    }

    /// <summary>
    /// Get game session by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<GameSessionDto>> GetGameSession(int id)
    {
        try
        {
            var session = await _unitOfWork.Repository<GameSession>().GetByIdAsync(id);
            if (session == null)
                return NotFound();

            var sessionDto = new GameSessionDto
            {
                SessionId = session.SessionId,
                UserId = session.UserId,
                StationId = session.StationId,
                StartTime = session.StartTime,
                EndTime = session.EndTime,
                Duration = session.Duration ?? TimeSpan.Zero,
                Status = session.Status.ToString(),
                TotalCost = session.TotalCost,
                Notes = session.Notes,
                CreatedAt = session.CreatedAt,
                Username = session.User.Username,
                StationName = session.Station.StationName
            };

            return Ok(sessionDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving game session with ID {SessionId}", id);
            return StatusCode(500, "An error occurred while retrieving the game session");
        }
    }

    /// <summary>
    /// Start a new game session
    /// </summary>
    [HttpPost("start")]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<ActionResult<GameSessionDto>> StartGameSession([FromBody] StartGameSessionRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Validate user exists
            var user = await _unitOfWork.Repository<User>().GetByIdAsync(request.UserId);
            if (user == null)
                return BadRequest("User not found");

            // Validate station exists and is available
            var station = await _unitOfWork.Repository<GameStation>().GetByIdAsync(request.StationId);
            if (station == null)
                return BadRequest("Game station not found");

            if (!station.IsActive)
                return BadRequest("Game station is not active");

            if (!station.IsAvailable)
                return BadRequest("Game station is currently occupied");

            // Check for existing active session for user
            var allSessions = await _unitOfWork.Repository<GameSession>().GetAllAsync();
            var activeUserSession = allSessions.FirstOrDefault(s => 
                s.UserId == request.UserId && s.Status == SessionStatus.Active);

            if (activeUserSession != null)
                return BadRequest("User already has an active session");

            // Check for existing active session on station
            var activeStationSession = allSessions.FirstOrDefault(s => 
                s.StationId == request.StationId && s.Status == SessionStatus.Active);

            if (activeStationSession != null)
                return BadRequest("Station already has an active session");

            var session = new GameSession
            {
                UserId = request.UserId,
                StationId = request.StationId,
                StartTime = DateTime.UtcNow,
                Status = SessionStatus.Active,
                Notes = request.Notes ?? string.Empty,
                CreatedAt = DateTime.UtcNow
            };

            // Mark station as occupied
            station.IsAvailable = false;

            await _unitOfWork.Repository<GameSession>().AddAsync(session);
            _unitOfWork.Repository<GameStation>().Update(station);
            await _unitOfWork.SaveChangesAsync();

            var sessionDto = new GameSessionDto
            {
                SessionId = session.SessionId,
                UserId = session.UserId,
                StationId = session.StationId,
                StartTime = session.StartTime,
                EndTime = session.EndTime,
                Duration = session.Duration ?? TimeSpan.Zero,
                Status = session.Status.ToString(),
                TotalCost = session.TotalCost,
                Notes = session.Notes,
                CreatedAt = session.CreatedAt,
                Username = user.Username,
                StationName = station.StationName
            };

            _logger.LogInformation("Started new game session: {SessionId} for user {UserId} at station {StationId}", 
                session.SessionId, request.UserId, request.StationId);
            
            return CreatedAtAction(nameof(GetGameSession), new { id = session.SessionId }, sessionDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting game session");
            return StatusCode(500, "An error occurred while starting the game session");
        }
    }

    /// <summary>
    /// End an active game session
    /// </summary>
    [HttpPost("{id}/end")]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<ActionResult<GameSessionDto>> EndGameSession(int id, [FromBody] EndGameSessionRequest? request = null)
    {
        try
        {
            var session = await _unitOfWork.Repository<GameSession>().GetByIdAsync(id);
            if (session == null)
                return NotFound();

            if (session.Status != SessionStatus.Active)
                return BadRequest("Can only end active sessions");

            var endTime = DateTime.UtcNow;
            var duration = endTime - session.StartTime;

            // Get station for cost calculation
            var station = await _unitOfWork.Repository<GameStation>().GetByIdAsync(session.StationId);
            var totalCost = (decimal)duration.TotalHours * station!.HourlyRate;

            // Update session
            session.EndTime = endTime;
            session.Status = SessionStatus.Completed;
            session.TotalCost = totalCost;

            if (!string.IsNullOrEmpty(request?.Notes))
            {
                session.Notes = string.IsNullOrEmpty(session.Notes) 
                    ? request.Notes 
                    : $"{session.Notes}; {request.Notes}";
            }

            // Mark station as available
            station.IsAvailable = true;

            // Create transaction for the session
            var transaction = new Transaction
            {
                UserId = session.UserId,
                SessionId = session.SessionId,
                Description = $"Game session at {station.StationName}",
                Amount = totalCost,
                Type = TransactionType.GameTime,
                PaymentMethod = PaymentMethod.Cash, // Default, should be specified by request
                Status = TransactionStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _unitOfWork.Repository<GameSession>().Update(session);
            _unitOfWork.Repository<GameStation>().Update(station);
            await _unitOfWork.Repository<Transaction>().AddAsync(transaction);
            await _unitOfWork.SaveChangesAsync();

            var sessionDto = new GameSessionDto
            {
                SessionId = session.SessionId,
                UserId = session.UserId,
                StationId = session.StationId,
                StartTime = session.StartTime,
                EndTime = session.EndTime,
                Duration = session.Duration ?? TimeSpan.Zero,
                Status = session.Status.ToString(),
                TotalCost = session.TotalCost,
                Notes = session.Notes,
                CreatedAt = session.CreatedAt,
                Username = session.User.Username,
                StationName = station.StationName
            };

            _logger.LogInformation("Ended game session {SessionId}, Duration: {Duration}, Cost: {TotalCost}", 
                id, duration, totalCost);

            return Ok(sessionDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ending game session with ID {SessionId}", id);
            return StatusCode(500, "An error occurred while ending the game session");
        }
    }

    /// <summary>
    /// Pause an active game session
    /// </summary>
    [HttpPost("{id}/pause")]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<ActionResult<GameSessionDto>> PauseGameSession(int id, [FromBody] PauseGameSessionRequest? request = null)
    {
        try
        {
            var session = await _unitOfWork.Repository<GameSession>().GetByIdAsync(id);
            if (session == null)
                return NotFound();

            if (session.Status != SessionStatus.Active)
                return BadRequest("Can only pause active sessions");

            session.Status = SessionStatus.Paused;

            if (!string.IsNullOrEmpty(request?.Reason))
            {
                session.Notes = string.IsNullOrEmpty(session.Notes) 
                    ? $"Paused: {request.Reason}" 
                    : $"{session.Notes}; Paused: {request.Reason}";
            }

            _unitOfWork.Repository<GameSession>().Update(session);
            await _unitOfWork.SaveChangesAsync();

            var sessionDto = new GameSessionDto
            {
                SessionId = session.SessionId,
                UserId = session.UserId,
                StationId = session.StationId,
                StartTime = session.StartTime,
                EndTime = session.EndTime,
                Duration = session.Duration ?? TimeSpan.Zero,
                Status = session.Status.ToString(),
                TotalCost = session.TotalCost,
                Notes = session.Notes,
                CreatedAt = session.CreatedAt,
                Username = session.User?.Username ?? "Unknown",
                StationName = session.Station?.StationName ?? "Unknown"
            };

            _logger.LogInformation("Paused game session {SessionId}", id);
            return Ok(sessionDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pausing game session with ID {SessionId}", id);
            return StatusCode(500, "An error occurred while pausing the game session");
        }
    }

    /// <summary>
    /// Resume a paused game session
    /// </summary>
    [HttpPost("{id}/resume")]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<ActionResult<GameSessionDto>> ResumeGameSession(int id)
    {
        try
        {
            var session = await _unitOfWork.Repository<GameSession>().GetByIdAsync(id);
            if (session == null)
                return NotFound();

            if (session.Status != SessionStatus.Paused)
                return BadRequest("Can only resume paused sessions");

            session.Status = SessionStatus.Active;
            session.Notes = string.IsNullOrEmpty(session.Notes) 
                ? "Resumed" 
                : $"{session.Notes}; Resumed";

            _unitOfWork.Repository<GameSession>().Update(session);
            await _unitOfWork.SaveChangesAsync();

            var sessionDto = new GameSessionDto
            {
                SessionId = session.SessionId,
                UserId = session.UserId,
                StationId = session.StationId,
                StartTime = session.StartTime,
                EndTime = session.EndTime,
                Duration = session.Duration ?? TimeSpan.Zero,
                Status = session.Status.ToString(),
                TotalCost = session.TotalCost,
                Notes = session.Notes,
                CreatedAt = session.CreatedAt,
                Username = session.User?.Username ?? "Unknown",
                StationName = session.Station?.StationName ?? "Unknown"
            };

            _logger.LogInformation("Resumed game session {SessionId}", id);
            return Ok(sessionDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resuming game session with ID {SessionId}", id);
            return StatusCode(500, "An error occurred while resuming the game session");
        }
    }

    /// <summary>
    /// Get active sessions
    /// </summary>
    [HttpGet("active")]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<ActionResult<List<GameSessionDto>>> GetActiveSessions()
    {
        try
        {
            var allSessions = await _unitOfWork.Repository<GameSession>().GetAllAsync();
            var activeSessions = allSessions
                .Where(s => s.Status == SessionStatus.Active)
                .Select(s => new GameSessionDto
                {
                    SessionId = s.SessionId,
                    UserId = s.UserId,
                    StationId = s.StationId,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    Duration = s.Duration ?? TimeSpan.Zero,
                    Status = s.Status.ToString(),
                    TotalCost = s.TotalCost,
                    Notes = s.Notes,
                    CreatedAt = s.CreatedAt,
                    Username = s.User.Username,
                    StationName = s.Station.StationName
                })
                .ToList();

            return Ok(activeSessions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active sessions");
            return StatusCode(500, "An error occurred while retrieving active sessions");
        }
    }

    /// <summary>
    /// Get user's session history
    /// </summary>
    [HttpGet("user/{userId}")]
    public async Task<ActionResult<PagedResponse<GameSessionDto>>> GetUserSessions(int userId, [FromQuery] GetUserSessionsRequest request)
    {
        try
        {
            var allSessions = await _unitOfWork.Repository<GameSession>().GetAllAsync();
            var userSessions = allSessions
                .Where(s => s.UserId == userId)
                .AsQueryable();

            if (request.Status.HasValue)
            {
                userSessions = userSessions.Where(s => s.Status == request.Status.Value);
            }

            if (request.StartDate.HasValue)
            {
                userSessions = userSessions.Where(s => s.StartTime >= request.StartDate.Value);
            }

            if (request.EndDate.HasValue)
            {
                userSessions = userSessions.Where(s => s.StartTime <= request.EndDate.Value);
            }

            userSessions = userSessions.OrderByDescending(s => s.StartTime);

            var totalCount = userSessions.Count();
            var pagedSessions = userSessions
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(s => new GameSessionDto
                {
                    SessionId = s.SessionId,
                    UserId = s.UserId,
                    StationId = s.StationId,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    Duration = s.Duration ?? TimeSpan.Zero,
                    Status = s.Status.ToString(),
                    TotalCost = s.TotalCost,
                    Notes = s.Notes,
                    CreatedAt = s.CreatedAt,
                    Username = s.User.Username,
                    StationName = s.Station.StationName
                })
                .ToList();

            var response = new PagedResponse<GameSessionDto>
            {
                Data = pagedSessions,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sessions for user {UserId}", userId);
            return StatusCode(500, "An error occurred while retrieving user sessions");
        }
    }
}

// DTOs and Request/Response Models
public class GameSessionDto
{
    public int SessionId { get; set; }
    public int UserId { get; set; }
    public int StationId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public TimeSpan Duration { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalCost { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string Username { get; set; } = string.Empty;
    public string StationName { get; set; } = string.Empty;
}

public class GetGameSessionsRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int? UserId { get; set; }
    public int? StationId { get; set; }
    public SessionStatus? Status { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool? IsActive { get; set; }
    public string? SortBy { get; set; } = "StartTime";
    public bool SortDescending { get; set; } = true;
}

public class StartGameSessionRequest
{
    [Required]
    public int UserId { get; set; }

    [Required]
    public int StationId { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}

public class EndGameSessionRequest
{
    [StringLength(500)]
    public string? Notes { get; set; }
}

public class PauseGameSessionRequest
{
    [StringLength(200)]
    public string? Reason { get; set; }
}

public class GetUserSessionsRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public SessionStatus? Status { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

