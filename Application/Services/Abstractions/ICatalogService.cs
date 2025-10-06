using Domain.Entities;

namespace Application.Services.Abstractions;

public interface ICatalogService
{
    Task<Catalog> CreateAsync(string name, Guid? ownerId, Guid? parentId = null);
    Task<Catalog?> GetByIdAsync(Guid id);
    Task<List<Catalog>> GetByOwnerAsync(Guid ownerId);
    Task<Catalog?> UpdateAsync(Guid id, string newName, Guid? newParentCatalogId = null);
    Task<bool> DeleteAsync(Guid id);
}