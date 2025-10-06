namespace DeputyApp.Controllers.Requests;

/// <summary>
///     DTO для авторизации пользователя.
/// </summary>
public record LoginRequest(
    /// <summary>Email пользователя.</summary>
    string Email,
    /// <summary>Пароль пользователя.</summary>
    string Password
);