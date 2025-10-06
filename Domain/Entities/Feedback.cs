namespace Domain.Entities;

public class Feedback
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}