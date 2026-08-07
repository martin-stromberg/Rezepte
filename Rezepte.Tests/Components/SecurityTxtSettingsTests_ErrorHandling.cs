using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Rezepte.Web;
using Rezepte.Web.Dtos;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Rezepte.Tests.Components;

public sealed class SecurityTxtSettingsTests_ErrorHandling : IDisposable
{
    private readonly TestContext _context = new();

    [Fact]
    public void Loading_WhenHttpRequestFails_ShowsGenericErrorWithoutTechnicalDetails()
    {
        var apiClient = CreateApiClient(_ => throw new HttpRequestException("boom-load"));
        _context.Services.AddSingleton(apiClient);

        var cut = _context.RenderComponent<Rezepte.Web.Components.Settings.SecurityTxtSettings>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Einstellungen konnten nicht geladen werden.");
            cut.Markup.Should().NotContain("boom-load");
        });
    }

    [Fact]
    public void Saving_WhenRequestTimesOut_ShowsTimeoutErrorWithoutTechnicalDetails()
    {
        var settings = new SecurityTxtSettings(
            Enabled: true,
            Contact: "mailto:security@example.com",
            Expires: DateTimeOffset.UtcNow.AddDays(7),
            Encryption: null,
            Acknowledgments: null,
            PreferredLanguages: null,
            Canonical: null,
            Policy: null,
            Hiring: null);

        var apiClient = CreateApiClient(request =>
        {
            if (request.Method == HttpMethod.Get)
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(settings)
                };
            }

            throw new TaskCanceledException("boom-timeout");
        });

        _context.Services.AddSingleton(apiClient);
        var cut = _context.RenderComponent<Rezepte.Web.Components.Settings.SecurityTxtSettings>();

        cut.WaitForAssertion(() => cut.Find("#securitytxt-enabled"));
        cut.Find("button.btn.btn-primary").Click();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Speichern dauert zu lange. Bitte erneut versuchen.");
            cut.Markup.Should().NotContain("boom-timeout");
        });
    }

    [Fact]
    public void RenderedForm_DoesNotContainCanonicalInput()
    {
        var settings = new SecurityTxtSettings(
            Enabled: true,
            Contact: "mailto:security@example.com",
            Expires: DateTimeOffset.UtcNow.AddDays(7),
            Encryption: null,
            Acknowledgments: null,
            PreferredLanguages: null,
            Canonical: "https://example.com/ignored",
            Policy: null,
            Hiring: null);

        var apiClient = CreateApiClient(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(settings)
        });
        _context.Services.AddSingleton(apiClient);

        var cut = _context.RenderComponent<Rezepte.Web.Components.Settings.SecurityTxtSettings>();

        cut.WaitForAssertion(() => cut.Find("#securitytxt-enabled"));
        cut.FindAll("#securitytxt-canonical").Should().BeEmpty();
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private ApiClient CreateApiClient(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
    {
        var handler = new DelegateHandler(responseFactory);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost")
        };
        var navigationManager = new TestNavigationManager();
        return new ApiClient(httpClient, navigationManager);
    }

    private sealed class DelegateHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(responseFactory(request));
        }
    }

    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager()
        {
            Initialize("http://localhost/", "http://localhost/");
        }
    }
}
