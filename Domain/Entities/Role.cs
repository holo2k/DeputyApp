using System.Text.Json.Serialization;

namespace Domain.Entities;

public class Role
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty; // e.g. "Deputy", "Helper", "Admin"
    [JsonIgnore]
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}