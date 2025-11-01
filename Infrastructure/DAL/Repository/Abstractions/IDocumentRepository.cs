using Domain.Entities;

namespace Infrastructure.DAL.Repository.Abstractions;

public interface IDocumentRepository : IRepository<Document>
{
    Task<IEnumerable<Document>> GetByCatalogAsync(Guid catalogId);
    Task<Document?> GetByFileName(string fileName);
}