using DeputyApp.BL.Services.Abstractions;
using DeputyApp.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeputyApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PostsController : ControllerBase
{
    private readonly IPostService _posts;

    public PostsController(IPostService posts)
    {
        _posts = posts;
    }

    /// <summary>Get published posts.</summary>
    [HttpGet]
    public async Task<IActionResult> GetPublished([FromQuery] int skip = 0, [FromQuery] int take = 20)
    {
        var list = await _posts.GetPublishedAsync(skip, take);
        return Ok(list);
    }

    /// <summary>Create post (admin).</summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] Post post)
    {
        var created = await _posts.CreateAsync(post);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Get post by id.</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var p = await _posts.GetByIdAsync(id);
        if (p == null) return NotFound();
        return Ok(p);
    }

    /// <summary>Publish post (admin).</summary>
    [HttpPost("{id}/publish")]
    [Authorize]
    public async Task<IActionResult> Publish(Guid id)
    {
        await _posts.PublishAsync(id);
        return NoContent();
    }

    /// <summary>Delete post (admin).</summary>
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _posts.DeleteAsync(id);
        return NoContent();
    }
}