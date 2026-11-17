using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Rezepte.Tests.TestSupport;
using Rezepte.Web.Data;
using Rezepte.Web.Entities;
using Xunit;

namespace Rezepte.Tests.Controllers;

/// <summary>
/// Class representing the share endpoint authentication tests.
/// </summary>
public sealed class ShareEndpointAuthenticationTests
{
    /// <summary>
    /// Post share returns unauthorized without authentication.
    /// </summary>
    [Fact]
    public async Task PostShare_ReturnsUnauthorized_WithoutAuthentication()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateAnonymousClient();

        using var response = await client.PostAsJsonAsync("/api/recipes/any-recipe/share", new { includeSideDishes = false });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// Get share page returns ok without authentication for a valid token.
    /// </summary>
    [Fact]
    public async Task GetSharePage_ReturnsOk_WithoutAuthentication()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateAnonymousClient();
        var seeded = await SeedAsync(factory);

        using var response = await client.GetAsync($"/share/{seeded.Token}");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().Contain("Shared Cake");
        body.Should().NotContain("app-navbar");
    }

    /// <summary>
    /// Get share page shows error without authentication for an unknown token.
    /// </summary>
    [Fact]
    public async Task GetSharePage_ShowsError_ForUnknownToken()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateAnonymousClient();
        await SeedAsync(factory);

        using var response = await client.GetAsync("/share/unknown-token");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().Contain("alert-warning");
        body.Should().NotContain("app-navbar");
    }

    /// <summary>
    /// Get shared image returns ok without authentication.
    /// </summary>
    [Fact]
    public async Task GetSharedImage_ReturnsOk_WithoutAuthentication()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateAnonymousClient();
        var seeded = await SeedAsync(factory);

        using var response = await client.GetAsync($"/api/share/{seeded.Token}/image/{seeded.ImageId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("image/png");
        response.Headers.ETag.Should().NotBeNull();
    }

    /// <summary>
    /// Get shared image returns not found for unknown token.
    /// </summary>
    [Fact]
    public async Task GetSharedImage_ReturnsNotFound_ForUnknownToken()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateAnonymousClient();
        var seeded = await SeedAsync(factory);

        using var response = await client.GetAsync($"/api/share/unknown-token/image/{seeded.ImageId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Get shared image returns not found for an image of a foreign recipe.
    /// </summary>
    [Fact]
    public async Task GetSharedImage_ReturnsNotFound_ForForeignImage()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateAnonymousClient();
        var seeded = await SeedAsync(factory);

        using var response = await client.GetAsync($"/api/share/{seeded.Token}/image/{seeded.ForeignImageId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static SqliteFileWebApplicationFactory CreateFactory()
        => new(Path.Combine(Path.GetTempPath(), $"rezepte-share-auth-{Guid.NewGuid():N}.db"));

    private static async Task<(string Token, string ImageId, string ForeignImageId)> SeedAsync(SqliteFileWebApplicationFactory factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<RezepteDbContext>();

        var recipe = new Recipe
        {
            UserId = "owner-1",
            Title = "Shared Cake",
            Portions = 4
        };
        recipe.Images.Add(new RecipeImage { FileName = "cake.png", ContentType = "image/png", Data = [1, 2, 3] });

        var foreign = new Recipe
        {
            UserId = "owner-1",
            Title = "Foreign Recipe"
        };
        foreign.Images.Add(new RecipeImage { FileName = "secret.png", ContentType = "image/png", Data = [9, 9] });

        db.Recipes.AddRange(recipe, foreign);
        db.RecipeShareLinks.Add(new RecipeShareLink
        {
            Token = "valid-token",
            RecipeId = recipe.Id,
            UserId = "owner-1",
            IncludeSideDishes = false,
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        });
        await db.SaveChangesAsync();

        return ("valid-token", recipe.Images.Single().Id, foreign.Images.Single().Id);
    }
}
