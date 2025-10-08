using Application.Services.Abstractions;
using DeputyApp.DAL.UnitOfWork;
using Domain.Entities;

namespace Application.Services.Implementations;

public class EventService : IEventService
{
    private readonly IUnitOfWork _uow;
    public event Func<Event, Task>? EventCreatedOrUpdated;

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

        if (EventCreatedOrUpdated != null)
            await EventCreatedOrUpdated.Invoke(e);

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

    public async Task<IEnumerable<Event>> GetMyUpcomingAsync(Guid userId, DateTimeOffset from, DateTimeOffset to)
    {
        var events = await _uow.Events.GetMyUpcomingAsync(from, to, userId);
        return events;
    }
}