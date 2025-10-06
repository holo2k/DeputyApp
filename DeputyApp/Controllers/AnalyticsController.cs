using DeputyApp.BL.Services.Abstractions;
using DeputyApp.Controllers.Requests;
using Microsoft.AspNetCore.Mvc;

namespace DeputyApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analytics;

    public AnalyticsController(IAnalyticsService analytics)
    {
        _analytics = analytics;
    }

    /// <summary>Track event (open API from clients).</summary>
    [HttpPost("track")]
    public async Task<IActionResult> Track([FromBody] TrackRequest req)
    {
        await _analytics.TrackAsync(req.EventType, req.UserId, req.PayloadJson);
        return Accepted();
    }

    /// <summary>Query events (admin).</summary>
    [HttpGet("query")]
    public async Task<IActionResult> Query([FromQuery] DateTimeOffset from, [FromQuery] DateTimeOffset to,
        [FromQuery] string? eventType)
    {
        var list = await _analytics.QueryAsync(from, to, eventType);
        return Ok(list);
    }
}