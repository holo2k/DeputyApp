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

    public EventsController(IEventService events, IAuthService authService)
    {
        _events = events;
        _authService = authService;
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

        var created = await CreateEventInternalAsync(req, userId, false);
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

        var created = await CreateEventInternalAsync(req, userId, true);
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