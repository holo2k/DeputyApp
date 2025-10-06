using DeputyApp.Entities;

namespace DeputyApp.DAL.Repository.Abstractions;

public interface IEventRepository : IRepository<Event>
{
    Task<IEnumerable<Event>> GetUpcomingAsync(DateTimeOffset from, DateTimeOffset to);
}