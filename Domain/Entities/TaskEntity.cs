namespace Domain.Entities;

public class TaskEntity
{
    public Guid Id { get; set; }
    public Guid AuthorId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime StartDate { get; set; }
    public DateTime ExpectedEndDate { get; set; }
    public int Priority { get; set; }
    public int? StatusId { get; set; }
    public bool IsArchived { get; set; }
    public ICollection<User> Users { get; set; } = new List<User>();
}