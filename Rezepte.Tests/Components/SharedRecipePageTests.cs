using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Rezepte.Web.Components.Pages;
using Rezepte.Web.Services;
using Xunit;

namespace Rezepte.Tests.Components;

/// <summary>
/// Class representing the shared recipe page tests.
/// </summary>
public sealed class SharedRecipePageTests : IDisposable
{
    private readonly TestContext _context = new();
    private readonly Mock<IRecipeShareService> _shareService = new();

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public SharedRecipePageTests()
    {
        _context.Services.AddLocalization();
        _context.Services.AddSingleton(_shareService.Object);
    }

    private static SharedRecipeView CreateView(List<SharedSideDishView>? sideDishes = null) => new(
        Title: "Shared Cake",
        Description: "Line one",
        Portions: 4,
        SourceUri: "https://example.com/source",
        ImageUrls: ["/api/share/token-abc/image/img-1"],
        Steps: [new SharedStepView("Mischen", "Alles verrühren", 10, false, [new SharedIngredientView(1m, "Tasse", "Mehl")])],
        SideDishes: sideDishes ?? []);

    /// <summary>
    /// Renders shared recipe data for a valid token.
    /// </summary>
    [Fact]
    public void RendersSharedRecipe_ForValidToken()
    {
        _shareService.Setup(s => s.GetSharedRecipeAsync("token-abc", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateView());

        var cut = _context.RenderComponent<SharedRecipePage>(parameters => parameters.Add(p => p.Token, "token-abc"));

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Shared Cake"));
        cut.Markup.Should().Contain("Mehl");
        cut.Markup.Should().Contain("Alles verrühren");
        cut.Find("img").GetAttribute("src").Should().Be("/api/share/token-abc/image/img-1");
        cut.Find("a[href=\"https://example.com/source\"]").Should().NotBeNull();
    }

    /// <summary>
    /// Renders side dish links pointing to the public side dish route.
    /// </summary>
    [Fact]
    public void RendersSideDishLinks_PointingToSharedSideDishRoute()
    {
        var view = CreateView([new SharedSideDishView("side-1", "Salat", 2, "/api/share/token-abc/image/img-2", "/share/token-abc/sidedish/side-1")]);
        _shareService.Setup(s => s.GetSharedRecipeAsync("token-abc", It.IsAny<CancellationToken>()))
            .ReturnsAsync(view);

        var cut = _context.RenderComponent<SharedRecipePage>(parameters => parameters.Add(p => p.Token, "token-abc"));

        cut.WaitForAssertion(() =>
        {
            var link = cut.Find("a[href=\"/share/token-abc/sidedish/side-1\"]");
            link.TextContent.Should().Contain("Salat");
        });
    }

    /// <summary>
    /// Shows the generic error message when the link is invalid or expired.
    /// </summary>
    [Fact]
    public void ShowsError_WhenServiceReturnsNull()
    {
        _shareService.Setup(s => s.GetSharedRecipeAsync("bad-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync((SharedRecipeView?)null);

        var cut = _context.RenderComponent<SharedRecipePage>(parameters => parameters.Add(p => p.Token, "bad-token"));

        cut.WaitForAssertion(() => cut.Find(".alert-warning"));
        cut.Markup.Should().Contain("ungültig oder abgelaufen");
    }

    /// <summary>
    /// Loads the side dish route via the side dish service method.
    /// </summary>
    [Fact]
    public void LoadsSideDish_WhenRecipeIdParameterIsSet()
    {
        _shareService.Setup(s => s.GetSharedSideDishAsync("token-abc", "side-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateView() with { Title = "Beilage Salat" });

        var cut = _context.RenderComponent<SharedRecipePage>(parameters => parameters
            .Add(p => p.Token, "token-abc")
            .Add(p => p.RecipeId, "side-1"));

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Beilage Salat"));
        _shareService.Verify(s => s.GetSharedSideDishAsync("token-abc", "side-1", It.IsAny<CancellationToken>()), Times.Once);
        _shareService.Verify(s => s.GetSharedRecipeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Dispose.
    /// </summary>
    public void Dispose()
    {
        _context.Dispose();
    }
}
