using Domain.Entities;
using Infrastructure.DAL.Repository.Abstractions;
using Microsoft.EntityFrameworkCore;
using Task = System.Threading.Tasks.Task;

namespace Infrastructure.DAL.Repository.Implementations;

public class CatalogRepository : ICatalogRepository
{
    private readonly AppDbContext _db;

    public CatalogRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Catalog?> GetByIdAsync(Guid id)
    {
        return await _db.Catalogs
            .Include(c => c.Children)
            .Include(c => c.Documents)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<List<Catalog>> GetByOwnerAsync(Guid ownerId)
    {
        return await _db.Catalogs
            .Where(c => c.OwnerId == ownerId)
            .Include(c => c.Children)
            .Include(c => c.Documents)
            .ToListAsync();
    }

    public async Task<List<Catalog>> GetAllAsync()
    {
        return await _db.Catalogs
            .Include(c => c.Children)
            .Include(c => c.Documents)
            .ToListAsync();
    }

    public async Task<Catalog> AddAsync(Catalog catalog)
    {
        _db.Catalogs.Add(catalog);
        await _db.SaveChangesAsync();
        return catalog;
    }

    public async Task<Catalog> UpdateAsync(Catalog catalog)
    {
        _db.Catalogs.Update(catalog);
        await _db.SaveChangesAsync();
        return catalog;
    }

    public async Task DeleteAsync(Catalog catalog)
    {
        _db.Catalogs.Remove(catalog);
        await _db.SaveChangesAsync();
    }
}