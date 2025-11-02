using Domain.Entities;

namespace Application.Dtos;

public class UpdateUserRequest
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public Guid? DeputyId { get; set; } = null;
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}