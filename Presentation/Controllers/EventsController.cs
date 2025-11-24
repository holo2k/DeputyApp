using Application.Dtos;
using Application.Services.Abstractions;
using Domain.Constants;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Controllers.Requests;

namespace Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IEventService _events;
    private readonly IDocumentService _documentService;

    public EventsController(IEventService events, IAuthService authService, IDocumentService documentService)
    {
        _events = events;
        _authService = authService;
        _documentService = documentService;
    }

    [HttpPost("{id}/attachments")]
    [Authorize(Roles = UserRoles.Admin)]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> UploadAttachment(Guid id, IFormFile file, [FromBody] string? description)
    {
        if (file == null || file.Length == 0)
            return BadRequest("file required");

        using var s = file.OpenReadStream();
        // сохраняем документ глобально через DocumentService (upload -> returns Document)
        var doc = await _documentService.UploadAsync(file.FileName, s, file.ContentType, _authService.GetCurrentUserId(), null);
        await _events.AttachDocumentAsync(id, doc.Id, _authService.GetCurrentUserId(), description);
        return Ok();
    }

    [HttpPost("{id}/rsvp")]
    [Authorize]
    public async Task<IActionResult> Rsvp(Guid id, [FromBody] RsvpRequest req)
    {
        var userId = _authService.GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized();

        await _events.RSVPAsync(id, userId, req.Status, req.ExcuseDocumentId, req.ExcuseNote);
        return NoContent();
    }

    /// <summary>
    ///     Получить список предстоящих событий в указанном диапазоне дат.
    /// </summary>
    /// <remarks>
    ///     Метод возвращает события, которые начинаются или заканчиваются в указанном интервале времени.
    /// </remarks>
    /// <param name="from">Начальная дата диапазона.</param>
    /// <param name="to">Конечная дата диапазона.</param>
    /// <returns>Список событий в формате <see cref="EventResponseDto" />.</returns>
    [HttpGet("upcoming")]
    [ProducesResponseType(typeof(List<EventResponseDto>), 200)]
    public async Task<IActionResult> GetUpcoming([FromQuery] DateTimeOffset from, [FromQuery] DateTimeOffset to)
    {
        var list = await _events.GetUpcomingAsync(from, to);

        var dtoList = list.Select(ev => new EventResponseDto
        {
            Id = ev.Id,
            Title = ev.Title,
            Description = ev.Description,
            StartAt = ev.StartAt,
            EndAt = ev.EndAt,
            Location = ev.Location,
            IsPublic = ev.IsPublic,
            OrganizerId = ev.OrganizerId ?? Guid.Empty,
            OrganizerFullName = ev.Organizer?.FullName ?? "",
            CreatedAt = ev.CreatedAt
        }).ToList();

        return Ok(dtoList);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var ev = await _events.GetWithDetailsAsync(id);

        if (!ev.IsPublic)
            return Forbid("Через этот эндпоинт нет доступа к приватным событиям");

        if (ev == null)
            return NotFound();

        var dto = new EventDetailDto
        {
            Id = ev.Id,
            Title = ev.Title,
            Type = ev.Type,
            EndAt = ev.EndAt,
            StartAt = ev.StartAt,
            Description = ev.Description,
            IsPublic = ev.IsPublic,
            Location = ev.Location,
            Attachments = ev.Attachments.Select(a => new AttachmentDto(a.Id, a.DocumentId, a.Document.FileName, a.Document.Url, a.Description)).ToList(),
            Attendees = ev.Participants.Select(p => new AttendeeDto(p.UserId, p.User.FullName, p.Status, p.ExcuseDocumentId, p.ExcuseNote)).ToList()
        };

        return Ok(dto);
    }


    /// <summary>
    ///     Получить список предстоящих событий в указанном диапазоне дат.
    /// </summary>
    /// <remarks>
    ///     Метод возвращает события, которые начинаются или заканчиваются в указанном интервале времени.
    /// </remarks>
    /// <param name="from">Начальная дата диапазона.</param>
    /// <param name="to">Конечная дата диапазона.</param>
    /// <returns>Список событий в формате <see cref="EventResponseDto" />.</returns>
    [HttpGet("my-upcoming")]
    [ProducesResponseType(typeof(List<EventResponseDto>), 200)]
    public async Task<IActionResult> GetMyUpcoming([FromQuery] DateTimeOffset from, [FromQuery] DateTimeOffset to)
    {
        var userId = _authService.GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var user = await _authService.GetCurrentUserAsync();
        var roles = _authService.GetCurrentUserRoles();

        if (roles.Contains(UserRoles.Helper)) // Если помощник - получает события своего депутата
            userId = user.Deputy.Id;

        var list = await _events.GetMyUpcomingAsync(userId, from, to);

        var dtoList = list.Select(ev => new EventResponseDto
        {
            Id = ev.Id,
            Title = ev.Title,
            Description = ev.Description,
            StartAt = ev.StartAt,
            EndAt = ev.EndAt,
            Location = ev.Location,
            IsPublic = ev.IsPublic,
            OrganizerId = ev.OrganizerId ?? Guid.Empty,
            OrganizerFullName = ev.Organizer?.FullName ?? "",
            CreatedAt = ev.CreatedAt
        }).ToList();

        return Ok(dtoList);
    }

    //TODO
    /// <summary>
    ///     Создать приватное событие (только для авторизованных пользователей).
    /// </summary>
    /// <param name="req">Данные события для создания (<see cref="CreateEventRequest" />).</param>
    /// <returns>Созданное событие в формате <see cref="EventResponseDto" />.</returns>
    /// <response code="201">Событие успешно создано.</response>
    /// <response code="401">Пользователь не авторизован.</response>
    [HttpPost("create-private")]
    [Authorize]
    [ProducesResponseType(typeof(EventResponseDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreatePrivate([FromBody] CreateEventRequest req)
    {
        var userId = _authService.GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized();
        var user = await _authService.GetCurrentUserAsync();
        var roles = _authService.GetCurrentUserRoles();

        if (roles.Contains(UserRoles.Helper)) // Если помощник - создает событие для своего депутата
            userId = user.Deputy.Id;

        var created = await CreateEventInternalAsync(req, userId, isPublic: false);
        return CreatedAtAction(nameof(GetUpcoming), new { id = created.Id }, created);
    }

    /// <summary>
    ///     Создать публичное событие (только для администратора).
    /// </summary>
    /// <param name="req">Данные события для создания (<see cref="CreateEventRequest" />).</param>
    /// <returns>Созданное событие в формате <see cref="EventResponseDto" />.</returns>
    /// <response code="201">Событие успешно создано.</response>
    /// <response code="401">Пользователь не авторизован.</response>
    [HttpPost("create-public")]
    [Authorize(Roles = UserRoles.Admin)]
    [ProducesResponseType(typeof(EventResponseDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreatePublic([FromBody] CreateEventRequest req)
    {
        var userId = _authService.GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized();

        var created = await CreateEventInternalAsync(req, userId, isPublic: true);
        return CreatedAtAction(nameof(GetUpcoming), new { id = created.Id }, created);
    }

    private async Task<EventResponseDto> CreateEventInternalAsync(CreateEventRequest req, Guid userId, bool isPublic)
    {
        var ev = new Event
        {
            Id = Guid.NewGuid(),
            Title = req.Title,
            Description = req.Description,
            StartAt = req.StartAt,
            EndAt = req.EndAt,
            Location = req.Location,
            IsPublic = isPublic,
            OrganizerId = userId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var created = await _events.CreateAsync(ev);

        return new EventResponseDto
        {
            Id = created.Id,
            Title = created.Title,
            Description = created.Description,
            StartAt = created.StartAt,
            EndAt = created.EndAt,
            Location = created.Location,
            IsPublic = created.IsPublic,
            OrganizerId = created.OrganizerId ?? Guid.Empty,
            OrganizerFullName = created.Organizer?.FullName ?? string.Empty,
            CreatedAt = created.CreatedAt
        };
    }


    /// <summary>
    ///     Удалить событие (только для авторизованных пользователей).
    /// </summary>
    /// <param name="id">Идентификатор события для удаления.</param>
    /// <response code="204">Событие успешно удалено.</response>
    /// <response code="401">Пользователь не авторизован.</response>
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _events.DeleteAsync(id);
        return NoContent();
    }
}