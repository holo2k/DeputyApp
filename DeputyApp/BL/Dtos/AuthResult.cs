using DeputyApp.Entities;

namespace DeputyApp.BL.Dtos;

public record AuthResult(string Token, User User);