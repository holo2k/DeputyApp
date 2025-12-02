using System.Text.Json.Serialization;
using Domain.Entities;

namespace Application.Dtos;

public class TaskResponse
{
    public Guid AuthorId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime StartDate { get; set; }
    public DateTime ExpectedEndDate { get; set; }
    public int Priority { get; set; }
    public string Status { get; set; }
    public bool IsArchived { get; set; }
    public ICollection<User> Users { get; set; } = new List<User>();
}
