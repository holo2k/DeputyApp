using System.Text;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace DeputyApp.BL.Encrypt;

public class PasswordHasher : IPasswordHasher
{
    public string HashPassword(string password, string salt)
    {
        return Convert.ToBase64String(KeyDerivation.Pbkdf2(
            password,
            Encoding.ASCII.GetBytes(salt),
            KeyDerivationPrf.HMACSHA512,
            100_000,
            64));
    }

    public bool Verify(string hash, string salt, string password)
    {
        if (hash == null) return false;
        var h = HashPassword(password, salt);
        return SlowEquals(h, hash);
    }

    private static bool SlowEquals(string a, string b)
    {
        var ab = Encoding.UTF8.GetBytes(a);
        var bb = Encoding.UTF8.GetBytes(b);
        var diff = ab.Length ^ bb.Length;
        for (var i = 0; i < Math.Min(ab.Length, bb.Length); i++) diff |= ab[i] ^ bb[i];
        return diff == 0;
    }
}