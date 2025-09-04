using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using GamingCafe.Data.Interfaces;

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

    public async Task EnsureDatabaseCreatedAsync()
    {
        await _context.Database.EnsureCreatedAsync();
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
