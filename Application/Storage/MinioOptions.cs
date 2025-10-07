namespace Application.Storage;

public class MinioOptions
{
    public string Endpoint { get; set; } = "";
    public string AccessKey { get; set; } = "";
    public string SecretKey { get; set; } = "";
    public string Bucket { get; set; } = "";
    public bool Secure { get; set; } = false;
}