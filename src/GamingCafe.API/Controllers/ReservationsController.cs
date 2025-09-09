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
public class ReservationsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ReservationsController> _logger;

    public ReservationsController(IUnitOfWork unitOfWork, ILogger<ReservationsController> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Get paginated list of reservations with filtering
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResponse<ReservationDto>>> GetReservations([FromQuery] GetReservationsRequest request)
    {
        try
        {
            var reservations = await _unitOfWork.Repository<Reservation>().GetAllAsync();
            var filteredReservations = reservations.AsQueryable();

            // Apply filters
            if (request.UserId.HasValue)
            {
                filteredReservations = filteredReservations.Where(r => r.UserId == request.UserId.Value);
            }

            if (request.StationId.HasValue)
            {
                filteredReservations = filteredReservations.Where(r => r.StationId == request.StationId.Value);
            }

            if (request.Status.HasValue)
            {
                filteredReservations = filteredReservations.Where(r => r.Status == request.Status.Value);
            }

            if (request.StartDate.HasValue)
            {
                filteredReservations = filteredReservations.Where(r => r.ReservationDate >= request.StartDate.Value);
            }

            if (request.EndDate.HasValue)
            {
                filteredReservations = filteredReservations.Where(r => r.ReservationDate <= request.EndDate.Value);
            }

            // Apply sorting
            filteredReservations = filteredReservations.OrderByDescending(r => r.CreatedAt);

            var totalCount = filteredReservations.Count();
            var pagedReservations = filteredReservations
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(r => new ReservationDto
                {
                    ReservationId = r.ReservationId,
                    UserId = r.UserId,
                    StationId = r.StationId,
                    ReservationDate = r.ReservationDate,
                    StartTime = r.StartTime,
                    EndTime = r.EndTime,
                    Status = r.Status.ToString(),
                    EstimatedCost = r.EstimatedCost,
                    Notes = r.Notes,
                    CreatedAt = r.CreatedAt,
                    CancelledAt = r.CancelledAt,
                    Username = r.User.Username,
                    StationName = r.Station.StationName
                })
                .ToList();

            var response = new PagedResponse<ReservationDto>
            {
                Data = pagedReservations,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reservations");
            return StatusCode(500, "An error occurred while retrieving reservations");
        }
    }

    /// <summary>
    /// Get reservation by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ReservationDto>> GetReservation(int id)
    {
        try
        {
            var reservation = await _unitOfWork.Repository<Reservation>().GetByIdAsync(id);
            if (reservation == null)
                return NotFound();

            var reservationDto = new ReservationDto
            {
                ReservationId = reservation.ReservationId,
                UserId = reservation.UserId,
                StationId = reservation.StationId,
                ReservationDate = reservation.ReservationDate,
                StartTime = reservation.StartTime,
                EndTime = reservation.EndTime,
                Status = reservation.Status.ToString(),
                EstimatedCost = reservation.EstimatedCost,
                Notes = reservation.Notes,
                CreatedAt = reservation.CreatedAt,
                CancelledAt = reservation.CancelledAt,
                Username = reservation.User.Username,
                StationName = reservation.Station.StationName
            };

            return Ok(reservationDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reservation with ID {ReservationId}", id);
            return StatusCode(500, "An error occurred while retrieving the reservation");
        }
    }

    /// <summary>
    /// Create a new reservation
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ReservationDto>> CreateReservation([FromBody] CreateReservationRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Validate user exists
            var user = await _unitOfWork.Repository<User>().GetByIdAsync(request.UserId);
            if (user == null)
                return BadRequest("User not found");

            // Validate station exists
            var station = await _unitOfWork.Repository<GameStation>().GetByIdAsync(request.StationId);
            if (station == null)
                return BadRequest("Game station not found");

            if (!station.IsActive)
                return BadRequest("Game station is not active");

            // Validate time slot
            if (request.StartTime >= request.EndTime)
                return BadRequest("Start time must be before end time");

            if (request.ReservationDate.Date < DateTime.Today)
                return BadRequest("Cannot make reservations for past dates");

            // Check for conflicts
            var allReservations = await _unitOfWork.Repository<Reservation>().GetAllAsync();
            var conflictingReservations = allReservations.Where(r => 
                r.StationId == request.StationId &&
                r.ReservationDate.Date == request.ReservationDate.Date &&
                r.Status != ReservationStatus.Cancelled &&
                r.Status != ReservationStatus.Completed &&
                ((request.StartTime >= r.StartTime && request.StartTime < r.EndTime) ||
                 (request.EndTime > r.StartTime && request.EndTime <= r.EndTime) ||
                 (request.StartTime <= r.StartTime && request.EndTime >= r.EndTime))
            ).ToList();

            if (conflictingReservations.Any())
                return BadRequest("Time slot conflicts with existing reservation");

            // Calculate cost
            var duration = request.EndTime - request.StartTime;
            var estimatedCost = (decimal)duration.TotalHours * station.HourlyRate;

            var reservation = new Reservation
            {
                UserId = request.UserId,
                StationId = request.StationId,
                ReservationDate = request.ReservationDate.Date,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                Status = ReservationStatus.Pending,
                EstimatedCost = estimatedCost,
                Notes = request.Notes!,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<Reservation>().AddAsync(reservation);
            await _unitOfWork.SaveChangesAsync();

            var reservationDto = new ReservationDto
            {
                ReservationId = reservation.ReservationId,
                UserId = reservation.UserId,
                StationId = reservation.StationId,
                ReservationDate = reservation.ReservationDate,
                StartTime = reservation.StartTime,
                EndTime = reservation.EndTime,
                Status = reservation.Status.ToString(),
                EstimatedCost = reservation.EstimatedCost,
                Notes = reservation.Notes,
                CreatedAt = reservation.CreatedAt,
                CancelledAt = reservation.CancelledAt,
                Username = user.Username,
                StationName = station.StationName
            };

            _logger.LogInformation("Created new reservation: {ReservationId} for user {UserId} at station {StationId}", 
                reservation.ReservationId, request.UserId, request.StationId);
            
            return CreatedAtAction(nameof(GetReservation), new { id = reservation.ReservationId }, reservationDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating reservation");
            return StatusCode(500, "An error occurred while creating the reservation");
        }
    }

    /// <summary>
    /// Update reservation status
    /// </summary>
    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<ActionResult<ReservationDto>> UpdateReservationStatus(int id, [FromBody] UpdateReservationStatusRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var reservation = await _unitOfWork.Repository<Reservation>().GetByIdAsync(id);
            if (reservation == null)
                return NotFound();

            var oldStatus = reservation.Status;
            reservation.Status = request.Status;

            if (request.Status == ReservationStatus.Cancelled)
            {
                reservation.CancelledAt = DateTime.UtcNow;
                if (!string.IsNullOrEmpty(request.Notes))
                {
                    reservation.CancellationReason = request.Notes;
                }
            }

            if (!string.IsNullOrEmpty(request.Notes) && request.Status != ReservationStatus.Cancelled)
            {
                reservation.Notes = string.IsNullOrEmpty(reservation.Notes) 
                    ? request.Notes 
                    : $"{reservation.Notes}; {request.Notes}";
            }

            _unitOfWork.Repository<Reservation>().Update(reservation);
            await _unitOfWork.SaveChangesAsync();

            var reservationDto = new ReservationDto
            {
                ReservationId = reservation.ReservationId,
                UserId = reservation.UserId,
                StationId = reservation.StationId,
                ReservationDate = reservation.ReservationDate,
                StartTime = reservation.StartTime,
                EndTime = reservation.EndTime,
                Status = reservation.Status.ToString(),
                EstimatedCost = reservation.EstimatedCost,
                Notes = reservation.Notes,
                CreatedAt = reservation.CreatedAt,
                CancelledAt = reservation.CancelledAt,
                Username = reservation.User?.Username ?? "Unknown",
                StationName = reservation.Station?.StationName ?? "Unknown"
            };

            _logger.LogInformation("Updated reservation {ReservationId} status from {OldStatus} to {NewStatus}", 
                id, oldStatus, request.Status);

            return Ok(reservationDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating reservation status for ID {ReservationId}", id);
            return StatusCode(500, "An error occurred while updating the reservation status");
        }
    }

    /// <summary>
    /// Cancel a reservation
    /// </summary>
    [HttpPost("{id}/cancel")]
    public async Task<ActionResult> CancelReservation(int id, [FromBody] CancelReservationRequest? request = null)
    {
        try
        {
            var reservation = await _unitOfWork.Repository<Reservation>().GetByIdAsync(id);
            if (reservation == null)
                return NotFound();

            if (reservation.Status == ReservationStatus.Completed)
                return BadRequest("Cannot cancel completed reservations");

            if (reservation.Status == ReservationStatus.Cancelled)
                return BadRequest("Reservation is already cancelled");

            reservation.Status = ReservationStatus.Cancelled;
            reservation.CancelledAt = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(request?.Reason))
            {
                reservation.CancellationReason = request.Reason;
            }

            _unitOfWork.Repository<Reservation>().Update(reservation);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Cancelled reservation {ReservationId}", id);
            return Ok(new { message = "Reservation cancelled successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling reservation with ID {ReservationId}", id);
            return StatusCode(500, "An error occurred while cancelling the reservation");
        }
    }
}

// DTOs and Request/Response Models
public class ReservationDto
{
    public int ReservationId { get; set; }
    public int UserId { get; set; }
    public int StationId { get; set; }
    public DateTime ReservationDate { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal EstimatedCost { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string Username { get; set; } = string.Empty;
    public string StationName { get; set; } = string.Empty;
}

public class GetReservationsRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int? UserId { get; set; }
    public int? StationId { get; set; }
    public ReservationStatus? Status { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class CreateReservationRequest
{
    [Required]
    public int UserId { get; set; }

    [Required]
    public int StationId { get; set; }

    [Required]
    public DateTime ReservationDate { get; set; }

    [Required]
    public DateTime StartTime { get; set; }

    [Required]
    public DateTime EndTime { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}

public class UpdateReservationStatusRequest
{
    [Required]
    public ReservationStatus Status { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}

public class CancelReservationRequest
{
    [StringLength(200)]
    public string? Reason { get; set; }
}

