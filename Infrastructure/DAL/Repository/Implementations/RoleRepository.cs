using Domain.Entities;
using Infrastructure.DAL.Repository.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DAL.Repository.Implementations;

public class RoleRepository(AppDbContext db) : GenericRepository<Role>(db), IRoleRepository
{
    public async Task<Role?> FindByNameAsync(string name)
    {
        return await _set.AsNoTracking().FirstOrDefaultAsync(r => r.Name == name);
    }
}