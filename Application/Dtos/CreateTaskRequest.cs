namespace Application.Dtos;

public class CreateTaskRequest
{
    public string Title { get; set; }
    public string Description { get; set; }
    public DateTime ExpectedEndDate { get; set; }
    public int Priority { get; set; }
    public int? StatusId { get; set; }
}