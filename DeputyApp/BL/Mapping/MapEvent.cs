using DeputyApp.Controllers.Dtos;
using DeputyApp.Entities;

namespace DeputyApp.BL.Mapping;

public static class EventExtensions
{
    public static EventResponseDto Map(this Event ev)
    {
        return new EventResponseDto
        {
            Id = ev.Id,
            Title = ev.Title,
            Description = ev.Description,
            StartAt = ev.StartAt,
            EndAt = ev.EndAt,
            Location = ev.Location,
            IsPublic = ev.IsPublic,
            OrganizerId = (Guid)ev.OrganizerId!,
            OrganizerFullName = ev.Organizer?.FullName ?? "",
            CreatedAt = ev.CreatedAt
        };
    }
}