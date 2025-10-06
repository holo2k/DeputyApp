using DeputyApp.Entities;

namespace DeputyApp.Controllers.Dtos;

/// <summary>
///     DTO для результата авторизации.
/// </summary>
public record AuthResult(
    /// <summary>JWT-токен пользователя.</summary>
    string Token,
    /// <summary>Информация о пользователе.</summary>
    User User
);