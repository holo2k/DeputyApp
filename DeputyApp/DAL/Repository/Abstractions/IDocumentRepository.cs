using DeputyApp.Entities;

namespace DeputyApp.DAL.Repository.Abstractions;

public interface IDocumentRepository : IRepository<Document>
{
    Task<IEnumerable<Document>> GetByCatalogAsync(Guid catalogId);
}