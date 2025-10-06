namespace Domain.Entities;

public class Event
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTimeOffset StartAt { get; set; }
    public DateTimeOffset EndAt { get; set; }
    public string? Location { get; set; }
    public Guid? OrganizerId { get; set; }
    public User? Organizer { get; set; }
    public bool IsPublic { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}