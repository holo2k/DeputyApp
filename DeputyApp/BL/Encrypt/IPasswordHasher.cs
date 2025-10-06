namespace DeputyApp.BL.Encrypt;

public interface IPasswordHasher
{
    public string HashPassword(string password, string salt);
    bool Verify(string hash, string salt, string password);
}