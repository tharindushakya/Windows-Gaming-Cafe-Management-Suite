namespace GamingCafe.Data.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IRepository<T> Repository<T>() where T : class;
    Task<int> SaveChangesAsync();
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
    
    // Bulk operations
    Task<int> BulkInsertAsync<T>(IEnumerable<T> entities) where T : class;
    Task<int> BulkUpdateAsync<T>(IEnumerable<T> entities) where T : class;
    Task<int> BulkDeleteAsync<T>(IEnumerable<T> entities) where T : class;
    
    // Audit operations
    void EnableAuditTrail(string userId);
    void DisableAuditTrail();
    
    // Database operations
    Task<bool> DatabaseExistsAsync();
    Task EnsureDatabaseCreatedAsync();
    Task MigrateDatabaseAsync();
    
    // Attempt to atomically update a wallet balance by delta (positive credit, negative debit).
    Task<(bool Success, decimal NewBalance)> TryAtomicUpdateWalletBalanceAsync(int walletId, decimal delta);
}
