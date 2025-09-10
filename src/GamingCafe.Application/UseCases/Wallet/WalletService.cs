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

            // Use a serializable transaction to avoid phantom reads and ensure strict consistency for balance updates.
            await using var tx = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            try
            {
                var prior = wallet.Balance;
                wallet.Balance += cmd.Amount;
                _db.WalletTransactions.Add(new GamingCafe.Core.Models.WalletTransaction
                {
                    WalletId = wallet.WalletId,
                    UserId = wallet.UserId,
                    Amount = cmd.Amount,
                    Type = cmd.Amount >= 0 ? GamingCafe.Core.Models.WalletTransactionType.Credit : GamingCafe.Core.Models.WalletTransactionType.Debit,
                    Description = cmd.Reason,
                    BalanceBefore = prior,
                    BalanceAfter = wallet.Balance,
                    TransactionDate = DateTime.UtcNow,
                    Status = GamingCafe.Core.Models.WalletTransactionStatus.Completed
                });

                try
                {
                    await _db.SaveChangesAsync();
                }
                catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
                {
                    // Concurrency conflict detected: another update changed the wallet. Fail and let caller retry.
                    await tx.RollbackAsync();
                    return false;
                }

                await tx.CommitAsync();
                // Record domain metric for wallet transactions
                GamingCafe.Core.Observability.WalletTransactionsCounter.Add(1);
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
