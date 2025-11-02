using Application.Dtos;
using Domain.Entities;

namespace Application.Services.Abstractions;

public interface IAuthService
{
    Task<AuthResult?> AuthenticateAsync(string email, string password);

    Task<User> CreateUserAsync(string email, string fullName, string jobTitle, string password, Guid? deputyId = null,
        params string[] roleNames);

    Guid GetCurrentUserId();
    List<string> GetCurrentUserRoles();
    Task<UserDto?> GetCurrentUserAsync();
    Task<UserDto?> GetUserById(Guid id);
    string GenerateJwtToken(User user);

    void Logout(string token);
}