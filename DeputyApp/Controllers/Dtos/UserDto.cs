namespace DeputyApp.Controllers.Dtos;

/// <summary>
///     DTO для возврата информации о пользователе.
/// </summary>
public record UserDto(
    /// <summary>Идентификатор пользователя.</summary>
    Guid Id,
    /// <summary>Email пользователя.</summary>
    string Email,
    /// <summary>Полное имя пользователя.</summary>
    string FullName,
    /// <summary>Список ролей пользователя.</summary>
    string[] Roles
);