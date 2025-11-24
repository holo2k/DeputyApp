using Application.Dtos;
using Application.Mappers;
using Application.Services.Abstractions;
using DeputyApp.DAL.UnitOfWork;
using Domain.Constants;
using Domain.Entities;
using Infrastructure.DAL.Repository.Abstractions;

namespace Application.Services.Implementations;

public class TaskService(HttpClient httpClient, IAuthService auth, IUnitOfWork uow) : ITaskService
{
    private readonly ITaskRepository taskRepository = uow.Tasks;
    private readonly IUserRepository userRepository = uow.Users;
    public async Task<Guid> CreateAsync(CreateTaskRequest request)
    {
        var currentUserId = auth.GetCurrentUserId();
        var entity = new TaskEntity
        {
            AuthorId = currentUserId,
            Description = request.Description,
            ExpectedEndDate = request.ExpectedEndDate,
            Priority = request.Priority,
            StatusId = request.StatusId,
            Title = request.Title,
        };
        entity.AuthorId = currentUserId;
        await taskRepository.AddAsync(entity);
        
        return entity.Id;
    }

    public async Task Delete(Guid id)
    {
        var currentUser = await auth.GetCurrentUserAsync();
        if (currentUser is null) throw new UnauthorizedAccessException();
        var task = await taskRepository.GetByIdAsync(id);
        if (task is null) throw new UnauthorizedAccessException();
        if (currentUser.Roles.All(x=>x != "Admin") || task.AuthorId != currentUser.Id) 
            throw new Exception($"Permission denied for task with id {task.Id}");        
        var entity = await taskRepository.GetByIdAsync(id);
        if (entity is null) throw new Exception($"Task with id {id} was not found");
        uow.Tasks.Delete(entity);
    }

    public async Task<TaskResponse> Update(CreateTaskRequest request, Guid taskId)
    {
        var task = await taskRepository.GetByIdAsync(taskId);
        if (task is null) throw new Exception($"Task with id {taskId} was not found");
        var currentUserId =  auth.GetCurrentUserId();
        var currentUserRole = auth.GetCurrentUserRoles();
        if (currentUserRole.All(x=>x != "Admin") || task.AuthorId != currentUserId) 
            throw new Exception($"Permission denied for task with id {taskId}");
        
        task.UpdateFrom(request);
        taskRepository.Update(task);
        return task.ToTaskResponse();
    }

    public async Task<IEnumerable<TaskResponse>> GetAllAsync()
    {
        var tasks = await taskRepository.ListAsync();
        var models = tasks.Select(x => x.ToTaskResponse());

        return models;
    }

    public async Task<TaskResponse> GetByIdAsync(Guid id)
    {
        var currentUser = await auth.GetCurrentUserAsync();
        if (currentUser is null) throw new UnauthorizedAccessException();
        var task = await taskRepository.GetByIdAsync(id);
        if (task is null) throw new UnauthorizedAccessException();
        if (currentUser.Roles.All(x=>x != "Admin") || task.AuthorId != currentUser.Id) 
            throw new Exception($"Permission denied for task with id {task.Id}");   
        if (task is null) throw new Exception($"Task with id {id} was not found");
        return task.ToTaskResponse();
    }
    
    public async Task<Guid> SetArchivedStatus(Guid id, bool isArchived)
    {
        var currentUser = await auth.GetCurrentUserAsync();
        if (currentUser is null) throw new UnauthorizedAccessException();
        var task = await taskRepository.GetByIdAsync(id);
        if (task is null) throw new Exception($"Task with id {id} was not found");
        if (currentUser.Roles.All(x=>x != "Admin") || task.AuthorId != currentUser.Id) 
            throw new Exception($"Permission denied for task with id {task.Id}");   
        task.IsArchived = isArchived;
        taskRepository.Update(task);
        return id;
    }

    public async Task<Guid> AddUserAsync(Guid userId, Guid taskId)
    {
        var task = await taskRepository.GetByIdAsync(taskId);
        if (task is null) 
            throw new Exception($"Task with id {taskId} was not found");
        
        var author = await auth.GetCurrentUserAsync();
        if (author is null) 
            throw new Exception($"User with id {userId} was not found");
        
        if (author.Roles.All(x=>x != "Admin") || task.AuthorId != author.Id) 
            throw new Exception($"Permission denied for item with id {taskId}");
        
        var user = await userRepository.GetByIdAsync(userId);
        if (user is null) throw new Exception($"User with id {userId} was not found");
        task.Users.Add(user);
        taskRepository.Update(task);
        userRepository.Update(user);
        
        return task.Id;
    }
    
    public async Task<IEnumerable<TaskResponse>> GetByCurrentUser()
    {
        var userId = auth.GetCurrentUserId();
        if (userId == Guid.Empty) throw new Exception($"User with id {userId} was not found");
        var tasks = await taskRepository.ListAsync(task=>task.AuthorId == userId || task.Users.Any(user=>user.Id == userId));
        return tasks.Select(x => x.ToTaskResponse());
    }

    public async Task<ICollection<TaskResponse>> GetByUserId(Guid userId)
    {
        var currentUserRoles = auth.GetCurrentUserRoles();
        if (currentUserRoles.All(x => x != UserRoles.Admin))
            throw new Exception($"Permission denied for task with id {userId}");
        
        var tasks = await taskRepository.ListAsync(task=>task.AuthorId == userId || task.Users.Any(user=>user.Id == userId));

        return tasks.Select(x=>x.ToTaskResponse()).ToList();
    }
}