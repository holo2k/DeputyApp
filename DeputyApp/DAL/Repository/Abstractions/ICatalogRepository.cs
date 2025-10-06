using DeputyApp.Entities;

namespace DeputyApp.DAL.Repository.Abstractions;

public interface ICatalogRepository
{
    Task<Catalog?> GetByIdAsync(Guid id);
    Task<List<Catalog>> GetByOwnerAsync(Guid ownerId);
    Task<List<Catalog>> GetAllAsync();
    Task<Catalog> AddAsync(Catalog catalog);
    Task<Catalog> UpdateAsync(Catalog catalog);
    Task DeleteAsync(Catalog catalog);
}