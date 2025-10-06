namespace DeputyApp.Entities;

public class AnalyticsEvent
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty; // e.g. "open_post", "send_feedback"
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public string? PayloadJson { get; set; } // free-form JSON
}