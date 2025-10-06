using DeputyApp.DAL.Repository.Abstractions;
using DeputyApp.Entities;
using Microsoft.EntityFrameworkCore;

namespace DeputyApp.DAL.Repository.Implementations;

public class PostRepository : GenericRepository<Post>, IPostRepository
{
    public PostRepository(AppDbContext db) : base(db)
    {
    }

    public async Task<IEnumerable<Post>> GetPublishedAsync(int limit = 50)
    {
        return await _set.AsNoTracking()
            .Where(p => p.PublishedAt != null)
            .OrderByDescending(p => p.PublishedAt)
            .Take(limit)
            .ToListAsync();
    }
}