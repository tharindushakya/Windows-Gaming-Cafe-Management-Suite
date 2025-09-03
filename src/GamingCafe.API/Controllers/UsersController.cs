using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GamingCafe.Data;
using GamingCafe.Core.Models;
using BCrypt.Net;

namespace GamingCafe.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly GamingCafeContext _context;

    public UsersController(GamingCafeContext context)
    {
        _context = context;
    }

    // GET: api/users
    [HttpGet]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
    {
        var users = await _context.Users
            .Select(u => new UserDto
            {
                UserId = u.UserId,
                Username = u.Username,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                PhoneNumber = u.PhoneNumber,
                DateOfBirth = u.DateOfBirth,
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt,
                IsActive = u.IsActive,
                Role = u.Role.ToString(),
                WalletBalance = u.WalletBalance,
                LoyaltyPoints = u.LoyaltyPoints,
                MembershipExpiryDate = u.MembershipExpiryDate
            })
            .ToListAsync();

        return Ok(users);
    }

    // GET: api/users/5
    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUser(int id)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
        {
            return NotFound();
        }

        // Users can only access their own data unless they're admin/manager
        if (!User.IsInRole("Admin") && !User.IsInRole("Manager") && 
            user.UserId.ToString() != User.FindFirst("userId")?.Value)
        {
            return Forbid();
        }

        var userDto = new UserDto
        {
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            DateOfBirth = user.DateOfBirth,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            IsActive = user.IsActive,
            Role = user.Role.ToString(),
            WalletBalance = user.WalletBalance,
            LoyaltyPoints = user.LoyaltyPoints,
            MembershipExpiryDate = user.MembershipExpiryDate
        };

        return Ok(userDto);
    }

    // PUT: api/users/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutUser(int id, UpdateUserRequest request)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        // Users can only update their own data unless they're admin/manager
        if (!User.IsInRole("Admin") && !User.IsInRole("Manager") && 
            user.UserId.ToString() != User.FindFirst("userId")?.Value)
        {
            return Forbid();
        }

        // Update allowed fields
        user.FirstName = request.FirstName ?? user.FirstName;
        user.LastName = request.LastName ?? user.LastName;
        user.PhoneNumber = request.PhoneNumber ?? user.PhoneNumber;
        user.DateOfBirth = request.DateOfBirth ?? user.DateOfBirth;

        // Only admins can update these fields
        if (User.IsInRole("Admin"))
        {
            user.IsActive = request.IsActive ?? user.IsActive;
            if (Enum.TryParse<UserRole>(request.Role, out var role))
            {
                user.Role = role;
            }
        }

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!UserExists(id))
            {
                return NotFound();
            }
            throw;
        }

        return NoContent();
    }

    // POST: api/users
    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<UserDto>> PostUser(CreateUserRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Check if username already exists
        if (await _context.Users.AnyAsync(u => u.Username == request.Username))
        {
            return BadRequest("Username already exists");
        }

        // Check if email already exists
        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
        {
            return BadRequest("Email already exists");
        }

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber ?? "",
            DateOfBirth = request.DateOfBirth,
            IsActive = request.IsActive ?? true,
            Role = Enum.TryParse<UserRole>(request.Role, out var role) ? role : UserRole.Customer,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var userDto = new UserDto
        {
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            DateOfBirth = user.DateOfBirth,
            CreatedAt = user.CreatedAt,
            IsActive = user.IsActive,
            Role = user.Role.ToString(),
            WalletBalance = user.WalletBalance,
            LoyaltyPoints = user.LoyaltyPoints
        };

        return CreatedAtAction("GetUser", new { id = user.UserId }, userDto);
    }

    // DELETE: api/users/5
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        // Soft delete - just deactivate the user
        user.IsActive = false;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // POST: api/users/5/wallet/add
    [HttpPost("{id}/wallet/add")]
    public async Task<IActionResult> AddToWallet(int id, WalletTransactionRequest request)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        // Users can only add to their own wallet unless they're admin/manager
        if (!User.IsInRole("Admin") && !User.IsInRole("Manager") && 
            user.UserId.ToString() != User.FindFirst("userId")?.Value)
        {
            return Forbid();
        }

        if (request.Amount <= 0)
        {
            return BadRequest("Amount must be positive");
        }

        user.WalletBalance += request.Amount;

        var walletTransaction = new WalletTransaction
        {
            UserId = user.UserId,
            Amount = request.Amount,
            Type = WalletTransactionType.Credit,
            Description = request.Description ?? "Wallet top-up",
            CreatedAt = DateTime.UtcNow
        };

        _context.WalletTransactions.Add(walletTransaction);
        await _context.SaveChangesAsync();

        return Ok(new { NewBalance = user.WalletBalance });
    }

    // GET: api/users/5/transactions
    [HttpGet("{id}/transactions")]
    public async Task<ActionResult<IEnumerable<TransactionDto>>> GetUserTransactions(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        // Users can only access their own transactions unless they're admin/manager
        if (!User.IsInRole("Admin") && !User.IsInRole("Manager") && 
            user.UserId.ToString() != User.FindFirst("userId")?.Value)
        {
            return Forbid();
        }

        var transactions = await _context.Transactions
            .Where(t => t.UserId == id)
            .Include(t => t.Product)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TransactionDto
            {
                TransactionId = t.TransactionId,
                Amount = t.Amount,
                Type = t.Type.ToString(),
                PaymentMethod = t.PaymentMethod.ToString(),
                Status = t.Status.ToString(),
                Description = t.Description,
                CreatedAt = t.CreatedAt,
                ProductName = t.Product != null ? t.Product.Name : null
            })
            .ToListAsync();

        return Ok(transactions);
    }

    private bool UserExists(int id)
    {
        return _context.Users.Any(e => e.UserId == id);
    }
}

public class UserDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; }
    public string Role { get; set; } = string.Empty;
    public decimal WalletBalance { get; set; }
    public int LoyaltyPoints { get; set; }
    public DateTime? MembershipExpiryDate { get; set; }
}

public class CreateUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime DateOfBirth { get; set; }
    public bool? IsActive { get; set; }
    public string? Role { get; set; }
}

public class UpdateUserRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public bool? IsActive { get; set; }
    public string? Role { get; set; }
}

public class WalletTransactionRequest
{
    public decimal Amount { get; set; }
    public string? Description { get; set; }
}

public class TransactionDto
{
    public int TransactionId { get; set; }
    public decimal Amount { get; set; }
    public string Type { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? ProductName { get; set; }
}
