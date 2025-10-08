using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DAL.Repository;

public class GenericRepository<T> : IRepository<T> where T : class
{
    private readonly AppDbContext _db;
    protected readonly DbSet<T> Set;

    public GenericRepository(AppDbContext db)
    {
        _db = db;
        Set = db.Set<T>();
    }


    public virtual async Task AddAsync(T entity)
    {
        await Set.AddAsync(entity);
    }


    public virtual async Task AddRangeAsync(IEnumerable<T> entities)
    {
        await Set.AddRangeAsync(entities);
    }


    public virtual void Delete(T entity)
    {
        Set.Remove(entity);
    }


    public virtual void DeleteRange(IEnumerable<T> entities)
    {
        Set.RemoveRange(entities);
    }


    public virtual async Task<T?> GetByIdAsync(Guid id)
    {
        return await Set.FindAsync(id);
    }


    public virtual async Task<T?> GetByIdAsync(Guid id, params Expression<Func<T, object>>[] includes)
    {
        if (includes == null || includes.Length == 0) return await GetByIdAsync(id);
        var query = ApplyIncludes(Set.AsNoTracking(), includes);
        return await query.FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id);
    }


    public virtual async Task<IEnumerable<T>> ListAsync(Expression<Func<T, bool>>? predicate = null, int? skip = null,
        int? take = null, params Expression<Func<T, object>>[] includes)
    {
        var q = Set.AsNoTracking();
        q = ApplyIncludes(q, includes);
        if (predicate != null) q = q.Where(predicate);
        if (skip.HasValue) q = q.Skip(skip.Value);
        if (take.HasValue) q = q.Take(take.Value);
        return await q.ToListAsync();
    }


    public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
    {
        if (predicate == null) return await Set.CountAsync();
        return await Set.CountAsync(predicate);
    }


    public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
    {
        return await Set.AnyAsync(predicate);
    }


    public virtual void Update(T entity)
    {
        Set.Update(entity);
    }


    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate,
        params Expression<Func<T, object>>[] includes)
    {
        var q = Set.AsNoTracking();
        q = ApplyIncludes(q, includes);
        q = q.Where(predicate);
        return await q.ToListAsync();
    }


    public virtual async Task<T?> FindSingleAsync(Expression<Func<T, bool>> predicate,
        params Expression<Func<T, object>>[] includes)
    {
        var q = Set.AsNoTracking();
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