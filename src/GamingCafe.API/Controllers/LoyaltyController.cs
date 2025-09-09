using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GamingCafe.Core.Models;
using GamingCafe.Core.Interfaces;
using GamingCafe.Data.Repositories;
using System.ComponentModel.DataAnnotations;

namespace GamingCafe.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LoyaltyController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<LoyaltyController> _logger;

    public LoyaltyController(IUnitOfWork unitOfWork, ILogger<LoyaltyController> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Get paginated list of loyalty programs
    /// </summary>
    [HttpGet("programs")]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<ActionResult<PagedResponse<LoyaltyProgramDto>>> GetLoyaltyPrograms([FromQuery] GetLoyaltyProgramsRequest request)
    {
        try
        {
            var programs = await _unitOfWork.Repository<LoyaltyProgram>().GetAllAsync();
            var filteredPrograms = programs.AsQueryable();

            // Apply filters
            if (request.IsActive.HasValue)
            {
                filteredPrograms = filteredPrograms.Where(p => p.IsActive == request.IsActive.Value);
            }

            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                filteredPrograms = filteredPrograms.Where(p => 
                    p.ProgramName.ToLower().Contains(request.SearchTerm.ToLower()) ||
                    p.Description.ToLower().Contains(request.SearchTerm.ToLower()));
            }

            // Apply sorting
            filteredPrograms = request.SortBy?.ToLower() switch
            {
                "name" => request.SortDescending ? 
                    filteredPrograms.OrderByDescending(p => p.ProgramName) : 
                    filteredPrograms.OrderBy(p => p.ProgramName),
                "pointsperdollar" => request.SortDescending ? 
                    filteredPrograms.OrderByDescending(p => p.PointsPerDollar) : 
                    filteredPrograms.OrderBy(p => p.PointsPerDollar),
                "redemptionrate" => request.SortDescending ? 
                    filteredPrograms.OrderByDescending(p => p.RedemptionRate) : 
                    filteredPrograms.OrderBy(p => p.RedemptionRate),
                "createdat" => request.SortDescending ? 
                    filteredPrograms.OrderByDescending(p => p.CreatedAt) : 
                    filteredPrograms.OrderBy(p => p.CreatedAt),
                _ => filteredPrograms.OrderBy(p => p.ProgramName)
            };

            var totalCount = filteredPrograms.Count();
            var pagedPrograms = filteredPrograms
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(p => new LoyaltyProgramDto
                {
                    ProgramId = p.ProgramId,
                    ProgramName = p.ProgramName,
                    Description = p.Description,
                    PointsPerDollar = p.PointsPerDollar,
                    RedemptionRate = p.RedemptionRate,
                    MinimumSpend = p.MinimumSpend,
                    BonusThreshold = p.BonusThreshold,
                    BonusMultiplier = p.BonusMultiplier,
                    IsActive = p.IsActive,
                    CreatedAt = p.CreatedAt,
                    ActiveMemberCount = p.Users.Count(u => u.IsActive)
                })
                .ToList();

            var response = new PagedResponse<LoyaltyProgramDto>
            {
                Data = pagedPrograms,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving loyalty programs");
            return StatusCode(500, "An error occurred while retrieving loyalty programs");
        }
    }

    /// <summary>
    /// Get loyalty program by ID
    /// </summary>
    [HttpGet("programs/{id}")]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<ActionResult<LoyaltyProgramDto>> GetLoyaltyProgram(int id)
    {
        try
        {
            var program = await _unitOfWork.Repository<LoyaltyProgram>().GetByIdAsync(id);
            if (program == null)
                return NotFound();

            var programDto = new LoyaltyProgramDto
            {
                ProgramId = program.ProgramId,
                ProgramName = program.ProgramName,
                Description = program.Description,
                PointsPerDollar = program.PointsPerDollar,
                RedemptionRate = program.RedemptionRate,
                MinimumSpend = program.MinimumSpend,
                BonusThreshold = program.BonusThreshold,
                BonusMultiplier = program.BonusMultiplier,
                IsActive = program.IsActive,
                CreatedAt = program.CreatedAt,
                ActiveMemberCount = program.Users.Count(u => u.IsActive)
            };

            return Ok(programDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving loyalty program with ID {ProgramId}", id);
            return StatusCode(500, "An error occurred while retrieving the loyalty program");
        }
    }

    /// <summary>
    /// Create a new loyalty program
    /// </summary>
    [HttpPost("programs")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<LoyaltyProgramDto>> CreateLoyaltyProgram([FromBody] CreateLoyaltyProgramRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Check for duplicate program name
            var allPrograms = await _unitOfWork.Repository<LoyaltyProgram>().GetAllAsync();
            if (allPrograms.Any(p => p.ProgramName.ToLower() == request.ProgramName.ToLower()))
                return BadRequest("A loyalty program with this name already exists");

            var program = new LoyaltyProgram
            {
                ProgramName = request.ProgramName,
                Description = request.Description,
                PointsPerDollar = (int)request.PointsPerDollar,
                RedemptionRate = request.RedemptionRate,
                MinimumSpend = request.MinimumSpend,
                BonusThreshold = request.BonusThreshold,
                BonusMultiplier = request.BonusMultiplier,
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<LoyaltyProgram>().AddAsync(program);
            await _unitOfWork.SaveChangesAsync();

            var programDto = new LoyaltyProgramDto
            {
                ProgramId = program.ProgramId,
                ProgramName = program.ProgramName,
                Description = program.Description,
                PointsPerDollar = program.PointsPerDollar,
                RedemptionRate = program.RedemptionRate,
                MinimumSpend = program.MinimumSpend,
                BonusThreshold = program.BonusThreshold,
                BonusMultiplier = program.BonusMultiplier,
                IsActive = program.IsActive,
                CreatedAt = program.CreatedAt,
                ActiveMemberCount = 0
            };

            _logger.LogInformation("Created new loyalty program: {ProgramName} (ID: {ProgramId})", 
                program.ProgramName, program.ProgramId);

            return CreatedAtAction(nameof(GetLoyaltyProgram), new { id = program.ProgramId }, programDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating loyalty program");
            return StatusCode(500, "An error occurred while creating the loyalty program");
        }
    }

    /// <summary>
    /// Update a loyalty program
    /// </summary>
    [HttpPut("programs/{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<LoyaltyProgramDto>> UpdateLoyaltyProgram(int id, [FromBody] UpdateLoyaltyProgramRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var program = await _unitOfWork.Repository<LoyaltyProgram>().GetByIdAsync(id);
            if (program == null)
                return NotFound();

            // Check for duplicate program name (excluding current program)
            var allPrograms = await _unitOfWork.Repository<LoyaltyProgram>().GetAllAsync();
            if (allPrograms.Any(p => p.ProgramId != id && p.ProgramName.ToLower() == request.ProgramName.ToLower()))
                return BadRequest("A loyalty program with this name already exists");

            // Update properties
            program.ProgramName = request.ProgramName;
            program.Description = request.Description;
            program.PointsPerDollar = (int)request.PointsPerDollar;
            program.RedemptionRate = request.RedemptionRate;
            program.MinimumSpend = request.MinimumSpend;
            program.BonusThreshold = request.BonusThreshold;
            program.BonusMultiplier = request.BonusMultiplier;
            program.IsActive = request.IsActive;

            _unitOfWork.Repository<LoyaltyProgram>().Update(program);
            await _unitOfWork.SaveChangesAsync();

            var programDto = new LoyaltyProgramDto
            {
                ProgramId = program.ProgramId,
                ProgramName = program.ProgramName,
                Description = program.Description,
                PointsPerDollar = program.PointsPerDollar,
                RedemptionRate = program.RedemptionRate,
                MinimumSpend = program.MinimumSpend,
                BonusThreshold = program.BonusThreshold,
                BonusMultiplier = program.BonusMultiplier,
                IsActive = program.IsActive,
                CreatedAt = program.CreatedAt,
                ActiveMemberCount = program.Users.Count(u => u.IsActive)
            };

            _logger.LogInformation("Updated loyalty program: {ProgramName} (ID: {ProgramId})", 
                program.ProgramName, program.ProgramId);

            return Ok(programDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating loyalty program with ID {ProgramId}", id);
            return StatusCode(500, "An error occurred while updating the loyalty program");
        }
    }

    /// <summary>
    /// Delete a loyalty program
    /// </summary>
    [HttpDelete("programs/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> DeleteLoyaltyProgram(int id)
    {
        try
        {
            var program = await _unitOfWork.Repository<LoyaltyProgram>().GetByIdAsync(id);
            if (program == null)
                return NotFound();

            // Check if program has active members
            if (program.Users.Any(u => u.IsActive))
                return BadRequest("Cannot delete loyalty program with active members");

            // Instead of hard delete, mark as inactive
            program.IsActive = false;
            _unitOfWork.Repository<LoyaltyProgram>().Update(program);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Deactivated loyalty program: {ProgramName} (ID: {ProgramId})", 
                program.ProgramName, program.ProgramId);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting loyalty program with ID {ProgramId}", id);
            return StatusCode(500, "An error occurred while deleting the loyalty program");
        }
    }

    /// <summary>
    /// Get user's loyalty points and statistics
    /// </summary>
    [HttpGet("users/{userId}/points")]
    public async Task<ActionResult<UserLoyaltyDto>> GetUserLoyaltyPoints(int userId)
    {
        try
        {
            var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId);
            if (user == null)
                return NotFound("User not found");

            // Calculate points from transactions
            var allTransactions = await _unitOfWork.Repository<Transaction>().GetAllAsync();
            var userTransactions = allTransactions.Where(t => t.UserId == userId && t.Status == TransactionStatus.Completed);

            var totalSpent = userTransactions.Sum(t => t.Amount);
            var pointsEarned = user.LoyaltyProgram != null 
                ? (int)(totalSpent * user.LoyaltyProgram.PointsPerDollar)
                : 0;

            // Points redeemed (assume we track this in user wallet adjustments or separate system)
            var pointsRedeemed = 0; // TODO: Implement points redemption tracking

            var loyaltyDto = new UserLoyaltyDto
            {
                UserId = userId,
                Username = user.Username,
                LoyaltyProgramId = user.LoyaltyProgramId,
                LoyaltyProgramName = user.LoyaltyProgram?.ProgramName ?? "None",
                CurrentPoints = user.LoyaltyPoints,
                PointsEarned = pointsEarned,
                PointsRedeemed = pointsRedeemed,
                TotalSpent = totalSpent,
                MemberSince = user.CreatedAt,
                LastActivity = userTransactions.Any() ? userTransactions.Max(t => t.CreatedAt) : user.CreatedAt,
                Tier = CalculateUserTier(totalSpent, user.LoyaltyProgram)
            };

            return Ok(loyaltyDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving loyalty points for user {UserId}", userId);
            return StatusCode(500, "An error occurred while retrieving loyalty points");
        }
    }

    /// <summary>
    /// Award loyalty points to a user
    /// </summary>
    [HttpPost("users/{userId}/award-points")]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<ActionResult<UserLoyaltyDto>> AwardLoyaltyPoints(int userId, [FromBody] AwardPointsRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId);
            if (user == null)
                return NotFound("User not found");

            if (user.LoyaltyProgram == null)
                return BadRequest("User is not enrolled in a loyalty program");

            // Award points
            user.LoyaltyPoints += request.Points;

            // Create transaction record for point award
            var transaction = new Transaction
            {
                UserId = userId,
                Description = request.Reason ?? "Loyalty points awarded",
                Amount = 0, // No monetary value for point awards
                Type = TransactionType.LoyaltyRedemption,
                PaymentMethod = PaymentMethod.Cash, // Default payment method
                Status = TransactionStatus.Completed,
                CreatedAt = DateTime.UtcNow
            };

            _unitOfWork.Repository<User>().Update(user);
            await _unitOfWork.Repository<Transaction>().AddAsync(transaction);
            await _unitOfWork.SaveChangesAsync();

            // Return updated loyalty information
            var allTransactions = await _unitOfWork.Repository<Transaction>().GetAllAsync();
            var userTransactions = allTransactions.Where(t => t.UserId == userId && t.Status == TransactionStatus.Completed);
            var totalSpent = userTransactions.Sum(t => t.Amount);

            var loyaltyDto = new UserLoyaltyDto
            {
                UserId = userId,
                Username = user.Username,
                LoyaltyProgramId = user.LoyaltyProgramId,
                LoyaltyProgramName = user.LoyaltyProgram.ProgramName,
                CurrentPoints = user.LoyaltyPoints,
                PointsEarned = (int)(totalSpent * user.LoyaltyProgram.PointsPerDollar) + request.Points,
                PointsRedeemed = 0,
                TotalSpent = totalSpent,
                MemberSince = user.CreatedAt,
                LastActivity = DateTime.UtcNow,
                Tier = CalculateUserTier(totalSpent, user.LoyaltyProgram)
            };

            _logger.LogInformation("Awarded {Points} loyalty points to user {UserId}", request.Points, userId);
            return Ok(loyaltyDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error awarding loyalty points to user {UserId}", userId);
            return StatusCode(500, "An error occurred while awarding loyalty points");
        }
    }

    /// <summary>
    /// Redeem loyalty points
    /// </summary>
    [HttpPost("users/{userId}/redeem-points")]
    public async Task<ActionResult<RedemptionDto>> RedeemLoyaltyPoints(int userId, [FromBody] RedeemPointsRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId);
            if (user == null)
                return NotFound("User not found");

            if (user.LoyaltyProgram == null)
                return BadRequest("User is not enrolled in a loyalty program");

            if (user.LoyaltyPoints < request.Points)
                return BadRequest("Insufficient loyalty points");

            // Calculate redemption value
            var redemptionValue = request.Points * user.LoyaltyProgram.RedemptionRate;

            // Deduct points
            user.LoyaltyPoints -= request.Points;

            // Ensure user has a wallet
            var wallet = await _unitOfWork.Repository<Wallet>().FirstOrDefaultAsync(w => w.UserId == userId);
            if (wallet == null)
            {
                wallet = new Wallet
                {
                    UserId = userId,
                    Balance = 0m,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Repository<Wallet>().AddAsync(wallet);
                await _unitOfWork.SaveChangesAsync();
            }

            // Atomically credit wallet
            var (success, newBal) = await _unitOfWork.TryAtomicUpdateWalletBalanceAsync(wallet.WalletId, redemptionValue);
            if (!success)
            {
                return Conflict(new { message = "Failed to credit wallet for loyalty redemption. Please retry." });
            }

            // Create transaction record for redemption
            var transaction = new Transaction
            {
                UserId = userId,
                Description = $"Loyalty points redemption: {request.Points} points for ${redemptionValue:F2}",
                Amount = redemptionValue,
                Type = TransactionType.WalletTopup,
                PaymentMethod = PaymentMethod.LoyaltyPoints,
                Status = TransactionStatus.Completed,
                CreatedAt = DateTime.UtcNow,
                ProcessedAt = DateTime.UtcNow
            };

            _unitOfWork.Repository<User>().Update(user);
            await _unitOfWork.Repository<Transaction>().AddAsync(transaction);
            await _unitOfWork.SaveChangesAsync();

            var redemptionDto = new RedemptionDto
            {
                UserId = userId,
                Username = user.Username,
                PointsRedeemed = request.Points,
                RedemptionValue = redemptionValue,
                RemainingPoints = user.LoyaltyPoints,
                NewWalletBalance = newBal,
                TransactionId = transaction.TransactionId,
                RedeemedAt = DateTime.UtcNow
            };

            _logger.LogInformation("User {UserId} redeemed {Points} points for ${Value:F2}", 
                userId, request.Points, redemptionValue);

            return Ok(redemptionDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error redeeming loyalty points for user {UserId}", userId);
            return StatusCode(500, "An error occurred while redeeming loyalty points");
        }
    }

    /// <summary>
    /// Enroll user in loyalty program
    /// </summary>
    [HttpPost("users/{userId}/enroll/{programId}")]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<ActionResult<UserLoyaltyDto>> EnrollUserInProgram(int userId, int programId)
    {
        try
        {
            var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId);
            if (user == null)
                return NotFound("User not found");

            var program = await _unitOfWork.Repository<LoyaltyProgram>().GetByIdAsync(programId);
            if (program == null)
                return NotFound("Loyalty program not found");

            if (!program.IsActive)
                return BadRequest("Loyalty program is not active");

            user.LoyaltyProgramId = programId;
            _unitOfWork.Repository<User>().Update(user);
            await _unitOfWork.SaveChangesAsync();

            // Return loyalty information
            var allTransactions = await _unitOfWork.Repository<Transaction>().GetAllAsync();
            var userTransactions = allTransactions.Where(t => t.UserId == userId && t.Status == TransactionStatus.Completed);
            var totalSpent = userTransactions.Sum(t => t.Amount);

            var loyaltyDto = new UserLoyaltyDto
            {
                UserId = userId,
                Username = user.Username,
                LoyaltyProgramId = user.LoyaltyProgramId,
                LoyaltyProgramName = program.ProgramName,
                CurrentPoints = user.LoyaltyPoints,
                PointsEarned = (int)(totalSpent * program.PointsPerDollar),
                PointsRedeemed = 0,
                TotalSpent = totalSpent,
                MemberSince = DateTime.UtcNow,
                LastActivity = userTransactions.Any() ? userTransactions.Max(t => t.CreatedAt) : user.CreatedAt,
                Tier = CalculateUserTier(totalSpent, program)
            };

            _logger.LogInformation("Enrolled user {UserId} in loyalty program {ProgramId}", userId, programId);
            return Ok(loyaltyDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enrolling user {UserId} in program {ProgramId}", userId, programId);
            return StatusCode(500, "An error occurred while enrolling user in loyalty program");
        }
    }

    /// <summary>
    /// Get loyalty program leaderboard
    /// </summary>
    [HttpGet("programs/{programId}/leaderboard")]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<ActionResult<List<UserLoyaltyDto>>> GetLoyaltyLeaderboard(int programId, [FromQuery] int top = 10)
    {
        try
        {
            var program = await _unitOfWork.Repository<LoyaltyProgram>().GetByIdAsync(programId);
            if (program == null)
                return NotFound("Loyalty program not found");

            var allUsers = await _unitOfWork.Repository<User>().GetAllAsync();
            var allTransactions = await _unitOfWork.Repository<Transaction>().GetAllAsync();

            var programUsers = allUsers
                .Where(u => u.LoyaltyProgramId == programId && u.IsActive)
                .Select(u =>
                {
                    var userTransactions = allTransactions.Where(t => t.UserId == u.UserId && t.Status == TransactionStatus.Completed);
                    var totalSpent = userTransactions.Sum(t => t.Amount);

                    return new UserLoyaltyDto
                    {
                        UserId = u.UserId,
                        Username = u.Username,
                        LoyaltyProgramId = u.LoyaltyProgramId,
                        LoyaltyProgramName = program.ProgramName,
                        CurrentPoints = u.LoyaltyPoints,
                        PointsEarned = (int)(totalSpent * program.PointsPerDollar),
                        PointsRedeemed = 0,
                        TotalSpent = totalSpent,
                        MemberSince = u.CreatedAt,
                        LastActivity = userTransactions.Any() ? userTransactions.Max(t => t.CreatedAt) : u.CreatedAt,
                        Tier = CalculateUserTier(totalSpent, program)
                    };
                })
                .OrderByDescending(u => u.CurrentPoints)
                .Take(top)
                .ToList();

            return Ok(programUsers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving leaderboard for program {ProgramId}", programId);
            return StatusCode(500, "An error occurred while retrieving the leaderboard");
        }
    }

    private static string CalculateUserTier(decimal totalSpent, LoyaltyProgram? program)
    {
        if (program == null) return "None";

        if (totalSpent >= program.BonusThreshold)
            return "Gold";
        else if (totalSpent >= program.MinimumSpend)
            return "Silver";
        else
            return "Bronze";
    }
}

// DTOs and Request/Response Models
public class LoyaltyProgramDto
{
    public int ProgramId { get; set; }
    public string ProgramName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal PointsPerDollar { get; set; }
    public decimal RedemptionRate { get; set; }
    public decimal MinimumSpend { get; set; }
    public decimal BonusThreshold { get; set; }
    public decimal BonusMultiplier { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ActiveMemberCount { get; set; }
}

public class UserLoyaltyDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public int? LoyaltyProgramId { get; set; }
    public string LoyaltyProgramName { get; set; } = string.Empty;
    public int CurrentPoints { get; set; }
    public int PointsEarned { get; set; }
    public int PointsRedeemed { get; set; }
    public decimal TotalSpent { get; set; }
    public DateTime MemberSince { get; set; }
    public DateTime LastActivity { get; set; }
    public string Tier { get; set; } = string.Empty;
}

public class RedemptionDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public int PointsRedeemed { get; set; }
    public decimal RedemptionValue { get; set; }
    public int RemainingPoints { get; set; }
    public decimal NewWalletBalance { get; set; }
    public int TransactionId { get; set; }
    public DateTime RedeemedAt { get; set; }
}

public class GetLoyaltyProgramsRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public bool? IsActive { get; set; }
    public string? SearchTerm { get; set; }
    public string? SortBy { get; set; } = "Name";
    public bool SortDescending { get; set; } = false;
}

public class CreateLoyaltyProgramRequest
{
    [Required]
    [StringLength(100)]
    public string ProgramName { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Range(0.01, 99.99)]
    public decimal PointsPerDollar { get; set; }

    [Required]
    [Range(0.001, 1.0)]
    public decimal RedemptionRate { get; set; }

    [Required]
    [Range(0, 9999.99)]
    public decimal MinimumSpend { get; set; }

    [Required]
    [Range(0, 99999.99)]
    public decimal BonusThreshold { get; set; }

    [Required]
    [Range(1.0, 10.0)]
    public decimal BonusMultiplier { get; set; }

    public bool IsActive { get; set; } = true;
}

public class UpdateLoyaltyProgramRequest
{
    [Required]
    [StringLength(100)]
    public string ProgramName { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Range(0.01, 99.99)]
    public decimal PointsPerDollar { get; set; }

    [Required]
    [Range(0.001, 1.0)]
    public decimal RedemptionRate { get; set; }

    [Required]
    [Range(0, 9999.99)]
    public decimal MinimumSpend { get; set; }

    [Required]
    [Range(0, 99999.99)]
    public decimal BonusThreshold { get; set; }

    [Required]
    [Range(1.0, 10.0)]
    public decimal BonusMultiplier { get; set; }

    public bool IsActive { get; set; }
}

public class AwardPointsRequest
{
    [Required]
    [Range(1, 10000)]
    public int Points { get; set; }

    [StringLength(200)]
    public string? Reason { get; set; }
}

public class RedeemPointsRequest
{
    [Required]
    [Range(1, 100000)]
    public int Points { get; set; }
}

