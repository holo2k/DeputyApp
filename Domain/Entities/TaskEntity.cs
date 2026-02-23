using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Domain.GlobalModels.Abstractions;

namespace Domain.Entities;

public class TaskEntity : INotifiable
{
    public Guid Id { get; set; }
    public Guid StatusId { get; set; }
    public Guid AuthorId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime StartDate { get; set; }
    public DateTime ExpectedEndDate { get; set; }
    public int Priority { get; set; }
    public bool IsArchived { get; set; }
    public ICollection<User> Users { get; set; } = new List<User>();
    public virtual Status Status { get; set; }

    /// <summary>
    /// Endpoint для уведомления в телеграм
    /// </summary>
    [NotMapped]
    public string TelegramEndpoint => "send-notify-task";

    [NotMapped]
    public string AuthorName => Users?
     .Where(x => x.Id == AuthorId)
     .Select(x => x.FullName)
     .FirstOrDefault() ?? "Unknown";
}