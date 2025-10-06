using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace DeputyApp.DAL.Repository;

public class GenericRepository<T> : IRepository<T> where T : class
{
    protected readonly AppDbContext _db;
    protected readonly DbSet<T> _set;

    public GenericRepository(AppDbContext db)
    {
        _db = db;
        _set = db.Set<T>();
    }


    public virtual async Task AddAsync(T entity)
    {
        await _set.AddAsync(entity);
    }


    public virtual async Task AddRangeAsync(IEnumerable<T> entities)
    {
        await _set.AddRangeAsync(entities);
    }


    public virtual void Delete(T entity)
    {
        _set.Remove(entity);
    }


    public virtual void DeleteRange(IEnumerable<T> entities)
    {
        _set.RemoveRange(entities);
    }


    public virtual async Task<T?> GetByIdAsync(Guid id)
    {
        return await _set.FindAsync(id);
    }


    public virtual async Task<T?> GetByIdAsync(Guid id, params Expression<Func<T, object>>[] includes)
    {
        if (includes == null || includes.Length == 0) return await GetByIdAsync(id);
        var query = ApplyIncludes(_set.AsNoTracking(), includes);
        return await query.FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id);
    }


    public virtual async Task<IEnumerable<T>> ListAsync(Expression<Func<T, bool>>? predicate = null, int? skip = null,
        int? take = null, params Expression<Func<T, object>>[] includes)
    {
        var q = _set.AsNoTracking();
        q = ApplyIncludes(q, includes);
        if (predicate != null) q = q.Where(predicate);
        if (skip.HasValue) q = q.Skip(skip.Value);
        if (take.HasValue) q = q.Take(take.Value);
        return await q.ToListAsync();
    }


    public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
    {
        if (predicate == null) return await _set.CountAsync();
        return await _set.CountAsync(predicate);
    }


    public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
    {
        return await _set.AnyAsync(predicate);
    }


    public virtual void Update(T entity)
    {
        _set.Update(entity);
    }


    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate,
        params Expression<Func<T, object>>[] includes)
    {
        var q = _set.AsNoTracking();
        q = ApplyIncludes(q, includes);
        q = q.Where(predicate);
        return await q.ToListAsync();
    }


    public virtual async Task<T?> FindSingleAsync(Expression<Func<T, bool>> predicate,
        params Expression<Func<T, object>>[] includes)
    {
        var q = _set.AsNoTracking();
        q = ApplyIncludes(q, includes);
        return await q.FirstOrDefaultAsync(predicate);
    }


    protected IQueryable<T> ApplyIncludes(IQueryable<T> query, params Expression<Func<T, object>>[]? includes)
    {
        if (includes == null || includes.Length == 0) return query;
        foreach (var inc in includes) query = query.Include(inc);
        return query;
    }
}