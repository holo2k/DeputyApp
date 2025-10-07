﻿using Domain.Entities;

namespace Application.Dtos;

public record UserDto(
    Guid Id,
    string Email,
    string FullName,
    string JobTitle,
    ICollection<Post> Posts,
    ICollection<Event> Events,
    ICollection<Document> Documents,
    string[] Roles
);