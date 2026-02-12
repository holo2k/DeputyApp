using System.Text.RegularExpressions;
using Application.Dtos;
using Application.Services.Abstractions;
using DeputyApp.DAL.UnitOfWork;
using Domain.Entities;
using Domain.Enums;
using Task = System.Threading.Tasks.Task;

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

    public async Task<DocumentDto?> GetByFileNameAsync(string fileName)
    {
        return (await _uow.Documents.GetByFileName(fileName)).ToDto();
    }

    public async Task DeleteAsync(Guid id)
    {
        var d = await _uow.Documents.GetByIdAsync(id);
        if (d == null) return;
        await _storage.DeleteAsync(d.Url);
        _uow.Documents.Delete(d);
        await _uow.SaveChangesAsync();
    }


    public async Task<DocumentDto> UploadAsync(
        string fileName,
        Stream content,
        string contentType,
        Guid? uploadedById,
        UploadFileRequest request)
    {
        var extension = Path.GetExtension(fileName);
        var baseName = Path.GetFileNameWithoutExtension(fileName);

        var existed = await GetByFileNameAsync(baseName);

        if (existed is not null)
        {
            var match = Regex.Match(existed.FileName, @"^(.*?)(?:\((\d+)\))?$");
            var pureName = match.Groups[1].Value;
            var number = int.TryParse(match.Groups[2].Value, out var n) ? n + 1 : 1;
            baseName = $"{pureName}({number})";
        }

        var encodedFileName = $"{baseName}-{Guid.NewGuid()}{extension}";
        var url = await _storage.UploadAsync(encodedFileName, content, contentType);

        var doc = new Document
        {
            Id = Guid.NewGuid(),
            FileName = baseName,
            FileNameEncoded = encodedFileName,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = request.DocumentStatus,
            Url = url,
            ContentType = contentType,
            Size = content.CanSeek ? content.Length : 0,
            UploadedAt = DateTimeOffset.UtcNow,
            UploadedById = uploadedById,
            CatalogId = request.CatalogId
        };

        await _uow.Documents.AddAsync(doc);
        await _uow.SaveChangesAsync();
        return doc.ToDto();
    }

    public async Task<DocumentDto?> UpdateStatusAsync(Guid documentId, DocumentStatus newStatus)
    {
        var doc = await _uow.Documents.GetByIdAsync(documentId);
        if (doc is null)
            return null;

        doc.Status = newStatus;
        _uow.Documents.Update(doc);
        await _uow.SaveChangesAsync();

        return doc.ToDto();
    }

    public async Task<IEnumerable<DocumentDto>> GetByCatalogAsync(Guid catalogId)
    {
        return (await _uow.Documents.GetByCatalogAsync(catalogId)).Select(x=>x.ToDto());
    }
}