using DeputyApp.DAL.Repository.Abstractions;
using DeputyApp.Entities;
using Microsoft.EntityFrameworkCore;

namespace DeputyApp.DAL.Repository.Implementations;

public class FeedbackRepository : GenericRepository<Feedback>, IFeedbackRepository
{
    public FeedbackRepository(AppDbContext db) : base(db)
    {
    }

    public async Task<IEnumerable<Feedback>> RecentAsync(int days = 30)
    {
        var since = DateTimeOffset.UtcNow.AddDays(-days);
        return await _set.AsNoTracking().Where(f => f.CreatedAt >= since).ToListAsync();
    }
}