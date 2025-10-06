using DeputyApp.BL.Services.Abstractions;
using DeputyApp.Entities;
using Microsoft.AspNetCore.Mvc;

namespace DeputyApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FeedbackController : ControllerBase
{
    private readonly IFeedbackService _feedback;

    public FeedbackController(IFeedbackService feedback)
    {
        _feedback = feedback;
    }

    /// <summary>Send feedback (open).</summary>
    [HttpPost]
    public async Task<IActionResult> Send([FromBody] Feedback fb)
    {
        var f = await _feedback.CreateAsync(fb);
        return CreatedAtAction(nameof(Send), new { id = f.Id }, f);
    }

    /// <summary>Get recent feedbacks (admin).</summary>
    [HttpGet("recent")]
    public async Task<IActionResult> Recent([FromQuery] int days = 30)
    {
        var list = await _feedback.RecentAsync(days);
        return Ok(list);
    }
}