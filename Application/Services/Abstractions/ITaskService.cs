using Application.Dtos;

namespace Application.Services.Abstractions;

public interface ITaskService
{
    Task<Guid> CreateAsync(TaskCreateRequest request);
    Task Delete(Guid id);
    Task<IEnumerable<TaskResponse>> GetAllAsync();
    Task<TaskResponse> GetByIdAsync(Guid id);
    Task<Guid> SetArchivedStatus(Guid id, bool isArchived);
    Task<Guid> AddUserAsync(Guid userId, Guid taskId);
    Task<IEnumerable<TaskResponse>> GetByCurrentUser(bool includeAuthor = true, bool includeAssigned = true);
    Task<ICollection<TaskResponse>> GetByUserId(Guid userId);
    Task<TaskResponse> Update(TaskCreateRequest request, Guid taskId);
    Task<IEnumerable<TaskResponse>> GetAssignedTasks();
    Task<IEnumerable<TaskResponse>> GetAuthorTasks();
    Task<Guid> RemoveUserAsync(Guid taskId, Guid userId);
}