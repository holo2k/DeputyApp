using Domain.Entities;

namespace Infrastructure.DAL.Repository.Abstractions
{
    public interface IEventAttachmentRepository : IRepository<EventAttachment>
    {
        Task<IEnumerable<EventAttachment>> GetByEventIdAsync(Guid eventId);
    }
}
