using Application.Dtos;
using Application.Services.Abstractions;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Controllers.Requests;

namespace Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CatalogsController(ICatalogService catalogService, IAuthService authService) : ControllerBase
{
    /// <summary>
    ///     Создать новый каталог.
    /// </summary>
    /// <param name="req">Данные для создания каталога: имя и родительский каталог (необязательный).</param>
    /// <returns>
    ///     201 Created с информацией о созданном каталоге.
    ///     401 Unauthorized если пользователь не авторизован.
    /// </returns>
    [HttpPost]
    [ProducesResponseType(typeof(CatalogResponse), 201)]
    public async Task<IActionResult> Create([FromBody] CreateCatalogRequest req)
    {
        var userId = authService.GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var catalog = await catalogService.CreateAsync(req.Name, userId, req.ParentCatalogId);
        var response = new CatalogResponse(catalog.Id, catalog.Name, catalog.ParentCatalogId);
        return CreatedAtAction(nameof(GetById), new { id = catalog.Id }, response);
    }

    /// <summary>
    ///     Получить каталог по его идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор каталога.</param>
    /// <returns>
    ///     200 OK с данными каталога.
    ///     404 NotFound если каталог не найден.
    /// </returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(CatalogResponse), 200)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = authService.GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var catalog = await catalogService.GetByIdAsync(id);
        if (catalog == null) return NotFound();
        var response = new CatalogResponse(catalog.Id, catalog.Name, catalog.ParentCatalogId);
        return Ok(response);
    }

    /// <summary>
    ///     Получить все каталоги, принадлежащие текущему пользователю.
    /// </summary>
    /// <returns>
    ///     200 OK с массивом каталогов.
    ///     401 Unauthorized если пользователь не авторизован.
    /// </returns>
    [HttpGet("my")]
    [ProducesResponseType(typeof(List<CatalogResponse>), 200)]
    [Authorize]
    public async Task<IActionResult> GetMine()
    {
        var userId = authService.GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var catalogs = await catalogService.GetByOwnerAsync(userId);

        var resp = catalogs.Select(c => new CatalogResponse(
            c.Id,
            c.Name,
            c.ParentCatalogId
        )).ToList();

        return Ok(resp);
    }

    /// <summary>
    ///     Обновить данные каталога.
    /// </summary>
    /// <param name="id">Идентификатор каталога.</param>
    /// <param name="req">Новые данные каталога: имя и родительский каталог.</param>
    /// <returns>
    ///     200 OK с обновлённым каталогом.
    ///     404 NotFound если каталог не найден.
    /// </returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(Catalog), 200)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCatalogRequest req)
    {
        var userId = authService.GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var userCatalog = await catalogService.GetByIdAsync(id);
        if (userCatalog?.OwnerId != userId) return Unauthorized("Нет доступа к чужому каталогу");

        var catalog = await catalogService.UpdateAsync(id, req.NewName, req.NewParentCatalogId);
        if (catalog == null) return NotFound();
        var response = new CatalogResponse(catalog.Id, catalog.Name, catalog.ParentCatalogId);
        return Ok(response);
    }

    /// <summary>
    ///     Удалить каталог.
    /// </summary>
    /// <param name="id">Идентификатор каталога.</param>
    /// <returns>
    ///     204 NoContent при успешном удалении.
    ///     404 NotFound если каталог не найден или содержит дочерние каталоги.
    /// </returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = authService.GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var userCatalog = await catalogService.GetByIdAsync(id);
        if (userCatalog?.OwnerId != userId) return Unauthorized("Нет доступа к чужому каталогу");

        var result = await catalogService.DeleteAsync(id);
        if (!result) return NotFound();
        return NoContent();
    }
}