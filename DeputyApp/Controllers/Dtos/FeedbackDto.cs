namespace DeputyApp.Controllers.Dtos;

/// <summary>
///     DTO для возврата информации об обратной связи.
/// </summary>
public class FeedbackDto
{
    /// <summary>Идентификатор обратной связи.</summary>
    public Guid Id { get; set; }

    /// <summary>Имя пользователя, оставившего отзыв.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Email пользователя, оставившего отзыв.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Текст сообщения.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Дата и время создания отзыва.</summary>
    public DateTimeOffset CreatedAt { get; set; }
}