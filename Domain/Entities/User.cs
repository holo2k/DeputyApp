using System.Text.Json.Serialization;

namespace Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Salt { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Guid? DeputyId { get; set; }
    public virtual User? Deputy { get; set; }
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public virtual ICollection<Post> Posts { get; set; } = new List<Post>();
    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
    public virtual ICollection<Event> EventsOrganized { get; set; } = new List<Event>();
    [JsonIgnore]
    public virtual ICollection<TaskEntity> Tasks { get; set; } = new List<TaskEntity>();
    public virtual ICollection<UserEvent> Events { get; set; } = new List<UserEvent>();
}