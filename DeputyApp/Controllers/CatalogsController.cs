using DeputyApp.BL.Services.Abstractions;
using DeputyApp.Controllers.Requests;
using Microsoft.AspNetCore.Mvc;

namespace DeputyApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CatalogsController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICatalogService _catalogService;

    public CatalogsController(ICatalogService catalogService, IAuthService authService)
    {
        _catalogService = catalogService;
        _authService = authService;
    }

    /// <summary>
    ///     Создать новый каталог.
    /// </summary>
    /// <param name="req">Данные для создания каталога: имя и родительский каталог (необязательный).</param>
    /// <returns>
    ///     201 Created с информацией о созданном каталоге.
    ///     401 Unauthorized если пользователь не авторизован.
    /// </returns>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCatalogRequest req)
    {
        var userId = _authService.GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var catalog = await _catalogService.CreateAsync(req.Name, userId, req.ParentCatalogId);
        return CreatedAtAction(nameof(GetById), new { id = catalog.Id }, catalog);
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
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = _authService.GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var catalog = await _catalogService.GetByIdAsync(id);
        if (catalog == null) return NotFound();
        return Ok(catalog);
    }

    /// <summary>
    ///     Получить все каталоги, принадлежащие текущему пользователю.
    /// </summary>
    /// <returns>
    ///     200 OK с массивом каталогов.
    ///     401 Unauthorized если пользователь не авторизован.
    /// </returns>
    [HttpGet("my")]
    public async Task<IActionResult> GetMine()
    {
        var userId = _authService.GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var catalogs = await _catalogService.GetByOwnerAsync(userId!);
        return Ok(catalogs);
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
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCatalogRequest req)
    {
        var userId = _authService.GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var userCatalog = await _catalogService.GetByIdAsync(id);
        if (userCatalog?.OwnerId != userId) return Unauthorized("Нет доступа к чужому каталогу");

        var catalog = await _catalogService.UpdateAsync(id, req.NewName, req.NewParentCatalogId);
        if (catalog == null) return NotFound();
        return Ok(catalog);
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
        var userId = _authService.GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var userCatalog = await _catalogService.GetByIdAsync(id);
        if (userCatalog?.OwnerId != userId) return Unauthorized("Нет доступа к чужому каталогу");

        var result = await _catalogService.DeleteAsync(id);
        if (!result) return NotFound();
        return NoContent();
    }
}