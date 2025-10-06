using DeputyApp.BL.Services.Abstractions;
using Minio;
using Minio.DataModel.Args;

namespace DeputyApp.BL.Storage;

public class MinioFileStorage : IFileStorage
{
    private readonly string _bucket;
    private readonly IMinioClient _client;

    public MinioFileStorage(string endpoint, string accessKey, string secretKey, string bucket, bool secure = false)
    {
        _client = new MinioClient()
            .WithEndpoint(endpoint)
            .WithCredentials(accessKey, secretKey)
            .WithSSL(secure)
            .Build();
        _bucket = bucket;
    }

    public async Task<string> UploadAsync(string fileName, Stream content, string contentType)
    {
        try
        {
            var exists = await _client.BucketExistsAsync(new BucketExistsArgs().WithBucket(_bucket));
            if (!exists) await _client.MakeBucketAsync(new MakeBucketArgs().WithBucket(_bucket));

            // Put object
            await _client.PutObjectAsync(
                new PutObjectArgs()
                    .WithBucket(_bucket)
                    .WithObject(fileName)
                    .WithStreamData(content)
                    .WithObjectSize(content.Length)
                    .WithContentType(contentType)
            );

            // Return object URL (presigned GET valid long time)
            return $"{_bucket}/{fileName}";
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("MinIO upload failed", ex);
        }
    }

    public async Task DeleteAsync(string url)
    {
        // url stored as "bucket/object" or object name. Try best-effort.
        var objectName = ExtractObjectName(url);
        if (string.IsNullOrEmpty(objectName)) return;
        try
        {
            await _client.RemoveObjectAsync(new RemoveObjectArgs().WithBucket(_bucket).WithObject(objectName));
        }
        catch (Exception)
        {
            /* ignore */
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
        if (url.Contains("/"))
        {
            var parts = url.Split('/', 2);
            return parts.Length == 2 ? parts[1] : parts[0];
        }

        return url;
    }
}