using System.IdentityModel.Tokens.Jwt;
using System.Linq.Expressions;
using System.Security.Claims;
using Application.Services.Implementations;
using DeputyApp.DAL.UnitOfWork;
using Domain.Entities;
using Infrastructure.DAL.Repository.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;
using Shared.Encrypt;
using Shared.Middleware;
using Task = System.Threading.Tasks.Task;

namespace DeputyApp.Tests;

[TestFixture]
public class AuthServiceTests
{
    [SetUp]
    public void SetUp()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _userRepoMock = new Mock<IUserRepository>();
        _roleRepoMock = new Mock<IRoleRepository>();
        _blacklistMock = new Mock<IBlackListService>();
        _hasherMock = new Mock<IPasswordHasher>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _configurationMock = new Mock<IConfiguration>();

        _uowMock.SetupGet(x => x.Users).Returns(_userRepoMock.Object);
        _uowMock.SetupGet(x => x.Roles).Returns(_roleRepoMock.Object);

        _configurationMock.Setup(c => c["Jwt:Issuer"]).Returns("test-issuer");
        _configurationMock.Setup(c => c["Jwt:Audience"]).Returns("test-audience");
        _configurationMock.Setup(c => c["Jwt:ExpiresInMinutes"]).Returns("60");

        Environment.SetEnvironmentVariable("JWT_KEY", "supersecret_test_key_which_is_long_enough");

        _service = new AuthService(
            _uowMock.Object,
            _hasherMock.Object,
            _blacklistMock.Object,
            _configurationMock.Object,
            _httpContextAccessorMock.Object,
            _userRepoMock.Object
        );
    }

    private Mock<IUnitOfWork> _uowMock = null!;
    private Mock<IUserRepository> _userRepoMock = null!;
    private Mock<IRoleRepository> _roleRepoMock = null!;
    private Mock<IBlackListService> _blacklistMock = null!;
    private Mock<IPasswordHasher> _hasherMock = null!;
    private Mock<IHttpContextAccessor> _httpContextAccessorMock = null!;
    private Mock<IConfiguration> _configurationMock = null!;
    private AuthService _service = null!;

    [Test]
    public async Task AuthenticateAsync_UserExistsAndPasswordCorrect_ReturnsAuthResult()
    {
        var email = "user@example.com";
        var user = new User
        {
            Id = Guid.NewGuid(), Email = email, PasswordHash = "hash", Salt = "salt", UserRoles = new List<UserRole>()
        };
        _userRepoMock.Setup(r =>
                r.FindSingleAsync(It.IsAny<Expression<Func<User, bool>>>(),
                    It.IsAny<Expression<Func<User, object>>[]>()))
            .ReturnsAsync(user);
        _hasherMock.Setup(h => h.Verify(user.PasswordHash, user.Salt, "password")).Returns(true);

        var result = await _service.AuthenticateAsync(email, "password");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Token, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task AuthenticateAsync_UserNotFound_ReturnsNull()
    {
        _userRepoMock.Setup(r =>
                r.FindSingleAsync(It.IsAny<Expression<Func<User, bool>>>(),
                    It.IsAny<Expression<Func<User, object>>[]>()))
            .ReturnsAsync((User?)null);

        var res = await _service.AuthenticateAsync("noone@x", "password");
        Assert.That(res, Is.Null);
    }

    [Test]
    public async Task AuthenticateAsync_WrongPassword_ReturnsNull()
    {
        var user = new User
            { Id = Guid.NewGuid(), Email = "a@b.c", PasswordHash = "h", Salt = "s", UserRoles = new List<UserRole>() };
        _userRepoMock.Setup(r =>
                r.FindSingleAsync(It.IsAny<Expression<Func<User, bool>>>(),
                    It.IsAny<Expression<Func<User, object>>[]>()))
            .ReturnsAsync(user);
        _hasherMock.Setup(h => h.Verify(user.PasswordHash, user.Salt, "bad")).Returns(false);

        var res = await _service.AuthenticateAsync(user.Email, "bad");
        Assert.That(res, Is.Null);
    }

    [Test]
    public async Task CreateUserAsync_NewUser_CreatesUserAndRoles()
    {
        var email = "new@user";
        _uowMock.Setup(u => u.Users.ExistsAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(false);

        _roleRepoMock.Setup(r => r.ListAsync(It.IsAny<Expression<Func<Role, bool>>>(), It.IsAny<int?>(),
                It.IsAny<int?>(), It.IsAny<Expression<Func<Role, object>>[]>()))
            .ReturnsAsync(Enumerable.Empty<Role>());

        _uowMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        _hasherMock.Setup(h => h.HashPassword(It.IsAny<string>(), It.IsAny<string>())).Returns("hashed");

        var created = await _service.CreateUserAsync(email, "Full", "Job", "pwd", Guid.CreateVersion7(), "Deputy");

        Assert.That(created, Is.Not.Null);
        _uowMock.Verify(u => u.Users.AddAsync(It.Is<User>(u => u.Email == email)), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public void GenerateJwtToken_IncludesRoleClaims()
    {
        var role = new Role { Id = Guid.NewGuid(), Name = "Admin" };
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "a@b",
            FullName = "X",
            UserRoles = new List<UserRole> { new() { Role = role, RoleId = role.Id } }
        };

        var token = _service.GenerateJwtToken(user);
        Assert.That(token, Is.Not.Null.And.Not.Empty);

        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var roleClaims = parsed.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
        Assert.That(roleClaims, Does.Contain("Admin"));
    }

    [Test]
    public void GetCurrentUserId_WhenHttpContextHasClaims_ReturnsId()
    {
        var id = Guid.NewGuid();
        var claimsPrincipal =
            new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, id.ToString()) }));
        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(new DefaultHttpContext { User = claimsPrincipal });

        var res = _service.GetCurrentUserId();
        Assert.That(res, Is.EqualTo(id));
    }

    [Test]
    public async Task GetCurrentUser_ReturnsUserDto_WhenExists()
    {
        var id = Guid.NewGuid();
        var user = new User
        {
            Id = id,
            Email = "x@y",
            FullName = "Name",
            JobTitle = "Job",
            Posts = new List<Post>(),
            EventsOrganized = new List<Event>(),
            Documents = new List<Document>(),
            UserRoles = new List<UserRole> { new() { Role = new Role { Name = "Admin" } } }
        };

        _httpContextAccessorMock.Setup(a => a.HttpContext)
            .Returns(new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    { new Claim(ClaimTypes.NameIdentifier, id.ToString()) }))
            });

        _userRepoMock.Setup(r => r.FindByIdAsync(id)).ReturnsAsync(user);

        var dto = await _service.GetCurrentUserAsync();
        Assert.That(dto, Is.Not.Null);
        Assert.That(dto!.Email, Is.EqualTo(user.Email));
        Assert.That(dto.Roles, Does.Contain("Admin"));
    }

    [Test]
    public void Logout_CallsBlacklist()
    {
        var token = "tok";
        _service.Logout(token);
        _blacklistMock.Verify(b => b.AddTokenToBlacklist(token), Times.Once);
    }

    [Test]
    public async Task GetUserById_ReturnsDto_WhenExists()
    {
        var id = Guid.NewGuid();
        var user = new User
        {
            Id = id,
            Email = "t@t",
            FullName = "N",
            JobTitle = "J",
            Posts = new List<Post>(),
            EventsOrganized = new List<Event>(),
            Documents = new List<Document>(),
            UserRoles = new List<UserRole> { new() { Role = new Role { Name = "Staff" } } }
        };

        _userRepoMock.Setup(r => r.FindByIdAsync(id)).ReturnsAsync(user);

        var dto = await _service.GetUserById(id);
        Assert.That(dto, Is.Not.Null);
        Assert.That(dto!.Id, Is.EqualTo(id));
        Assert.That(dto.Email, Is.EqualTo("t@t"));
        Assert.That(dto.Roles, Does.Contain("Staff"));
    }
}