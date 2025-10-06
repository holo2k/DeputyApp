using DeputyApp.DAL.Repository.Abstractions;
using DeputyApp.Entities;
using Microsoft.EntityFrameworkCore;

namespace DeputyApp.DAL.Repository.Implementations;

public class RoleRepository : GenericRepository<Role>, IRoleRepository
{
    public RoleRepository(AppDbContext db) : base(db)
    {
    }

    public async Task<Role?> FindByNameAsync(string name)
    {
        return await _set.AsNoTracking().FirstOrDefaultAsync(r => r.Name == name);
    }
}