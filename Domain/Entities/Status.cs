namespace Domain.Entities;

public class Status
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public bool IsDefault { get; set; }
    public ICollection<TaskEntity> TaskEntities { get; set; } = new List<TaskEntity>();
}