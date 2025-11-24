using System.Collections.Generic;
using Domain.Entities;
using Infrastructure.DAL.Repository.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DAL.Repository.Implementations;

public class EventRepository : GenericRepository<Event>, IEventRepository
{
    private AppDbContext _db;

    public EventRepository(AppDbContext db) : base(db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Event>> GetUpcomingAsync(DateTimeOffset from, DateTimeOffset to)
    {
        return await Set.AsNoTracking().Where(e => e.StartAt >= from && e.StartAt <= to && e.IsPublic)
            .OrderBy(e => e.StartAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Event>> GetMyUpcomingAsync(DateTimeOffset from, DateTimeOffset to, Guid userId)
    {
        return await Set.AsNoTracking()
            .Where(e => e.StartAt >= from && e.StartAt <= to && !e.IsPublic && e.OrganizerId == userId)
            .OrderBy(e => e.StartAt)
            .ToListAsync();
    }

    public async Task<Event?> GetWithDetailsAsync(Guid id)
    {
        return await Set.Include(e => e.Attachments).ThenInclude(a => a.Document)
                    .Include(e => e.Participants).ThenInclude(p => p.User)
                    .FirstOrDefaultAsync(e => e.Id == id);
    }
}