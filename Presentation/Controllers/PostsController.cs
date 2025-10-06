using Application.Dtos;
using Application.Services.Abstractions;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Controllers.Requests;

namespace Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PostsController(IPostService posts, IAuthService authService) : ControllerBase
{
    /// <summary>
    ///     Получить список опубликованных постов.
    /// </summary>
    /// <remarks>
    ///     Метод возвращает посты с пагинацией.
    ///     Можно указать параметры skip и take для пропуска и ограничения количества постов.
    /// </remarks>
    /// <param name="skip">Количество пропущенных постов (по умолчанию 0).</param>
    /// <param name="take">Количество постов для выборки (по умолчанию 20).</param>
    /// <returns>Список постов в формате <see cref="PostResponse" />.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<PostResponse>), 200)]
    public async Task<IActionResult> GetPublished([FromQuery] int skip = 0, [FromQuery] int take = 20)
    {
        var list = await posts.GetPublishedAsync(skip, take);

        var respList = list.Select(p => new PostResponse
            {
                Id = p.Id,
                Title = p.Title,
                Summary = p.Summary,
                Body = p.Body,
                ThumbnailUrl = p.ThumbnailUrl,
                CreatedAt = p.CreatedAt,
                PublishedAt = p.PublishedAt
            }
        ).ToList();

        return Ok(respList);
    }

    /// <summary>
    ///     Создать и опубликовать пост.
    /// </summary>
    /// <remarks>
    ///     Тело запроса содержит данные поста: заголовок, краткое описание, текст и URL миниатюры.
    ///     Возвращается DTO созданного поста с его идентификатором и датами создания и публикации.
    /// </remarks>
    /// <param name="req">Данные поста для создания (<see cref="CreatePostRequest" />).</param>
    /// <returns>Созданный пост в формате <see cref="PostResponse" />.</returns>
    /// <response code="201">Пост успешно создан.</response>
    /// <response code="401">Пользователь не авторизован.</response>
    [HttpPost]
    [ProducesResponseType(typeof(PostResponse), 200)]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreatePostRequest req)
    {
        var userId = authService.GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var post = new Post
        {
            Title = req.Title,
            Summary = req.Summary,
            Body = req.Body,
            ThumbnailUrl = req.ThumbnailUrl,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedById = userId
        };

        var created = await posts.CreateAsync(post);

        var resp = new PostResponse
        {
            Id = created.Id,
            Title = created.Title,
            Summary = created.Summary,
            Body = created.Body,
            ThumbnailUrl = created.ThumbnailUrl,
            CreatedAt = created.CreatedAt,
            PublishedAt = created.PublishedAt
        };

        await posts.PublishAsync(resp.Id); //опубликовать?
        return CreatedAtAction(nameof(GetById), new { id = resp.Id }, resp);
    }

    /// <summary>
    ///     Получить пост по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор поста.</param>
    /// <returns>Пост в формате <see cref="PostResponse" />.</returns>
    /// <response code="200">Пост найден и возвращен.</response>
    /// <response code="404">Пост с указанным ID не найден.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(PostResponse), 200)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var p = await posts.GetByIdAsync(id);
        if (p == null) return NotFound();

        var resp = new PostResponse
        {
            Id = p.Id,
            Title = p.Title,
            Summary = p.Summary,
            Body = p.Body,
            ThumbnailUrl = p.ThumbnailUrl,
            CreatedAt = p.CreatedAt,
            PublishedAt = p.PublishedAt
        };

        return Ok(resp);
    }

    /// <summary>
    ///     Удалить пост (только для авторизованных пользователей).
    /// </summary>
    /// <param name="id">Идентификатор поста для удаления.</param>
    /// <response code="204">Пост успешно удален.</response>
    /// <response code="401">Пользователь не авторизован.</response>
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id)
    {
        await posts.DeleteAsync(id);
        return NoContent();
    }
}