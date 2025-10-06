using Application.Services.Abstractions;
using DeputyApp.DAL.UnitOfWork;
using Domain.Entities;

namespace Application.Services.Implementations;

public class UserService(IUnitOfWork uow) : IUserService
{
    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await uow.Users.GetByIdAsync(id);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await uow.Users.FindSingleAsync(u => u.Email == email);
    }


    public async Task AssignRoleAsync(Guid userId, string roleName)
    {
        var user = await uow.Users.GetByIdAsync(userId);
        if (user == null) throw new KeyNotFoundException("User not found");
        var role = (await uow.Roles.ListAsync(r => r.Name == roleName)).FirstOrDefault();
        if (role == null)
        {
            role = new Role { Id = Guid.NewGuid(), Name = roleName };
            await uow.Roles.AddAsync(role);
        }

        if (user.UserRoles.All(ur => ur.RoleId != role.Id))
            user.UserRoles.Add(new UserRole { RoleId = role.Id, UserId = user.Id });
        uow.Users.Update(user);
        await uow.SaveChangesAsync();
    }


    public async Task<IEnumerable<User>> ListAsync(int skip = 0, int take = 50)
    {
        return await uow.Users.ListAsync(null, skip, take);
    }
}