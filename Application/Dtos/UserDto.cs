﻿namespace Application.Dtos;

public record UserDto(
    Guid Id,
    string Email,
    string FullName,
    string[] Roles
);