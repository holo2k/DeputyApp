using Application.Dtos;
using Application.Services.Abstractions;
using DeputyApp.DAL.UnitOfWork;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace Presentation.Controllers;

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
    /// <param name="request">
    ///     Идентификатор каталога, в который прикрепить документ,
    ///     статус (ToDo, InProgress, Done),
    ///     дата начала и дата окончания (необязательные).
    /// </param>
    /// <returns>
    ///     200 OK с информацией о загруженном документе.
    ///     400 BadRequest если файл не указан или пустой.
    ///     Ограничение размера файла: 50 МБ.
    /// </returns>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(Document), StatusCodes.Status200OK)]
    [Authorize]
    [RequestSizeLimit(50 * 1024 * 1024)] // 50 MB
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload([FromForm] IFormFile? file, [FromForm] UploadFileRequest request)
    {
        var user = await _authService.GetCurrentUserAsync();
        if (user == null)
            return Unauthorized();

        var roles = _authService.GetCurrentUserRoles();

        if (request.CatalogId == Guid.Empty)
            return BadRequest("CatalogId обязателен");

        var userCatalog = await _catalogService.GetByIdAsync(request.CatalogId);
        if (userCatalog == null)
            return NotFound("Каталог не найден");

        var ownerId = userCatalog.OwnerId;

        if (!((ownerId == null &&
               roles.Contains(UserRoles
                   .Deputy)) // Пользователь - депутат, загружает в публичные каталоги (для публичных ownerId = null)
              || ownerId == user.Id // Загружает в свой каталог
              || (roles.Contains(UserRoles.Helper) &&
                  user.Deputy!.Id == ownerId))) // Пользователь - помощник, загружает в каталог депутата
            return Forbid();

        if (file == null || file.Length == 0)
            return BadRequest("Файл обязателен");

        const long maxBytes = 50L * 1024 * 1024; // 50 MB
        if (file.Length > maxBytes)
            return BadRequest($"Максимальный размер файла {maxBytes} байт");

        try
        {
            await using var s = file.OpenReadStream();
            var uploaded = await _docs.UploadAsync(file.FileName, s, file.ContentType, user.Id, request);
            return Ok(uploaded);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Upload failed");
            return StatusCode(StatusCodes.Status500InternalServerError, "Ошибка загрузки файла");
        }
    }

    /// <summary>
    ///     Обновить статус документа
    /// </summary>
    [HttpPost("update")]
    [ProducesResponseType(typeof(Document), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Authorize]
    public async Task<IActionResult> UpdateStatus([FromQuery] Guid documentId, [FromQuery] DocumentStatus newStatus)
    {
        var userId = _authService.GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var user = await _authService.GetCurrentUserAsync();
        var roles = _authService.GetCurrentUserRoles();

        var doc = await _unitOfWork.Documents.GetByIdAsync(documentId);
        if (doc == null) return NotFound("Документ не найден");

        // Помощник: может менять только свои документы и документы депутата
        if (roles.Contains(UserRoles.Helper))
        {
            var isOwnDoc = doc.UploadedById == userId;
            var isDeputyDoc = doc.Catalog.OwnerId == user.Deputy.Id;

            if (!isOwnDoc && !isDeputyDoc)
                return Forbid();
        }

        // Депутат: может менять только документы, находящиеся в его каталоге или в общем
        if (roles.Contains(UserRoles.Deputy))
            if (doc.Catalog.OwnerId != userId && doc.Catalog.OwnerId != null)
                return Forbid();

        var updated = await _docs.UpdateStatusAsync(documentId, newStatus);
        return Ok(updated);
    }

    /// <summary>
    ///     Получить список документов по каталогу.
    /// </summary>
    [HttpGet("by-catalog/{catalogId:guid}")]
    [ProducesResponseType(typeof(List<Document>), StatusCodes.Status200OK)]
    [Authorize]
    public async Task<IActionResult> ByCatalog(Guid catalogId)
    {
        var user = await _authService.GetCurrentUserAsync();
        if (user == null)
            return Unauthorized();

        var roles = _authService.GetCurrentUserRoles();
        var isHelper = roles.Contains(UserRoles.Helper);
        var isDeputy = roles.Contains(UserRoles.Deputy);

        var catalog = await _catalogService.GetByIdAsync(catalogId);
        if (catalog == null)
            return NotFound("Каталог не найден");

        var canAccess =
            // общие каталоги
            catalog.OwnerId == null ||
            // владелец каталога всегда имеет доступ
            catalog.OwnerId == user.Id ||
            // помощник видит документы в каталоге своего депутата
            (isHelper && user.Deputy != null && catalog.OwnerId == user.Deputy.Id);

        if (!canAccess)
            return Forbid();

        var docs = await _docs.GetByCatalogAsync(catalogId);
        return Ok(docs);
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