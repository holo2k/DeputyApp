using Application.Services.Abstractions;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Presentation.Controllers.Requests;

namespace Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalyticsController(IAnalyticsService analytics) : ControllerBase
{
    /// <summary>
    ///     Отправка события аналитики (открытый API для клиентов).
    /// </summary>
    /// <remarks>
    ///     Клиенты могут использовать этот метод для отправки различных событий в систему аналитики.
    ///     PayloadJson — это произвольный JSON с дополнительными данными события.
    /// </remarks>
    /// <param name="req">Данные события для трекинга (<see cref="TrackRequest" />).</param>
    /// <response code="202">Событие успешно принято для обработки.</response>
    [HttpPost("track")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> Track([FromBody] TrackRequest req)
    {
        await analytics.TrackAsync(req.EventType, req.UserId, req.PayloadJson);
        return Accepted();
    }

    /// <summary>
    ///     Получение событий аналитики в заданном диапазоне (только для администраторов).
    /// </summary>
    /// <remarks>
    ///     Метод позволяет фильтровать события по типу (eventType) и временным рамкам.
    ///     Возвращает список событий в формате JSON.
    /// </remarks>
    /// <param name="from">Начальная дата диапазона.</param>
    /// <param name="to">Конечная дата диапазона.</param>
    /// <param name="eventType">Необязательный фильтр по типу события.</param>
    /// <response code="200">Список событий удовлетворяющих фильтрам.</response>
    [HttpGet("query")]
    [ProducesResponseType(typeof(List<AnalyticsEvent>), 200)]
    public async Task<IActionResult> Query([FromQuery] DateTimeOffset from, [FromQuery] DateTimeOffset to,
        [FromQuery] string? eventType)
    {
        var list = await analytics.QueryAsync(from, to, eventType);
        return Ok(list);
    }
}