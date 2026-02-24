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
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [HttpGet("{fileName}")]
    [Authorize]
    public async Task<IActionResult> GetFile(string fileName, [FromServices] IHttpClientFactory httpClientFactory)
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

        // 1) Доступ только если файл общий (ownerId == null) или свой / депутата
        if (ownerId != null && !isOwn && !isDeputyFile)
            return Forbid();

        // 2) получаем presigned URL (внутренне — ваш _presignClient)
        var presigned = await _storage.GetPresignedUrlAsync(doc.FileNameEncoded, TimeSpan.FromMinutes(1));
        if (string.IsNullOrEmpty(presigned))
            return NotFound();

        var http = httpClientFactory.CreateClient();
        // просим заголовки и тело без полной загрузки в память
        using var req = new HttpRequestMessage(HttpMethod.Get, presigned);
        var resp = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, HttpContext.RequestAborted);
        if (!resp.IsSuccessStatusCode)
            return StatusCode((int)resp.StatusCode);

        var remoteStream = await resp.Content.ReadAsStreamAsync(HttpContext.RequestAborted);

        // установить заголовки ответа
        var contentType = resp.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
        if (resp.Content.Headers.ContentLength.HasValue)
            Response.ContentLength = resp.Content.Headers.ContentLength.Value;
        Response.ContentType = contentType;
        Response.Headers["Content-Disposition"] = $"attachment; filename=\"{doc.FileName ?? fileName}\"";

        // освободить ресурсы после завершения отклика
        HttpContext.Response.OnCompleted(() =>
        {
            remoteStream.Dispose();
            resp.Dispose();
            return Task.CompletedTask;
        });

        // FileStreamResult отправит поток напрямую клиенту
        return File(remoteStream, contentType);
    }
}