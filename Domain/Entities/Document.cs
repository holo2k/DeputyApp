using System.Text.Json.Serialization;

namespace Domain.Entities;

public class Document
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty; // S3 or CDN link
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public Guid? UploadedById { get; set; }

    [JsonIgnore] public User? UploadedBy { get; set; }

    public Guid? CatalogId { get; set; }

    [JsonIgnore] public Catalog? Catalog { get; set; }

    public Guid? PostId { get; set; }
    public Post? Post { get; set; }
    public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;
}