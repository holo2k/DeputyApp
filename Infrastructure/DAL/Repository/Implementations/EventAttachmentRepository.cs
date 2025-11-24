using Domain.Entities;
using Infrastructure.DAL.Repository.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DAL.Repository.Implementations
{
    public class EventAttachmentRepository : GenericRepository<EventAttachment>, IEventAttachmentRepository
    {
        public EventAttachmentRepository(AppDbContext db) : base(db) { }

        public async Task<IEnumerable<EventAttachment>> GetByEventIdAsync(Guid eventId)
        {
            return await Set.AsNoTracking()
                .Where(a => a.EventId == eventId)
                .Include(a => a.Document)
                .Include(a => a.UploadedBy)
                .ToListAsync();
        }
    }
}
