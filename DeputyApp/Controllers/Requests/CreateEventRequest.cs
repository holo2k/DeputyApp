namespace DeputyApp.Controllers.Requests;

public class CreateEventRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset StartAt { get; set; }
    public DateTimeOffset EndAt { get; set; }
    public string Location { get; set; } = string.Empty;
    public bool IsPublic { get; set; } = true;
}