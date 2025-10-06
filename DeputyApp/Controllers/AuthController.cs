using Application.Dtos;
using Application.Services.Abstractions;
using DeputyApp.Controllers.Requests;
using Microsoft.AspNetCore.Mvc;

namespace DeputyApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService auth) : ControllerBase
{
    /// <summary>
    ///     Аутентификация пользователя и получение JWT токена.
    /// </summary>
    /// <param name="req">Объект запроса с Email и паролем пользователя.</param>
    /// <returns>
    ///     200 OK с JWT токеном при успешной аутентификации.
    ///     401 Unauthorized если Email или пароль неверные.
    /// </returns>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var res = await auth.AuthenticateAsync(req.Email, req.Password);
        if (res == null) return Unauthorized();
        return Ok(res);
    }

    /// <summary>
    ///     Создание нового пользователя.
    /// </summary>
    /// <param name="req">Объект запроса с Email, полным именем, паролем и ролями пользователя.</param>
    /// <returns>
    ///     201 Created с информацией о созданном пользователе.
    ///     Возвращаются роли в виде массива строк.
    /// </returns>
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest req)
    {
        var currentUser = await auth.GetCurrentUser();
        if (currentUser == null || currentUser.UserRoles.All(x => x.Role.Name != "Admin")) return Unauthorized();

        var user = await auth.CreateUserAsync(req.Email, req.FullName, req.JobTitle, req.Password,
            req.Roles ?? new[] { "" });
        var dto = new UserDto(
            user.Id,
            user.Email,
            user.FullName,
            user.UserRoles.Select(ur => ur.Role.Name).ToArray()
        );
        return CreatedAtAction(nameof(Get), new { id = user.Id }, dto);
    }

    /// <summary>
    ///     Получить информацию о пользователе по его уникальному идентификатору.
    /// </summary>
    /// <param name="id">Уникальный идентификатор пользователя.</param>
    /// <returns>
    ///     200 OK с информацией о пользователе, если найден.
    ///     404 Not Found если пользователь не найден.
    /// </returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> Get([FromRoute] Guid id)
    {
        var user = await auth.GetUserById(id);
        if (user == null) return NotFound();
        return Ok(user);
    }

    /// <summary>
    ///     Получить информацию о текущем аутентифицированном пользователе.
    /// </summary>
    /// <returns>
    ///     200 OK с информацией о текущем пользователе.
    ///     401 Unauthorized если пользователь не аутентифицирован.
    /// </returns>
    [HttpGet("current")]
    public async Task<IActionResult> Get()
    {
        var user = await auth.GetCurrentUser();
        if (user == null) return Unauthorized();
        return Ok(user);
    }
}