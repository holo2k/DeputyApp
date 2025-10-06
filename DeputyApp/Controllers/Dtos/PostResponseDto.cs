namespace DeputyApp.Controllers.Dtos;

/// <summary>
///     DTO для возврата информации о посте.
/// </summary>
public record PostResponse(
    /// <summary>Идентификатор поста.</summary>
    Guid Id,
    /// <summary>Заголовок поста.</summary>
    string Title,
    /// <summary>Краткое описание поста.</summary>
    string Summary,
    /// <summary>Полное содержание поста.</summary>
    string Body,
    /// <summary>URL миниатюры (опционально).</summary>
    string? ThumbnailUrl,
    /// <summary>Дата и время создания поста.</summary>
    DateTimeOffset CreatedAt,
    /// <summary>Дата и время публикации поста (опционально).</summary>
    DateTimeOffset? PublishedAt
);