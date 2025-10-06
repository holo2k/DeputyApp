using Application.Dtos;
using Domain.Entities;

namespace Application.Services.Abstractions;

public interface IAuthService
{
    Task<AuthResult?> AuthenticateAsync(string email, string password);

    Task<User> CreateUserAsync(string email, string fullName, string jobTitle, string password,
        params string[] roleNames);

    Guid GetCurrentUserId();
    List<string> GetCurrentUserRoles();
    Task<User?> GetCurrentUser();
    Task<User?> GetUserById(Guid id);
    string GenerateJwtToken(User user);

    void Logout(string token);
}