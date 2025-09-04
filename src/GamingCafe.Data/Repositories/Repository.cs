using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Linq.Expressions;
using GamingCafe.Data.Interfaces;
using GamingCafe.Core.Models.Common;

namespace GamingCafe.Data.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    protected readonly GamingCafeContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(GamingCafeContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public virtual async Task<T?> GetByIdAsync(int id, params Expression<Func<T, object>>[] includes)
    {
        IQueryable<T> query = _dbSet;
        query = includes.Aggregate(query, (current, include) => current.Include(include));
        return await query.FirstOrDefaultAsync(GetIdExpression(id));
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        IQueryable<T> query = _dbSet;
        
        // Apply soft delete filter if entity implements ISoftDelete
        if (typeof(ISoftDelete).IsAssignableFrom(typeof(T)))
        {
            query = query.Where(e => !((ISoftDelete)e).IsDeleted);
        }
        
        return await query.ToListAsync();
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync(params Expression<Func<T, object>>[] includes)
    {
        IQueryable<T> query = _dbSet;
        query = includes.Aggregate(query, (current, include) => current.Include(include));
        
        // Apply soft delete filter if entity implements ISoftDelete
        if (typeof(ISoftDelete).IsAssignableFrom(typeof(T)))
        {
            query = query.Where(e => !((ISoftDelete)e).IsDeleted);
        }
        
        return await query.ToListAsync();
    }

    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> expression)
    {
        IQueryable<T> query = _dbSet.Where(expression);
        
        // Apply soft delete filter if entity implements ISoftDelete
        if (typeof(ISoftDelete).IsAssignableFrom(typeof(T)))
        {
            query = query.Where(e => !((ISoftDelete)e).IsDeleted);
        }
        
        return await query.ToListAsync();
    }

    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> expression, params Expression<Func<T, object>>[] includes)
    {
        IQueryable<T> query = _dbSet.Where(expression);
        query = includes.Aggregate(query, (current, include) => current.Include(include));
        
        // Apply soft delete filter if entity implements ISoftDelete
        if (typeof(ISoftDelete).IsAssignableFrom(typeof(T)))
        {
            query = query.Where(e => !((ISoftDelete)e).IsDeleted);
        }
        
        return await query.ToListAsync();
    }

    public virtual async Task<T?> FindFirstAsync(Expression<Func<T, bool>> expression)
    {
        IQueryable<T> query = _dbSet.Where(expression);
        
        // Apply soft delete filter if entity implements ISoftDelete
        if (typeof(ISoftDelete).IsAssignableFrom(typeof(T)))
        {
            query = query.Where(e => !((ISoftDelete)e).IsDeleted);
        }
        
        return await query.FirstOrDefaultAsync();
    }

    public virtual async Task<T?> FindFirstAsync(Expression<Func<T, bool>> expression, params Expression<Func<T, object>>[] includes)
    {
        IQueryable<T> query = _dbSet.Where(expression);
        query = includes.Aggregate(query, (current, include) => current.Include(include));
        
        // Apply soft delete filter if entity implements ISoftDelete
        if (typeof(ISoftDelete).IsAssignableFrom(typeof(T)))
        {
            query = query.Where(e => !((ISoftDelete)e).IsDeleted);
        }
        
        return await query.FirstOrDefaultAsync();
    }

    public virtual async Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(
        int page, 
        int pageSize, 
        Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        params Expression<Func<T, object>>[] includes)
    {
        IQueryable<T> query = _dbSet;

        // Apply includes
        query = includes.Aggregate(query, (current, include) => current.Include(include));

        // Apply soft delete filter if entity implements ISoftDelete
        if (typeof(ISoftDelete).IsAssignableFrom(typeof(T)))
        {
            query = query.Where(e => !((ISoftDelete)e).IsDeleted);
        }

        // Apply filter
        if (filter != null)
        {
            query = query.Where(filter);
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync();

        // Apply ordering
        if (orderBy != null)
        {
            query = orderBy(query);
        }

        // Apply pagination
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public virtual async Task<int> CountAsync()
    {
        IQueryable<T> query = _dbSet;
        
        // Apply soft delete filter if entity implements ISoftDelete
        if (typeof(ISoftDelete).IsAssignableFrom(typeof(T)))
        {
            query = query.Where(e => !((ISoftDelete)e).IsDeleted);
        }
        
        return await query.CountAsync();
    }

    public virtual async Task<int> CountAsync(Expression<Func<T, bool>> expression)
    {
        IQueryable<T> query = _dbSet.Where(expression);
        
        // Apply soft delete filter if entity implements ISoftDelete
        if (typeof(ISoftDelete).IsAssignableFrom(typeof(T)))
        {
            query = query.Where(e => !((ISoftDelete)e).IsDeleted);
        }
        
        return await query.CountAsync();
    }

    public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> expression)
    {
        IQueryable<T> query = _dbSet.Where(expression);
        
        // Apply soft delete filter if entity implements ISoftDelete
        if (typeof(ISoftDelete).IsAssignableFrom(typeof(T)))
        {
            query = query.Where(e => !((ISoftDelete)e).IsDeleted);
        }
        
        return await query.AnyAsync();
    }

    public virtual async Task<T> AddAsync(T entity)
    {
        // Set audit properties if entity implements IAuditable
        if (entity is IAuditable auditable)
        {
            auditable.CreatedAt = DateTime.UtcNow;
            // CreatedBy will be set by UnitOfWork if audit trail is enabled
        }

        var result = await _dbSet.AddAsync(entity);
        return result.Entity;
    }

    public virtual async Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities)
    {
        var entityList = entities.ToList();
        
        // Set audit properties if entities implement IAuditable
        foreach (var entity in entityList.OfType<IAuditable>())
        {
            entity.CreatedAt = DateTime.UtcNow;
            // CreatedBy will be set by UnitOfWork if audit trail is enabled
        }

        await _dbSet.AddRangeAsync(entityList);
        return entityList;
    }

    public virtual Task<T> UpdateAsync(T entity)
    {
        // Set audit properties if entity implements IAuditable
        if (entity is IAuditable auditable)
        {
            auditable.UpdatedAt = DateTime.UtcNow;
            // UpdatedBy will be set by UnitOfWork if audit trail is enabled
        }

        _dbSet.Update(entity);
        return Task.FromResult(entity);
    }

    public virtual Task<IEnumerable<T>> UpdateRangeAsync(IEnumerable<T> entities)
    {
        var entityList = entities.ToList();
        
        // Set audit properties if entities implement IAuditable
        foreach (var entity in entityList.OfType<IAuditable>())
        {
            entity.UpdatedAt = DateTime.UtcNow;
            // UpdatedBy will be set by UnitOfWork if audit trail is enabled
        }

        _dbSet.UpdateRange(entityList);
        return Task.FromResult(entityList.AsEnumerable());
    }

    public virtual async Task DeleteAsync(int id)
    {
        var entity = await GetByIdAsync(id);
        if (entity != null)
        {
            await DeleteAsync(entity);
        }
    }

    public virtual Task DeleteAsync(T entity)
    {
        _dbSet.Remove(entity);
        return Task.CompletedTask;
    }

    public virtual Task DeleteRangeAsync(IEnumerable<T> entities)
    {
        _dbSet.RemoveRange(entities);
        return Task.CompletedTask;
    }

    public virtual async Task SoftDeleteAsync(int id)
    {
        var entity = await GetByIdAsync(id);
        if (entity != null)
        {
            await SoftDeleteAsync(entity);
        }
    }

    public virtual Task SoftDeleteAsync(T entity)
    {
        if (entity is ISoftDelete softDeleteEntity)
        {
            softDeleteEntity.IsDeleted = true;
            softDeleteEntity.DeletedAt = DateTime.UtcNow;
            // DeletedBy will be set by UnitOfWork if audit trail is enabled
            
            _dbSet.Update(entity);
        }
        else
        {
            throw new InvalidOperationException($"Entity {typeof(T).Name} does not implement ISoftDelete");
        }
        
        return Task.CompletedTask;
    }

    public virtual async Task RestoreAsync(int id)
    {
        var entity = await _dbSet.FindAsync(id);
        if (entity != null)
        {
            await RestoreAsync(entity);
        }
    }

    public virtual Task RestoreAsync(T entity)
    {
        if (entity is ISoftDelete softDeleteEntity)
        {
            softDeleteEntity.IsDeleted = false;
            softDeleteEntity.DeletedAt = null;
            softDeleteEntity.DeletedBy = null;
            
            _dbSet.Update(entity);
        }
        else
        {
            throw new InvalidOperationException($"Entity {typeof(T).Name} does not implement ISoftDelete");
        }
        
        return Task.CompletedTask;
    }

    public virtual async Task<IEnumerable<TResult>> SelectAsync<TResult>(Expression<Func<T, TResult>> selector)
    {
        IQueryable<T> query = _dbSet;
        
        // Apply soft delete filter if entity implements ISoftDelete
        if (typeof(ISoftDelete).IsAssignableFrom(typeof(T)))
        {
            query = query.Where(e => !((ISoftDelete)e).IsDeleted);
        }
        
        return await query.Select(selector).ToListAsync();
    }

    public virtual async Task<IEnumerable<TResult>> SelectAsync<TResult>(
        Expression<Func<T, TResult>> selector,
        Expression<Func<T, bool>> filter)
    {
        IQueryable<T> query = _dbSet.Where(filter);
        
        // Apply soft delete filter if entity implements ISoftDelete
        if (typeof(ISoftDelete).IsAssignableFrom(typeof(T)))
        {
            query = query.Where(e => !((ISoftDelete)e).IsDeleted);
        }
        
        return await query.Select(selector).ToListAsync();
    }

    public virtual async Task<IEnumerable<T>> FromSqlRawAsync(string sql, params object[] parameters)
    {
        return await _dbSet.FromSqlRaw(sql, parameters).ToListAsync();
    }

    public virtual async Task<int> ExecuteSqlRawAsync(string sql, params object[] parameters)
    {
        return await _context.Database.ExecuteSqlRawAsync(sql, parameters);
    }

    // Helper method to create expression for finding by ID
    private Expression<Func<T, bool>> GetIdExpression(int id)
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        var property = Expression.Property(parameter, "Id");
        var constant = Expression.Constant(id);
        var equality = Expression.Equal(property, constant);
        
        return Expression.Lambda<Func<T, bool>>(equality, parameter);
    }
}
