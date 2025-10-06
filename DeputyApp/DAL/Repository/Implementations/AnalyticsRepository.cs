using DeputyApp.DAL.Repository.Abstractions;
using DeputyApp.Entities;

namespace DeputyApp.DAL.Repository.Implementations;

public class AnalyticsRepository : GenericRepository<AnalyticsEvent>, IAnalyticsRepository
{
    public AnalyticsRepository(AppDbContext db) : base(db)
    {
    }

    public async Task AddEventAsync(AnalyticsEvent analyticsEvent)
    {
        await _set.AddAsync(analyticsEvent);
    }
}