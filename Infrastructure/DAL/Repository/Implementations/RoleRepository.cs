using Domain.Entities;
using Infrastructure.DAL.Repository.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DAL.Repository.Implementations;

public class RoleRepository : GenericRepository<Role>, IRoleRepository
{
    private readonly AppDbContext _db;

    public RoleRepository(AppDbContext db) : base(db)
    {
        _db = db;
    }

    public async Task<Role?> FindByNameAsync(string name)
    {
        return await Set.AsNoTracking().FirstOrDefaultAsync(r => r.Name == name);
    }
}