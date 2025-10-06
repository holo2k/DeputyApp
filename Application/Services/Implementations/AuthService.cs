using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Application.Dtos;
using Application.Services.Abstractions;
using DeputyApp.DAL.UnitOfWork;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Shared.Encrypt;
using Shared.Middleware;

namespace Application.Services.Implementations;

public class AuthService(
    IUnitOfWork uow,
    IPasswordHasher hasher,
    IBlackListService blacklistService,
    IConfiguration configuration,
    IHttpContextAccessor httpContextAccessor)
    : IAuthService
{
    public async Task<AuthResult?> AuthenticateAsync(string email, string password)
    {
        var user = await uow.Users.FindSingleAsync(u => u.Email == email);
        if (user == null) return null;
        if (!hasher.Verify(user.PasswordHash, user.Salt, password)) return null;

        var token = GenerateJwtToken(user);

        return new AuthResult(token, user);
    }


    public async Task<User> CreateUserAsync(string email, string fullName, string jobTitle, string password,
        params string[] roleNames)
    {
        if (await uow.Users.ExistsAsync(u => u.Email == email)) throw new InvalidOperationException("User exists");
        var salt = Guid.NewGuid().ToString();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            FullName = fullName,
            JobTitle = jobTitle,
            PasswordHash = hasher.HashPassword(password, salt),
            Salt = salt,
            CreatedAt = DateTimeOffset.UtcNow
        };
        await uow.Users.AddAsync(user);


        foreach (var rn in roleNames)
        {
            var role = (await uow.Roles.ListAsync(r => r.Name == rn)).FirstOrDefault();
            if (role == null)
            {
                role = new Role { Id = Guid.NewGuid(), Name = rn };
                await uow.Roles.AddAsync(role);
            }

            user.UserRoles.Add(new UserRole { RoleId = role.Id, UserId = user.Id });
        }


        await uow.SaveChangesAsync();
        return user;
    }

    public async Task<User?> GetCurrentUser()
    {
        var id = GetCurrentUserId();

        if (id != Guid.Empty) return await uow.Users.GetByIdAsync(id!);

        return null;
    }

    public Guid GetCurrentUserId()
    {
        var claimsIdentity = httpContextAccessor.HttpContext?.User.Identity as ClaimsIdentity;
        var id = Guid.Parse(claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
        return id;
    }


    public List<string> GetCurrentUserRoles()
    {
        var claimsIdentity = httpContextAccessor.HttpContext?.User.Identity as ClaimsIdentity;
        return claimsIdentity?.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToList() ?? new List<string>();
    }

    public void Logout(string token)
    {
        blacklistService.AddTokenToBlacklist(token);
    }

    public async Task<User?> GetUserById(Guid id)
    {
        if (id != Guid.Empty) return await uow.Users.GetByIdAsync(id!);

        return null;
    }

    public string GenerateJwtToken(User user)
    {
        var userRoles = new List<string>();

        userRoles = user.UserRoles.Select(x => x.Role.Name).ToList();

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user?.Id.ToString() ?? string.Empty),
            new(ClaimTypes.NameIdentifier, user?.Id.ToString() ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };


        claims.AddRange(userRoles.Select(role => new Claim(ClaimTypes.Role, role)));
        var securityKey =
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("JWT_KEY")!));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            configuration["Jwt:Issuer"],
            configuration["Jwt:Audience"],
            claims,
            expires: DateTime.Now.AddMinutes(Convert.ToDouble(configuration["Jwt:ExpiresInMinutes"])),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}