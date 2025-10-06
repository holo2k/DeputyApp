
using Domain.Entities;

namespace Application.Services.Abstractions;

public interface IDocumentService
{
    Task<Document> UploadAsync(string fileName, Stream content, string contentType, Guid? uploadedById,
        Guid? catalogId);

    Task<IEnumerable<Document>> GetByCatalogAsync(Guid catalogId);
    Task DeleteAsync(Guid id);
}