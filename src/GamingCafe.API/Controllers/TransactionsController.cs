using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using GamingCafe.Core.Models;
using GamingCafe.Core.Interfaces;
using GamingCafe.Data.Repositories;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace GamingCafe.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class TransactionsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TransactionsController> _logger;
    private readonly GamingCafe.Application.UseCases.Wallet.WalletService _walletService;

        public TransactionsController(IUnitOfWork unitOfWork, ILogger<TransactionsController> logger, GamingCafe.Application.UseCases.Wallet.WalletService walletService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
            _walletService = walletService;
    }

    /// <summary>
    /// Get paginated list of transactions with filtering and sorting
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResponse<TransactionDto>>> GetTransactions([FromQuery] GetTransactionsRequest request)
    {
        try
        {
            // Get all transactions first
            var transactions = await _unitOfWork.Repository<Transaction>().GetAllAsync();
            var filteredTransactions = transactions.AsQueryable();

            // Apply filters
            if (request.UserId.HasValue)
            {
                filteredTransactions = filteredTransactions.Where(t => t.UserId == request.UserId.Value);
            }

            if (request.SessionId.HasValue)
            {
                filteredTransactions = filteredTransactions.Where(t => t.SessionId == request.SessionId.Value);
            }

            if (request.ProductId.HasValue)
            {
                filteredTransactions = filteredTransactions.Where(t => t.ProductId == request.ProductId.Value);
            }

            if (request.Type.HasValue)
            {
                filteredTransactions = filteredTransactions.Where(t => t.Type == request.Type.Value);
            }

            if (request.Status.HasValue)
            {
                filteredTransactions = filteredTransactions.Where(t => t.Status == request.Status.Value);
            }

            if (request.PaymentMethod.HasValue)
            {
                filteredTransactions = filteredTransactions.Where(t => t.PaymentMethod == request.PaymentMethod.Value);
            }

            if (request.MinAmount.HasValue)
            {
                filteredTransactions = filteredTransactions.Where(t => t.Amount >= request.MinAmount.Value);
            }

            if (request.MaxAmount.HasValue)
            {
                filteredTransactions = filteredTransactions.Where(t => t.Amount <= request.MaxAmount.Value);
            }

            if (request.StartDate.HasValue)
            {
                filteredTransactions = filteredTransactions.Where(t => t.CreatedAt >= request.StartDate.Value);
            }

            if (request.EndDate.HasValue)
            {
                filteredTransactions = filteredTransactions.Where(t => t.CreatedAt <= request.EndDate.Value);
            }

            if (!string.IsNullOrEmpty(request.Search))
            {
                filteredTransactions = filteredTransactions.Where(t => 
                    t.Description.Contains(request.Search, StringComparison.OrdinalIgnoreCase) ||
                    t.PaymentReference.Contains(request.Search, StringComparison.OrdinalIgnoreCase) ||
                    t.Notes.Contains(request.Search, StringComparison.OrdinalIgnoreCase));
            }

            // Apply sorting
            filteredTransactions = request.SortBy?.ToLower() switch
            {
                "amount" => request.SortDescending ? 
                    filteredTransactions.OrderByDescending(t => t.Amount) : 
                    filteredTransactions.OrderBy(t => t.Amount),
                "type" => request.SortDescending ? 
                    filteredTransactions.OrderByDescending(t => t.Type) : 
                    filteredTransactions.OrderBy(t => t.Type),
                "status" => request.SortDescending ? 
                    filteredTransactions.OrderByDescending(t => t.Status) : 
                    filteredTransactions.OrderBy(t => t.Status),
                "processedat" => request.SortDescending ? 
                    filteredTransactions.OrderByDescending(t => t.ProcessedAt) : 
                    filteredTransactions.OrderBy(t => t.ProcessedAt),
                "createdat" => request.SortDescending ? 
                    filteredTransactions.OrderByDescending(t => t.CreatedAt) : 
                    filteredTransactions.OrderBy(t => t.CreatedAt),
                _ => filteredTransactions.OrderByDescending(t => t.CreatedAt)
            };

            var totalCount = filteredTransactions.Count();
            var pagedTransactions = filteredTransactions
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList() // Convert to list first to enable null propagation
                .Select(t => new TransactionDto
                {
                    TransactionId = t.TransactionId,
                    UserId = t.UserId,
                    SessionId = t.SessionId,
                    ProductId = t.ProductId,
                    Description = t.Description,
                    Amount = t.Amount,
                    Type = t.Type.ToString(),
                    PaymentMethod = t.PaymentMethod.ToString(),
                    Status = t.Status.ToString(),
                    PaymentReference = t.PaymentReference,
                    CreatedAt = t.CreatedAt,
                    ProcessedAt = t.ProcessedAt,
                    Notes = t.Notes,
                    Username = t.User?.Username ?? "Unknown",
                    ProductName = t.Product?.Name ?? null
                })
                .ToList();

            var response = new PagedResponse<TransactionDto>
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
            _logger.LogError(ex, "Error retrieving transactions");
            return StatusCode(500, "An error occurred while retrieving transactions");
        }
    }

    /// <summary>
    /// Get transaction by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<TransactionDto>> GetTransaction(int id)
    {
        try
        {
            var transaction = await _unitOfWork.Repository<Transaction>().GetByIdAsync(id);
            if (transaction == null)
                return NotFound();

            var transactionDto = new TransactionDto
            {
                TransactionId = transaction.TransactionId,
                UserId = transaction.UserId,
                SessionId = transaction.SessionId,
                ProductId = transaction.ProductId,
                Description = transaction.Description,
                Amount = transaction.Amount,
                Type = transaction.Type.ToString(),
                PaymentMethod = transaction.PaymentMethod.ToString(),
                Status = transaction.Status.ToString(),
                PaymentReference = transaction.PaymentReference,
                CreatedAt = transaction.CreatedAt,
                ProcessedAt = transaction.ProcessedAt,
                Notes = transaction.Notes,
                Username = transaction.User.Username,
                ProductName = transaction.Product?.Name
            };

            return Ok(transactionDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving transaction with ID {TransactionId}", id);
            return StatusCode(500, "An error occurred while retrieving the transaction");
        }
    }

    /// <summary>
    /// Create a new transaction
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<ActionResult<TransactionDto>> CreateTransaction([FromBody] CreateTransactionRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Validate user exists
            var user = await _unitOfWork.Repository<User>().GetByIdAsync(request.UserId);
            if (user == null)
                return BadRequest("User not found");

            // Validate session exists if provided
            if (request.SessionId.HasValue)
            {
                var session = await _unitOfWork.Repository<GameSession>().GetByIdAsync(request.SessionId.Value);
                if (session == null)
                    return BadRequest("Game session not found");
            }

            // Validate product exists if provided
            if (request.ProductId.HasValue)
            {
                var product = await _unitOfWork.Repository<Product>().GetByIdAsync(request.ProductId.Value);
                if (product == null)
                    return BadRequest("Product not found");
            }

            var transaction = new Transaction
            {
                UserId = request.UserId,
                SessionId = request.SessionId,
                ProductId = request.ProductId,
                Description = request.Description,
                Amount = request.Amount,
                Type = request.Type,
                PaymentMethod = request.PaymentMethod,
                Status = TransactionStatus.Pending,
                PaymentReference = request.PaymentReference!,
                Notes = request.Notes!,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<Transaction>().AddAsync(transaction);
            await _unitOfWork.SaveChangesAsync();

            var transactionDto = new TransactionDto
            {
                TransactionId = transaction.TransactionId,
                UserId = transaction.UserId,
                SessionId = transaction.SessionId,
                ProductId = transaction.ProductId,
                Description = transaction.Description,
                Amount = transaction.Amount,
                Type = transaction.Type.ToString(),
                PaymentMethod = transaction.PaymentMethod.ToString(),
                Status = transaction.Status.ToString(),
                PaymentReference = transaction.PaymentReference,
                CreatedAt = transaction.CreatedAt,
                ProcessedAt = transaction.ProcessedAt,
                Notes = transaction.Notes,
                Username = user.Username,
                ProductName = null // Will be loaded separately if needed
            };

            _logger.LogInformation("Created new transaction: {TransactionId} for user {UserId}", 
                transaction.TransactionId, request.UserId);
            
            return CreatedAtAction(nameof(GetTransaction), new { id = transaction.TransactionId }, transactionDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating transaction");
            return StatusCode(500, "An error occurred while creating the transaction");
        }
    }

    /// <summary>
    /// Update transaction status
    /// </summary>
    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<ActionResult<TransactionDto>> UpdateTransactionStatus(int id, [FromBody] UpdateTransactionStatusRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var transaction = await _unitOfWork.Repository<Transaction>().GetByIdAsync(id);
            if (transaction == null)
                return NotFound();

            var oldStatus = transaction.Status;
            transaction.Status = request.Status;
            
            if (request.Status == TransactionStatus.Completed && transaction.ProcessedAt == null)
            {
                transaction.ProcessedAt = DateTime.UtcNow;
            }

            if (!string.IsNullOrEmpty(request.Notes))
            {
                transaction.Notes = string.IsNullOrEmpty(transaction.Notes) 
                    ? request.Notes 
                    : $"{transaction.Notes}; {request.Notes}";
            }

            _unitOfWork.Repository<Transaction>().Update(transaction);
            await _unitOfWork.SaveChangesAsync();

            var transactionDto = new TransactionDto
            {
                TransactionId = transaction.TransactionId,
                UserId = transaction.UserId,
                SessionId = transaction.SessionId,
                ProductId = transaction.ProductId,
                Description = transaction.Description,
                Amount = transaction.Amount,
                Type = transaction.Type.ToString(),
                PaymentMethod = transaction.PaymentMethod.ToString(),
                Status = transaction.Status.ToString(),
                PaymentReference = transaction.PaymentReference,
                CreatedAt = transaction.CreatedAt,
                ProcessedAt = transaction.ProcessedAt,
                Notes = transaction.Notes,
                Username = transaction.User?.Username ?? "Unknown",
                ProductName = transaction.Product?.Name
            };

            _logger.LogInformation("Updated transaction {TransactionId} status from {OldStatus} to {NewStatus}", 
                id, oldStatus, request.Status);

            return Ok(transactionDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating transaction status for ID {TransactionId}", id);
            return StatusCode(500, "An error occurred while updating the transaction status");
        }
    }

    /// <summary>
    /// Process refund for a transaction
    /// </summary>
    [HttpPost("{id}/refund")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<TransactionDto>> ProcessRefund(int id, [FromBody] ProcessRefundRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var originalTransaction = await _unitOfWork.Repository<Transaction>().GetByIdAsync(id);
            if (originalTransaction == null)
                return NotFound();

            if (originalTransaction.Status != TransactionStatus.Completed)
                return BadRequest("Can only refund completed transactions");

            if (request.RefundAmount <= 0 || request.RefundAmount > originalTransaction.Amount)
                return BadRequest("Invalid refund amount");

            // Create refund transaction
            var refundTransaction = new Transaction
            {
                UserId = originalTransaction.UserId,
                SessionId = originalTransaction.SessionId,
                ProductId = originalTransaction.ProductId,
                Description = $"Refund for transaction {originalTransaction.TransactionId}: {request.Reason}",
                Amount = -request.RefundAmount, // Negative amount for refund
                Type = TransactionType.Refund,
                PaymentMethod = originalTransaction.PaymentMethod,
                Status = TransactionStatus.Completed,
                PaymentReference = $"REFUND-{originalTransaction.TransactionId}",
                Notes = request.Reason ?? "Refund processed",
                CreatedAt = DateTime.UtcNow,
                ProcessedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<Transaction>().AddAsync(refundTransaction);

            // Update original transaction status if full refund
            if (request.RefundAmount == originalTransaction.Amount)
            {
                originalTransaction.Status = TransactionStatus.Refunded;
                _unitOfWork.Repository<Transaction>().Update(originalTransaction);
            }

            // Update user wallet if applicable (atomic)
            if (originalTransaction.PaymentMethod == PaymentMethod.Wallet)
            {
                var cmd = new GamingCafe.Application.UseCases.Wallet.UpdateWalletCommand(originalTransaction.UserId, request.RefundAmount, $"Refund for transaction {originalTransaction.TransactionId}");
                var applied = await _walletService.TryApplyAsync(cmd);
                if (!applied)
                {
                    return Conflict(new { message = "Failed to credit wallet for refund. Please retry." });
                }
                // The WalletService records a WalletTransaction; we rely on it to write details.
            }

            await _unitOfWork.SaveChangesAsync();

            var refundDto = new TransactionDto
            {
                TransactionId = refundTransaction.TransactionId,
                UserId = refundTransaction.UserId,
                SessionId = refundTransaction.SessionId,
                ProductId = refundTransaction.ProductId,
                Description = refundTransaction.Description,
                Amount = refundTransaction.Amount,
                Type = refundTransaction.Type.ToString(),
                PaymentMethod = refundTransaction.PaymentMethod.ToString(),
                Status = refundTransaction.Status.ToString(),
                PaymentReference = refundTransaction.PaymentReference,
                CreatedAt = refundTransaction.CreatedAt,
                ProcessedAt = refundTransaction.ProcessedAt,
                Notes = refundTransaction.Notes,
                Username = originalTransaction.User?.Username ?? "Unknown",
                ProductName = originalTransaction.Product?.Name
            };

            _logger.LogInformation("Processed refund of {RefundAmount} for transaction {TransactionId}", 
                request.RefundAmount, id);

            return Ok(refundDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refund for transaction {TransactionId}", id);
            return StatusCode(500, "An error occurred while processing the refund");
        }
    }

    /// <summary>
    /// Get transaction statistics
    /// </summary>
    [HttpGet("stats")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<TransactionStatsDto>> GetTransactionStats([FromQuery] GetStatsRequest request)
    {
        try
        {
            var startDate = request.StartDate ?? DateTime.UtcNow.AddDays(-30);
            var endDate = request.EndDate ?? DateTime.UtcNow;

            var allTransactions = await _unitOfWork.Repository<Transaction>().GetAllAsync();
            var transactions = allTransactions
                .Where(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate)
                .ToList();

            var completedTransactions = transactions.Where(t => t.Status == TransactionStatus.Completed).ToList();

            var stats = new TransactionStatsDto
            {
                TotalTransactions = transactions.Count,
                CompletedTransactions = completedTransactions.Count,
                PendingTransactions = transactions.Count(t => t.Status == TransactionStatus.Pending),
                FailedTransactions = transactions.Count(t => t.Status == TransactionStatus.Failed),
                RefundedTransactions = transactions.Count(t => t.Status == TransactionStatus.Refunded),
                
                TotalRevenue = completedTransactions.Where(t => t.Amount > 0).Sum(t => t.Amount),
                TotalRefunds = completedTransactions.Where(t => t.Amount < 0).Sum(t => Math.Abs(t.Amount)),
                
                GameTimeRevenue = completedTransactions.Where(t => t.Type == TransactionType.GameTime && t.Amount > 0).Sum(t => t.Amount),
                ProductRevenue = completedTransactions.Where(t => t.Type == TransactionType.Product && t.Amount > 0).Sum(t => t.Amount),
                WalletTopupRevenue = completedTransactions.Where(t => t.Type == TransactionType.WalletTopup && t.Amount > 0).Sum(t => t.Amount),
                
                CashTransactions = completedTransactions.Count(t => t.PaymentMethod == PaymentMethod.Cash),
                CardTransactions = completedTransactions.Count(t => t.PaymentMethod == PaymentMethod.CreditCard || t.PaymentMethod == PaymentMethod.DebitCard),
                WalletTransactions = completedTransactions.Count(t => t.PaymentMethod == PaymentMethod.Wallet),
                
                AverageTransactionAmount = completedTransactions.Any() ? completedTransactions.Average(t => t.Amount) : 0,
                
                StartDate = startDate,
                EndDate = endDate
            };

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving transaction statistics");
            return StatusCode(500, "An error occurred while retrieving transaction statistics");
        }
    }

    /// <summary>
    /// Get user's transaction history
    /// </summary>
    [HttpGet("user/{userId}")]
    public async Task<ActionResult<PagedResponse<TransactionDto>>> GetUserTransactions(int userId, [FromQuery] GetUserTransactionsRequest request)
    {
        try
        {
            var allTransactions = await _unitOfWork.Repository<Transaction>().GetAllAsync();
            var userTransactions = allTransactions
                .Where(t => t.UserId == userId)
                .AsQueryable();

            if (request.Type.HasValue)
            {
                userTransactions = userTransactions.Where(t => t.Type == request.Type.Value);
            }

            if (request.Status.HasValue)
            {
                userTransactions = userTransactions.Where(t => t.Status == request.Status.Value);
            }

            if (request.StartDate.HasValue)
            {
                userTransactions = userTransactions.Where(t => t.CreatedAt >= request.StartDate.Value);
            }

            if (request.EndDate.HasValue)
            {
                userTransactions = userTransactions.Where(t => t.CreatedAt <= request.EndDate.Value);
            }

            userTransactions = userTransactions.OrderByDescending(t => t.CreatedAt);

            var totalCount = userTransactions.Count();
            var pagedTransactions = userTransactions
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(t => new TransactionDto
                {
                    TransactionId = t.TransactionId,
                    UserId = t.UserId,
                    SessionId = t.SessionId,
                    ProductId = t.ProductId,
                    Description = t.Description,
                    Amount = t.Amount,
                    Type = t.Type.ToString(),
                    PaymentMethod = t.PaymentMethod.ToString(),
                    Status = t.Status.ToString(),
                    PaymentReference = t.PaymentReference,
                    CreatedAt = t.CreatedAt,
                    ProcessedAt = t.ProcessedAt,
                    Notes = t.Notes,
                    Username = t.User.Username,
                    ProductName = t.Product != null ? t.Product.Name : null
                })
                .ToList();

            var response = new PagedResponse<TransactionDto>
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
            _logger.LogError(ex, "Error retrieving transactions for user {UserId}", userId);
            return StatusCode(500, "An error occurred while retrieving user transactions");
        }
    }
}

// DTOs and Request/Response Models
public class TransactionDto
{
    public int TransactionId { get; set; }
    public int UserId { get; set; }
    public int? SessionId { get; set; }
    public int? ProductId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Type { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PaymentReference { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? ProductName { get; set; }
}

public class GetTransactionsRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int? UserId { get; set; }
    public int? SessionId { get; set; }
    public int? ProductId { get; set; }
    public TransactionType? Type { get; set; }
    public TransactionStatus? Status { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Search { get; set; }
    public string? SortBy { get; set; } = "CreatedAt";
    public bool SortDescending { get; set; } = true;
}

public class CreateTransactionRequest
{
    [Required]
    public int UserId { get; set; }

    public int? SessionId { get; set; }

    public int? ProductId { get; set; }

    [Required]
    [StringLength(100)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required]
    public TransactionType Type { get; set; }

    [Required]
    public PaymentMethod PaymentMethod { get; set; }

    [StringLength(100)]
    public string? PaymentReference { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}

public class UpdateTransactionStatusRequest
{
    [Required]
    public TransactionStatus Status { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}

public class ProcessRefundRequest
{
    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal RefundAmount { get; set; }

    [Required]
    [StringLength(200)]
    public string Reason { get; set; } = string.Empty;
}

public class GetStatsRequest
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class GetUserTransactionsRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public TransactionType? Type { get; set; }
    public TransactionStatus? Status { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class TransactionStatsDto
{
    public int TotalTransactions { get; set; }
    public int CompletedTransactions { get; set; }
    public int PendingTransactions { get; set; }
    public int FailedTransactions { get; set; }
    public int RefundedTransactions { get; set; }

    public decimal TotalRevenue { get; set; }
    public decimal TotalRefunds { get; set; }
    public decimal NetRevenue => TotalRevenue - TotalRefunds;

    public decimal GameTimeRevenue { get; set; }
    public decimal ProductRevenue { get; set; }
    public decimal WalletTopupRevenue { get; set; }

    public int CashTransactions { get; set; }
    public int CardTransactions { get; set; }
    public int WalletTransactions { get; set; }

    public decimal AverageTransactionAmount { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

