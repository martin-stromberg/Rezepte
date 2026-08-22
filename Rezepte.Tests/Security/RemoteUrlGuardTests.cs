using FluentAssertions;
using Rezepte.Web.Security;
using Xunit;

namespace Rezepte.Tests.Security;

public class RemoteUrlGuardTests
{
    [Theory]
    [InlineData("http://127.0.0.1/recipe")]
    [InlineData("http://localhost/recipe")]
    [InlineData("http://10.0.0.5/recipe")]
    [InlineData("http://192.168.1.10/recipe")]
    [InlineData("http://172.16.4.4/recipe")]
    [InlineData("http://169.254.169.254/latest/meta-data")]
    [InlineData("http://[::1]/recipe")]
    public async Task TryValidateAsync_RejectsInternalAddresses(string url)
    {
        var (ok, error, uri) = await RemoteUrlGuard.TryValidateAsync(url, CancellationToken.None);

        ok.Should().BeFalse();
        uri.Should().BeNull();
        error.Should().NotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData("ftp://example.com/recipe")]
    [InlineData("file:///etc/passwd")]
    [InlineData("not-a-url")]
    [InlineData("")]
    [InlineData(null)]
    public async Task TryValidateAsync_RejectsUnsupportedSchemes(string? url)
    {
        var (ok, _, uri) = await RemoteUrlGuard.TryValidateAsync(url, CancellationToken.None);

        ok.Should().BeFalse();
        uri.Should().BeNull();
    }

    [Fact]
    public async Task TryValidateAsync_RejectsNonStandardPorts()
    {
        var (ok, _, _) = await RemoteUrlGuard.TryValidateAsync("http://example.com:8080/recipe", CancellationToken.None);

        ok.Should().BeFalse();
    }

    [Fact]
    public async Task TryValidateAsync_AcceptsPublicHttpsAddress()
    {
        var (ok, error, uri) = await RemoteUrlGuard.TryValidateAsync("https://93.184.216.34/recipe", CancellationToken.None);

        ok.Should().BeTrue();
        error.Should().BeNull();
        uri!.Host.Should().Be("93.184.216.34");
    }
}
