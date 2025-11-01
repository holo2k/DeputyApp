using Application.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

[ApiController]
[Route("api/files")]
public class FilesController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly IFileStorage _storage;

    public FilesController(IFileStorage storage, IDocumentService documentService)
    {
        _storage = storage;
        _documentService = documentService;
    }

    /// <summary>
    ///     Получить временную ссылку на скачивание файла
    /// </summary>
    [HttpGet("{fileName}")]
    public async Task<IActionResult> GetFileUrl(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return BadRequest("fileName не задан");

        var doc = await _documentService.GetByFileNameAsync(fileName);

        try
        {
            // Генерируем ссылку на 5 минут
            var url = await _storage.GetPresignedUrlAsync(doc.FileNameEncoded, TimeSpan.FromMinutes(5));
            return Ok(new { url });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}