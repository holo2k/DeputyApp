namespace DeputyApp.Controllers.Requests;

/// <summary>
///     DTO для создания нового поста.
/// </summary>
public record CreatePostRequest(
    /// <summary>Заголовок поста.</summary>
    string Title,
    /// <summary>Краткое описание/резюме поста.</summary>
    string Summary,
    /// <summary>Полное содержание поста.</summary>
    string Body,
    /// <summary>URL миниатюры (опционально).</summary>
    string? ThumbnailUrl
);