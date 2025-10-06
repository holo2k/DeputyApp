using DeputyApp.BL.Services.Abstractions;
using DeputyApp.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeputyApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly IEventService _events;

    public EventsController(IEventService events)
    {
        _events = events;
    }

    /// <summary>Create event (admin).</summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] Event ev)
    {
        var created = await _events.CreateAsync(ev);
        return CreatedAtAction(nameof(GetUpcoming), new { id = created.Id }, created);
    }

    /// <summary>Get upcoming events in range.</summary>
    [HttpGet("upcoming")]
    public async Task<IActionResult> GetUpcoming([FromQuery] DateTimeOffset from, [FromQuery] DateTimeOffset to)
    {
        var list = await _events.GetUpcomingAsync(from, to);
        return Ok(list);
    }

    /// <summary>Delete event (admin).</summary>
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _events.DeleteAsync(id);
        return NoContent();
    }
}