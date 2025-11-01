using Application.Dtos;
using Application.Services.Abstractions;
using DeputyApp.DAL.UnitOfWork;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    [RequestSizeLimit(50_000_000)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload(IFormFile? file, [FromForm] UploadFileRequest request)
    {
        var userId = _authService.GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized();

        var userCatalog = await _catalogService.GetByIdAsync(request.CatalogId);
        if (userCatalog?.OwnerId != userId)
            return Unauthorized("Нет доступа к чужому каталогу");

        if (file == null || file.Length == 0)
            return BadRequest("Файл обязателен");

        await using var s = file.OpenReadStream();
        var uploaded = await _docs.UploadAsync(file.FileName, s, file.ContentType, userId, request);

        return Ok(uploaded);
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

        var userDoc = await _unitOfWork.Documents.GetByIdAsync(documentId);
        if (userDoc?.UploadedById != userId) return Unauthorized("Нет доступа к чужому документу");

        var doc = _docs.UpdateStatusAsync(documentId, newStatus);
        return Ok(doc);
    }

    /// <summary>
    ///     Получить список документов, привязанных к конкретному каталогу.
    /// </summary>
    /// <param name="catalogId">Идентификатор каталога.</param>
    /// <returns>
    ///     200 OK с массивом документов, принадлежащих каталогу.
    /// </returns>
    [HttpGet("by-catalog/{catalogId}")]
    [ProducesResponseType(typeof(List<Document>), 200)]
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