using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Rezepte.Tests.ViewModels;
using Rezepte.Web.Components.Shared;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Rezepte.Tests.Components;

/// <summary>
/// Class representing the share recipe dialog tests.
/// </summary>
public sealed class ShareRecipeDialogTests : IDisposable
{
    private readonly BunitContext _context = new();

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public ShareRecipeDialogTests()
    {
        _context.Services.AddLocalization();
    }

    /// <summary>
    /// Hides the side dish checkbox when the recipe has no side dishes.
    /// </summary>
    [Fact]
    public void HidesSideDishCheckbox_WithoutSideDishes()
    {
        var factory = new ApiClientTestFactory(_ => new HttpResponseMessage(HttpStatusCode.OK));
        _context.Services.AddSingleton(factory.Create());

        var cut = _context.Render<ShareRecipeDialog>(parameters => parameters
            .Add(p => p.Show, true)
            .Add(p => p.RecipeId, "recipe-1")
            .Add(p => p.HasSideDishes, false));

        cut.FindAll("#share-include-sidedishes").Should().BeEmpty();
    }

    /// <summary>
    /// Shows the side dish checkbox when the recipe has side dishes.
    /// </summary>
    [Fact]
    public void ShowsSideDishCheckbox_WithSideDishes()
    {
        var factory = new ApiClientTestFactory(_ => new HttpResponseMessage(HttpStatusCode.OK));
        _context.Services.AddSingleton(factory.Create());

        var cut = _context.Render<ShareRecipeDialog>(parameters => parameters
            .Add(p => p.Show, true)
            .Add(p => p.RecipeId, "recipe-1")
            .Add(p => p.HasSideDishes, true));

        cut.Find("#share-include-sidedishes").Should().NotBeNull();
    }

    /// <summary>
    /// Posts the create request with the side dish flag and shows the absolute link.
    /// </summary>
    [Fact]
    public void PostsCreateRequest_WithFlag_AndShowsAbsoluteLink()
    {
        string? requestBody = null;
        var factory = new ApiClientTestFactory(async request =>
        {
            requestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { token = "tok-123", expiresAt = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc) })
            };
        });
        _context.Services.AddSingleton(factory.Create());

        var cut = _context.Render<ShareRecipeDialog>(parameters => parameters
            .Add(p => p.Show, true)
            .Add(p => p.RecipeId, "recipe-1")
            .Add(p => p.HasSideDishes, true));

        cut.Find("#share-include-sidedishes").Change(true);
        cut.Find("button.btn-primary").Click();

        cut.WaitForAssertion(() => cut.Find("#share-link-url").GetAttribute("value").Should().Be("http://localhost/share/tok-123"));
        factory.Requests.Should().ContainSingle();
        factory.Requests[0].RequestUri!.ToString().Should().Be("http://localhost/api/recipes/recipe-1/share");
        requestBody.Should().Contain("\"includeSideDishes\":true");
    }

    /// <summary>
    /// Invokes the clipboard javascript when the copy button is clicked.
    /// </summary>
    [Fact]
    public void InvokesClipboardJs_OnCopy()
    {
        _context.JSInterop.Setup<bool>("rezepteCopyToClipboard", _ => true).SetResult(true);
        var factory = new ApiClientTestFactory(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new { token = "tok-123", expiresAt = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc) })
        });
        _context.Services.AddSingleton(factory.Create());

        var cut = _context.Render<ShareRecipeDialog>(parameters => parameters
            .Add(p => p.Show, true)
            .Add(p => p.RecipeId, "recipe-1"));

        cut.Find("button.btn-primary").Click();
        cut.WaitForAssertion(() => cut.Find("#share-link-url"));

        cut.Find("button.btn-primary").Click();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Zwischenablage"));
        _context.JSInterop.VerifyInvoke("rezepteCopyToClipboard", 1);
    }

    /// <summary>
    /// Shows an error message when the create request fails.
    /// </summary>
    [Fact]
    public void ShowsError_WhenCreateFails()
    {
        var factory = new ApiClientTestFactory(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        _context.Services.AddSingleton(factory.Create());

        var cut = _context.Render<ShareRecipeDialog>(parameters => parameters
            .Add(p => p.Show, true)
            .Add(p => p.RecipeId, "recipe-1"));

        cut.Find("button.btn-primary").Click();

        cut.WaitForAssertion(() => cut.Find(".alert-danger"));
    }

    /// <summary>
    /// Dispose.
    /// </summary>
    public void Dispose()
    {
        _context.Dispose();
    }
}
