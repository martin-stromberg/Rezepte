using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Rezepte.Web;
using System.Net;
using Xunit;

namespace Rezepte.Tests.Controllers;

public sealed class SecurityTxtControllerTests_Authentication
{
    [Fact]
    public async Task GetSecurityTxt_RequiresNoAuthentication()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"rezepte-securitytxt-auth-{Guid.NewGuid():N}.db");
        await using var factory = new SecurityTxtWebApplicationFactory(databasePath);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        using var response = await client.GetAsync("/security.txt");

        response.StatusCode.Should().NotBe(HttpStatusCode.Redirect);
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Found);
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    private sealed class SecurityTxtWebApplicationFactory(string databasePath) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            builder.UseSetting(WebHostDefaults.EnvironmentKey, "Production");
            builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Default"] = $"Data Source={databasePath}"
                });
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
            {
                return;
            }

            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }
    }
}
