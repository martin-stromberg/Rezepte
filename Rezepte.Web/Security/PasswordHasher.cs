using System.Security.Cryptography;
using System.Text;

namespace Rezepte.Web.Security;

/// <summary>
/// Result of a password verification against a stored PBKDF2 hash.
/// </summary>
public enum PasswordVerificationResult
{
    /// <summary>Verification failed (wrong password, malformed or policy-violating hash).</summary>
    Failed,

    /// <summary>Verification succeeded with the current hashing policy.</summary>
    Success,

    /// <summary>Verification succeeded, but the stored hash uses outdated parameters and should be rehashed.</summary>
    SuccessRehashNeeded
}

public static class PasswordHasher
{
    /// <summary>Iteration count used for newly created password hashes (OWASP recommendation for PBKDF2-HMAC-SHA256).</summary>
    public const int CurrentIterations = 210_000;

    /// <summary>Minimum iteration count accepted during verification. Hashes below this are rejected.</summary>
    public const int MinIterations = 100_000;

    /// <summary>Maximum iteration count accepted during verification. Hashes above this are rejected to bound verification cost.</summary>
    public const int MaxIterations = 1_000_000;

    /// <summary>Salt length in bytes for newly created hashes; enforced on stored salts during verification.</summary>
    public const int SaltLengthBytes = 16;

    /// <summary>Derived key length in bytes; enforced on stored hashes during verification.</summary>
    public const int HashLengthBytes = 32;

    // PBKDF2 with HMACSHA256
    public static string Hash(string password, int iterations = CurrentIterations)
    {
        ArgumentNullException.ThrowIfNull(password);
        if (iterations < MinIterations || iterations > MaxIterations)
            throw new ArgumentOutOfRangeException(
                nameof(iterations),
                iterations,
                $"Iterations must be between {MinIterations} and {MaxIterations}.");
        var salt = RandomNumberGenerator.GetBytes(SaltLengthBytes);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            HashLengthBytes);
        return $"{iterations}.{Convert.ToHexString(salt)}.{Convert.ToHexString(hash)}";
    }

    public static PasswordVerificationResult Verify(string password, string hashString)
    {
        ArgumentNullException.ThrowIfNull(password);
        ArgumentNullException.ThrowIfNull(hashString);
        var parts = hashString.Split('.');
        if (parts.Length != 3) return PasswordVerificationResult.Failed;
        if (!int.TryParse(parts[0], out var iterations)
            || iterations < MinIterations
            || iterations > MaxIterations)
            return PasswordVerificationResult.Failed;

        byte[] salt;
        byte[] expectedHash;
        try
        {
            salt = Convert.FromHexString(parts[1]);
            expectedHash = Convert.FromHexString(parts[2]);
        }
        catch (FormatException)
        {
            return PasswordVerificationResult.Failed;
        }

        if (salt.Length != SaltLengthBytes || expectedHash.Length != HashLengthBytes)
            return PasswordVerificationResult.Failed;

        var actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expectedHash.Length);
        if (!CryptographicOperations.FixedTimeEquals(actualHash, expectedHash))
            return PasswordVerificationResult.Failed;

        return iterations < CurrentIterations
            ? PasswordVerificationResult.SuccessRehashNeeded
            : PasswordVerificationResult.Success;
    }
}
