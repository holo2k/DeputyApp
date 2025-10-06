namespace Presentation.Controllers.Requests;

public record CreateUserRequest(
    string Email,
    string JobTitle,
    string FullName,
    string Password,
    string[]? Roles
);