using DeputyApp.DAL.Repository.Abstractions;
using DeputyApp.Entities;
using Microsoft.EntityFrameworkCore;

namespace DeputyApp.DAL.Repository.Implementations;

public class DocumentRepository : GenericRepository<Document>, IDocumentRepository
{
    public DocumentRepository(AppDbContext db) : base(db)
    {
    }

    public async Task<IEnumerable<Document>> GetByCatalogAsync(Guid catalogId)
    {
        return await _set.AsNoTracking().Where(d => d.CatalogId == catalogId).ToListAsync();
    }
}