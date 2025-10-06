using Application.Services.Abstractions;
using Domain.Entities;
using Infrastructure.DAL;
using Infrastructure.DAL.Repository.Abstractions;

namespace Application.Services.Implementations;

public class CatalogService(ICatalogRepository repo, AppDbContext db) : ICatalogService
{
    public async Task<Catalog> CreateAsync(string name, Guid? ownerId, Guid? parentCatalogId = null)
    {
        if (parentCatalogId.HasValue)
        {
            var parent = await repo.GetByIdAsync(parentCatalogId.Value);
            if (parent == null)
                throw new InvalidOperationException("Parent catalog not found");
        }

        var catalog = new Catalog
        {
            Id = Guid.NewGuid(),
            Name = name,
            OwnerId = ownerId,
            ParentCatalogId = parentCatalogId
        };

        await repo.AddAsync(catalog);
        await db.SaveChangesAsync();
        return catalog;
    }

    public async Task<Catalog?> GetByIdAsync(Guid id)
    {
        return await repo.GetByIdAsync(id);
    }

    public async Task<List<Catalog>> GetByOwnerAsync(Guid ownerId)
    {
        return await repo.GetByOwnerAsync(ownerId);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var catalog = await repo.GetByIdAsync(id);
        if (catalog == null) return false;

        if (catalog.Children.Count > 0)
            throw new InvalidOperationException("Cannot delete catalog with child catalogs");

        await repo.DeleteAsync(catalog);
        await db.SaveChangesAsync();
        return true;
    }


    public async Task<Catalog?> UpdateAsync(Guid id, string newName, Guid? newParentCatalogId = null)
    {
        var catalog = await repo.GetByIdAsync(id);
        if (catalog == null) return null;

        catalog.Name = newName;
        catalog.ParentCatalogId = newParentCatalogId;

        await repo.UpdateAsync(catalog);
        await db.SaveChangesAsync();
        return catalog;
    }
}