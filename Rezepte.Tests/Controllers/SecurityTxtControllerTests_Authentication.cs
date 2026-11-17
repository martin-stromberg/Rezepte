using FluentAssertions;
using Rezepte.Tests.TestSupport;
using System.Net;
using Xunit;

namespace Rezepte.Tests.Controllers;

/// <summary>
/// Class representing the security txt controller tests authentication.
/// </summary>
public sealed class SecurityTxtControllerTests_Authentication
{
    /// <summary>
    /// Get security txt requires no authentication.
    /// </summary>
    [Fact]
    public async Task GetSecurityTxt_RequiresNoAuthentication()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"rezepte-securitytxt-auth-{Guid.NewGuid():N}.db");
        await using var factory = new SqliteFileWebApplicationFactory(databasePath);
        using var client = factory.CreateAnonymousClient();

        using var response = await client.GetAsync("/security.txt");

        response.StatusCode.Should().NotBe(HttpStatusCode.Redirect);
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Found);
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }
}
