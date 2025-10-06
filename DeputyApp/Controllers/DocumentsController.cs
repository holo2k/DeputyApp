using DeputyApp.BL.Services.Abstractions;
using DeputyApp.DAL.UnitOfWork;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeputyApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICatalogService _catalogService;
    private readonly IDocumentService _docs;
    private readonly IUnitOfWork _unitOfWork;

    public DocumentsController(IDocumentService docs, IAuthService authService, ICatalogService catalogService,
        IUnitOfWork unitOfWork)
    {
        _docs = docs;
        _authService = authService;
        _catalogService = catalogService;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    ///     Загрузить документ на сервер.
    /// </summary>
    /// <param name="file">Файл для загрузки.</param>
    /// <param name="catalogId">Идентификатор каталога, в который прикрепить документ (необязательный).</param>
    /// <returns>
    ///     200 OK с информацией о загруженном документе.
    ///     400 BadRequest если файл не указан или пустой.
    ///     Ограничение размера файла: 50 МБ.
    /// </returns>
    [HttpPost("upload")]
    [Authorize]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> Upload(IFormFile? file, [FromQuery] Guid catalogId)
    {
        var userId = _authService.GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var userCatalog = await _catalogService.GetByIdAsync(catalogId);
        if (userCatalog?.OwnerId != userId) return Unauthorized("Нет доступа к чужому каталогу");

        if (file == null || file.Length == 0) return BadRequest("Файл обязателен");
        await using var s = file.OpenReadStream();
        var uploaded = await _docs.UploadAsync(file.FileName, s, file.ContentType, null, catalogId);
        return Ok(uploaded);
    }

    /// <summary>
    ///     Получить список документов, привязанных к конкретному каталогу.
    /// </summary>
    /// <param name="catalogId">Идентификатор каталога.</param>
    /// <returns>
    ///     200 OK с массивом документов, принадлежащих каталогу.
    /// </returns>
    [HttpGet("by-catalog/{catalogId}")]
    public async Task<IActionResult> ByCatalog(Guid catalogId)
    {
        var userId = _authService.GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var userCatalog = await _catalogService.GetByIdAsync(catalogId);
        if (userCatalog?.OwnerId != userId) return Unauthorized("Нет доступа к чужому каталогу");

        var list = await _docs.GetByCatalogAsync(catalogId);
        return Ok(list);
    }

    /// <summary>
    ///     Удалить документ по его идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор документа.</param>
    /// <returns>
    ///     204 NoContent при успешном удалении.
    ///     404 NotFound если документ не найден.
    /// </returns>
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = _authService.GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var userDoc = await _unitOfWork.Documents.GetByIdAsync(id);
        if (userDoc?.UploadedById != userId) return Unauthorized("Нет доступа к чужому документу");

        await _docs.DeleteAsync(id);
        return NoContent();
    }
}