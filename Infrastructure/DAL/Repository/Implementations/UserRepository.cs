using Domain.Entities;
using Infrastructure.DAL.Repository.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DAL.Repository.Implementations;

public class UserRepository(AppDbContext db) : GenericRepository<User>(db), IUserRepository
{
    public async Task<User?> FindByEmailAsync(string email)
    {
        return await _set.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<User?> FindByIdAsync(Guid id)
    {
        return await _set
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(u => u.Id == id);
    }
}