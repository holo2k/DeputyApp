using Domain.Entities;
using Infrastructure.DAL.Repository.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DAL.Repository.Implementations;

public class FeedbackRepository : GenericRepository<Feedback>, IFeedbackRepository
{
    private AppDbContext _db;

    public FeedbackRepository(AppDbContext db) : base(db)
    {
        _db = db;
    }


    public async Task<IEnumerable<Feedback>> RecentAsync(int days = 30)
    {
        var since = DateTimeOffset.UtcNow.AddDays(-days);
        return await Set.AsNoTracking().Where(f => f.CreatedAt >= since).ToListAsync();
    }
}