using DeputyApp.Entities;

namespace DeputyApp.DAL.Repository.Abstractions;

public interface IUserRepository : IRepository<User>
{
    Task<User?> FindByEmailAsync(string email);
}