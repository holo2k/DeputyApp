using Application.Dtos;
using Domain.Entities;

namespace Application.Mappers;

public static class StatusMapper
{
    public static StatusResponse ToResponse(this Status status)
    {
        return new StatusResponse
        {
            Name = status.Name.ToLower(),
            TaskEntities = status.TaskEntities.Select(x => x.ToTaskResponse(status.Name)),
            IsDefault = status.IsDefault,
        };;
    }

    public static Status ToTaskEntity(this StatusRequest status)
    {
        return new Status
        {
            Name = status.Name.ToLower(),
            IsDefault = status.IsDefault,
        };
    }
}