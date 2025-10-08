using Application.Services.Abstractions;
using Domain.Entities;
using Infrastructure.DAL;
using Infrastructure.DAL.Repository.Abstractions;

namespace Application.Services.Implementations;

public class CatalogService : ICatalogService
{
    private readonly ICatalogRepository _catalogRepository;
    private readonly AppDbContext _db;

    public CatalogService(ICatalogRepository catalogRepository, AppDbContext db)
    {
        _catalogRepository = catalogRepository;
        _db = db;
    }

    public async Task<Catalog> CreateAsync(string name, Guid? ownerId, Guid? parentCatalogId = null)
    {
        if (parentCatalogId.HasValue)
        {
            var parent = await _catalogRepository.GetByIdAsync(parentCatalogId.Value);
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

        await _catalogRepository.AddAsync(catalog);
        await _db.SaveChangesAsync();
        return catalog;
    }

    public async Task<Catalog?> GetByIdAsync(Guid id)
    {
        return await _catalogRepository.GetByIdAsync(id);
    }

    public async Task<List<Catalog>> GetByOwnerAsync(Guid ownerId)
    {
        return await _catalogRepository.GetByOwnerAsync(ownerId);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var catalog = await _catalogRepository.GetByIdAsync(id);
        if (catalog == null) return false;

        if (catalog.Children.Count > 0)
            throw new InvalidOperationException("Cannot delete catalog with child catalogs");

        await _catalogRepository.DeleteAsync(catalog);
        await _db.SaveChangesAsync();
        return true;
    }


    public async Task<Catalog?> UpdateAsync(Guid id, string newName, Guid? newParentCatalogId = null)
    {
        var catalog = await _catalogRepository.GetByIdAsync(id);
        if (catalog == null) return null;

        catalog.Name = newName;
        catalog.ParentCatalogId = newParentCatalogId;

        await _catalogRepository.UpdateAsync(catalog);
        await _db.SaveChangesAsync();
        return catalog;
    }
}