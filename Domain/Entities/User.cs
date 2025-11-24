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
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<Post> Posts { get; set; } = new List<Post>();
    public ICollection<Document> Documents { get; set; } = new List<Document>();
    public ICollection<Event> EventsOrganized { get; set; } = new List<Event>();
    public ICollection<TaskEntity> Tasks { get; set; } = new List<TaskEntity>();
}