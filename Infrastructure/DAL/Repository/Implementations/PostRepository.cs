using Domain.Entities;
using Infrastructure.DAL.Repository.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DAL.Repository.Implementations;

public class PostRepository(AppDbContext db) : GenericRepository<Post>(db), IPostRepository
{
    public async Task<IEnumerable<Post>> GetPublishedAsync(int limit = 50)
    {
        return await _set.AsNoTracking()
            .Where(p => p.PublishedAt != null)
            .OrderByDescending(p => p.PublishedAt)
            .Take(limit)
            .ToListAsync();
    }
}