using Domain.Entities;
using Infrastructure.DAL.Repository.Abstractions;
using Task = System.Threading.Tasks.Task;

namespace Infrastructure.DAL.Repository.Implementations;

public class AnalyticsRepository : GenericRepository<AnalyticsEvent>, IAnalyticsRepository
{
    private readonly AppDbContext _db;

    public AnalyticsRepository(AppDbContext db) : base(db)
    {
        _db = db;
    }

    public async Task AddEventAsync(AnalyticsEvent analyticsEvent)
    {
        await Set.AddAsync(analyticsEvent);
    }
}