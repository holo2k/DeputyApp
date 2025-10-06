using Domain.Entities;
using Infrastructure.DAL.Repository.Abstractions;

namespace Infrastructure.DAL.Repository.Implementations;

public class AnalyticsRepository(AppDbContext db) : GenericRepository<AnalyticsEvent>(db), IAnalyticsRepository
{
    public async Task AddEventAsync(AnalyticsEvent analyticsEvent)
    {
        await _set.AddAsync(analyticsEvent);
    }
}