using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using Rezepte.Web.Security;

namespace Rezepte.Web.Services;

public interface ITokenService
{
    string CreateToken(string userId, string username, bool isAdmin = false);
    string? GetToken(string userId);
}

public class TokenService : ITokenService
{
    private readonly IMemoryCache _cache;
    private readonly IJwtSigningKeyProvider _signingKeyProvider;

    public TokenService(IMemoryCache cache, IJwtSigningKeyProvider signingKeyProvider)
    {
        _cache = cache;
        _signingKeyProvider = signingKeyProvider;
    }

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

    public string? GetToken(string userId)
    {
        return _cache.TryGetValue<string>(TokenKey(userId), out var token) ? token : null;
    }

    private static string TokenKey(string userId) => $"token:{userId}";
}
