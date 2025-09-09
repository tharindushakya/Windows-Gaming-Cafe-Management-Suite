using System;
using System.Threading.Tasks;
using GamingCafe.Data;
using GamingCafe.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace GamingCafe.Application.UseCases.Wallet
{
    public class WalletService
    {
        private readonly GamingCafeContext _db;

        public WalletService(GamingCafeContext db)
        {
            _db = db;
        }

        // Very small orchestration: updates the canonical Wallet and records a WalletTransaction.
        // Caller should handle retries/transactions as appropriate.
        public async Task<bool> TryApplyAsync(UpdateWalletCommand cmd)
        {
            // Load the canonical wallet for the user
            var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == cmd.UserId);
            if (wallet == null)
            {
                // Create a wallet if missing
                wallet = new GamingCafe.Core.Models.Wallet { UserId = cmd.UserId, Balance = 0m };
                _db.Wallets.Add(wallet);
                await _db.SaveChangesAsync();
            }

            // Simple optimistic update: adjust balance and add transaction inside a transaction
            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var prior = wallet.Balance;
                wallet.Balance += cmd.Amount;
                _db.WalletTransactions.Add(new GamingCafe.Core.Models.WalletTransaction
                {
                    WalletId = wallet.WalletId,
                    UserId = wallet.UserId,
                    Amount = cmd.Amount,
                    Type = GamingCafe.Core.Models.WalletTransactionType.Credit,
                    Description = cmd.Reason,
                    BalanceBefore = prior,
                    BalanceAfter = wallet.Balance,
                    TransactionDate = DateTime.UtcNow,
                    Status = GamingCafe.Core.Models.WalletTransactionStatus.Completed
                });

                await _db.SaveChangesAsync();
                await tx.CommitAsync();
                return true;
            }
            catch
            {
                await tx.RollbackAsync();
                return false;
            }
        }
    }
}
