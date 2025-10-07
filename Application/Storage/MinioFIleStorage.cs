using Application.Services.Abstractions;
using Minio;
using Minio.DataModel.Args;

namespace Application.Storage;

public class MinioFileStorage
    : IFileStorage
{
    private readonly string _bucket;
    private readonly IMinioClient _client;

    public MinioFileStorage(MinioOptions options)
    {
        _bucket = options.Bucket;
        options.Endpoint = "localhost:9000";
        _client = new MinioClient()
            .WithEndpoint(options.Endpoint)
            .WithCredentials(options.AccessKey, options.SecretKey)
            .WithSSL(options.Secure)
            .Build();
    }

    public async Task<string> UploadAsync(string fileName, Stream content, string contentType)
    {
        try
        {
            var exists = await _client.BucketExistsAsync(new BucketExistsArgs().WithBucket(_bucket));
            if (!exists) await _client.MakeBucketAsync(new MakeBucketArgs().WithBucket(_bucket));

            await _client.PutObjectAsync(
                new PutObjectArgs()
                    .WithBucket(_bucket)
                    .WithObject(fileName)
                    .WithStreamData(content)
                    .WithObjectSize(content.Length)
                    .WithContentType(contentType)
            );

            return $"{_bucket}/{fileName}";
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("MinIO upload failed", ex);
        }
    }

    public async Task DeleteAsync(string url)
    {
        var objectName = ExtractObjectName(url);
        if (string.IsNullOrEmpty(objectName)) return;
        try
        {
            await _client.RemoveObjectAsync(new RemoveObjectArgs().WithBucket(_bucket).WithObject(objectName));
        }
        catch (Exception)
        {
        }
    }

    public async Task<string> GetPresignedUrlAsync(string fileName, TimeSpan validFor)
    {
        try
        {
            var presigned = await _client.PresignedGetObjectAsync(new PresignedGetObjectArgs()
                .WithBucket(_bucket)
                .WithObject(fileName)
                .WithExpiry((int)validFor.TotalSeconds));
            return presigned;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Presigned URL generation failed", ex);
        }
    }

    private string ExtractObjectName(string url)
    {
        if (string.IsNullOrEmpty(url)) return null!;
        if (!url.Contains("/")) return url;
        var parts = url.Split('/', 2);
        return parts.Length == 2 ? parts[1] : parts[0];
    }
}