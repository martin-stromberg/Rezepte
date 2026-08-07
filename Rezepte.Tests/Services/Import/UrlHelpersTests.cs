using System;
using FluentAssertions;
using Rezepte.Import.PluginSdk;
using Xunit;

namespace Rezepte.Tests.Services.Import;

public class UrlHelpersTests
{
    [Theory]
    [InlineData("http://example.com")]
    [InlineData("https://example.com/recipes/1")]
    [InlineData("  https://example.com/recipes/1  ")]
    public void TryCreateHttpUri_ShouldAcceptHttpAndHttps(string value)
    {
        UrlHelpers.TryCreateHttpUri(value, out var uri).Should().BeTrue();

        uri.Host.Should().Be("example.com");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("example.com")]
    [InlineData("/recipes/1")]
    [InlineData("ftp://example.com")]
    [InlineData("file:///tmp/recipe.html")]
    [InlineData("javascript:alert(1)")]
    public void TryCreateHttpUri_ShouldRejectNonHttpValues(string? value)
    {
        UrlHelpers.TryCreateHttpUri(value, out _).Should().BeFalse();
    }

    [Theory]
    [InlineData("HTTPS://Example.COM/Recipes", "https://example.com/Recipes")]
    [InlineData("http://EXAMPLE.com:80/path", "http://example.com/path")]
    [InlineData("https://example.com:443/path", "https://example.com/path")]
    [InlineData("http://example.com:8080/path", "http://example.com:8080/path")]
    [InlineData("https://example.com/path?b=2&a=1", "https://example.com/path?b=2&a=1")]
    public void NormalizeHttpUrl_ShouldLowercaseSchemeAndHostAndDropDefaultPorts(string value, string expected)
    {
        UrlHelpers.NormalizeHttpUrl(value).Should().Be(expected);
    }

    [Fact]
    public void NormalizeHttpUrl_ShouldAppendRootPathForAuthorityOnlyUrls()
    {
        UrlHelpers.NormalizeHttpUrl("https://example.com").Should().Be("https://example.com/");
    }

    [Theory]
    [InlineData("")]
    [InlineData("not a url")]
    [InlineData("ftp://example.com")]
    public void NormalizeHttpUrl_ShouldThrowForInvalidUrls(string value)
    {
        var act = () => UrlHelpers.NormalizeHttpUrl(value);

        act.Should().Throw<FormatException>();
    }
}
