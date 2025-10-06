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

    /// <summary>Upload document (authorized).</summary>
    [HttpPost("upload")]
    [Authorize]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> Upload(IFormFile file, [FromQuery] Guid? catalogId)
    {
        if (file == null || file.Length == 0) return BadRequest("file required");
        using var s = file.OpenReadStream();
        var uploaded = await _docs.UploadAsync(file.FileName, s, file.ContentType, null, catalogId);
        return Ok(uploaded);
    }

    /// <summary>List documents by catalog.</summary>
    [HttpGet("by-catalog/{catalogId}")]
    public async Task<IActionResult> ByCatalog(Guid catalogId)
    {
        var list = await _docs.GetByCatalogAsync(catalogId);
        return Ok(list);
    }

    /// <summary>Delete document (admin).</summary>
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _docs.DeleteAsync(id);
        return NoContent();
    }
}