using Application.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

[ApiController]
[Route("api/files")]
public class FilesController : ControllerBase
{
    private readonly IFileStorage _storage;

    public FilesController(IFileStorage storage)
    {
        _storage = storage;
    }

    /// <summary>
    ///     Получить временную ссылку на скачивание файла
    /// </summary>
    [HttpGet("{fileName}")]
    public async Task<IActionResult> GetFileUrl(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return BadRequest("fileName не задан");

        try
        {
            // Генерируем ссылку на 5 минут
            var url = await _storage.GetPresignedUrlAsync(fileName, TimeSpan.FromMinutes(5));
            return Ok(new { url });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}