using Domain.Entities;
using Infrastructure.DAL.Repository.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DAL.Repository.Implementations;

public class UserRepository : GenericRepository<User>, IUserRepository
{
    private AppDbContext _db;

    public UserRepository(AppDbContext db) : base(db)
    {
        _db = db;
    }

    public async Task<User?> FindByEmailAsync(string email)
    {
        return await Set
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<User?> FindByIdAsync(Guid id)
    {
        return await Set
            .Include(user => user.UserRoles)
            .ThenInclude(user => user.Role)
            .Include(user => user.Documents)
            .Include(user => user.Posts)
            .Include(user => user.EventsOrganized)
            .FirstOrDefaultAsync(u => u.Id == id);
    }
}