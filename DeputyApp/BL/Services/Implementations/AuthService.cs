using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DeputyApp.BL.Encrypt;
using DeputyApp.BL.Services.Abstractions;
using DeputyApp.Controllers.Dtos;
using DeputyApp.DAL.UnitOfWork;
using DeputyApp.Entities;
using DeputyApp.Middleware;
using Microsoft.IdentityModel.Tokens;

namespace DeputyApp.BL.Services.Implementations;

public class AuthService : IAuthService
{
    private readonly IPasswordHasher _hasher;
    private readonly IUnitOfWork _uow;
    private readonly IBlackListService blacklistService;
    private readonly IConfiguration configuration;
    private readonly IHttpContextAccessor httpContextAccessor;


    public AuthService(IUnitOfWork uow, IPasswordHasher hasher, IBlackListService blacklistService,
        IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
    {
        _uow = uow;
        _hasher = hasher;
        this.blacklistService = blacklistService;
        this.configuration = configuration;
        this.httpContextAccessor = httpContextAccessor;
    }

    public async Task<AuthResult?> AuthenticateAsync(string email, string password)
    {
        var user = await _uow.Users.FindSingleAsync(u => u.Email == email);
        if (user == null) return null;
        if (!_hasher.Verify(user.PasswordHash, user.Salt, password)) return null;

        var token = GenerateJwtToken(user);

        return new AuthResult(token, user);
    }


    public async Task<User> CreateUserAsync(string email, string fullName, string password, params string[] roleNames)
    {
        if (await _uow.Users.ExistsAsync(u => u.Email == email)) throw new InvalidOperationException("User exists");
        var salt = Guid.NewGuid().ToString();
        var user = new User
        {
            Id = Guid.NewGuid(), Email = email, FullName = fullName,
            PasswordHash = _hasher.HashPassword(password, salt), Salt = salt,
            CreatedAt = DateTimeOffset.UtcNow
        };
        await _uow.Users.AddAsync(user);


        foreach (var rn in roleNames)
        {
            var role = (await _uow.Roles.ListAsync(r => r.Name == rn)).FirstOrDefault();
            if (role == null)
            {
                role = new Role { Id = Guid.NewGuid(), Name = rn };
                await _uow.Roles.AddAsync(role);
            }

            user.UserRoles.Add(new UserRole { RoleId = role.Id, UserId = user.Id });
        }


        await _uow.SaveChangesAsync();
        return user;
    }

    public async Task<User?> GetCurrentUser()
    {
        var id = GetCurrentUserId();

        if (id != Guid.Empty) return await _uow.Users.GetByIdAsync(id!);

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

    public string GenerateJwtToken<T>(T user)
    {
        var userRoles = new List<string>();

        userRoles.Add("USER");

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, (user as User)?.Id.ToString() ?? string.Empty),
            new(ClaimTypes.NameIdentifier, (user as User)?.Id.ToString() ?? string.Empty),
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

    public void Logout(string token)
    {
        blacklistService.AddTokenToBlacklist(token);
    }

    public async Task<User?> GetUserById(Guid id)
    {
        if (id != Guid.Empty) return await _uow.Users.GetByIdAsync(id!);

        return null;
    }
}