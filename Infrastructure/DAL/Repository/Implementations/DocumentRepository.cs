using Domain.Entities;
using Infrastructure.DAL.Repository.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DAL.Repository.Implementations;

public class DocumentRepository(AppDbContext db) : GenericRepository<Document>(db), IDocumentRepository
{
    public async Task<IEnumerable<Document>> GetByCatalogAsync(Guid catalogId)
    {
        return await _set.AsNoTracking().Where(d => d.CatalogId == catalogId).ToListAsync();
    }
}