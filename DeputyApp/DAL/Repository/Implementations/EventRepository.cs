using DeputyApp.DAL.Repository.Abstractions;
using DeputyApp.Entities;
using Microsoft.EntityFrameworkCore;

namespace DeputyApp.DAL.Repository.Implementations;

public class EventRepository : GenericRepository<Event>, IEventRepository
{
    public EventRepository(AppDbContext db) : base(db)
    {
    }

    public async Task<IEnumerable<Event>> GetUpcomingAsync(DateTimeOffset from, DateTimeOffset to)
    {
        return await _set.AsNoTracking().Where(e => e.StartAt >= from && e.StartAt <= to).OrderBy(e => e.StartAt)
            .ToListAsync();
    }
}