namespace DeputyApp.Controllers.Requests;

/// <summary>
///     DTO для создания нового события.
/// </summary>
public class CreateEventRequest
{
    /// <summary>
    ///     Название события.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    ///     Описание события.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    ///     Дата и время начала события.
    /// </summary>
    public DateTimeOffset StartAt { get; set; }

    /// <summary>
    ///     Дата и время окончания события.
    /// </summary>
    public DateTimeOffset EndAt { get; set; }

    /// <summary>
    ///     Место проведения события.
    /// </summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>
    ///     Флаг, указывающий, является ли событие публичным (видимым для всех).
    /// </summary>
    public bool IsPublic { get; set; } = true;
}