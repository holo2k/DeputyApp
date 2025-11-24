using Application.Dtos;
using Application.Services.Abstractions;
using Domain.Constants;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Controllers.Requests;

namespace Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly IUserService _userService;

    public AuthController(IAuthService auth, IUserService userService)
    {
        _auth = auth;
        _userService = userService;
    }

    /// <summary>
    ///     Аутентификация пользователя и получение JWT токена.
    /// </summary>
    /// <param name="req">Объект запроса с Email и паролем пользователя.</param>
    /// <returns>
    ///     200 OK с JWT токеном при успешной аутентификации.
    ///     401 Unauthorized если Email или пароль неверные.
    /// </returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResult), 200)]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var res = await _auth.AuthenticateAsync(req.Email, req.Password);
        if (res == null) return Unauthorized();
        return Ok(res);
    }

    /// <summary>
    ///     Создание нового пользователя. (Роли - Admin, Deputy, Helper)
    /// </summary>
    /// <param name="req">
    ///     Объект запроса с Email, полным именем, паролем и ролями пользователя, а также ID привязанного
    ///     депутата, если пользователь помощник.
    /// </param>
    /// <returns>
    ///     201 Created с информацией о созданном пользователе.
    ///     Возвращаются роли в виде массива строк.
    /// </returns>
    [HttpPost("create")]
    [ProducesResponseType(typeof(User), 200)]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest req)
    {
        if (req.Roles is null) return ValidationProblem("Не заданы роли пользователя");
        if (!req.Roles.Any(r => r is UserRoles.Helper or UserRoles.Admin or UserRoles.Deputy))
            return ValidationProblem("Не заданы корректные роли пользователя");
        if (req.Roles.Contains(UserRoles.Helper) && req.DeputyId is null)
            return ValidationProblem("Не задано айди депутата помощника");

        var user = await _auth.CreateUserAsync(req.Email, req.FullName, req.JobTitle, req.Password, req.DeputyId,
            req.Roles);
        var dto = new UserDto(user.Id, user.Email, user.FullName, user.JobTitle, user.Posts, user.EventsOrganized,
            user.Documents,
            user.Tasks,
            user.Deputy,
            user.UserRoles.Select(r => r.Role.Name).ToArray());
        return CreatedAtAction(nameof(Get), new { id = user.Id }, dto);
    }

    /// <summary>
    ///     Обновление пользователя.
    /// </summary>
    /// <param name="req">Объект запроса с Email, полным именем, паролем и ролями пользователя.</param>
    /// <returns>
    ///     201 Created с информацией о созданном пользователе.
    ///     Возвращаются роли в виде массива строк.
    /// </returns>
    [HttpPost("update")]
    [ProducesResponseType(typeof(User), 200)]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update([FromBody] UpdateUserRequest req)
    {
        var user = await _userService.UpdateUser(req);
        var dto = new UserDto(user.Id, user.Email, user.FullName, user.JobTitle, user.Posts, user.EventsOrganized,
            user.Documents,
            user.Tasks,
            user.Deputy,
            user.UserRoles.Select(r => r.Role.Name).ToArray());
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
    [ProducesResponseType(typeof(User), 200)]
    public async Task<IActionResult> Get([FromRoute] Guid id)
    {
        var user = await _auth.GetUserById(id);
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
    [ProducesResponseType(typeof(UserDto), 200)]
    public async Task<IActionResult> Get()
    {
        var user = await _auth.GetCurrentUserAsync();
        if (user == null) return Unauthorized();
        return Ok(user);
    }
}