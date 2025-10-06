﻿namespace DeputyApp.Entities;

public class Role
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty; // e.g. "Deputy", "Staff", "Admin"
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}