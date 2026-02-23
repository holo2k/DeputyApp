namespace Application.Storage;

public class MinioOptions
{
    public string Endpoint { get; set; } = "";
    public string AccessKey { get; set; } = "minioadmin";
    public string SecretKey { get; set; } = "minioadmin";
    public string Bucket { get; set; } = "";
    public bool Secure { get; set; } = false;
}