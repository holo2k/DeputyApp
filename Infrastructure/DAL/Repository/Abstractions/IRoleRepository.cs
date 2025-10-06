using Domain.Entities;

namespace Infrastructure.DAL.Repository.Abstractions;

public interface IRoleRepository : IRepository<Role>
{
    Task<Role?> FindByNameAsync(string name);
}