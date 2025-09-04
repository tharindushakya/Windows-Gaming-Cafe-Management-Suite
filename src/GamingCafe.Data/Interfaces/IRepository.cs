using System.Linq.Expressions;

namespace GamingCafe.Data.Interfaces;

public interface IRepository<T> where T : class
{
    // Basic CRUD operations
    Task<T?> GetByIdAsync(int id);
    Task<T?> GetByIdAsync(int id, params Expression<Func<T, object>>[] includes);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> GetAllAsync(params Expression<Func<T, object>>[] includes);
    
    // Query operations
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> expression);
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> expression, params Expression<Func<T, object>>[] includes);
    Task<T?> FindFirstAsync(Expression<Func<T, bool>> expression);
    Task<T?> FindFirstAsync(Expression<Func<T, bool>> expression, params Expression<Func<T, object>>[] includes);
    
    // Pagination
    Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(
        int page, 
        int pageSize, 
        Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        params Expression<Func<T, object>>[] includes);
    
    // Count operations
    Task<int> CountAsync();
    Task<int> CountAsync(Expression<Func<T, bool>> expression);
    Task<bool> ExistsAsync(Expression<Func<T, bool>> expression);
    
    // Modification operations
    Task<T> AddAsync(T entity);
    Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities);
    Task<T> UpdateAsync(T entity);
    Task<IEnumerable<T>> UpdateRangeAsync(IEnumerable<T> entities);
    Task DeleteAsync(int id);
    Task DeleteAsync(T entity);
    Task DeleteRangeAsync(IEnumerable<T> entities);
    
    // Soft delete operations
    Task SoftDeleteAsync(int id);
    Task SoftDeleteAsync(T entity);
    Task RestoreAsync(int id);
    Task RestoreAsync(T entity);
    
    // Advanced queries
    Task<IEnumerable<TResult>> SelectAsync<TResult>(Expression<Func<T, TResult>> selector);
    Task<IEnumerable<TResult>> SelectAsync<TResult>(
        Expression<Func<T, TResult>> selector,
        Expression<Func<T, bool>> filter);
    
    // Raw SQL support
    Task<IEnumerable<T>> FromSqlRawAsync(string sql, params object[] parameters);
    Task<int> ExecuteSqlRawAsync(string sql, params object[] parameters);
}
