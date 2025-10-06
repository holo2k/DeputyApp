using System.Linq.Expressions;

namespace Infrastructure.DAL.Repository;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id);
    Task<T?> GetByIdAsync(Guid id, params Expression<Func<T, object>>[] includes);


    Task<IEnumerable<T>> ListAsync(Expression<Func<T, bool>>? predicate = null, int? skip = null, int? take = null,
        params Expression<Func<T, object>>[] includes);


    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);


    Task AddAsync(T entity);
    Task AddRangeAsync(IEnumerable<T> entities);


    void Update(T entity);
    void Delete(T entity);
    void DeleteRange(IEnumerable<T> entities);


    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes);
    Task<T?> FindSingleAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes);
}