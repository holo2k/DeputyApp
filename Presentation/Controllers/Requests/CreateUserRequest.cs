namespace Presentation.Controllers.Requests;

public record CreateUserRequest(
    string Email,
    string JobTitle,
    string FullName,
    string Password,
    Guid? DeputyId,
    string[]? Roles
);