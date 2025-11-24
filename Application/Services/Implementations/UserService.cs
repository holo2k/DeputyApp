using Application.Dtos;
using Application.Services.Abstractions;
using DeputyApp.DAL.UnitOfWork;
using Domain.Constants;
using Domain.Entities;
using Task = System.Threading.Tasks.Task;

namespace Application.Services.Implementations;

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

        if (user.UserRoles.All(ur => ur.RoleId != role.Id))
            user.UserRoles.Add(new UserRole { RoleId = role.Id, UserId = user.Id });
        _uow.Users.Update(user);
        await _uow.SaveChangesAsync();
    }

    public async Task<User?> UpdateUser(UpdateUserRequest request)
    {
        var user = await _uow.Users.GetByIdAsync(request.Id);

        if (request.DeputyId is not null)
        {
            var deputy = await _uow.Users.GetByIdAsync((Guid)request.DeputyId);

            if (deputy == null ||
                deputy.UserRoles.Any(r => r.Role.Name != UserRoles.Deputy) ||
                user.UserRoles.Any(r => r.Role.Name != UserRoles.Helper))
                throw new KeyNotFoundException("Не найден депутат либо попытка привязать депутата к депутату");
        }

        if (request.DeputyId == user.Id)
            throw new ArgumentException("Нельзя связать пользователя с самим собой");

        user.Email = request.Email;
        user.JobTitle = request.JobTitle;
        user.FullName = request.FullName;
        user.UserRoles = request.UserRoles;
        user.DeputyId = request.DeputyId;

        _uow.Users.Update(user);
        await _uow.SaveChangesAsync();

        return user;
    }


    public async Task<IEnumerable<User>> ListAsync(int skip = 0, int take = 50)
    {
        return await _uow.Users.ListAsync(null, skip, take);
    }
}