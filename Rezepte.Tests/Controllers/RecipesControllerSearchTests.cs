using System.Security.Claims;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using Rezepte.Web.Configuration;
using Rezepte.Web.Controllers;
using Rezepte.Web.Services;
using Xunit;

namespace Rezepte.Tests.Controllers;

/// <summary>
/// Class representing the recipes controller search tests.
/// </summary>
public class RecipesControllerSearchTests
{
    private const string UserId = "user-a";

    private static RecipesController CreateController(Mock<IRecipeService> recipes, bool withUserClaim = true)
    {
        var identity = withUserClaim
            ? new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, UserId) }, "TestAuth")
            : new ClaimsIdentity();

        var controller = new RecipesController(recipes.Object, Options.Create(new ImageOptions()))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity)
                }
            }
        };
        return controller;
    }

    /// <summary>
    /// Search async should clamp page and page size before forwarding them to the service.
    /// </summary>
    /// <param name="page">The page query value passed to the controller.</param>
    /// <param name="pageSize">The page size query value passed to the controller.</param>
    /// <param name="expectedPage">The page value the service is expected to receive.</param>
    /// <param name="expectedPageSize">The page size value the service is expected to receive.</param>
    [Theory]
    [InlineData(0, 0, 1, 1)]
    [InlineData(-3, 150, 1, 100)]
    public async Task SearchAsync_WithOutOfRangePaging_ClampsPageAndPageSize(int page, int pageSize, int expectedPage, int expectedPageSize)
    {
        var recipes = new Mock<IRecipeService>();
        recipes.Setup(service => service.SearchAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RecipeService.SearchResult());
        var controller = CreateController(recipes);

        await controller.SearchAsync("x", null, null, page: page, pageSize: pageSize);

        recipes.Verify(service => service.SearchAsync(UserId, "x", null, null, expectedPage, expectedPageSize, "relevance", It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Search async should return unauthorized when the user has no name identifier claim.
    /// </summary>
    [Fact]
    public async Task SearchAsync_WithoutUserClaim_ReturnsUnauthorized()
    {
        var recipes = new Mock<IRecipeService>();
        var controller = CreateController(recipes, withUserClaim: false);

        var result = await controller.SearchAsync("x", null, null);

        result.Should().BeOfType<UnauthorizedResult>();
        recipes.Verify(service => service.SearchAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Search async should map the service result to the response dto.
    /// </summary>
    [Fact]
    public async Task SearchAsync_MapsToResponseDto()
    {
        var longSnippet = new string('a', 250);
        var recipes = new Mock<IRecipeService>();
        recipes.Setup(service => service.SearchAsync(UserId, "Honig", null, null, 1, 10, "relevance", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RecipeService.SearchResult
            {
                TotalCount = 7,
                Items = new[]
                {
                    new RecipeService.SearchResultItem
                    {
                        Id = "recipe-1",
                        Title = "Honigkuchen",
                        CreatedAt = new DateTime(2024, 3, 5),
                        Snippet = longSnippet,
                        PrimaryImageUrl = null,
                        Tags = new[] { "dessert" }
                    }
                }
            });
        var controller = CreateController(recipes);

        var result = await controller.SearchAsync("Honig", null, null, page: 1, pageSize: 10);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        document.RootElement.GetProperty("Total").GetInt32().Should().Be(7);

        var item = document.RootElement.GetProperty("Items")[0];
        item.GetProperty("Title").GetString().Should().Be("Honigkuchen");
        item.GetProperty("PublishedLabel").GetString().Should().Be("2024-03-05");
        var snippet = item.GetProperty("Snippet").GetString()!;
        snippet.Length.Should().Be(203);
        snippet.Should().EndWith("...");
        item.GetProperty("Tags").GetArrayLength().Should().Be(1);
    }
}
