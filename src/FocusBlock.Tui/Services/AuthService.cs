using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;

namespace FocusBlock.Tui.Services;

public class AuthService
{
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public (string Hash, string Salt) HashPassword(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = ComputeArgon2id(password, salt);
        return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
    }

    public bool VerifyPassword(string password, string hash, string salt)
    {
        byte[] expected = Convert.FromBase64String(hash);
        byte[] saltBytes = Convert.FromBase64String(salt);
        byte[] computed = ComputeArgon2id(password, saltBytes);
        return CryptographicOperations.FixedTimeEquals(computed, expected);
    }

    private static byte[] ComputeArgon2id(string password, byte[] salt)
    {
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = 4,
            MemorySize = 16 * 1024,   // 16 MB
            Iterations = 3,
        };
        return argon2.GetBytes(HashSize);
    }
}