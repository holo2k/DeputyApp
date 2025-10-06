using DeputyApp.BL.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeputyApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _docs;

    public DocumentsController(IDocumentService docs)
    {
        _docs = docs;
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
    public async Task<IActionResult> Upload(IFormFile file, [FromQuery] Guid? catalogId)
    {
        if (file == null || file.Length == 0) return BadRequest("Файл обязателен");
        using var s = file.OpenReadStream();
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
        var list = await _docs.GetByCatalogAsync(catalogId);
        return Ok(list);
    }

    /// <summary>
    ///     Удалить документ по его идентификатору (только для администраторов).
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
        await _docs.DeleteAsync(id);
        return NoContent();
    }
}