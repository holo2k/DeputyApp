using Application.Dtos;
using Application.Services.Abstractions;
using DeputyApp.Controllers.Requests;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeputyApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FeedbackController(IFeedbackService feedback, IAuthService authService) : ControllerBase
{
    /// <summary>
    ///     Отправка обратной связи пользователем.
    /// </summary>
    /// <remarks>
    ///     Этот метод позволяет авторизованному пользователю отправить обратную связь.
    ///     В теле запроса указываются имя, email и сообщение.
    ///     Возвращается DTO созданного отзыва с его идентификатором и датой создания.
    /// </remarks>
    /// <param name="dto">Данные обратной связи (Name, Email, Message).</param>
    /// <returns>Созданный отзыв в формате <see cref="FeedbackDto" />.</returns>
    /// <response code="201">Обратная связь успешно создана.</response>
    /// <response code="401">Пользователь не авторизован.</response>
    [HttpPost]
    public async Task<IActionResult> Send([FromBody] FeedbackRequest dto)
    {
        var userId = authService.GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var fb = new Feedback
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = dto.Name,
            Email = dto.Email,
            Message = dto.Message,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var created = await feedback.CreateAsync(fb);

        var response = new FeedbackDto
        {
            Id = created.Id,
            Name = created.Name,
            Email = created.Email,
            Message = created.Message,
            CreatedAt = created.CreatedAt
        };

        return CreatedAtAction(nameof(Send), new { id = response.Id }, response);
    }

    /// <summary>
    ///     Получение последних отзывов (админский метод).
    /// </summary>
    /// <remarks>
    ///     Метод позволяет администраторам просмотреть последние отзывы за указанное количество дней.
    ///     Возвращает список <see cref="FeedbackDto" /> с информацией об отзывах.
    /// </remarks>
    /// <param name="days">Количество дней для фильтрации отзывов (по умолчанию 30).</param>
    /// <returns>Список последних отзывов.</returns>
    /// <response code="200">Список отзывов успешно получен.</response>
    /// <response code="401">Пользователь не авторизован.</response>
    [HttpGet("recent")]
    [Authorize]
    public async Task<IActionResult> Recent([FromQuery] int days = 30)
    {
        var list = await feedback.RecentAsync(days);

        var dtoList = list.Select(f => new FeedbackDto
        {
            Id = f.Id,
            Name = f.Name,
            Email = f.Email,
            Message = f.Message,
            CreatedAt = f.CreatedAt
        }).ToList();

        return Ok(dtoList);
    }
}