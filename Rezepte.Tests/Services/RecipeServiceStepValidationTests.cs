using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Moq;
using Rezepte.Web.Data;
using Rezepte.Web.Services;
using Xunit;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Rezepte.Tests.Services;

public class RecipeServiceStepValidationTests
{
    private const string UserA = "user-a";

    private static RezepteDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<RezepteDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new RezepteDbContext(options);
    }

    private static IWebHostEnvironment CreateMockEnv()
    {
        var mock = new Mock<IWebHostEnvironment>();
        mock.SetupGet(e => e.WebRootPath).Returns("wwwroot");
        return mock.Object;
    }

    private static IHttpContextAccessor CreateMockHttpContextAccessor()
    {
        var mock = new Mock<IHttpContextAccessor>();
        // Provide a HttpContext with an authenticated user to avoid NREs in RecipeService.CurrentUserId
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, UserA)
        }, "test"));
        mock.SetupGet(m => m.HttpContext).Returns(context);
        return mock.Object;
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenStepDescriptionMissing()
    {
        using var db = CreateDb();
        var cb = new Rezepte.Web.Entities.Cookbook { Id = Guid.NewGuid().ToString(), Name = "CB", UserId = UserA };
        db.Cookbooks.Add(cb);
        await db.SaveChangesAsync(CancellationToken.None);

        var sut = new RecipeService(db, CreateMockEnv(), CreateMockHttpContextAccessor());
        var steps = new[]
        {
            new RecipeCreateStep(
                Title: "Step1",
                Description: "", // missing description
                DurationMinutes: 5,
                RequiresOvernightRest: false,
                Ingredients: Array.Empty<RecipeCreateIngredient>()
            )
        };

        var (ok, error, recipe) = await sut.CreateAsync(
            UserA,
            cb.Id,
            "Test",
            description: null,
            uri: null,
            portions: null,
            steps: steps,
            ct: CancellationToken.None);

        ok.Should().BeFalse();
        error.Should().NotBeNull();
        recipe.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenStepDurationNegative()
    {
        using var db = CreateDb();
        var cb = new Rezepte.Web.Entities.Cookbook { Id = Guid.NewGuid().ToString(), Name = "CB", UserId = UserA };
        db.Cookbooks.Add(cb);
        await db.SaveChangesAsync(CancellationToken.None);

        var sut = new RecipeService(db, CreateMockEnv(), CreateMockHttpContextAccessor());
        var steps = new[]
        {
            new RecipeCreateStep(
                Title: "Step1",
                Description: "Do something",
                DurationMinutes: -1, // invalid
                RequiresOvernightRest: false,
                Ingredients: Array.Empty<RecipeCreateIngredient>()
            )
        };

        var (ok, error, recipe) = await sut.CreateAsync(
            UserA,
            cb.Id,
            "Test",
            description: null,
            uri: null,
            portions: null,
            steps: steps,
            ct: CancellationToken.None);

        ok.Should().BeFalse();
        error.Should().NotBeNull();
        recipe.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenIngredientNameMissing()
    {
        using var db = CreateDb();
        var cb = new Rezepte.Web.Entities.Cookbook { Id = Guid.NewGuid().ToString(), Name = "CB", UserId = UserA };
        db.Cookbooks.Add(cb);
        await db.SaveChangesAsync(CancellationToken.None);

        var sut = new RecipeService(db, CreateMockEnv(), CreateMockHttpContextAccessor());
        var steps = new[]
        {
            new RecipeCreateStep(
                Title: "Step1",
                Description: "Do something",
                DurationMinutes: 5,
                RequiresOvernightRest: false,
                Ingredients: new[] { new RecipeCreateIngredient(1m, "g", "") } // missing ingredient name
            )
        };

        var (ok, error, recipe) = await sut.CreateAsync(
            UserA,
            cb.Id,
            "Test",
            description: null,
            uri: null,
            portions: null,
            steps: steps,
            ct: CancellationToken.None);

        ok.Should().BeFalse();
        error.Should().NotBeNull();
        recipe.Should().BeNull();
    }
}