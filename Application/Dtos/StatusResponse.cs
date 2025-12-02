namespace Application.Dtos;

public class StatusResponse
{
    public string Name { get; set; }
    public bool IsDefault { get; set; }
    public IEnumerable<TaskResponse> TaskEntities { get; set; }
}