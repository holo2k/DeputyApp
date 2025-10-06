using Domain.Entities;
using Infrastructure.DAL.Repository.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DAL.Repository.Implementations;

public class EventRepository(AppDbContext db) : GenericRepository<Event>(db), IEventRepository
{
    public async Task<IEnumerable<Event>> GetUpcomingAsync(DateTimeOffset from, DateTimeOffset to)
    {
        return await _set.AsNoTracking().Where(e => e.StartAt >= from && e.StartAt <= to).OrderBy(e => e.StartAt)
            .ToListAsync();
    }
}