using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Rezepte.Web.Security;
using Xunit;

namespace Rezepte.Tests.Security;

/// <summary>
/// Class representing the jwt signing key provider tests.
/// </summary>
public class JwtSigningKeyProviderTests
{
    /// <summary>
    /// Constructor throws outside development when secret is missing or weak.
    /// </summary>
    /// <param name="secret">The secret parameter.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("JWT_KEY")]
    [InlineData("too-short-secret")]
    public void Constructor_ThrowsOutsideDevelopment_WhenSecretIsMissingOrWeak(string? secret)
    {
        var configuration = BuildConfiguration(secret);
        var environment = new TestHostEnvironment(Environments.Production);

        var act = () => new JwtSigningKeyProvider(configuration, environment);

        act.Should().Throw<InvalidOperationException>();
    }

    /// <summary>
    /// Constructor generates ephemeral key in development when secret is missing.
    /// </summary>
    [Fact]
    public void Constructor_GeneratesEphemeralKeyInDevelopment_WhenSecretIsMissing()
    {
        var configuration = BuildConfiguration(null);
        var environment = new TestHostEnvironment(Environments.Development);

        var first = new JwtSigningKeyProvider(configuration, environment);
        var second = new JwtSigningKeyProvider(configuration, environment);

        first.Key.Should().HaveCount(32);
        first.Key.Should().NotEqual(second.Key);
    }

    /// <summary>
    /// Constructor uses configured secret issuer and audience.
    /// </summary>
    [Fact]
    public void Constructor_UsesConfiguredSecretIssuerAndAudience()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = new string('k', JwtSigningKeyProvider.MinimumKeyLength),
                ["Jwt:Issuer"] = "issuer",
                ["Jwt:Audience"] = "audience"
            })
            .Build();

        var provider = new JwtSigningKeyProvider(configuration, new TestHostEnvironment(Environments.Production));

        provider.Issuer.Should().Be("issuer");
        provider.Audience.Should().Be("audience");
        provider.Key.Should().HaveCount(32);
    }

    private static IConfiguration BuildConfiguration(string? secret)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Key"] = secret })
            .Build();
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "Rezepte.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; }
            = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
