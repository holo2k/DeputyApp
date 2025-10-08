using Domain.Entities;
using Infrastructure.DAL.Repository.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DAL.Repository.Implementations;

public class PostRepository : GenericRepository<Post>, IPostRepository
{
    private AppDbContext _db;

    public PostRepository(AppDbContext db) : base(db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Post>> GetPublishedAsync(int limit = 50)
    {
        return await Set.AsNoTracking()
            .Where(p => p.PublishedAt != null)
            .OrderByDescending(p => p.PublishedAt)
            .Take(limit)
            .ToListAsync();
    }
}