using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using GamingCafe.Core.Interfaces;
using GamingCafe.Core;

namespace GamingCafe.Data.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly GamingCafeContext _context;
    private readonly Dictionary<Type, object> _repositories;
    private IDbContextTransaction? _transaction;
    private bool _auditTrailEnabled;
    private string? _currentUserId;
    private bool _disposed;

    public UnitOfWork(GamingCafeContext context)
    {
        _context = context;
        _repositories = new Dictionary<Type, object>();
    }

    /// <summary>
    /// Attempt to atomically change a wallet balance by delta (positive credit, negative debit) using a single SQL statement.
    /// Returns true and sets out newBalance when successful, false when the update could not be applied (e.g., insufficient funds).
    /// This avoids read-modify-write races and is safe for concurrent access.
    /// </summary>
    public async Task<(bool Success, decimal NewBalance)> TryAtomicUpdateWalletBalanceAsync(int walletId, decimal delta)
    {
        // Observe and trace the wallet update operation
        Observability.WalletUpdateCounter.Add(1);
        var sw = System.Diagnostics.Stopwatch.StartNew();

        using (var activity = Observability.ActivitySource.StartActivity("TryAtomicUpdateWalletBalance", System.Diagnostics.ActivityKind.Internal))
        {
            activity?.SetTag("wallet.id", walletId);
            activity?.SetTag("wallet.delta", delta);

            // Use a single SQL UPDATE that conditionally updates when balance check passes (for debits)
            // For credit (delta > 0) we always update.
            if (delta >= 0)
            {
                var sql = "UPDATE \"Wallets\" SET \"Balance\" = \"Balance\" + @delta, \"UpdatedAt\" = now() WHERE \"WalletId\" = @wid RETURNING \"Balance\";";
                var newBalance = await ExecuteSqlScalarDecimalAsync(sql, ("delta", delta), ("wid", walletId));
                if (newBalance.HasValue)
                {
                    Observability.WalletUpdateSuccessCounter.Add(1);
                    activity?.SetTag("wallet.result", "success");
                    sw.Stop();
                    Observability.WalletUpdateDuration.Record(sw.Elapsed.TotalMilliseconds);
                    return (true, newBalance.Value);
                }
                activity?.SetTag("wallet.result", "failed");
                sw.Stop();
                Observability.WalletUpdateDuration.Record(sw.Elapsed.TotalMilliseconds);
                return (false, 0m);
            }
            else
            {
                // Debit: only subtract if sufficient funds
                var sql = "UPDATE \"Wallets\" SET \"Balance\" = \"Balance\" + @delta, \"UpdatedAt\" = now() WHERE \"WalletId\" = @wid AND \"Balance\" >= @min RETURNING \"Balance\";";
                var newBalance = await ExecuteSqlScalarDecimalAsync(sql, ("delta", delta), ("wid", walletId), ("min", Math.Abs(delta)));
                if (newBalance.HasValue)
                {
                    Observability.WalletUpdateSuccessCounter.Add(1);
                    activity?.SetTag("wallet.result", "success");
                    sw.Stop();
                    Observability.WalletUpdateDuration.Record(sw.Elapsed.TotalMilliseconds);
                    return (true, newBalance.Value);
                }
                activity?.SetTag("wallet.result", "failed");
                sw.Stop();
                Observability.WalletUpdateDuration.Record(sw.Elapsed.TotalMilliseconds);
                return (false, 0m);
            }
        }
    }

    private async Task<decimal?> ExecuteSqlScalarDecimalAsync(string sql, params (string name, object value)[] parameters)
    {
        await using var conn = _context.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (name, value) in parameters)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value ?? DBNull.Value;
            cmd.Parameters.Add(p);
        }

        var result = await cmd.ExecuteScalarAsync();
        if (result == null || result == DBNull.Value)
            return null;

        return Convert.ToDecimal(result);
    }

    public IRepository<T> Repository<T>() where T : class
    {
        var type = typeof(T);
        
        if (!_repositories.ContainsKey(type))
        {
            var repositoryType = typeof(Repository<>).MakeGenericType(type);
            var repositoryInstance = Activator.CreateInstance(repositoryType, _context);
            _repositories[type] = repositoryInstance ?? throw new InvalidOperationException($"Could not create repository for type {type.Name}");
        }

        return (IRepository<T>)_repositories[type];
    }

    public async Task<int> SaveChangesAsync()
    {
        return await SaveChangesAsync(CancellationToken.None);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        // Apply audit trail if enabled
        if (_auditTrailEnabled && !string.IsNullOrEmpty(_currentUserId))
        {
            ApplyAuditTrail();
        }

        try
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // Handle optimistic concurrency conflicts
            throw new InvalidOperationException("The record you attempted to edit was modified by another user after you got the original value. The edit operation was canceled.", ex);
        }
    }

    public async Task BeginTransactionAsync()
    {
        if (_transaction != null)
        {
            throw new InvalidOperationException("A transaction is already in progress.");
        }

        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("No transaction in progress.");
        }

        try
        {
            await _transaction.CommitAsync();
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("No transaction in progress.");
        }

        try
        {
            await _transaction.RollbackAsync();
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task<int> BulkInsertAsync<T>(IEnumerable<T> entities) where T : class
    {
        var entityList = entities.ToList();
        await _context.Set<T>().AddRangeAsync(entityList);
        return await _context.SaveChangesAsync();
    }

    public async Task<int> BulkUpdateAsync<T>(IEnumerable<T> entities) where T : class
    {
        var entityList = entities.ToList();
        _context.Set<T>().UpdateRange(entityList);
        return await _context.SaveChangesAsync();
    }

    public async Task<int> BulkDeleteAsync<T>(IEnumerable<T> entities) where T : class
    {
        var entityList = entities.ToList();
        _context.Set<T>().RemoveRange(entityList);
        return await _context.SaveChangesAsync();
    }

    public void EnableAuditTrail(string userId)
    {
        _auditTrailEnabled = true;
        _currentUserId = userId;
    }

    public void DisableAuditTrail()
    {
        _auditTrailEnabled = false;
        _currentUserId = null;
    }

    public async Task<bool> DatabaseExistsAsync()
    {
        return await _context.Database.CanConnectAsync();
    }

    public Task EnsureDatabaseCreatedAsync()
    {
        // EnsureCreated is intentionally removed to avoid migration/race conditions. Use migrations instead.
        throw new InvalidOperationException("EnsureDatabaseCreatedAsync is not supported. Use MigrateDatabaseAsync which relies on EF Core migrations.");
    }

    public async Task MigrateDatabaseAsync()
    {
        await _context.Database.MigrateAsync();
    }

    private void ApplyAuditTrail()
    {
        var entries = _context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                // Try to set creation date if the entity has such a property
                var createdAtProperty = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "CreatedAt");
                if (createdAtProperty != null)
                {
                    createdAtProperty.CurrentValue = DateTime.UtcNow;
                }

                var createdByProperty = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "CreatedBy");
                if (createdByProperty != null && !string.IsNullOrEmpty(_currentUserId))
                {
                    createdByProperty.CurrentValue = _currentUserId;
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                // Try to set updated date if the entity has such a property
                var updatedAtProperty = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "UpdatedAt");
                if (updatedAtProperty != null)
                {
                    updatedAtProperty.CurrentValue = DateTime.UtcNow;
                }

                var updatedByProperty = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "UpdatedBy");
                if (updatedByProperty != null && !string.IsNullOrEmpty(_currentUserId))
                {
                    updatedByProperty.CurrentValue = _currentUserId;
                }
            }
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _transaction?.Dispose();
            _context?.Dispose();
            _disposed = true;
        }
    }
}
