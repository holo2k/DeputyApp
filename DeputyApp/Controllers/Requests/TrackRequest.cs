namespace DeputyApp.Controllers.Requests;

/// <summary>
///     DTO для отслеживания аналитических событий.
/// </summary>
public record TrackRequest(
    /// <summary>Тип события.</summary>
    string EventType,
    /// <summary>Идентификатор пользователя (опционально).</summary>
    Guid? UserId,
    /// <summary>Дополнительные данные события в формате JSON (опционально).</summary>
    string? PayloadJson
);