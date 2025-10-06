namespace DeputyApp.Controllers.Dtos;

/// <summary>
///     DTO для возврата информации о событии.
/// </summary>
public class EventResponseDto
{
    /// <summary>Идентификатор события.</summary>
    public Guid Id { get; set; }

    /// <summary>Название события.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Описание события.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Дата и время начала события.</summary>
    public DateTimeOffset StartAt { get; set; }

    /// <summary>Дата и время окончания события.</summary>
    public DateTimeOffset EndAt { get; set; }

    /// <summary>Место проведения события.</summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>Флаг, указывающий, является ли событие публичным.</summary>
    public bool IsPublic { get; set; }

    /// <summary>Идентификатор организатора события.</summary>
    public Guid OrganizerId { get; set; }

    /// <summary>Полное имя организатора события.</summary>
    public string OrganizerFullName { get; set; } = string.Empty;

    /// <summary>Дата и время создания события.</summary>
    public DateTimeOffset CreatedAt { get; set; }
}