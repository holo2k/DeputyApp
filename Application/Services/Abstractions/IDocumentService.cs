using Application.Dtos;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services.Abstractions;

public interface IDocumentService
{
    Task<Document> UploadAsync(string fileName, Stream content, string contentType, Guid? uploadedById,
        UploadFileRequest request);

    Task<IEnumerable<Document>> GetByCatalogAsync(Guid catalogId);
    Task<Document?> GetByFileNameAsync(string fileName);
    Task<Document?> UpdateStatusAsync(Guid documentId, DocumentStatus newStatus);
    Task DeleteAsync(Guid id);
}