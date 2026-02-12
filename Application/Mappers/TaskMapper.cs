using Application.Dtos;
using Domain.Entities;

namespace Application.Mappers;

public static class TaskMapper
{
    public static TaskResponse ToTaskResponse(this TaskEntity task, string status)
    {
        return new TaskResponse
        {
            TaskId = task.Id,
            AuthorId = task.AuthorId,
            AuthorName = task.Author.FullName,
            Title = task.Title,
            Description = task.Description,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt,
            StartDate = task.StartDate,
            ExpectedEndDate = task.ExpectedEndDate,
            Priority = task.Priority,
            Status = status,
            IsArchived = task.IsArchived,
            Users = task.Users.ToList()
        };
    }

    public static TaskEntity ToTaskEntity(this TaskCreateRequest task, Guid currentUserId, Guid statusId)
    {
        return new TaskEntity
        {
            AuthorId = currentUserId,
            Description = task.Description,
            ExpectedEndDate = task.ExpectedEndDate,
            Priority = task.Priority,
            StatusId = statusId,
            Title = task.Title,
        };
    }
}