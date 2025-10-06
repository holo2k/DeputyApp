namespace DeputyApp.Controllers.Requests;

/// <summary>
///     DTO для отправки обратной связи (feedback).
/// </summary>
public class FeedbackRequest
{
    /// <summary>Имя пользователя, отправляющего отзыв.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Email пользователя, отправляющего отзыв.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Сообщение обратной связи.</summary>
    public string Message { get; set; } = string.Empty;
}