namespace Presentation.Controllers.Requests;

public record LoginRequest(
    string Email,
    string Password
);