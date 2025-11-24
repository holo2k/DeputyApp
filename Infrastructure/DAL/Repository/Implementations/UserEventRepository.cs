using Domain.Entities;
using Infrastructure.DAL.Repository.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DAL.Repository.Implementations
{
    public class UserEventRepository : GenericRepository<UserEvent>, IUserEventRepository
    {
        public UserEventRepository(AppDbContext db) : base(db) { }

        public async Task<UserEvent?> GetAsync(Guid userId, Guid eventId)
        {
            return await Set.AsNoTracking()
                .Include(ue => ue.User)
                .Include(ue => ue.ExcuseDocument)
                .FirstOrDefaultAsync(ue => ue.UserId == userId && ue.EventId == eventId);
        }

        public async Task<IEnumerable<UserEvent>> GetByEventIdAsync(Guid eventId)
        {
            return await Set.AsNoTracking()
                .Where(ue => ue.EventId == eventId)
                .Include(ue => ue.User)
                .Include(ue => ue.ExcuseDocument)
                .ToListAsync();
        }

        public async Task<IEnumerable<UserEvent>> GetByUserIdAsync(Guid userId)
        {
            return await Set.AsNoTracking()
                .Where(ue => ue.UserId == userId)
                .Include(ue => ue.Event)
                .Include(ue => ue.ExcuseDocument)
                .ToListAsync();
        }
    }
}
