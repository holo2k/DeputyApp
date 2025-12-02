namespace Application.Dtos;

public class TaskCreateRequest
{
    public string Title { get; set; }
    public string Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime ExpectedEndDate { get; set; }
    public int Priority { get; set; }
    public string Status { get; set; }
}