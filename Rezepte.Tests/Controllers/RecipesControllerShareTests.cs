using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using Rezepte.Web.Configuration;
using Rezepte.Web.Controllers;
using Rezepte.Web.Entities;
using Rezepte.Web.Services;
using Xunit;

namespace Rezepte.Tests.Controllers;

/// <summary>
/// Class representing the recipes controller share tests.
/// </summary>
public class RecipesControllerShareTests
{
    private const string UserId = "user-a";

    private static RecipesController CreateController(Mock<IRecipeService> recipes, Mock<IRecipeShareService> shareService, bool withUserClaim = true)
    {
        var identity = withUserClaim
            ? new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, UserId) }, "TestAuth")
            : new ClaimsIdentity();

        return new RecipesController(recipes.Object, shareService.Object, Options.Create(new ImageOptions()))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity)
                }
            }
        };
    }

    /// <summary>
    /// Create share link should return token and expiry.
    /// </summary>
    [Fact]
    public async Task CreateShareLink_ReturnsTokenAndExpiry()
    {
        var recipes = new Mock<IRecipeService>();
        var shareService = new Mock<IRecipeShareService>();
        var expiresAt = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        shareService
            .Setup(s => s.CreateShareLinkAsync(UserId, "recipe-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RecipeShareLink { Token = "secure-token", RecipeId = "recipe-1", UserId = UserId, IncludeSideDishes = true, ExpiresAt = expiresAt });
        var controller = CreateController(recipes, shareService);

        var result = await controller.CreateShareLink("recipe-1", new RecipesController.CreateShareLinkRequest(true), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<RecipesController.ShareLinkResponse>().Subject;
        response.Token.Should().Be("secure-token");
        response.ExpiresAt.Should().Be(expiresAt);
        shareService.Verify(s => s.CreateShareLinkAsync(UserId, "recipe-1", true, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Create share link should return not found for foreign or missing recipe.
    /// </summary>
    [Fact]
    public async Task CreateShareLink_ReturnsNotFound_ForForeignRecipe()
    {
        var recipes = new Mock<IRecipeService>();
        var shareService = new Mock<IRecipeShareService>();
        shareService
            .Setup(s => s.CreateShareLinkAsync(UserId, "foreign-recipe", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RecipeShareLink?)null);
        var controller = CreateController(recipes, shareService);

        var result = await controller.CreateShareLink("foreign-recipe", new RecipesController.CreateShareLinkRequest(false), CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    /// <summary>
    /// Create share link should return unauthorized without user claim.
    /// </summary>
    [Fact]
    public async Task CreateShareLink_WithoutUserClaim_ReturnsUnauthorized()
    {
        var recipes = new Mock<IRecipeService>();
        var shareService = new Mock<IRecipeShareService>();
        var controller = CreateController(recipes, shareService, withUserClaim: false);

        var result = await controller.CreateShareLink("recipe-1", new RecipesController.CreateShareLinkRequest(false), CancellationToken.None);

        result.Should().BeOfType<UnauthorizedResult>();
        shareService.Verify(s => s.CreateShareLinkAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
