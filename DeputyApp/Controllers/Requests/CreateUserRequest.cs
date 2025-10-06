namespace DeputyApp.Controllers.Requests;

public record CreateUserRequest(string Email, string FullName, string Password, string[]? Roles);