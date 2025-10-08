using Application.Services.Abstractions;
using DeputyApp.DAL.UnitOfWork;
using Domain.Entities;

namespace Application.Services.Implementations;

public class DocumentService : IDocumentService
{
    private readonly IFileStorage _storage;
    private readonly IUnitOfWork _uow;

    public DocumentService(IFileStorage storage, IUnitOfWork uow)
    {
        _storage = storage;
        _uow = uow;
    }

    public async Task DeleteAsync(Guid id)
    {
        var d = await _uow.Documents.GetByIdAsync(id);
        if (d == null) return;
        await _storage.DeleteAsync(d.Url);
        _uow.Documents.Delete(d);
        await _uow.SaveChangesAsync();
    }


    public async Task<Document> UploadAsync(string fileName, Stream content, string contentType, Guid? uploadedById,
        Guid? catalogId)
    {
        var url = await _storage.UploadAsync(fileName, content, contentType);
        var doc = new Document
        {
            Id = Guid.NewGuid(), FileName = fileName, Url = url, ContentType = contentType, Size = content.Length,
            UploadedAt = DateTimeOffset.UtcNow, UploadedById = uploadedById, CatalogId = catalogId
        };
        await _uow.Documents.AddAsync(doc);
        await _uow.SaveChangesAsync();
        return doc;
    }


    public async Task<IEnumerable<Document>> GetByCatalogAsync(Guid catalogId)
    {
        return await _uow.Documents.GetByCatalogAsync(catalogId);
    }
}