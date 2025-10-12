using System.Security.Cryptography;
using System.Text;

namespace Rezepte.Web.Security;

public static class PasswordHasher
{
    // PBKDF2 with HMACSHA256
    public static string Hash(string password, int iterations = 100_000)
    {
        ArgumentNullException.ThrowIfNull(password);
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            32);
        return $"{iterations}.{Convert.ToHexString(salt)}.{Convert.ToHexString(hash)}";
    }

    public static bool Verify(string password, string hashString)
    {
        ArgumentNullException.ThrowIfNull(password);
        ArgumentNullException.ThrowIfNull(hashString);
        var parts = hashString.Split('.');
        if (parts.Length != 3) return false;
        var iterations = int.Parse(parts[0]);
        var salt = Convert.FromHexString(parts[1]);
        var expectedHash = Convert.FromHexString(parts[2]);
        var actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expectedHash.Length);
        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}
