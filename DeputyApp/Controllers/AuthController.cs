using DeputyApp.BL.Services.Abstractions;
using DeputyApp.Controllers.Requests;
using Microsoft.AspNetCore.Mvc;
using LoginRequest = Microsoft.AspNetCore.Identity.Data.LoginRequest;

namespace DeputyApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth)
    {
        _auth = auth;
    }

    /// <summary>
    ///     Authenticate user returns access and refresh tokens.
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var res = await _auth.AuthenticateAsync(req.Email, req.Password);
        if (res == null) return Unauthorized();
        return Ok(res);
    }

    /// <summary>
    ///     Create user (admin operation).
    /// </summary>
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest req)
    {
        var user = await _auth.CreateUserAsync(req.Email, req.FullName, req.Password, req.Roles ?? new string[0]);
        return CreatedAtAction(nameof(Get), new { id = user.Id }, user);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get([FromRoute] Guid id)
    {
        var user = await _auth.GetUserById(id);
        return Ok(user);
    }

    [HttpGet("current")]
    public async Task<IActionResult> Get()
    {
        var user = await _auth.GetCurrentUser();
        return Ok(user);
    }
}