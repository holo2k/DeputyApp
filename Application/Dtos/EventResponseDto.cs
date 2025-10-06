namespace Application.Dtos;

public class EventResponseDto
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTimeOffset StartAt { get; set; }

    public DateTimeOffset EndAt { get; set; }

    public string Location { get; set; } = string.Empty;

    public bool IsPublic { get; set; }

    public Guid OrganizerId { get; set; }

    public string OrganizerFullName { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
}