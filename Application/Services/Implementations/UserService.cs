using Application.Dtos;
using Application.Services.Abstractions;
using DeputyApp.DAL.UnitOfWork;
using Domain.Constants;
using Domain.Entities;
using Infrastructure.DAL.Repository.Abstractions;
using Infrastructure.DAL.Repository.Implementations;
using Task = System.Threading.Tasks.Task;

namespace Application.Services.Implementations;

public class UserService : IUserService
{
    private readonly IUnitOfWork _uow;
    private readonly IUserRepository _userRepository;

    public UserService(IUnitOfWork uow, IUserRepository userRepository)
    {
        _uow = uow;
        _userRepository = userRepository;
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
        var user = await _userRepository.FindByIdAsync(userId);
        if (user == null)
            throw new KeyNotFoundException("User not found");

        // Получаем роль из базы
        var role = (await _uow.Roles.ListAsync(r => r.Name == roleName)).FirstOrDefault();
        if (role == null)
        {
            role = new Role { Id = Guid.NewGuid(), Name = roleName };
            await _uow.Roles.AddAsync(role);
            await _uow.SaveChangesAsync(); // сохраняем чтобы EF начал отслеживать объект
        }

        // Проверяем в базе, что такой UserRole ещё нет
        var exists = (user.UserRoles.ToList().Where(ur => ur.UserId == userId && ur.RoleId == role.Id)).Any();
        if (!exists)
        {
            var userRole = new UserRole { UserId = userId, RoleId = role.Id };
            user.UserRoles.Add(userRole);
            await _uow.SaveChangesAsync();
        }
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

        user.UserRoles = new List<UserRole>();

        foreach (var role in request.UserRoles)
        {
           await AssignRoleAsync(user.Id, role);
        }

        user.Email = request.Email;
        user.JobTitle = request.JobTitle;
        user.FullName = request.FullName;
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