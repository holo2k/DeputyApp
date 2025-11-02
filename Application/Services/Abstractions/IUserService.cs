using Application.Dtos;
using Domain.Entities;

namespace Application.Services.Abstractions;

public interface IUserService
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByEmailAsync(string email);
    Task AssignRoleAsync(Guid userId, string roleName);
    Task<User?> UpdateUser(UpdateUserRequest request);
    Task<IEnumerable<User>> ListAsync(int skip = 0, int take = 50);
}