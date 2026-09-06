using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using Rezepte.Web.Security;

namespace Rezepte.Web.Services;

/// <summary>
/// Defines the itoken service interface.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Creates the token.
    /// </summary>
    /// <param name="userId">The user id parameter.</param>
    /// <param name="username">The username parameter.</param>
    /// <param name="isAdmin">The is admin parameter.</param>
    /// <returns>The result.</returns>
    string CreateToken(string userId, string username, bool isAdmin = false);
    /// <summary>
    /// Gets the token.
    /// </summary>
    /// <param name="userId">The user id parameter.</param>
    /// <returns>The result.</returns>
    string? GetToken(string userId);
}

/// <summary>
/// Represents the token service class.
/// </summary>
public class TokenService : ITokenService
{
    private readonly IMemoryCache _cache;
    private readonly IJwtSigningKeyProvider _signingKeyProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="TokenService"/> class.
    /// </summary>
    /// <param name="cache">The cache parameter.</param>
    /// <param name="signingKeyProvider">The signing key provider parameter.</param>
    public TokenService(IMemoryCache cache, IJwtSigningKeyProvider signingKeyProvider)
    {
        _cache = cache;
        _signingKeyProvider = signingKeyProvider;
    }

    /// <summary>
    /// Creates the token.
    /// </summary>
    /// <param name="userId">The user id parameter.</param>
    /// <param name="username">The username parameter.</param>
    /// <param name="isAdmin">The is admin parameter.</param>
    /// <returns>The result.</returns>
    public string CreateToken(string userId, string username, bool isAdmin = false)
    {
        var handler = new JwtSecurityTokenHandler();
        var creds = new SigningCredentials(new SymmetricSecurityKey(_signingKeyProvider.Key), SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, username)
        };
        if (isAdmin)
        {
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));
        }
        var token = handler.CreateJwtSecurityToken(
            issuer: _signingKeyProvider.Issuer,
            audience: _signingKeyProvider.Audience,
            subject: new ClaimsIdentity(claims),
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddHours(8),
            issuedAt: DateTime.UtcNow,
            signingCredentials: creds);
        var jwt = handler.WriteToken(token);
        _cache.Set(TokenKey(userId), jwt, TimeSpan.FromHours(8));
        return jwt;
    }

    /// <summary>
    /// Gets the token.
    /// </summary>
    /// <param name="userId">The user id parameter.</param>
    /// <returns>The result.</returns>
    public string? GetToken(string userId)
    {
        return _cache.TryGetValue<string>(TokenKey(userId), out var token) ? token : null;
    }

    private static string TokenKey(string userId) => $"token:{userId}";
}
