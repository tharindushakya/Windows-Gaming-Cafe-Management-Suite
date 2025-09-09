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
public class ConsolesController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ConsolesController> _logger;

    public ConsolesController(IUnitOfWork unitOfWork, ILogger<ConsolesController> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Get paginated list of game stations with filtering
    /// </summary>
    [HttpGet("stations")]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<ActionResult<PagedResponse<GameStationDto>>> GetGameStations([FromQuery] GetGameStationsRequest request)
    {
        try
        {
            var stations = await _unitOfWork.Repository<GameStation>().GetAllAsync();
            var filteredStations = stations.AsQueryable();

            // Apply filters
            if (request.IsActive.HasValue)
            {
                filteredStations = filteredStations.Where(s => s.IsActive == request.IsActive.Value);
            }

            if (request.IsAvailable.HasValue)
            {
                filteredStations = filteredStations.Where(s => s.IsAvailable == request.IsAvailable.Value);
            }

            if (!string.IsNullOrEmpty(request.StationType))
            {
                filteredStations = filteredStations.Where(s => 
                    s.StationType.ToLower().Contains(request.StationType.ToLower()));
            }

            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                filteredStations = filteredStations.Where(s => 
                    s.StationName.ToLower().Contains(request.SearchTerm.ToLower()) ||
                    s.Location.ToLower().Contains(request.SearchTerm.ToLower()));
            }

            // Apply sorting
            filteredStations = request.SortBy?.ToLower() switch
            {
                "name" => request.SortDescending ? 
                    filteredStations.OrderByDescending(s => s.StationName) : 
                    filteredStations.OrderBy(s => s.StationName),
                "type" => request.SortDescending ? 
                    filteredStations.OrderByDescending(s => s.StationType) : 
                    filteredStations.OrderBy(s => s.StationType),
                "location" => request.SortDescending ? 
                    filteredStations.OrderByDescending(s => s.Location) : 
                    filteredStations.OrderBy(s => s.Location),
                "hourlyrate" => request.SortDescending ? 
                    filteredStations.OrderByDescending(s => s.HourlyRate) : 
                    filteredStations.OrderBy(s => s.HourlyRate),
                "status" => request.SortDescending ? 
                    filteredStations.OrderByDescending(s => s.IsActive) : 
                    filteredStations.OrderBy(s => s.IsActive),
                _ => filteredStations.OrderBy(s => s.StationName)
            };

            var totalCount = filteredStations.Count();
            var pagedStations = filteredStations
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(s => new GameStationDto
                {
                    StationId = s.StationId,
                    StationName = s.StationName,
                    StationType = s.StationType,
                    Location = s.Location,
                    HourlyRate = s.HourlyRate,
                    IsActive = s.IsActive,
                    IsAvailable = s.IsAvailable,
                    LastMaintenance = s.LastMaintenance,
                    Notes = s.Notes,
                    CreatedAt = s.CreatedAt,
                    ActiveSessionCount = s.GameSessions.Count(gs => gs.Status == SessionStatus.Active)
                })
                .ToList();

            var response = new PagedResponse<GameStationDto>
            {
                Data = pagedStations,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving game stations");
            return StatusCode(500, "An error occurred while retrieving game stations");
        }
    }

    /// <summary>
    /// Get game station by ID
    /// </summary>
    [HttpGet("stations/{id}")]
    public async Task<ActionResult<GameStationDto>> GetGameStation(int id)
    {
        try
        {
            var station = await _unitOfWork.Repository<GameStation>().GetByIdAsync(id);
            if (station == null)
                return NotFound();

            var stationDto = new GameStationDto
            {
                StationId = station.StationId,
                StationName = station.StationName,
                StationType = station.StationType,
                Location = station.Location,
                HourlyRate = station.HourlyRate,
                IsActive = station.IsActive,
                IsAvailable = station.IsAvailable,
                LastMaintenance = station.LastMaintenance,
                Notes = station.Notes,
                CreatedAt = station.CreatedAt,
                ActiveSessionCount = station.GameSessions.Count(gs => gs.Status == SessionStatus.Active)
            };

            return Ok(stationDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving game station with ID {StationId}", id);
            return StatusCode(500, "An error occurred while retrieving the game station");
        }
    }

    /// <summary>
    /// Create a new game station
    /// </summary>
    [HttpPost("stations")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<GameStationDto>> CreateGameStation([FromBody] CreateGameStationRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Check for duplicate station name
            var allStations = await _unitOfWork.Repository<GameStation>().GetAllAsync();
            if (allStations.Any(s => s.StationName.ToLower() == request.StationName.ToLower()))
                return BadRequest("A station with this name already exists");

            var station = new GameStation
            {
                StationName = request.StationName,
                StationType = request.StationType,
                Location = request.Location,
                HourlyRate = request.HourlyRate,
                IsActive = request.IsActive,
                IsAvailable = true, // New stations are available by default
                Notes = request.Notes ?? string.Empty,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<GameStation>().AddAsync(station);
            await _unitOfWork.SaveChangesAsync();

            var stationDto = new GameStationDto
            {
                StationId = station.StationId,
                StationName = station.StationName,
                StationType = station.StationType,
                Location = station.Location,
                HourlyRate = station.HourlyRate,
                IsActive = station.IsActive,
                IsAvailable = station.IsAvailable,
                LastMaintenance = station.LastMaintenance,
                Notes = station.Notes,
                CreatedAt = station.CreatedAt,
                ActiveSessionCount = 0
            };

            _logger.LogInformation("Created new game station: {StationName} (ID: {StationId})", 
                station.StationName, station.StationId);

            return CreatedAtAction(nameof(GetGameStation), new { id = station.StationId }, stationDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating game station");
            return StatusCode(500, "An error occurred while creating the game station");
        }
    }

    /// <summary>
    /// Update a game station
    /// </summary>
    [HttpPut("stations/{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<GameStationDto>> UpdateGameStation(int id, [FromBody] UpdateGameStationRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var station = await _unitOfWork.Repository<GameStation>().GetByIdAsync(id);
            if (station == null)
                return NotFound();

            // Check for duplicate station name (excluding current station)
            var allStations = await _unitOfWork.Repository<GameStation>().GetAllAsync();
            if (allStations.Any(s => s.StationId != id && s.StationName.ToLower() == request.StationName.ToLower()))
                return BadRequest("A station with this name already exists");

            // Update properties
            station.StationName = request.StationName;
            station.StationType = request.StationType;
            station.Location = request.Location;
            station.HourlyRate = request.HourlyRate;
            station.IsActive = request.IsActive;
            station.Notes = request.Notes ?? string.Empty;

            if (request.LastMaintenance.HasValue)
            {
                station.LastMaintenance = request.LastMaintenance.Value;
            }

            _unitOfWork.Repository<GameStation>().Update(station);
            await _unitOfWork.SaveChangesAsync();

            var stationDto = new GameStationDto
            {
                StationId = station.StationId,
                StationName = station.StationName,
                StationType = station.StationType,
                Location = station.Location,
                HourlyRate = station.HourlyRate,
                IsActive = station.IsActive,
                IsAvailable = station.IsAvailable,
                LastMaintenance = station.LastMaintenance,
                Notes = station.Notes,
                CreatedAt = station.CreatedAt,
                ActiveSessionCount = station.GameSessions.Count(gs => gs.Status == SessionStatus.Active)
            };

            _logger.LogInformation("Updated game station: {StationName} (ID: {StationId})", 
                station.StationName, station.StationId);

            return Ok(stationDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating game station with ID {StationId}", id);
            return StatusCode(500, "An error occurred while updating the game station");
        }
    }

    /// <summary>
    /// Delete a game station
    /// </summary>
    [HttpDelete("stations/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> DeleteGameStation(int id)
    {
        try
        {
            var station = await _unitOfWork.Repository<GameStation>().GetByIdAsync(id);
            if (station == null)
                return NotFound();

            // Check if station has active sessions
            if (station.GameSessions.Any(gs => gs.Status == SessionStatus.Active))
                return BadRequest("Cannot delete station with active sessions");

            // Instead of hard delete, mark as inactive
            station.IsActive = false;
            station.IsAvailable = false;
            _unitOfWork.Repository<GameStation>().Update(station);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Deactivated game station: {StationName} (ID: {StationId})", 
                station.StationName, station.StationId);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting game station with ID {StationId}", id);
            return StatusCode(500, "An error occurred while deleting the game station");
        }
    }

    /// <summary>
    /// Set station availability
    /// </summary>
    [HttpPost("stations/{id}/availability")]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<ActionResult<GameStationDto>> SetStationAvailability(int id, [FromBody] SetStationAvailabilityRequest request)
    {
        try
        {
            var station = await _unitOfWork.Repository<GameStation>().GetByIdAsync(id);
            if (station == null)
                return NotFound();

            // Check if station has active sessions when trying to mark unavailable
            if (!request.IsAvailable && station.GameSessions.Any(gs => gs.Status == SessionStatus.Active))
                return BadRequest("Cannot mark station as unavailable with active sessions");

            station.IsAvailable = request.IsAvailable;

            if (!string.IsNullOrEmpty(request.Reason))
            {
                station.Notes = string.IsNullOrEmpty(station.Notes) 
                    ? request.Reason 
                    : $"{station.Notes}; {request.Reason}";
            }

            _unitOfWork.Repository<GameStation>().Update(station);
            await _unitOfWork.SaveChangesAsync();

            var stationDto = new GameStationDto
            {
                StationId = station.StationId,
                StationName = station.StationName,
                StationType = station.StationType,
                Location = station.Location,
                HourlyRate = station.HourlyRate,
                IsActive = station.IsActive,
                IsAvailable = station.IsAvailable,
                LastMaintenance = station.LastMaintenance,
                Notes = station.Notes,
                CreatedAt = station.CreatedAt,
                ActiveSessionCount = station.GameSessions.Count(gs => gs.Status == SessionStatus.Active)
            };

            _logger.LogInformation("Set station {StationId} availability to {IsAvailable}", id, request.IsAvailable);
            return Ok(stationDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting availability for station {StationId}", id);
            return StatusCode(500, "An error occurred while setting station availability");
        }
    }

    /// <summary>
    /// Record maintenance for a station
    /// </summary>
    [HttpPost("stations/{id}/maintenance")]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<ActionResult<GameStationDto>> RecordMaintenance(int id, [FromBody] RecordMaintenanceRequest request)
    {
        try
        {
            var station = await _unitOfWork.Repository<GameStation>().GetByIdAsync(id);
            if (station == null)
                return NotFound();

            station.LastMaintenance = DateTime.UtcNow;
            
            if (!string.IsNullOrEmpty(request.Notes))
            {
                var maintenanceNote = $"Maintenance {DateTime.UtcNow:yyyy-MM-dd}: {request.Notes}";
                station.Notes = string.IsNullOrEmpty(station.Notes) 
                    ? maintenanceNote 
                    : $"{station.Notes}; {maintenanceNote}";
            }

            _unitOfWork.Repository<GameStation>().Update(station);
            await _unitOfWork.SaveChangesAsync();

            var stationDto = new GameStationDto
            {
                StationId = station.StationId,
                StationName = station.StationName,
                StationType = station.StationType,
                Location = station.Location,
                HourlyRate = station.HourlyRate,
                IsActive = station.IsActive,
                IsAvailable = station.IsAvailable,
                LastMaintenance = station.LastMaintenance,
                Notes = station.Notes,
                CreatedAt = station.CreatedAt,
                ActiveSessionCount = station.GameSessions.Count(gs => gs.Status == SessionStatus.Active)
            };

            _logger.LogInformation("Recorded maintenance for station {StationId}", id);
            return Ok(stationDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording maintenance for station {StationId}", id);
            return StatusCode(500, "An error occurred while recording maintenance");
        }
    }

    /// <summary>
    /// Get console systems
    /// </summary>
    [HttpGet("systems")]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<ActionResult<PagedResponse<GameConsoleDto>>> GetGameConsoles([FromQuery] GetGameConsolesRequest request)
    {
        try
        {
            var consoles = await _unitOfWork.Repository<GameConsole>().GetAllAsync();
            var filteredConsoles = consoles.AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(request.ConsoleName))
            {
                filteredConsoles = filteredConsoles.Where(c => 
                    c.ConsoleName.ToLower().Contains(request.ConsoleName.ToLower()));
            }

            if (!string.IsNullOrEmpty(request.Model))
            {
                filteredConsoles = filteredConsoles.Where(c => 
                    c.Model.ToLower().Contains(request.Model.ToLower()));
            }

            if (request.IsActive.HasValue)
            {
                filteredConsoles = filteredConsoles.Where(c => c.IsActive == request.IsActive.Value);
            }

            // Apply sorting
            filteredConsoles = request.SortBy?.ToLower() switch
            {
                "name" => request.SortDescending ? 
                    filteredConsoles.OrderByDescending(c => c.ConsoleName) : 
                    filteredConsoles.OrderBy(c => c.ConsoleName),
                "model" => request.SortDescending ? 
                    filteredConsoles.OrderByDescending(c => c.Model) : 
                    filteredConsoles.OrderBy(c => c.Model),
                "purchasedate" => request.SortDescending ? 
                    filteredConsoles.OrderByDescending(c => c.PurchaseDate) : 
                    filteredConsoles.OrderBy(c => c.PurchaseDate),
                _ => filteredConsoles.OrderBy(c => c.ConsoleName)
            };

            var totalCount = filteredConsoles.Count();
            var pagedConsoles = filteredConsoles
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(c => new GameConsoleDto
                {
                    ConsoleId = c.ConsoleId,
                    ConsoleName = c.ConsoleName,
                    Model = c.Model,
                    SerialNumber = c.SerialNumber,
                    PurchaseDate = c.PurchaseDate,
                    PurchasePrice = c.PurchasePrice,
                    IsActive = c.IsActive,
                    WarrantyExpiryDate = c.WarrantyExpiryDate,
                    Notes = c.Notes,
                    CreatedAt = c.CreatedAt
                })
                .ToList();

            var response = new PagedResponse<GameConsoleDto>
            {
                Data = pagedConsoles,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving game consoles");
            return StatusCode(500, "An error occurred while retrieving game consoles");
        }
    }

    /// <summary>
    /// Get available stations
    /// </summary>
    [HttpGet("stations/available")]
    public async Task<ActionResult<List<GameStationDto>>> GetAvailableStations()
    {
        try
        {
            var allStations = await _unitOfWork.Repository<GameStation>().GetAllAsync();
            var availableStations = allStations
                .Where(s => s.IsActive && s.IsAvailable)
                .Select(s => new GameStationDto
                {
                    StationId = s.StationId,
                    StationName = s.StationName,
                    StationType = s.StationType,
                    Location = s.Location,
                    HourlyRate = s.HourlyRate,
                    IsActive = s.IsActive,
                    IsAvailable = s.IsAvailable,
                    LastMaintenance = s.LastMaintenance,
                    Notes = s.Notes,
                    CreatedAt = s.CreatedAt,
                    ActiveSessionCount = 0
                })
                .ToList();

            return Ok(availableStations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving available stations");
            return StatusCode(500, "An error occurred while retrieving available stations");
        }
    }
}

// DTOs and Request/Response Models
public class GameStationDto
{
    public int StationId { get; set; }
    public string StationName { get; set; } = string.Empty;
    public string StationType { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public decimal HourlyRate { get; set; }
    public bool IsActive { get; set; }
    public bool IsAvailable { get; set; }
    public DateTime? LastMaintenance { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int ActiveSessionCount { get; set; }
}

public class GameConsoleDto
{
    public int ConsoleId { get; set; }
    public string ConsoleName { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public DateTime? PurchaseDate { get; set; }
    public decimal? PurchasePrice { get; set; }
    public bool IsActive { get; set; }
    public DateTime? WarrantyExpiryDate { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class GetGameStationsRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public bool? IsActive { get; set; }
    public bool? IsAvailable { get; set; }
    public string? StationType { get; set; }
    public string? SearchTerm { get; set; }
    public string? SortBy { get; set; } = "Name";
    public bool SortDescending { get; set; } = false;
}

public class GetGameConsolesRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? ConsoleName { get; set; }
    public string? Model { get; set; }
    public bool? IsActive { get; set; }
    public string? SortBy { get; set; } = "Name";
    public bool SortDescending { get; set; } = false;
}

public class CreateGameStationRequest
{
    [Required]
    [StringLength(100)]
    public string StationName { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string StationType { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Location { get; set; } = string.Empty;

    [Required]
    [Range(0.01, 999.99)]
    public decimal HourlyRate { get; set; }

    public bool IsActive { get; set; } = true;

    [StringLength(500)]
    public string? Notes { get; set; }
}

public class UpdateGameStationRequest
{
    [Required]
    [StringLength(100)]
    public string StationName { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string StationType { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Location { get; set; } = string.Empty;

    [Required]
    [Range(0.01, 999.99)]
    public decimal HourlyRate { get; set; }

    public bool IsActive { get; set; }

    public DateTime? LastMaintenance { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}

public class SetStationAvailabilityRequest
{
    public bool IsAvailable { get; set; }

    [StringLength(200)]
    public string? Reason { get; set; }
}

public class RecordMaintenanceRequest
{
    [StringLength(500)]
    public string? Notes { get; set; }
}

