using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Rezepte.Web;
using Rezepte.Web.Components.Pages;
using Xunit;

namespace Rezepte.Tests.Components;

/// <summary>
/// Class representing the recipe search component tests.
/// </summary>
public sealed class RecipeSearchComponentTests : IDisposable
{
    private readonly TestContext _context = new();
    private readonly PathBaseNavigationManager _navigationManager = new("http://localhost/sub/");

    /// <summary>
    /// Initializes a new instance of the <see cref="RecipeSearchComponentTests"/> class.
    /// </summary>
    public RecipeSearchComponentTests()
    {
        _context.Services.AddLocalization();
        _context.Services.AddSingleton<ILogger<RecipeSearch>>(NullLogger<RecipeSearch>.Instance);
        _context.Services.AddSingleton<NavigationManager>(_navigationManager);
    }

    /// <summary>
    /// Run search async shows the search failed alert when the self-call is cancelled while the
    /// search is still the active one.
    /// </summary>
    [Fact]
    public void RunSearchAsync_WhenCancelled_ShowsSearchFailed()
    {
        var handler = StubHttpMessageHandler.Throwing(new OperationCanceledException("boom-cancelled"));
        RegisterApiClient(handler);

        var cut = _context.RenderComponent<RecipeSearch>();

        cut.WaitForAssertion(() =>
        {
            cut.Find(".alert-danger").InnerHtml.Should().Contain("Die Suche ist fehlgeschlagen.");
        });
        cut.FindAll(".alert-secondary").Should().BeEmpty();
    }

    /// <summary>
    /// Run search async shows the search failed alert when the self-call fails with a
    /// non-cancellation exception.
    /// </summary>
    [Fact]
    public void RunSearchAsync_WhenRequestFails_ShowsSearchFailed()
    {
        var handler = StubHttpMessageHandler.Throwing(new HttpRequestException("boom-request"));
        RegisterApiClient(handler);

        var cut = _context.RenderComponent<RecipeSearch>();

        cut.WaitForAssertion(() =>
        {
            cut.Find(".alert-danger").InnerHtml.Should().Contain("Die Suche ist fehlgeschlagen.");
        });
        cut.FindAll(".alert-secondary").Should().BeEmpty();
    }

    /// <summary>
    /// Run search async shows the no search results alert for an empty result set.
    /// </summary>
    [Fact]
    public void RunSearchAsync_WhenEmpty_ShowsNoSearchResults()
    {
        var handler = StubHttpMessageHandler.WithJson(new
        {
            items = Array.Empty<object>(),
            total = 0
        });
        RegisterApiClient(handler);

        var cut = _context.RenderComponent<RecipeSearch>();

        cut.WaitForAssertion(() =>
        {
            cut.Find(".alert-secondary").InnerHtml.Should().Contain("Keine Treffer.");
        });
        cut.FindAll(".alert-danger").Should().BeEmpty();
    }

    /// <summary>
    /// Run search async renders the returned items and sends the request with a relative URI so the
    /// configured base address path is preserved.
    /// </summary>
    [Fact]
    public void RunSearchAsync_WhenResults_RendersListItems()
    {
        var handler = StubHttpMessageHandler.WithJson(new
        {
            items = new[]
            {
                new
                {
                    id = "recipe-1",
                    title = "Honigkuchen",
                    snippet = "Schnell gemacht",
                    primaryImageUrl = (string?)null,
                    cookbooks = (string[]?)null,
                    tags = (string[]?)null,
                    publishedLabel = "2024-01-01"
                }
            },
            total = 1
        });
        RegisterApiClient(handler);

        var cut = _context.RenderComponent<RecipeSearch>();

        cut.WaitForAssertion(() =>
        {
            cut.Find(".list-group-item").InnerHtml.Should().Contain("Honigkuchen");
        });

        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.RequestUri.Should().NotBeNull();
        handler.LastRequest.RequestUri!.AbsolutePath.Should().Be("/sub/api/recipes/search");
    }

    /// <summary>
    /// Dispose.
    /// </summary>
    public void Dispose()
    {
        _context.Dispose();
    }

    private void RegisterApiClient(StubHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler);
        _context.Services.AddSingleton(new ApiClient(httpClient, _navigationManager));
    }

    private sealed class PathBaseNavigationManager : NavigationManager
    {
        public PathBaseNavigationManager(string baseUri)
        {
            Initialize(baseUri, baseUri);
        }
    }
}
