using Domain.Entities;
using Infrastructure.DAL.Repository.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DAL.Repository.Implementations;

public class DocumentRepository : GenericRepository<Document>, IDocumentRepository
{
    private AppDbContext _db;

    public DocumentRepository(AppDbContext db) : base(db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Document>> GetByCatalogAsync(Guid catalogId)
    {
        return await Set.AsNoTracking().Where(d => d.CatalogId == catalogId).ToListAsync();
    }

    public async Task<Document?> GetByFileName(string fileName)
    {
        return (await Set.AsNoTracking().FirstOrDefaultAsync(d => d.FileName == fileName))!;
    }
}