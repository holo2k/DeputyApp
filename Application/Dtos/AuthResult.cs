using Domain.Entities;

namespace Application.Dtos;

public record AuthResult(
    string Token,
    User User
);