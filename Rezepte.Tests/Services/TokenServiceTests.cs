using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using Rezepte.Web.Security;
using Rezepte.Web.Services;
using Xunit;

namespace Rezepte.Tests.Services;

/// <summary>
/// Class representing the token service tests.
/// </summary>
public class TokenServiceTests
{
    private const string ConfiguredKey = "unit-test-signing-secret-0123456789";

    private static TokenService CreateSut(out IMemoryCache cache, string key = ConfiguredKey)
    {
        cache = new MemoryCache(new MemoryCacheOptions());
        return new TokenService(cache, new StubJwtSigningKeyProvider(key));
    }

    private static JwtSecurityToken ReadValidatedToken(string jwt, string signingKey)
    {
        var handler = new JwtSecurityTokenHandler();
        var parameters = new TokenValidationParameters
        {
            ValidIssuer = "rezepte",
            ValidAudience = "rezepte.api",
            IssuerSigningKey = new SymmetricSecurityKey(SHA256.HashData(Encoding.UTF8.GetBytes(signingKey))),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        handler.ValidateToken(jwt, parameters, out var validated);
        return (JwtSecurityToken)validated;
    }

    /// <summary>
    /// Create token should issue signed token with identity claims.
    /// </summary>
    [Fact]
    public void CreateToken_ShouldIssueSignedTokenWithIdentityClaims()
    {
        var sut = CreateSut(out _);

        var jwt = sut.CreateToken("user-1", "alice");

        var token = ReadValidatedToken(jwt, ConfiguredKey);
        token.Issuer.Should().Be("rezepte");
        token.Audiences.Should().Contain("rezepte.api");
        token.Claims.Should().Contain(c => c.Type == "nameid" && c.Value == "user-1");
        token.Claims.Should().Contain(c => c.Type == "unique_name" && c.Value == "alice");
        token.ValidTo.Should().BeCloseTo(DateTime.UtcNow.AddHours(8), TimeSpan.FromMinutes(1));
    }

    /// <summary>
    /// Create token should not add admin role by default.
    /// </summary>
    [Fact]
    public void CreateToken_ShouldNotAddAdminRoleByDefault()
    {
        var sut = CreateSut(out _);

        var token = ReadValidatedToken(sut.CreateToken("user-1", "alice"), ConfiguredKey);

        token.Claims.Should().NotContain(c => c.Type == "role");
    }

    /// <summary>
    /// Create token should add admin role for admins.
    /// </summary>
    [Fact]
    public void CreateToken_ShouldAddAdminRoleForAdmins()
    {
        var sut = CreateSut(out _);

        var token = ReadValidatedToken(sut.CreateToken("user-1", "alice", isAdmin: true), ConfiguredKey);

        token.Claims.Should().Contain(c => c.Type == "role" && c.Value == "Admin");
    }

    /// <summary>
    /// Create token should cache token per user.
    /// </summary>
    [Fact]
    public void CreateToken_ShouldCacheTokenPerUser()
    {
        var sut = CreateSut(out var cache);

        var first = sut.CreateToken("user-1", "alice");
        var second = sut.CreateToken("user-2", "bob");

        cache.Get<string>("token:user-1").Should().Be(first);
        sut.GetToken("user-1").Should().Be(first);
        sut.GetToken("user-2").Should().Be(second);
    }

    /// <summary>
    /// Create token should replace cached token for same user.
    /// </summary>
    [Fact]
    public void CreateToken_ShouldReplaceCachedTokenForSameUser()
    {
        var sut = CreateSut(out _);

        sut.CreateToken("user-1", "alice");
        var latest = sut.CreateToken("user-1", "alice", isAdmin: true);

        sut.GetToken("user-1").Should().Be(latest);
    }

    /// <summary>
    /// Get token should return null for unknown user.
    /// </summary>
    [Fact]
    public void GetToken_ShouldReturnNullForUnknownUser()
    {
        var sut = CreateSut(out _);

        sut.GetToken("unknown").Should().BeNull();
    }

    /// <summary>
    /// Create token should sign with derived key only.
    /// </summary>
    [Fact]
    public void CreateToken_ShouldSignWithDerivedKeyOnly()
    {
        var sut = CreateSut(out _);

        var jwt = sut.CreateToken("user-1", "alice");

        var act = () => ReadValidatedToken(jwt, "another-secret");
        act.Should().Throw<SecurityTokenException>();
    }

    private sealed class StubJwtSigningKeyProvider(string secret) : IJwtSigningKeyProvider
    {
        public byte[] Key { get; } = SHA256.HashData(Encoding.UTF8.GetBytes(secret));

        public string Issuer => "rezepte";

        public string Audience => "rezepte.api";
    }
}
