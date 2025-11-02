using Application.Services.Abstractions;
using Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

[ApiController]
[Route("api/files")]
public class FilesController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IDocumentService _documentService;
    private readonly IFileStorage _storage;

    public FilesController(IFileStorage storage, IDocumentService documentService, IAuthService authService)
    {
        _storage = storage;
        _documentService = documentService;
        _authService = authService;
    }

    /// <summary>
    ///     Получить временную ссылку на скачивание файла.
    /// </summary>
    /// <param name="fileName">Имя файла (уникальное в хранилище).</param>
    /// <returns>Временная ссылка (действует 5 минут).</returns>
    [HttpGet("{fileName}")]
    [Authorize]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFileUrl(string fileName)
    {
        var userId = _authService.GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(fileName))
            return BadRequest("fileName не задан");

        var user = await _authService.GetCurrentUserAsync();
        var roles = _authService.GetCurrentUserRoles();

        var doc = await _documentService.GetByFileNameAsync(fileName);
        if (doc == null)
            return NotFound("Документ не найден");

        var ownerId = doc.Catalog?.OwnerId;

        var isHelper = roles.Contains(UserRoles.Helper);
        var isOwn = doc.UploadedById == userId;
        var isDeputyFile = isHelper && user?.Deputy != null && doc.UploadedById == user.Deputy.Id;

        // Доступ только если файл общий (ownerId == null) или свой / депутата
        if (ownerId != null && !isOwn && !isDeputyFile)
            return Forbid();

        try
        {
            var url = await _storage.GetPresignedUrlAsync(doc.FileNameEncoded, TimeSpan.FromMinutes(5));
            return Ok(new { url });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}