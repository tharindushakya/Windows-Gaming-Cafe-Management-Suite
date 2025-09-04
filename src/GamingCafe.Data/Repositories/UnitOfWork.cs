using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using GamingCafe.Data.Interfaces;
using GamingCafe.Core.Models.Common;

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
        
        // Set audit properties if entities implement IAuditable
        if (_auditTrailEnabled && !string.IsNullOrEmpty(_currentUserId))
        {
            foreach (var entity in entityList.OfType<IAuditable>())
            {
                entity.CreatedAt = DateTime.UtcNow;
                entity.CreatedBy = _currentUserId;
            }
        }

        await _context.Set<T>().AddRangeAsync(entityList);
        return await _context.SaveChangesAsync();
    }

    public async Task<int> BulkUpdateAsync<T>(IEnumerable<T> entities) where T : class
    {
        var entityList = entities.ToList();
        
        // Set audit properties if entities implement IAuditable
        if (_auditTrailEnabled && !string.IsNullOrEmpty(_currentUserId))
        {
            foreach (var entity in entityList.OfType<IAuditable>())
            {
                entity.UpdatedAt = DateTime.UtcNow;
                entity.UpdatedBy = _currentUserId;
            }
        }

        _context.Set<T>().UpdateRange(entityList);
        return await _context.SaveChangesAsync();
    }

    public async Task<int> BulkDeleteAsync<T>(IEnumerable<T> entities) where T : class
    {
        var entityList = entities.ToList();
        
        // Check if entities support soft delete
        if (typeof(ISoftDelete).IsAssignableFrom(typeof(T)))
        {
            foreach (var entity in entityList.OfType<ISoftDelete>())
            {
                entity.IsDeleted = true;
                entity.DeletedAt = DateTime.UtcNow;
                if (_auditTrailEnabled && !string.IsNullOrEmpty(_currentUserId))
                {
                    entity.DeletedBy = _currentUserId;
                }
            }
            
            _context.Set<T>().UpdateRange(entityList);
        }
        else
        {
            _context.Set<T>().RemoveRange(entityList);
        }
        
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
            .Where(e => e.Entity is IAuditable && 
                       (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            var auditable = (IAuditable)entry.Entity;
            
            switch (entry.State)
            {
                case EntityState.Added:
                    auditable.CreatedAt = DateTime.UtcNow;
                    auditable.CreatedBy = _currentUserId;
                    break;
                
                case EntityState.Modified:
                    auditable.UpdatedAt = DateTime.UtcNow;
                    auditable.UpdatedBy = _currentUserId;
                    break;
            }
        }

        // Handle soft delete entities
        var softDeleteEntries = _context.ChangeTracker.Entries()
            .Where(e => e.Entity is ISoftDelete && e.State == EntityState.Modified);

        foreach (var entry in softDeleteEntries)
        {
            var softDelete = (ISoftDelete)entry.Entity;
            
            if (softDelete.IsDeleted && softDelete.DeletedAt == null)
            {
                softDelete.DeletedAt = DateTime.UtcNow;
                softDelete.DeletedBy = _currentUserId;
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
