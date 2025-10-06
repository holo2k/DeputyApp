using DeputyApp.DAL.Repository.Abstractions;
using DeputyApp.Entities;
using Microsoft.EntityFrameworkCore;

namespace DeputyApp.DAL.Repository.Implementations;

public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(AppDbContext db) : base(db)
    {
    }

    public async Task<User?> FindByEmailAsync(string email)
    {
        return await _set.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
    }
}