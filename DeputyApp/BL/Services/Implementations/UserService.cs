using DeputyApp.BL.Services.Abstractions;
using DeputyApp.DAL.UnitOfWork;
using DeputyApp.Entities;

namespace DeputyApp.BL.Services.Implementations;

public class UserService : IUserService
{
    private readonly IUnitOfWork _uow;

    public UserService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _uow.Users.GetByIdAsync(id);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _uow.Users.FindSingleAsync(u => u.Email == email);
    }


    public async Task AssignRoleAsync(Guid userId, string roleName)
    {
        var user = await _uow.Users.GetByIdAsync(userId);
        if (user == null) throw new KeyNotFoundException("User not found");
        var role = (await _uow.Roles.ListAsync(r => r.Name == roleName)).FirstOrDefault();
        if (role == null)
        {
            role = new Role { Id = Guid.NewGuid(), Name = roleName };
            await _uow.Roles.AddAsync(role);
        }

        if (!user.UserRoles.Any(ur => ur.RoleId == role.Id))
            user.UserRoles.Add(new UserRole { RoleId = role.Id, UserId = user.Id });
        _uow.Users.Update(user);
        await _uow.SaveChangesAsync();
    }


    public async Task<IEnumerable<User>> ListAsync(int skip = 0, int take = 50)
    {
        return await _uow.Users.ListAsync(null, skip, take);
    }
}