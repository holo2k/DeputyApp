namespace Application.Dtos;

public record PostResponse(
    Guid Id,
    string Title,
    string Summary,
    string Body,
    string? ThumbnailUrl,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PublishedAt
);