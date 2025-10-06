using Application.Services.Abstractions;
using DeputyApp.DAL.UnitOfWork;
using Domain.Entities;

namespace Application.Services.Implementations;

public class EventService : IEventService
{
    private readonly IUnitOfWork _uow;

    public EventService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Event> CreateAsync(Event e)
    {
        e.Id = Guid.NewGuid();
        e.CreatedAt = DateTimeOffset.UtcNow;
        await _uow.Events.AddAsync(e);
        await _uow.SaveChangesAsync();
        return e;
    }


    public async Task<IEnumerable<Event>> GetUpcomingAsync(DateTimeOffset from, DateTimeOffset to, int take = 50)
    {
        return await _uow.Events.GetUpcomingAsync(from, to);
    }


    public async Task DeleteAsync(Guid id)
    {
        var e = await _uow.Events.GetByIdAsync(id);
        if (e == null) return;
        _uow.Events.Delete(e);
        await _uow.SaveChangesAsync();
    }
}