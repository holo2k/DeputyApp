using Application.Dtos;

namespace Application.Services.Abstractions;

public interface ITaskService
{
    Task<Guid> CreateAsync(CreateTaskRequest request);
    Task Delete(Guid id);
    Task<IEnumerable<TaskResponse>> GetAllAsync();
    Task<TaskResponse> GetByIdAsync(Guid id);
    Task<Guid> SetArchivedStatus(Guid id, bool isArchived);
    Task<Guid> AddUserAsync(Guid userId, Guid taskId);
    Task<IEnumerable<TaskResponse>> GetByCurrentUser();
    Task<ICollection<TaskResponse>> GetByUserId(Guid userId);
    Task<TaskResponse> Update(CreateTaskRequest request, Guid taskId);
}