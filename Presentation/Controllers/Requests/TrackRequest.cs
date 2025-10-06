namespace Presentation.Controllers.Requests;

public record TrackRequest(
    string EventType,
    Guid? UserId,
    string? PayloadJson
);