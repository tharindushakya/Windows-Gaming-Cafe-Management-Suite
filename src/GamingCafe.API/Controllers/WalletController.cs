using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GamingCafe.Core.Models;
using GamingCafe.Data.Repositories;
using System.ComponentModel.DataAnnotations;

namespace GamingCafe.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WalletController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<WalletController> _logger;

    public WalletController(IUnitOfWork unitOfWork, ILogger<WalletController> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Get user wallet details
    /// </summary>
    [HttpGet("{userId}")]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<ActionResult<WalletDto>> GetWallet(int userId)
    {
        try
        {
            var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId);
            if (user == null)
                return NotFound("User not found");

            var wallet = await _unitOfWork.Repository<Wallet>().FirstOrDefaultAsync(w => w.UserId == userId);
            if (wallet == null)
            {
                // Create wallet if it doesn't exist
                wallet = new Wallet
                {
                    UserId = userId,
                    Balance = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                
                await _unitOfWork.Repository<Wallet>().AddAsync(wallet);
                await _unitOfWork.SaveChangesAsync();
            }

            var walletDto = new WalletDto
            {
                WalletId = wallet.WalletId,
                UserId = wallet.UserId,
                Username = user.Username,
                Balance = wallet.Balance,
                Status = wallet.IsActive ? "Active" : "Inactive",
                CreatedAt = wallet.CreatedAt,
                UpdatedAt = wallet.UpdatedAt
            };

            return Ok(walletDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving wallet for user {UserId}", userId);
            return StatusCode(500, "An error occurred while retrieving the wallet");
        }
    }

    /// <summary>
    /// Get current user's wallet
    /// </summary>
    [HttpGet("my-wallet")]
    [Authorize]
    public async Task<ActionResult<WalletDto>> GetMyWallet()
    {
        try
        {
            var userId = GetCurrentUserId();
            return await GetWallet(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current user's wallet");
            return StatusCode(500, "An error occurred while retrieving your wallet");
        }
    }

    /// <summary>
    /// Add money to wallet
    /// </summary>
    [HttpPost("{userId}/deposit")]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<ActionResult<WalletTransactionDto>> DepositToWallet(int userId, [FromBody] WalletDepositRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId);
            if (user == null)
                return NotFound("User not found");

            var wallet = await _unitOfWork.Repository<Wallet>().FirstOrDefaultAsync(w => w.UserId == userId);
            if (wallet == null)
            {
                // Create wallet if it doesn't exist
                wallet = new Wallet
                {
                    UserId = userId,
                    Balance = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Repository<Wallet>().AddAsync(wallet);
            }

            if (!wallet.IsActive)
                return BadRequest("Wallet is inactive");

            // Update wallet balance
            var oldBalance = wallet.Balance;
            wallet.Balance += request.Amount;
            wallet.UpdatedAt = DateTime.UtcNow;

            // Create wallet transaction
            var transaction = new WalletTransaction
            {
                WalletId = wallet.WalletId,
                Type = WalletTransactionType.Deposit,
                Amount = request.Amount,
                Description = request.Description ?? "Wallet deposit",
                BalanceBefore = oldBalance,
                BalanceAfter = wallet.Balance,
                PaymentMethod = request.PaymentMethod ?? "Cash",
                ProcessedBy = GetCurrentUserId(),
                TransactionDate = DateTime.UtcNow,
                Status = WalletTransactionStatus.Completed
            };

            _unitOfWork.Repository<Wallet>().Update(wallet);
            await _unitOfWork.Repository<WalletTransaction>().AddAsync(transaction);
            await _unitOfWork.SaveChangesAsync();

            var transactionDto = new WalletTransactionDto
            {
                TransactionId = transaction.TransactionId,
                WalletId = transaction.WalletId,
                UserId = userId,
                Username = user.Username,
                Type = transaction.Type.ToString(),
                Amount = transaction.Amount,
                Description = transaction.Description,
                BalanceBefore = transaction.BalanceBefore,
                BalanceAfter = transaction.BalanceAfter,
                PaymentMethod = transaction.PaymentMethod,
                ProcessedBy = GetCurrentUsername(),
                TransactionDate = transaction.TransactionDate,
                Status = transaction.Status.ToString()
            };

            _logger.LogInformation("Wallet deposit: ${Amount} added to user {UserId} wallet. New balance: ${Balance}", 
                request.Amount, userId, wallet.Balance);

            return Ok(transactionDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error depositing to wallet for user {UserId}", userId);
            return StatusCode(500, "An error occurred while processing the deposit");
        }
    }

    /// <summary>
    /// Deduct money from wallet
    /// </summary>
    [HttpPost("{userId}/withdraw")]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<ActionResult<WalletTransactionDto>> WithdrawFromWallet(int userId, [FromBody] WalletWithdrawRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId);
            if (user == null)
                return NotFound("User not found");

            var wallet = await _unitOfWork.Repository<Wallet>().FirstOrDefaultAsync(w => w.UserId == userId);
            if (wallet == null)
                return NotFound("Wallet not found");

            if (!wallet.IsActive)
                return BadRequest("Wallet is inactive");

            if (wallet.Balance < request.Amount)
                return BadRequest("Insufficient funds");

            // Update wallet balance
            var oldBalance = wallet.Balance;
            wallet.Balance -= request.Amount;
            wallet.UpdatedAt = DateTime.UtcNow;

            // Create wallet transaction
            var transaction = new WalletTransaction
            {
                WalletId = wallet.WalletId,
                Type = WalletTransactionType.Withdrawal,
                Amount = request.Amount,
                Description = request.Description ?? "Wallet withdrawal",
                BalanceBefore = oldBalance,
                BalanceAfter = wallet.Balance,
                PaymentMethod = request.PaymentMethod ?? "Cash",
                ProcessedBy = GetCurrentUserId(),
                TransactionDate = DateTime.UtcNow,
                Status = WalletTransactionStatus.Completed
            };

            _unitOfWork.Repository<Wallet>().Update(wallet);
            await _unitOfWork.Repository<WalletTransaction>().AddAsync(transaction);
            await _unitOfWork.SaveChangesAsync();

            var transactionDto = new WalletTransactionDto
            {
                TransactionId = transaction.TransactionId,
                WalletId = transaction.WalletId,
                UserId = userId,
                Username = user.Username,
                Type = transaction.Type.ToString(),
                Amount = transaction.Amount,
                Description = transaction.Description,
                BalanceBefore = transaction.BalanceBefore,
                BalanceAfter = transaction.BalanceAfter,
                PaymentMethod = transaction.PaymentMethod,
                ProcessedBy = GetCurrentUsername(),
                TransactionDate = transaction.TransactionDate,
                Status = transaction.Status.ToString()
            };

            _logger.LogInformation("Wallet withdrawal: ${Amount} deducted from user {UserId} wallet. New balance: ${Balance}", 
                request.Amount, userId, wallet.Balance);

            return Ok(transactionDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error withdrawing from wallet for user {UserId}", userId);
            return StatusCode(500, "An error occurred while processing the withdrawal");
        }
    }

    /// <summary>
    /// Transfer money between wallets
    /// </summary>
    [HttpPost("transfer")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<WalletTransferResponseDto>> TransferBetweenWallets([FromBody] WalletTransferRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (request.FromUserId == request.ToUserId)
                return BadRequest("Cannot transfer to the same wallet");

            var fromUser = await _unitOfWork.Repository<User>().GetByIdAsync(request.FromUserId);
            var toUser = await _unitOfWork.Repository<User>().GetByIdAsync(request.ToUserId);

            if (fromUser == null || toUser == null)
                return NotFound("One or both users not found");

            var fromWallet = await _unitOfWork.Repository<Wallet>().FirstOrDefaultAsync(w => w.UserId == request.FromUserId);
            var toWallet = await _unitOfWork.Repository<Wallet>().FirstOrDefaultAsync(w => w.UserId == request.ToUserId);

            if (fromWallet == null || toWallet == null)
                return BadRequest("One or both wallets not found");

            if (!fromWallet.IsActive || !toWallet.IsActive)
                return BadRequest("One or both wallets are inactive");

            if (fromWallet.Balance < request.Amount)
                return BadRequest("Insufficient funds in source wallet");

            // Update wallet balances
            var fromOldBalance = fromWallet.Balance;
            var toOldBalance = toWallet.Balance;

            fromWallet.Balance -= request.Amount;
            toWallet.Balance += request.Amount;

            fromWallet.UpdatedAt = DateTime.UtcNow;
            toWallet.UpdatedAt = DateTime.UtcNow;

            // Create debit transaction for sender
            var debitTransaction = new WalletTransaction
            {
                WalletId = fromWallet.WalletId,
                Type = WalletTransactionType.Transfer,
                Amount = request.Amount,
                Description = $"Transfer to {toUser.Username}: {request.Description}",
                BalanceBefore = fromOldBalance,
                BalanceAfter = fromWallet.Balance,
                ProcessedBy = GetCurrentUserId(),
                TransactionDate = DateTime.UtcNow,
                Status = WalletTransactionStatus.Completed,
                RelatedUserId = request.ToUserId
            };

            // Create credit transaction for receiver
            var creditTransaction = new WalletTransaction
            {
                WalletId = toWallet.WalletId,
                Type = WalletTransactionType.Transfer,
                Amount = request.Amount,
                Description = $"Transfer from {fromUser.Username}: {request.Description}",
                BalanceBefore = toOldBalance,
                BalanceAfter = toWallet.Balance,
                ProcessedBy = GetCurrentUserId(),
                TransactionDate = DateTime.UtcNow,
                Status = WalletTransactionStatus.Completed,
                RelatedUserId = request.FromUserId
            };

            _unitOfWork.Repository<Wallet>().Update(fromWallet);
            _unitOfWork.Repository<Wallet>().Update(toWallet);
            await _unitOfWork.Repository<WalletTransaction>().AddAsync(debitTransaction);
            await _unitOfWork.Repository<WalletTransaction>().AddAsync(creditTransaction);
            await _unitOfWork.SaveChangesAsync();

            var response = new WalletTransferResponseDto
            {
                FromUserId = request.FromUserId,
                FromUsername = fromUser.Username,
                ToUserId = request.ToUserId,
                ToUsername = toUser.Username,
                Amount = request.Amount,
                Description = request.Description,
                FromBalanceBefore = fromOldBalance,
                FromBalanceAfter = fromWallet.Balance,
                ToBalanceBefore = toOldBalance,
                ToBalanceAfter = toWallet.Balance,
                ProcessedBy = GetCurrentUsername(),
                TransferDate = DateTime.UtcNow,
                DebitTransactionId = debitTransaction.TransactionId,
                CreditTransactionId = creditTransaction.TransactionId
            };

            _logger.LogInformation("Wallet transfer: ${Amount} transferred from user {FromUserId} to user {ToUserId}", 
                request.Amount, request.FromUserId, request.ToUserId);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transferring between wallets");
            return StatusCode(500, "An error occurred while processing the transfer");
        }
    }

    /// <summary>
    /// Get wallet transaction history
    /// </summary>
    [HttpGet("{userId}/transactions")]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<ActionResult<PagedResponse<WalletTransactionDto>>> GetWalletTransactions(int userId, [FromQuery] GetWalletTransactionsRequest request)
    {
        try
        {
            var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId);
            if (user == null)
                return NotFound("User not found");

            var wallet = await _unitOfWork.Repository<Wallet>().FirstOrDefaultAsync(w => w.UserId == userId);
            if (wallet == null)
                return NotFound("Wallet not found");

            var transactions = await _unitOfWork.Repository<WalletTransaction>().GetAllAsync();
            var walletTransactions = transactions.Where(t => t.WalletId == wallet.WalletId).AsQueryable();

            // Apply filters
            if (request.Type.HasValue)
            {
                walletTransactions = walletTransactions.Where(t => t.Type == request.Type.Value);
            }

            if (request.StartDate.HasValue)
            {
                walletTransactions = walletTransactions.Where(t => t.TransactionDate >= request.StartDate.Value);
            }

            if (request.EndDate.HasValue)
            {
                walletTransactions = walletTransactions.Where(t => t.TransactionDate <= request.EndDate.Value);
            }

            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                walletTransactions = walletTransactions.Where(t => 
                    t.Description.ToLower().Contains(request.SearchTerm.ToLower()));
            }

            // Apply sorting
            walletTransactions = request.SortBy?.ToLower() switch
            {
                "amount" => request.SortDescending ? 
                    walletTransactions.OrderByDescending(t => t.Amount) : 
                    walletTransactions.OrderBy(t => t.Amount),
                "type" => request.SortDescending ? 
                    walletTransactions.OrderByDescending(t => t.Type) : 
                    walletTransactions.OrderBy(t => t.Type),
                "date" => request.SortDescending ? 
                    walletTransactions.OrderByDescending(t => t.TransactionDate) : 
                    walletTransactions.OrderBy(t => t.TransactionDate),
                _ => walletTransactions.OrderByDescending(t => t.TransactionDate)
            };

            var totalCount = walletTransactions.Count();
            var pagedTransactions = walletTransactions
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(t => new WalletTransactionDto
                {
                    TransactionId = t.TransactionId,
                    WalletId = t.WalletId,
                    UserId = userId,
                    Username = user.Username,
                    Type = t.Type.ToString(),
                    Amount = t.Amount,
                    Description = t.Description,
                    BalanceBefore = t.BalanceBefore,
                    BalanceAfter = t.BalanceAfter,
                    PaymentMethod = t.PaymentMethod,
                    ProcessedBy = GetUsernameById(t.ProcessedBy),
                    TransactionDate = t.TransactionDate,
                    Status = t.Status.ToString()
                })
                .ToList();

            var response = new PagedResponse<WalletTransactionDto>
            {
                Data = pagedTransactions,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving wallet transactions for user {UserId}", userId);
            return StatusCode(500, "An error occurred while retrieving wallet transactions");
        }
    }

    /// <summary>
    /// Get current user's wallet transactions
    /// </summary>
    [HttpGet("my-transactions")]
    [Authorize]
    public async Task<ActionResult<PagedResponse<WalletTransactionDto>>> GetMyWalletTransactions([FromQuery] GetWalletTransactionsRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            return await GetWalletTransactions(userId, request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current user's wallet transactions");
            return StatusCode(500, "An error occurred while retrieving your wallet transactions");
        }
    }

    /// <summary>
    /// Activate or deactivate a wallet
    /// </summary>
    [HttpPut("{userId}/status")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<WalletDto>> UpdateWalletStatus(int userId, [FromBody] UpdateWalletStatusRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId);
            if (user == null)
                return NotFound("User not found");

            var wallet = await _unitOfWork.Repository<Wallet>().FirstOrDefaultAsync(w => w.UserId == userId);
            if (wallet == null)
                return NotFound("Wallet not found");

            wallet.IsActive = request.IsActive;
            wallet.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<Wallet>().Update(wallet);
            await _unitOfWork.SaveChangesAsync();

            var walletDto = new WalletDto
            {
                WalletId = wallet.WalletId,
                UserId = wallet.UserId,
                Username = user.Username,
                Balance = wallet.Balance,
                Status = wallet.IsActive ? "Active" : "Inactive",
                CreatedAt = wallet.CreatedAt,
                UpdatedAt = wallet.UpdatedAt
            };

            _logger.LogInformation("Wallet status updated for user {UserId}: {Status}", userId, request.IsActive ? "Active" : "Inactive");

            return Ok(walletDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating wallet status for user {UserId}", userId);
            return StatusCode(500, "An error occurred while updating wallet status");
        }
    }

    /// <summary>
    /// Get wallet statistics
    /// </summary>
    [HttpGet("statistics")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<WalletStatsDto>> GetWalletStatistics([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var start = startDate ?? DateTime.UtcNow.Date.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow.Date.AddDays(1);

            var wallets = await _unitOfWork.Repository<Wallet>().GetAllAsync();
            var transactions = await _unitOfWork.Repository<WalletTransaction>().GetAllAsync();
            var periodTransactions = transactions.Where(t => t.TransactionDate >= start && t.TransactionDate < end);

            var stats = new WalletStatsDto
            {
                TotalWallets = wallets.Count(),
                ActiveWallets = wallets.Count(w => w.IsActive),
                TotalBalance = wallets.Where(w => w.IsActive).Sum(w => w.Balance),
                TotalTransactions = periodTransactions.Count(),
                TotalDeposits = periodTransactions.Where(t => t.Type == WalletTransactionType.Deposit).Sum(t => t.Amount),
                TotalWithdrawals = periodTransactions.Where(t => t.Type == WalletTransactionType.Withdrawal).Sum(t => t.Amount),
                TotalTransfers = periodTransactions.Where(t => t.Type == WalletTransactionType.Transfer).Sum(t => t.Amount),
                AverageWalletBalance = wallets.Where(w => w.IsActive).Any() ? wallets.Where(w => w.IsActive).Average(w => w.Balance) : 0,
                StartDate = start,
                EndDate = end,
                GeneratedAt = DateTime.UtcNow
            };

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating wallet statistics");
            return StatusCode(500, "An error occurred while generating wallet statistics");
        }
    }

    private int GetCurrentUserId()
    {
        // TODO: Implement proper user ID extraction from JWT token
        return 1; // Placeholder
    }

    private string GetCurrentUsername()
    {
        // TODO: Implement proper username extraction from JWT token
        return "System"; // Placeholder
    }

    private string GetUsernameById(int userId)
    {
        // TODO: Implement proper username lookup
        return "System"; // Placeholder
    }
}

// DTOs and Request/Response Models
public class WalletDto
{
    public int WalletId { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class WalletTransactionDto
{
    public int TransactionId { get; set; }
    public int WalletId { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    public string? PaymentMethod { get; set; }
    public string ProcessedBy { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class WalletTransferResponseDto
{
    public int FromUserId { get; set; }
    public string FromUsername { get; set; } = string.Empty;
    public int ToUserId { get; set; }
    public string ToUsername { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal FromBalanceBefore { get; set; }
    public decimal FromBalanceAfter { get; set; }
    public decimal ToBalanceBefore { get; set; }
    public decimal ToBalanceAfter { get; set; }
    public string ProcessedBy { get; set; } = string.Empty;
    public DateTime TransferDate { get; set; }
    public int DebitTransactionId { get; set; }
    public int CreditTransactionId { get; set; }
}

public class WalletStatsDto
{
    public int TotalWallets { get; set; }
    public int ActiveWallets { get; set; }
    public decimal TotalBalance { get; set; }
    public int TotalTransactions { get; set; }
    public decimal TotalDeposits { get; set; }
    public decimal TotalWithdrawals { get; set; }
    public decimal TotalTransfers { get; set; }
    public decimal AverageWalletBalance { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime GeneratedAt { get; set; }
}

public class WalletDepositRequest
{
    [Required]
    [Range(0.01, 9999999)]
    public decimal Amount { get; set; }

    [StringLength(200)]
    public string? Description { get; set; }

    [StringLength(50)]
    public string? PaymentMethod { get; set; }
}

public class WalletWithdrawRequest
{
    [Required]
    [Range(0.01, 9999999)]
    public decimal Amount { get; set; }

    [StringLength(200)]
    public string? Description { get; set; }

    [StringLength(50)]
    public string? PaymentMethod { get; set; }
}

public class WalletTransferRequest
{
    [Required]
    public int FromUserId { get; set; }

    [Required]
    public int ToUserId { get; set; }

    [Required]
    [Range(0.01, 9999999)]
    public decimal Amount { get; set; }

    [Required]
    [StringLength(200)]
    public string Description { get; set; } = string.Empty;
}

public class UpdateWalletStatusRequest
{
    [Required]
    public bool IsActive { get; set; }
}

public class GetWalletTransactionsRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public WalletTransactionType? Type { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? SearchTerm { get; set; }
    public string? SortBy { get; set; } = "Date";
    public bool SortDescending { get; set; } = true;
}

// Enums for wallet functionality
public enum WalletTransactionType
{
    Deposit = 0,
    Withdrawal = 1,
    Transfer = 2,
    Purchase = 3,
    Refund = 4,
    Adjustment = 5
}

public enum WalletTransactionStatus
{
    Pending = 0,
    Completed = 1,
    Failed = 2,
    Cancelled = 3
}
