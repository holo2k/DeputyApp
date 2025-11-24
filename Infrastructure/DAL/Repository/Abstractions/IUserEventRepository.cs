using Domain.Entities;

namespace Infrastructure.DAL.Repository.Abstractions
{
    public interface IUserEventRepository : IRepository<UserEvent>
    {
        Task<UserEvent?> GetAsync(Guid userId, Guid eventId);
        Task<IEnumerable<UserEvent>> GetByEventIdAsync(Guid eventId);
        Task<IEnumerable<UserEvent>> GetByUserIdAsync(Guid userId);
    }
}
