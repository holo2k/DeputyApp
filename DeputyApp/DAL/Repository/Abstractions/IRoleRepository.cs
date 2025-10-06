using DeputyApp.Entities;

namespace DeputyApp.DAL.Repository.Abstractions;

public interface IRoleRepository : IRepository<Role>
{
    Task<Role?> FindByNameAsync(string name);
}