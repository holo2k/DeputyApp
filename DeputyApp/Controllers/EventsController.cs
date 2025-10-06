using Application.Mapping;
using Application.Services.Abstractions;
using DeputyApp.Controllers.Requests;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeputyApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController(IEventService events, IAuthService authService) : ControllerBase
{
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
        var list = await events.GetUpcomingAsync(from, to);

        var dtoList = list.Select(x => x.Map()).ToList();

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
    /// <returns>Список событий в формате <see cref="EventDto" />.</returns>
    [HttpGet("my-upcoming")]
    public async Task<IActionResult> GetMyUpcoming([FromQuery] DateTimeOffset from, [FromQuery] DateTimeOffset to)
    {
        var userId = authService.GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var list = await events.GetMyUpcomingAsync(userId, from, to);

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
        var userId = authService.GetCurrentUserId();
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

        var created = await events.CreateAsync(ev);

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
        await events.DeleteAsync(id);
        return NoContent();
    }
}