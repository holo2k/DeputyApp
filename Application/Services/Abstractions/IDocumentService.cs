using Application.Dtos;
using Domain.Entities;
using Domain.Enums;
using Task = System.Threading.Tasks.Task;

namespace Application.Services.Abstractions;

public interface IDocumentService
{
    Task<DocumentDto> UploadAsync(string fileName, Stream content, string contentType, Guid? uploadedById,
        UploadFileRequest request);

    Task<IEnumerable<DocumentDto>> GetByCatalogAsync(Guid catalogId);
    Task<DocumentDto?> GetByFileNameAsync(string fileName);
    Task<DocumentDto?> UpdateStatusAsync(Guid documentId, DocumentStatus newStatus);
    Task DeleteAsync(Guid id);
}