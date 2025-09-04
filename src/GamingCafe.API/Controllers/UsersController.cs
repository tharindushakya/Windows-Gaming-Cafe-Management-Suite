using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GamingCafe.Data.Repositories;
using GamingCafe.Core.Models;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace GamingCafe.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUnitOfWork unitOfWork, ILogger<UsersController> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers([FromQuery] UserQueryParameters parameters)
    {
        try
        {
            var users = await _unitOfWork.Repository<User>().GetAllAsync();
            
            // Apply filtering
            if (!string.IsNullOrEmpty(parameters.Role))
            {
                if (Enum.TryParse<UserRole>(parameters.Role, out var role))
                {
                    users = users.Where(u => u.Role == role);
                }
            }
            
            if (parameters.IsActive.HasValue)
            {
                users = users.Where(u => u.IsActive == parameters.IsActive.Value);
            }
            
            if (!string.IsNullOrEmpty(parameters.SearchTerm))
            {
                users = users.Where(u => 
                    u.Username.Contains(parameters.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    u.FirstName.Contains(parameters.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    u.LastName.Contains(parameters.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    (u.Email != null && u.Email.Contains(parameters.SearchTerm, StringComparison.OrdinalIgnoreCase)));
            }

            // Apply sorting
            users = parameters.SortBy?.ToLower() switch
            {
                "username" => parameters.SortDirection == "desc" ? users.OrderByDescending(u => u.Username) : users.OrderBy(u => u.Username),
                "firstname" => parameters.SortDirection == "desc" ? users.OrderByDescending(u => u.FirstName) : users.OrderBy(u => u.FirstName),
                "lastname" => parameters.SortDirection == "desc" ? users.OrderByDescending(u => u.LastName) : users.OrderBy(u => u.LastName),
                "createdat" => parameters.SortDirection == "desc" ? users.OrderByDescending(u => u.CreatedAt) : users.OrderBy(u => u.CreatedAt),
                "lastloginat" => parameters.SortDirection == "desc" ? users.OrderByDescending(u => u.LastLoginAt) : users.OrderBy(u => u.LastLoginAt),
                _ => users.OrderBy(u => u.Username)
            };

            // Apply pagination
            var totalCount = users.Count();
            var pagedUsers = users
                .Skip((parameters.Page - 1) * parameters.PageSize)
                .Take(parameters.PageSize)
                .Select(u => new UserDto
                {
                    UserId = u.UserId,
                    Username = u.Username,
                    Email = u.Email,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    PhoneNumber = u.PhoneNumber,
                    DateOfBirth = u.DateOfBirth,
                    Role = u.Role.ToString(),
                    IsActive = u.IsActive,
                    WalletBalance = u.WalletBalance,
                    LoyaltyPoints = u.LoyaltyPoints,
                    MembershipExpiryDate = u.MembershipExpiryDate,
                    LastLoginAt = u.LastLoginAt,
                    CreatedAt = u.CreatedAt
                })
                .ToList();

            var response = new PagedResponse<UserDto>
            {
                Data = pagedUsers,
                Page = parameters.Page,
                PageSize = parameters.PageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / parameters.PageSize)
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users");
            return StatusCode(500, "An error occurred while retrieving users");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUser(int id)
    {
        try
        {
            var user = await _unitOfWork.Repository<User>().GetByIdAsync(id);
            
            if (user == null)
                return NotFound($"User with ID {id} not found");

            var userDto = new UserDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                DateOfBirth = user.DateOfBirth,
                Role = user.Role.ToString(),
                IsActive = user.IsActive,
                WalletBalance = user.WalletBalance,
                LoyaltyPoints = user.LoyaltyPoints,
                MembershipExpiryDate = user.MembershipExpiryDate,
                LastLoginAt = user.LastLoginAt,
                CreatedAt = user.CreatedAt
            };

            return Ok(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user with ID {UserId}", id);
            return StatusCode(500, "An error occurred while retrieving the user");
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Check if username already exists
            var existingUser = await _unitOfWork.Repository<User>().FirstOrDefaultAsync(u => u.Username == request.Username);
            if (existingUser != null)
                return Conflict("Username already exists");

            // Check if email already exists
            if (!string.IsNullOrEmpty(request.Email))
            {
                var existingEmail = await _unitOfWork.Repository<User>().FirstOrDefaultAsync(u => u.Email == request.Email);
                if (existingEmail != null)
                    return Conflict("Email already exists");
            }

            var user = new User
            {
                Username = request.Username,
                Email = request.Email ?? string.Empty,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PhoneNumber = request.PhoneNumber ?? string.Empty,
                DateOfBirth = request.DateOfBirth ?? DateTime.MinValue,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = Enum.Parse<UserRole>(request.Role),
                IsActive = true,
                WalletBalance = request.InitialWalletBalance ?? 0.00m,
                LoyaltyPoints = 0,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<User>().AddAsync(user);
            await _unitOfWork.SaveChangesAsync();

            var userDto = new UserDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                DateOfBirth = user.DateOfBirth,
                Role = user.Role.ToString(),
                IsActive = user.IsActive,
                WalletBalance = user.WalletBalance,
                LoyaltyPoints = user.LoyaltyPoints,
                MembershipExpiryDate = user.MembershipExpiryDate,
                LastLoginAt = user.LastLoginAt,
                CreatedAt = user.CreatedAt
            };

            _logger.LogInformation("User created with ID {UserId}", user.UserId);
            return CreatedAtAction(nameof(GetUser), new { id = user.UserId }, userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return StatusCode(500, "An error occurred while creating the user");
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<ActionResult<UserDto>> UpdateUser(int id, [FromBody] UpdateUserRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _unitOfWork.Repository<User>().GetByIdAsync(id);
            if (user == null)
                return NotFound($"User with ID {id} not found");

            // Check if username is being changed and if it already exists
            if (request.Username != user.Username)
            {
                var existingUser = await _unitOfWork.Repository<User>().FirstOrDefaultAsync(u => u.Username == request.Username);
                if (existingUser != null)
                    return Conflict("Username already exists");
            }

            // Check if email is being changed and if it already exists
            if (!string.IsNullOrEmpty(request.Email) && request.Email != user.Email)
            {
                var existingEmail = await _unitOfWork.Repository<User>().FirstOrDefaultAsync(u => u.Email == request.Email);
                if (existingEmail != null)
                    return Conflict("Email already exists");
            }

            // Update user properties
            user.Username = request.Username;
            user.Email = request.Email ?? string.Empty;
            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.PhoneNumber = request.PhoneNumber ?? string.Empty;
            user.DateOfBirth = request.DateOfBirth ?? user.DateOfBirth; // Keep existing if null
            
            if (Enum.TryParse<UserRole>(request.Role, out var role))
                user.Role = role;

            _unitOfWork.Repository<User>().Update(user);
            await _unitOfWork.SaveChangesAsync();

            var userDto = new UserDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                DateOfBirth = user.DateOfBirth,
                Role = user.Role.ToString(),
                IsActive = user.IsActive,
                WalletBalance = user.WalletBalance,
                LoyaltyPoints = user.LoyaltyPoints,
                MembershipExpiryDate = user.MembershipExpiryDate,
                LastLoginAt = user.LastLoginAt,
                CreatedAt = user.CreatedAt
            };

            _logger.LogInformation("User updated with ID {UserId}", user.UserId);
            return Ok(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user with ID {UserId}", id);
            return StatusCode(500, "An error occurred while updating the user");
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        try
        {
            var user = await _unitOfWork.Repository<User>().GetByIdAsync(id);
            if (user == null)
                return NotFound($"User with ID {id} not found");

            _unitOfWork.Repository<User>().Delete(user);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("User deleted with ID {UserId}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user with ID {UserId}", id);
            return StatusCode(500, "An error occurred while deleting the user");
        }
    }

    [HttpPatch("{id}/deactivate")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> DeactivateUser(int id)
    {
        try
        {
            var user = await _unitOfWork.Repository<User>().GetByIdAsync(id);
            if (user == null)
                return NotFound($"User with ID {id} not found");

            user.IsActive = false;
            _unitOfWork.Repository<User>().Update(user);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("User deactivated with ID {UserId}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating user with ID {UserId}", id);
            return StatusCode(500, "An error occurred while deactivating the user");
        }
    }

    [HttpPatch("{id}/activate")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> ActivateUser(int id)
    {
        try
        {
            var user = await _unitOfWork.Repository<User>().GetByIdAsync(id);
            if (user == null)
                return NotFound($"User with ID {id} not found");

            user.IsActive = true;
            _unitOfWork.Repository<User>().Update(user);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("User activated with ID {UserId}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating user with ID {UserId}", id);
            return StatusCode(500, "An error occurred while activating the user");
        }
    }

    [HttpPost("{id}/wallet/add")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> AddToWallet(int id, [FromBody] WalletTransactionRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _unitOfWork.Repository<User>().GetByIdAsync(id);
            if (user == null)
                return NotFound($"User with ID {id} not found");

            if (request.Amount <= 0)
                return BadRequest("Amount must be greater than zero");

            user.WalletBalance += request.Amount;
            _unitOfWork.Repository<User>().Update(user);

            // Create wallet transaction record
            var walletTransaction = new WalletTransaction
            {
                UserId = id,
                Amount = request.Amount,
                Type = WalletTransactionType.Credit,
                Description = request.Description ?? "Wallet top-up",
                Reference = $"TOPUP-{DateTime.UtcNow:yyyyMMddHHmmss}",
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<WalletTransaction>().AddAsync(walletTransaction);

            // Also create a main transaction record
            var transaction = new Transaction
            {
                UserId = id,
                Amount = request.Amount,
                Type = TransactionType.WalletTopup,
                Status = TransactionStatus.Completed,
                Description = request.Description ?? "Wallet top-up",
                PaymentMethod = PaymentMethod.Cash, // Assume cash for now
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<Transaction>().AddAsync(transaction);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Added {Amount} to wallet for user {UserId}", request.Amount, id);
            return Ok(new { WalletBalance = user.WalletBalance });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding to wallet for user with ID {UserId}", id);
            return StatusCode(500, "An error occurred while adding to wallet");
        }
    }

    [HttpPost("{id}/wallet/deduct")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> DeductFromWallet(int id, [FromBody] WalletTransactionRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _unitOfWork.Repository<User>().GetByIdAsync(id);
            if (user == null)
                return NotFound($"User with ID {id} not found");

            if (request.Amount <= 0)
                return BadRequest("Amount must be greater than zero");

            if (user.WalletBalance < request.Amount)
                return BadRequest("Insufficient wallet balance");

            user.WalletBalance -= request.Amount;
            _unitOfWork.Repository<User>().Update(user);

            // Create wallet transaction record
            var walletTransaction = new WalletTransaction
            {
                UserId = id,
                Amount = request.Amount,
                Type = WalletTransactionType.Debit,
                Description = request.Description ?? "Wallet deduction",
                Reference = $"DEBIT-{DateTime.UtcNow:yyyyMMddHHmmss}",
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<WalletTransaction>().AddAsync(walletTransaction);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Deducted {Amount} from wallet for user {UserId}", request.Amount, id);
            return Ok(new { WalletBalance = user.WalletBalance });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deducting from wallet for user with ID {UserId}", id);
            return StatusCode(500, "An error occurred while deducting from wallet");
        }
    }
}

// DTOs and Request/Response Models
public class UserDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public decimal WalletBalance { get; set; }
    public int LoyaltyPoints { get; set; }
    public DateTime? MembershipExpiryDate { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateUserRequest
{
    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [EmailAddress]
    public string? Email { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Phone]
    public string? PhoneNumber { get; set; }

    public DateTime? DateOfBirth { get; set; }

    [Required]
    public string Role { get; set; } = "Customer";

    [Range(0, double.MaxValue)]
    public decimal? InitialWalletBalance { get; set; }
}

public class UpdateUserRequest
{
    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [EmailAddress]
    public string? Email { get; set; }

    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Phone]
    public string? PhoneNumber { get; set; }

    public DateTime? DateOfBirth { get; set; }

    [Required]
    public string Role { get; set; } = "Customer";
}

public class WalletTransactionRequest
{
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero")]
    public decimal Amount { get; set; }

    public string? Description { get; set; }
}

public class UserQueryParameters
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SearchTerm { get; set; }
    public string? Role { get; set; }
    public bool? IsActive { get; set; }
    public string? SortBy { get; set; } = "Username";
    public string SortDirection { get; set; } = "asc";
}

public class PagedResponse<T>
{
    public IEnumerable<T> Data { get; set; } = new List<T>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}
