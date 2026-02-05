using System.Collections;
using System.Linq.Expressions;
using Application.Dtos;
using Application.Mappers;
using Application.Services.Abstractions;
using DeputyApp.DAL.UnitOfWork;
using Domain.Constants;
using Domain.Entities;
using Infrastructure.DAL.Repository.Abstractions;

namespace Application.Services.Implementations;

public class TaskService : ITaskService
{
    private readonly ITaskRepository taskRepository;
    private readonly IUserRepository userRepository;
    private readonly IStatusRepository statusRepository;
    private readonly IAuthService auth;
    private readonly IUnitOfWork uow;

    public TaskService(IAuthService auth, IUnitOfWork uow)
    {
        this.auth = auth ?? throw new ArgumentNullException(nameof(auth));
        taskRepository = uow?.Tasks ?? throw new ArgumentNullException(nameof(uow.Tasks));
        userRepository = uow?.Users ?? throw new ArgumentNullException(nameof(uow.Users));
        statusRepository = uow?.Statuses ?? throw new ArgumentNullException(nameof(uow.Statuses));
        this.uow  = uow ?? throw new ArgumentNullException(nameof(uow));
    }
    public async Task<Guid> CreateAsync(TaskCreateRequest request)
    {
        var currentUserId = auth.GetCurrentUserId();
        var status = await statusRepository.GetByNameAsync(request.Status.ToLower());
        ArgumentNullException.ThrowIfNull(status, "Такого статуса не существует");
        var entity = request.ToTaskEntity(currentUserId, status.Id);
        entity.AuthorId = currentUserId;
        await taskRepository.AddAsync(entity);
        await uow.SaveChangesAsync();

        return entity.Id;
    }

    public async Task Delete(Guid id)
    {
        var currentUser = await auth.GetCurrentUserAsync();
        if (currentUser is null) throw new UnauthorizedAccessException();
        var task = await taskRepository.GetByIdAsync(id, x => x.Users, x => x.Status);        

        if (task is null) throw new UnauthorizedAccessException();
        if (currentUser.Roles.All(x=>x != "Admin") || task.AuthorId != currentUser.Id) 
            throw new UnauthorizedAccessException($"Permission denied for task with id {task.Id}");        
        uow.Tasks.Delete(task);
        await uow.SaveChangesAsync();
    }

    public async Task<TaskResponse> Update(TaskCreateRequest request, Guid taskId)
    {
        var existingStatus = await statusRepository.GetByNameAsync(request.Status);
        if(existingStatus is null) throw new ArgumentNullException($"Status with name {request.Status} was not found");
        var task = await taskRepository.GetByIdAsync(taskId, x => x.Users, x => x.Status);        
        if (task is null) throw new ArgumentNullException($"Task with id {taskId} was not found");
        var currentUserId =  auth.GetCurrentUserId();
        var currentUserRole = auth.GetCurrentUserRoles();
        if (currentUserRole.All(x=>x != "Admin") || task.AuthorId != currentUserId) 
            throw new UnauthorizedAccessException($"Permission denied for task with id {taskId}");
        
        task.UpdateFrom(request);
        if (task.Status.Name != request.Status)
        {
            var newStatus =  await statusRepository.GetByNameAsync(request.Status);
            task.StatusId = newStatus.Id;
        }
        taskRepository.Update(task);
        await uow.SaveChangesAsync();
        return task.ToTaskResponse(task.Status.Name);
    }

    public async Task<IEnumerable<TaskResponse>> GetAllAsync()
    {
        var tasks = await taskRepository.ListAsync(includes:
        [
            x => x.Users,
            x => x.Status
        ]);
        if (!tasks.Any())
        {
            return  Enumerable.Empty<TaskResponse>();
        }
        var models = tasks.Select(x => x.ToTaskResponse(x.Status.Name));

        return models;
    }

    public async Task<TaskResponse> GetByIdAsync(Guid id)
    {
        var currentUser = await auth.GetCurrentUserAsync();
        if (currentUser is null) throw new UnauthorizedAccessException();
        var task = await taskRepository.GetByIdAsync(id, x=>x.Users, x=>x.Status);
        if (task is null) throw new UnauthorizedAccessException();
        if (currentUser.Roles.All(x=>x != "Admin") || task.AuthorId != currentUser.Id) 
            throw new Exception($"Permission denied for task with id {task.Id}");   
        return task is null ? throw new ArgumentNullException($"Task with id {id} was not found") : task.ToTaskResponse(task.Status.Name);
    }
    
    public async Task<Guid> SetArchivedStatus(Guid id, bool isArchived)
    {
        var currentUser = await auth.GetCurrentUserAsync();
        if (currentUser is null) throw new UnauthorizedAccessException();
        var task = await taskRepository.GetByIdAsync(id, x=>x.Users, x=>x.Status);
        if (task is null) throw new ArgumentNullException($"Task with id {id} was not found");
        if (currentUser.Roles.All(x=>x != "Admin") || task.AuthorId != currentUser.Id) 
            throw new UnauthorizedAccessException($"Permission denied for task with id {task.Id}");   
        task.IsArchived = isArchived;
        taskRepository.Update(task);
        await uow.SaveChangesAsync();
        return id;
    }

    public async Task<Guid> AddUserAsync(Guid userId, Guid taskId)
    {
        var task = await taskRepository.GetByIdAsync(taskId, x=>x.Users, x=>x.Status);
        if (task is null) 
            throw new ArgumentNullException($"Task with id {taskId} was not found");
        
        var author = await auth.GetCurrentUserAsync();
        if (author is null) 
            throw new ArgumentNullException($"User with id {userId} was not found");
        
        if (!(author.Roles.All(x=>x != "Admin")) || task.AuthorId != author.Id) 
            throw new UnauthorizedAccessException($"Permission denied for item with id {taskId}");
        
        var user = await userRepository.GetByIdAsync(userId, x=>x.Tasks);
        if (user is null) throw new ArgumentNullException($"User with id {userId} was not found");
        task.Users.Add(user);
        taskRepository.Update(task);
        userRepository.Update(user);
        await uow.SaveChangesAsync();

        return task.Id;
    }
    
    public async Task<IEnumerable<TaskResponse>> GetByCurrentUser(bool includeAuthor = true, bool includeAssigned = true)
    {
        var userId = auth.GetCurrentUserId();
        if (userId == Guid.Empty)
            throw new UnauthorizedAccessException($"User with id {userId} was not found");

        var tasks = await taskRepository.ListAsync(task => 
                (includeAuthor && task.AuthorId == userId) || (includeAssigned && task.Users.Any(u => u.Id == userId)),
            includes:
            [
                x => x.Users,
                x => x.Status
            ]
        );

        return tasks.Select(x => x.ToTaskResponse(x.Status.Name));
    }

    public async Task<ICollection<TaskResponse>> GetByUserId(Guid userId)
    {
        var currentUserRoles = auth.GetCurrentUserRoles();
        if (currentUserRoles.All(x => x != UserRoles.Admin))
            throw new UnauthorizedAccessException($"Permission denied for task with id {userId}");
        
        var tasks = await taskRepository.ListAsync(task=>task.AuthorId == userId || task.Users.Any(user=>user.Id == userId), includes:
        [
            x => x.Users,
            x => x.Status
        ]);

        return tasks.Select(x=>x.ToTaskResponse(x.Status.Name)).ToList();
    }

    public Task<IEnumerable<TaskResponse>> GetAssignedTasks()
    {
        return GetByCurrentUser(includeAuthor: false, includeAssigned: true);
    }

    public Task<IEnumerable<TaskResponse>> GetAuthorTasks()
    {
        return GetByCurrentUser(includeAuthor: true, includeAssigned: false);
    }
}