namespace Application.Services.Abstractions;

public interface IFileStorage
{
    Task<string> UploadAsync(string fileName, Stream content, string contentType);
    Task DeleteAsync(string url);
    Task<string> GetPresignedUrlAsync(string fileName, TimeSpan validFor);
}