using Domain.Entities;

namespace Infrastructure.DAL.Repository.Abstractions;

public interface IEventRepository : IRepository<Event>
{
    Task<IEnumerable<Event>> GetUpcomingAsync(DateTimeOffset from, DateTimeOffset to);
    Task<IEnumerable<Event>> GetMyUpcomingAsync(DateTimeOffset from, DateTimeOffset to, Guid userId);
}