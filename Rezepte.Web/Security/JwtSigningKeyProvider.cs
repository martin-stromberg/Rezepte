using System.Security.Cryptography;
using System.Text;

namespace Rezepte.Web.Security;

/// <summary>
/// Provides the signing key and the issuer/audience values used for API tokens.
/// </summary>
public interface IJwtSigningKeyProvider
{
    /// <summary>Derived 256 bit signing key.</summary>
    byte[] Key { get; }

    /// <summary>Token issuer.</summary>
    string Issuer { get; }

    /// <summary>Token audience.</summary>
    string Audience { get; }
}

/// <summary>
/// Resolves the JWT signing material from configuration. Outside of development a dedicated
/// secret is required; in development an ephemeral key is generated for the current process.
/// </summary>
public sealed class JwtSigningKeyProvider : IJwtSigningKeyProvider
{
    /// <summary>Value shipped in appsettings.json as a marker for "not configured".</summary>
    public const string PlaceholderKey = "JWT_KEY";

    /// <summary>Minimum number of characters required for a configured secret.</summary>
    public const int MinimumKeyLength = 32;

    private const string DefaultIssuer = "rezepte";
    private const string DefaultAudience = "rezepte.api";

    public JwtSigningKeyProvider(IConfiguration configuration, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        var issuer = configuration["Jwt:Issuer"];
        var audience = configuration["Jwt:Audience"];
        Issuer = string.IsNullOrWhiteSpace(issuer) ? DefaultIssuer : issuer;
        Audience = string.IsNullOrWhiteSpace(audience) ? DefaultAudience : audience;

        var secret = configuration["Jwt:Key"];
        if (!IsUsableSecret(secret))
        {
            if (!environment.IsDevelopment())
            {
                throw new InvalidOperationException(
                    $"Configuration value 'Jwt:Key' is missing or insecure. Set it to a private secret with at least {MinimumKeyLength} characters, for example through the environment variable 'Jwt__Key'.");
            }

            secret = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        }

        Key = SHA256.HashData(Encoding.UTF8.GetBytes(secret!));
    }

    /// <inheritdoc />
    public byte[] Key { get; }

    /// <inheritdoc />
    public string Issuer { get; }

    /// <inheritdoc />
    public string Audience { get; }

    private static bool IsUsableSecret(string? secret)
    {
        return !string.IsNullOrWhiteSpace(secret)
            && !string.Equals(secret, PlaceholderKey, StringComparison.OrdinalIgnoreCase)
            && secret.Length >= MinimumKeyLength;
    }
}
