using DeputyApp.BL.Mapping;
using DeputyApp.BL.Services.Abstractions;
using DeputyApp.Controllers.Requests;
using DeputyApp.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeputyApp.Controllers;

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
    /// <returns>Список событий в формате <see cref="EventDto" />.</returns>
    [HttpGet("upcoming")]
    public async Task<IActionResult> GetUpcoming([FromQuery] DateTimeOffset from, [FromQuery] DateTimeOffset to)
    {
        var list = await _events.GetUpcomingAsync(from, to);

        var dtoList = list.Select(x => x.Map()).ToList();

        return Ok(dtoList);
    }

    /// <summary>
    ///     Создать новое событие (только для авторизованных пользователей).
    /// </summary>
    /// <remarks>
    ///     Тело запроса содержит минимальный набор данных для создания события: название, описание, даты начала и окончания,
    ///     локацию и публичность.
    ///     Возвращается DTO созданного события с его идентификатором и датой создания.
    /// </remarks>
    /// <param name="req">Данные события для создания (<see cref="CreateEventRequest" />).</param>
    /// <returns>Созданное событие в формате <see cref="EventDto" />.</returns>
    /// <response code="201">Событие успешно создано.</response>
    /// <response code="401">Пользователь не авторизован.</response>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateEventRequest req)
    {
        var userId = _authService.GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var ev = new Event
        {
            Id = Guid.NewGuid(),
            Title = req.Title,
            Description = req.Description,
            StartAt = req.StartAt,
            EndAt = req.EndAt,
            Location = req.Location,
            IsPublic = req.IsPublic,
            OrganizerId = userId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var created = await _events.CreateAsync(ev);

        return CreatedAtAction(nameof(GetUpcoming), new { id = created.Id }, created.Map());
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