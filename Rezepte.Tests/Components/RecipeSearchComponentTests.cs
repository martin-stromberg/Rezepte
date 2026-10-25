using Bunit;
using Bunit.TestDoubles;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Rezepte.Web.Components.Pages;
using Rezepte.Web.Services;
using System.Collections.Concurrent;
using System.Security.Claims;
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
    /// Run search async shows the search failed alert when the search is cancelled while the
    /// search is still the active one.
    /// </summary>
    [Fact]
    public void RunSearchAsync_WhenCancelled_ShowsSearchFailed()
    {
        Authorize();
        var service = RegisterRecipeService();
        service.Setup(SearchAsyncSetup())
            .ThrowsAsync(new OperationCanceledException("boom-cancelled"));

        var cut = _context.RenderComponent<RecipeSearch>();

        cut.WaitForAssertion(() =>
        {
            cut.Find(".alert-danger").InnerHtml.Should().Contain("Die Suche ist fehlgeschlagen.");
        });
        cut.FindAll(".alert-secondary").Should().BeEmpty();
    }

    /// <summary>
    /// Run search async shows the search failed alert when the search fails with a
    /// non-cancellation exception.
    /// </summary>
    [Fact]
    public void RunSearchAsync_WhenRequestFails_ShowsSearchFailed()
    {
        Authorize();
        var service = RegisterRecipeService();
        service.Setup(SearchAsyncSetup())
            .ThrowsAsync(new InvalidOperationException("boom-request"));

        var cut = _context.RenderComponent<RecipeSearch>();

        cut.WaitForAssertion(() =>
        {
            cut.Find(".alert-danger").InnerHtml.Should().Contain("Die Suche ist fehlgeschlagen.");
        });
        cut.FindAll(".alert-secondary").Should().BeEmpty();
    }

    /// <summary>
    /// Run search async shows the search failed alert when there is no authenticated user.
    /// </summary>
    [Fact]
    public void RunSearchAsync_WhenUnauthenticated_ShowsSearchFailed()
    {
        _context.AddTestAuthorization().SetNotAuthorized();
        var service = RegisterRecipeService();

        var cut = _context.RenderComponent<RecipeSearch>();

        cut.WaitForAssertion(() =>
        {
            cut.Find(".alert-danger").InnerHtml.Should().Contain("Die Suche ist fehlgeschlagen.");
        });
        service.Verify(SearchAsyncSetup(), Times.Never);
    }

    /// <summary>
    /// Run search async shows the no search results alert for an empty result set.
    /// </summary>
    [Fact]
    public void RunSearchAsync_WhenEmpty_ShowsNoSearchResults()
    {
        Authorize();
        var service = RegisterRecipeService();
        service.Setup(SearchAsyncSetup())
            .ReturnsAsync(new RecipeService.SearchResult { Items = Array.Empty<RecipeService.SearchResultItem>(), TotalCount = 0 });

        var cut = _context.RenderComponent<RecipeSearch>();

        cut.WaitForAssertion(() =>
        {
            cut.Find(".alert-secondary").InnerHtml.Should().Contain("Keine Treffer.");
        });
        cut.FindAll(".alert-danger").Should().BeEmpty();
    }

    /// <summary>
    /// Run search async renders the returned items and queries the service with the
    /// authenticated user id and the current paging values.
    /// </summary>
    [Fact]
    public void RunSearchAsync_WhenResults_RendersListItems()
    {
        Authorize();
        var service = RegisterRecipeService();
        service.Setup(SearchAsyncSetup())
            .ReturnsAsync(new RecipeService.SearchResult
            {
                Items = new[]
                {
                    new RecipeService.SearchResultItem
                    {
                        Id = "recipe-1",
                        Title = "Honigkuchen",
                        Snippet = "Schnell gemacht",
                        CreatedAt = new DateTime(2024, 1, 1),
                        PublishedLabel = "2024-01-01"
                    }
                },
                TotalCount = 1
            });

        var cut = _context.RenderComponent<RecipeSearch>();

        cut.WaitForAssertion(() =>
        {
            cut.Find(".list-group-item").InnerHtml.Should().Contain("Honigkuchen");
        });

        service.Verify(s => s.SearchAsync(
            "user-1",
            It.IsAny<string?>(),
            null,
            null,
            1,
            20,
            "newest",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Dispose logs an in-flight search aborted by the component teardown as superseded
    /// instead of a cancellation failure, because a teardown abort is not a failed
    /// search attempt and must not be misclassified in the diagnostic signal.
    /// </summary>
    [Fact]
    public async Task Dispose_WhenSearchInFlight_LogsCancellationAsSuperseded()
    {
        Authorize();
        var logger = new ListLogger<RecipeSearch>();
        _context.Services.AddSingleton<ILogger<RecipeSearch>>(logger);

        var searchStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var service = RegisterRecipeService();
        service.Setup(SearchAsyncSetup())
            .Returns<string, string?, string?, string?, int, int, string, CancellationToken>(async (_, _, _, _, _, _, _, ct) =>
            {
                searchStarted.TrySetResult();
                await Task.Delay(Timeout.Infinite, ct);
                return new RecipeService.SearchResult();
            });

        var cut = _context.RenderComponent<RecipeSearch>();
        await searchStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // Dispose the component directly so the teardown cancellation is triggered, then
        // pump the renderer dispatcher so the faulted in-flight search continuation runs.
        ((IDisposable)cut.Instance).Dispose();
        await cut.InvokeAsync(() => Task.CompletedTask);

        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (logger.Entries.IsEmpty && DateTime.UtcNow < deadline)
        {
            await Task.Delay(50);
        }

        logger.Entries.Should().NotContain(
            e => e.Level >= LogLevel.Warning,
            "a search aborted by component teardown is a superseded search, not a cancellation failure");
        logger.Entries.Should().Contain(
            e => e.Level == LogLevel.Information,
            "a search aborted by component teardown is logged as superseded");
    }

    /// <summary>
    /// Dispose.
    /// </summary>
    public void Dispose()
    {
        _context.Dispose();
    }

    private void Authorize()
    {
        var auth = _context.AddTestAuthorization();
        auth.SetAuthorized("testuser");
        auth.SetClaims(new Claim(ClaimTypes.NameIdentifier, "user-1"));
    }

    private Mock<IRecipeService> RegisterRecipeService()
    {
        var service = new Mock<IRecipeService>();
        _context.Services.AddSingleton(service.Object);
        return service;
    }

    private static System.Linq.Expressions.Expression<Func<IRecipeService, Task<RecipeService.SearchResult>>> SearchAsyncSetup()
    {
        return s => s.SearchAsync(
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>());
    }

    private sealed class PathBaseNavigationManager : NavigationManager
    {
        public PathBaseNavigationManager(string baseUri)
        {
            Initialize(baseUri, baseUri);
        }
    }

    private sealed class ListLogger<T> : ILogger<T>
    {
        public ConcurrentQueue<(LogLevel Level, string Message)> Entries { get; } = new();

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Entries.Enqueue((logLevel, formatter(state, exception)));
        }
    }
}
