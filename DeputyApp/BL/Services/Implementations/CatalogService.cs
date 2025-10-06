using DeputyApp.BL.Services.Abstractions;
using DeputyApp.DAL;
using DeputyApp.DAL.Repository.Abstractions;
using DeputyApp.Entities;

namespace DeputyApp.BL.Services.Implementations;

public class CatalogService : ICatalogService
{
    private readonly AppDbContext _db;
    private readonly ICatalogRepository _repo;

    public CatalogService(ICatalogRepository repo, AppDbContext db)
    {
        _repo = repo;
        _db = db;
    }

    public async Task<Catalog> CreateAsync(string name, Guid? ownerId, Guid? parentCatalogId = null)
    {
        if (parentCatalogId.HasValue)
        {
            var parent = await _repo.GetByIdAsync(parentCatalogId.Value);
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

        await _repo.AddAsync(catalog);
        await _db.SaveChangesAsync();
        return catalog;
    }

    public async Task<Catalog?> GetByIdAsync(Guid id)
    {
        return await _repo.GetByIdAsync(id);
    }

    public async Task<List<Catalog>> GetByOwnerAsync(Guid ownerId)
    {
        return await _repo.GetByOwnerAsync(ownerId);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var catalog = await _repo.GetByIdAsync(id);
        if (catalog == null) return false;

        if (catalog.Children.Count > 0)
            throw new InvalidOperationException("Cannot delete catalog with child catalogs");

        await _repo.DeleteAsync(catalog);
        await _db.SaveChangesAsync();
        return true;
    }


    public async Task<Catalog?> UpdateAsync(Guid id, string newName, Guid? newParentCatalogId = null)
    {
        var catalog = await _repo.GetByIdAsync(id);
        if (catalog == null) return null;

        catalog.Name = newName;
        catalog.ParentCatalogId = newParentCatalogId;

        await _repo.UpdateAsync(catalog);
        await _db.SaveChangesAsync();
        return catalog;
    }
}