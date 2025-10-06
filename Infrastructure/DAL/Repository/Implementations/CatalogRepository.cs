using Domain.Entities;
using Infrastructure.DAL.Repository.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DAL.Repository.Implementations;

public class CatalogRepository(AppDbContext db) : ICatalogRepository
{
    public async Task<Catalog?> GetByIdAsync(Guid id)
    {
        return await db.Catalogs
            .Include(c => c.Children)
            .Include(c => c.Documents)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<List<Catalog>> GetByOwnerAsync(Guid ownerId)
    {
        return await db.Catalogs
            .Where(c => c.OwnerId == ownerId)
            .Include(c => c.Children)
            .Include(c => c.Documents)
            .ToListAsync();
    }

    public async Task<List<Catalog>> GetAllAsync()
    {
        return await db.Catalogs
            .Include(c => c.Children)
            .Include(c => c.Documents)
            .ToListAsync();
    }

    public async Task<Catalog> AddAsync(Catalog catalog)
    {
        db.Catalogs.Add(catalog);
        await db.SaveChangesAsync();
        return catalog;
    }

    public async Task<Catalog> UpdateAsync(Catalog catalog)
    {
        db.Catalogs.Update(catalog);
        await db.SaveChangesAsync();
        return catalog;
    }

    public async Task DeleteAsync(Catalog catalog)
    {
        db.Catalogs.Remove(catalog);
        await db.SaveChangesAsync();
    }
}