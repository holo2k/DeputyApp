using Application.Dtos;
using Application.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatusController(IStatusService statusService) : ControllerBase
{
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] StatusRequest request)
    {
        return Ok(await statusService.CreateAsync(request));
    }

    [HttpGet("get-all")]
    public async Task<IActionResult> GetAll()
    {
        var statuses = await statusService.GetAllAsync();
        return Ok(statuses);
    }

    [HttpGet("get-by/{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var status = await statusService.GetByIdAsync(id);
        return Ok(status);
    }

    [HttpPut("update/{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] StatusRequest request)
    {
        var updatedId = await statusService.UpdateAsync(id, request.Name);
        return Ok(updatedId);
    }

    [HttpDelete("delete/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] string defaultStatus)
    {
        return Ok(statusService.DeleteAsync(id, defaultStatus));
    }
}
