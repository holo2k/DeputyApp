using Domain.Entities;

namespace Application.Services.Abstractions;

public interface IEventService
{
    Task<Event> CreateAsync(Event e);
    Task<IEnumerable<Event>> GetUpcomingAsync(DateTimeOffset from, DateTimeOffset to, int take = 50);
    Task DeleteAsync(Guid id);
    Task<IEnumerable<Event>> GetMyUpcomingAsync(Guid userId, DateTimeOffset from, DateTimeOffset to);
}