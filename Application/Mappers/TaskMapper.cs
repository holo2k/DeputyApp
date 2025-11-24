using Application.Dtos;
using Domain.Entities;

namespace Application.Mappers;

public static class TaskMapper
{
    public static TaskResponse ToTaskResponse(this TaskEntity task)
    {
        return new TaskResponse
        {
            AuthorId = task.AuthorId,
            Title = task.Title,
            Description = task.Description,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt,
            StartDate = task.StartDate,
            ExpectedEndDate = task.ExpectedEndDate,
            Priority = task.Priority,
            StatusId = task.StatusId,
            IsArchived = task.IsArchived,
        };
    }
}