using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Application.Dtos;
using Application.Services.Abstractions;
using DeputyApp.DAL.UnitOfWork;
using Domain.Constants;
using Domain.Entities;
using Infrastructure.DAL.Repository.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Shared.Encrypt;
using Shared.Middleware;

namespace Application.Services.Implementations;

public class AuthService : IAuthService
{
    private readonly IBlackListService _blacklistService;
    private readonly IConfiguration _configuration;
    private readonly IPasswordHasher _hasher;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUnitOfWork _uow;
    private readonly IUserRepository _userRepository;

    public AuthService(IUnitOfWork uow, IPasswordHasher hasher, IBlackListService blacklistService,
        IConfiguration configuration, IHttpContextAccessor httpContextAccessor, IUserRepository userRepository)
    {
        _uow = uow;
        _hasher = hasher;
        _blacklistService = blacklistService;
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
        _userRepository = userRepository;
    }

    public async Task<AuthResult?> AuthenticateAsync(string email, string password)
    {
        var user = await _userRepository.FindByEmailAsync(email);
        if (user == null) return null;
        if (!_hasher.Verify(user.PasswordHash, user.Salt, password)) return null;

        var token = GenerateJwtToken(user);

        return new AuthResult(token, user);
    }


    public async Task<User> CreateUserAsync(string email, string fullName, string jobTitle, string password,
        Guid? deputyId,
        params string[] roleNames)
    {
        if (await _uow.Users.ExistsAsync(u => u.Email == email)) throw new InvalidOperationException("User exists");
        var salt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        var user = new User
        {
            Id = Guid.CreateVersion7(),
            Email = email,
            FullName = fullName,
            JobTitle = jobTitle,
            PasswordHash = _hasher.HashPassword(password, salt),
            Salt = salt,
            DeputyId = deputyId,
            CreatedAt = DateTimeOffset.UtcNow
        };
        await _uow.Users.AddAsync(user);

        var roles = new List<UserRole>();

        foreach (var rn in roleNames)
        {
            var role = (await _uow.Roles.ListAsync(r => r.Name == rn)).FirstOrDefault();

            if (role == null)
            {
                role = new Role { Id = Guid.CreateVersion7(), Name = rn };
                await _uow.Roles.AddAsync(role);
            }

            user.UserRoles.Add(new UserRole { RoleId = role.Id, UserId = user.Id });
            roles.Add(new UserRole { Role = role });
        }

        await _uow.SaveChangesAsync();

        user.UserRoles = roles;

        return user;
    }

    public async Task<UserDto?> GetCurrentUserAsync()
    {
        var id = GetCurrentUserId();

        if (id != Guid.Empty)
        {
            var user = await _userRepository.FindByIdAsync(id);
            return new UserDto(id, user.Email, user.FullName, user.JobTitle, user.Posts, user.EventsOrganized,
                user.Documents,
                user.Tasks,
                user.Deputy,
                user.UserRoles.Select(r => r.Role.Name).ToArray());
        }

        return null;
    }

    public Guid GetCurrentUserId()
    {
        var claimsIdentity = _httpContextAccessor.HttpContext?.User.Identity as ClaimsIdentity;
        var id = Guid.Parse(claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
        return id;
    }


    public List<string> GetCurrentUserRoles()
    {
        var claimsIdentity = _httpContextAccessor.HttpContext?.User.Identity as ClaimsIdentity;
        return claimsIdentity?.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToList() ?? new List<string>();
    }

    public void Logout(string token)
    {
        _blacklistService.AddTokenToBlacklist(token);
    }

    public async Task<UserDto?> GetUserById(Guid id)
    {
        if (id != Guid.Empty)
        {
            var user = await _userRepository.FindByIdAsync(id);
            return new UserDto(id, user.Email, user.FullName, user.JobTitle, user.Posts, user.EventsOrganized,
                user.Documents,
                user.Tasks,
                user.Deputy,
                user.UserRoles.Select(r => r.Role.Name).ToArray());
        }

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
            _configuration["Jwt:Issuer"],
            _configuration["Jwt:Audience"],
            claims,
            expires: DateTime.Now.AddMinutes(Convert.ToDouble(_configuration["Jwt:ExpiresInMinutes"])),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}