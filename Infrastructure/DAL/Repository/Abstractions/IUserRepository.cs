using Domain.Entities;

namespace Infrastructure.DAL.Repository.Abstractions;

public interface IUserRepository : IRepository<User>
{
    Task<User?> FindByEmailAsync(string email);
    Task<User?> FindByIdAsync(Guid id);
}