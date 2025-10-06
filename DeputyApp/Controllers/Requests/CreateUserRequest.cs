namespace DeputyApp.Controllers.Requests;

/// <summary>
///     DTO для создания нового пользователя.
/// </summary>
public record CreateUserRequest(
    /// <summary>Email нового пользователя.</summary>
    string Email,
    /// <summary>Полное имя пользователя.</summary>
    string FullName,
    /// <summary>Пароль пользователя.</summary>
    string Password,
    /// <summary>Массив ролей пользователя (опционально).</summary>
    string[]? Roles
);