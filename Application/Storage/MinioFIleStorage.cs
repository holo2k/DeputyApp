using Application.Services.Abstractions;
using Minio;
using Minio.DataModel.Args;

namespace Application.Storage;

public class MinioFileStorage: IFileStorage
{
    private readonly string _bucket;
    private readonly IMinioClient _internalClient;
    //private readonly IMinioClient _presignClient;

    public MinioFileStorage(MinioOptions options)
    {
        _bucket = options.Bucket;

        _internalClient = new MinioClient()
            .WithEndpoint("minio:9000" ?? options.Endpoint)
            .WithCredentials(options.AccessKey, options.SecretKey)
            .Build();

        //var publicEndpoint = "localhost:9000" ?? options.Endpoint;
        //_presignClient = new MinioClient()
        //    .WithEndpoint(publicEndpoint)
        //    .WithCredentials(options.AccessKey, options.SecretKey)
        //    .Build();
    }

    public async Task<string> UploadAsync(string fileName, Stream content, string contentType)
    {
        var exists = await _internalClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(_bucket));
        if (!exists)
            await _internalClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(_bucket));

        await _internalClient.PutObjectAsync(
            new PutObjectArgs()
                .WithBucket(_bucket)
                .WithObject(fileName)
                .WithStreamData(content)
                .WithObjectSize(content.Length)
                .WithContentType(contentType)
        );

        return $"{_bucket}/{fileName}";
    }

    public async Task<string> GetPresignedUrlAsync(string fileName, TimeSpan validFor)
    {
        var args = new PresignedGetObjectArgs()
            .WithBucket(_bucket)
            .WithObject(fileName)
            .WithExpiry((int)validFor.TotalSeconds);

        var url = await _internalClient.PresignedGetObjectAsync(args);
        return url;
    }

    public async Task DeleteAsync(string url)
    {
        var objectName = ExtractObjectName(url);
        if (string.IsNullOrEmpty(objectName)) return;
        try
        {
            await _internalClient.RemoveObjectAsync(new RemoveObjectArgs().WithBucket(_bucket).WithObject(objectName));
        }
        catch (Exception)
        {
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