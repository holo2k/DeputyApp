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
    ///     Создать новый личный каталог.
    /// </summary>
    /// <param name="req">Данные для создания каталога: имя и родительский каталог (необязательный).</param>
    /// <returns>
    ///     201 Created с информацией о созданном каталоге.
    ///     401 Unauthorized если пользователь не авторизован.
    /// </returns>
    [HttpPost("create-private")]
    [ProducesResponseType(typeof(CatalogResponse), 201)]
    [Authorize(Roles = $"{UserRoles.Deputy}, {UserRoles.Helper}")]
    public async Task<IActionResult> CreatePrivate([FromBody] CreateCatalogRequest req)
    {
        var userId = _authService.GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var catalog = await _catalogService.CreateAsync(req.Name, userId, req.ParentCatalogId);
        var response = new CatalogResponse(catalog.Id, catalog.Name, catalog.ParentCatalogId);
        return CreatedAtAction(nameof(GetById), new { id = catalog.Id }, response);
    }

    /// <summary>
    ///     Создать новый публичный каталог (только админ).
    /// </summary>
    /// <param name="req">Данные для создания каталога: имя и родительский каталог (необязательный).</param>
    /// <returns>
    ///     201 Created с информацией о созданном каталоге.
    ///     401 Unauthorized если пользователь не авторизован.
    /// </returns>
    [HttpPost("create-public")]
    [ProducesResponseType(typeof(CatalogResponse), 201)]
    [Authorize(Roles = UserRoles.Admin)]
    public async Task<IActionResult> CreatePublic([FromBody] CreateCatalogRequest req)
    {
        var catalog = await _catalogService.CreateAsync(req.Name, null, req.ParentCatalogId);
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
    [Authorize]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = _authService.GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var catalog = await _catalogService.GetByIdAsync(id);
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
    [HttpGet("mine")]
    [ProducesResponseType(typeof(List<CatalogResponse>), StatusCodes.Status200OK)]
    [Authorize]
    public async Task<IActionResult> GetMine()
    {
        var userId = _authService.GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized();

        return await GetCatalogsForOwnerAsync(userId);
    }

    /// TODO
    /// <summary>
    ///     Получить каталоги депутата (для помощников) (НЕ РАБОТАЕТ).
    /// </summary>
    /// <returns>
    ///     200 OK с массивом каталогов.
    ///     404 если у помощника нет депутата.
    /// </returns>
    [HttpGet("deputy")]
    [ProducesResponseType(typeof(List<CatalogResponse>), StatusCodes.Status200OK)]
    [Authorize(Roles = $"{UserRoles.Helper}, {UserRoles.Admin}")]
    public async Task<IActionResult> GetDeputyCatalogs()
    {
        //TODO связь помощника с депутатом
        var user = await _authService.GetCurrentUserAsync();
        if (user?.Deputy is null)
            return NotFound("У помощника нет назначенного депутата");

        return await GetCatalogsForOwnerAsync(user.Deputy.Id);
    }

    private async Task<IActionResult> GetCatalogsForOwnerAsync(Guid ownerId)
    {
        var catalogs = await _catalogService.GetByOwnerAsync(ownerId);

        var response = catalogs.Select(c => new CatalogResponse(
            c.Id,
            c.Name,
            c.ParentCatalogId
        )).ToList();

        return Ok(response);
    }


    /// <summary>
    ///     Получить все публичные каталоги.
    /// </summary>
    /// <returns>
    ///     200 OK с массивом каталогов.
    ///     401 Unauthorized если пользователь не авторизован.
    /// </returns>
    [HttpGet("public")]
    [ProducesResponseType(typeof(List<CatalogResponse>), 200)]
    [Authorize]
    public async Task<IActionResult> GetPublic()
    {
        var catalogs = await _catalogService.GetPublic();

        var resp = catalogs.Select(c => new CatalogResponse(
            c.Id,
            c.Name,
            c.ParentCatalogId
        )).ToList();

        return Ok(resp);
    }

    /// <summary>
    ///     Обновить данные своего каталога.
    /// </summary>
    /// <param name="id">Идентификатор каталога.</param>
    /// <param name="req">Новые данные каталога: имя и родительский каталог.</param>
    /// <returns>
    ///     200 OK с обновлённым каталогом.
    ///     404 NotFound если каталог не найден.
    /// </returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(Catalog), 200)]
    [Authorize]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCatalogRequest req)
    {
        var userId = _authService.GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var userCatalog = await _catalogService.GetByIdAsync(id);
        if (userCatalog?.OwnerId != userId) return Unauthorized("Нет доступа к чужому каталогу");

        var catalog = await _catalogService.UpdateAsync(id, req.NewName, req.NewParentCatalogId);
        if (catalog == null) return NotFound();
        var response = new CatalogResponse(catalog.Id, catalog.Name, catalog.ParentCatalogId);
        return Ok(response);
    }

    /// <summary>
    ///     Удалить свой каталог.
    /// </summary>
    /// <param name="id">Идентификатор каталога.</param>
    /// <returns>
    ///     204 NoContent при успешном удалении.
    ///     404 NotFound если каталог не найден или содержит дочерние каталоги.
    /// </returns>
    [HttpDelete("{id}")]
    [Authorize]
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