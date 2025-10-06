using DeputyApp.Entities;

namespace DeputyApp.BL.Services.Abstractions;

public interface IUserService
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByEmailAsync(string email);
    Task AssignRoleAsync(Guid userId, string roleName);
    Task<IEnumerable<User>> ListAsync(int skip = 0, int take = 50);
}