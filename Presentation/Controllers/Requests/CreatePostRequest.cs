namespace Presentation.Controllers.Requests;

public record CreatePostRequest(
    string Title,
    string Summary,
    string Body,
    string? ThumbnailUrl
);