using Application.Services.Abstractions;
using Domain.Entities;
using Domain.Enums;

namespace Application.Dtos;

public class DocumentDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileNameEncoded { get; set; } = string.Empty;
    public DocumentStatus? Status { get; set; } = null;
    public DateTimeOffset? StartDate { get; set; } = null;
    public DateTimeOffset? EndDate { get; set; } = null;
    public string Url { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public Guid? UploadedById { get; set; }
    public string UserName { get; set; }
    public Guid? CatalogId { get; set; }
    public Catalog? Catalog { get; set; }
    public Guid? PostId { get; set; }
    public Post? Post { get; set; }
    public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;
}

public static class DocumentMapper
{
    public static DocumentDto ToDto(this DocumentDto? documentDto)
    {
        return documentDto ?? new DocumentDto();
    }
    
    public static DocumentDto ToDto(this Document document)
    {
        return new DocumentDto
        {
            Id = document.Id,
            FileName = document.FileName,
            FileNameEncoded = document.FileNameEncoded,
            Status = document.Status,
            StartDate = document.StartDate,
            EndDate = document.EndDate,
            Url = document.Url,
            ContentType = document.ContentType,
            Size = document.Size,
            UploadedById = document.UploadedById,
            UserName = document.UploadedBy?.FullName ?? string.Empty,
            CatalogId = document.CatalogId,
            Catalog = document.Catalog,
            PostId = document.PostId,
            Post = document.Post,
            UploadedAt = document.UploadedAt
        };
    }

    public static async Task<Document> ToEntity(this DocumentDto document, IUserService userService)
    {
        return new Document
        {
            Id = document.Id,
            FileName = document.FileName,
            FileNameEncoded = document.FileNameEncoded,
            Status = document.Status,
            StartDate = document.StartDate,
            EndDate = document.EndDate,
            Url = document.Url,
            ContentType = document.ContentType,
            Size = document.Size,
            UploadedById = document.UploadedById,
            UploadedBy = await userService.GetByIdAsync(document.UploadedById!.Value),
            CatalogId = document.CatalogId,
            Catalog = document.Catalog,
            PostId = document.PostId,
            Post = document.Post,
            UploadedAt = document.UploadedAt
        };
    }
}