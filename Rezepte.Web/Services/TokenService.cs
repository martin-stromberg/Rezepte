using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

namespace Rezepte.Web.Services;

public interface ITokenService
{
    string CreateToken(string userId, string username);
    string? GetToken(string userId);
}

public class TokenService : ITokenService
{
    private readonly IMemoryCache _cache;
    private readonly byte[] _key;

    public TokenService(IMemoryCache cache, IConfiguration config)
    {
        _cache = cache;
        var key = config["Jwt:Key"] ?? "dev-super-secret-key-change";
        // Ensure 256-bit key: derive fixed-size key via SHA256 of provided secret
        var raw = Encoding.UTF8.GetBytes(key);
        _key = SHA256.HashData(raw);
    }

    public string CreateToken(string userId, string username)
    {
        var handler = new JwtSecurityTokenHandler();
        var creds = new SigningCredentials(new SymmetricSecurityKey(_key), SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, username)
        };
        var token = handler.CreateJwtSecurityToken(
            issuer: "rezepte",
            audience: "rezepte.api",
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
