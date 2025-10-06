using DeputyApp.BL.Dtos;
using DeputyApp.Entities;

namespace DeputyApp.BL.Services.Abstractions;

public interface IAuthService
{
    Task<AuthResult?> AuthenticateAsync(string email, string password);
    Task<User> CreateUserAsync(string email, string fullName, string password, params string[] roleNames);
    Guid? GetCurrentUserId();
    List<string> GetCurrentUserRoles();
    Task<User?> GetCurrentUser();
    Task<User?> GetUserById(Guid id);
    string GenerateJwtToken<T>(T user);
    void Logout(string token);
}