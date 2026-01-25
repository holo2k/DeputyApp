using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Domain.Enums;
using Domain.GlobalModels.Abstractions;

namespace Domain.Entities;

public class Event : INotifiable
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTimeOffset StartAt { get; set; }
    public DateTimeOffset EndAt { get; set; }
    public string? Location { get; set; }
    public Guid? OrganizerId { get; set; }
    [JsonIgnore] public User? Organizer { get; set; }
    public bool IsPublic { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public EventType Type { get; set; } = EventType.Event;

    /// <summary>
    /// Endpoint для уведомления в телеграм
    /// </summary>
    [NotMapped]
    public string TelegramEndpoint => "send-notify-event";
    public ICollection<EventAttachment> Attachments { get; set; } = new List<EventAttachment>();
    public ICollection<UserEvent> Participants { get; set; } = new List<UserEvent>();
}