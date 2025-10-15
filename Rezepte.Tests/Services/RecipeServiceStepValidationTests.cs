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
        // Optional: Setup mock if needed for tests
        return mock.Object;
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenStepDescriptionMissing()
    {
        using var db = CreateDb();
        var cb = new Rezepte.Web.Entities.Cookbook { Name = "CB", UserId = UserA };
        db.Cookbooks.Add(cb);
        await db.SaveChangesAsync();

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

        var (ok, error, recipe) = await sut.CreateAsync(UserA, cb.Id, "Test", null, null, steps, CancellationToken.None);

        ok.Should().BeFalse();
        error.Should().NotBeNull();
        recipe.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenStepDurationNegative()
    {
        using var db = CreateDb();
        var cb = new Rezepte.Web.Entities.Cookbook { Name = "CB", UserId = UserA };
        db.Cookbooks.Add(cb);
        await db.SaveChangesAsync();

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

        var (ok, error, recipe) = await sut.CreateAsync(UserA, cb.Id, "Test", null, null, steps, CancellationToken.None);

        ok.Should().BeFalse();
        error.Should().NotBeNull();
        recipe.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenIngredientNameMissing()
    {
        using var db = CreateDb();
        var cb = new Rezepte.Web.Entities.Cookbook { Name = "CB", UserId = UserA };
        db.Cookbooks.Add(cb);
        await db.SaveChangesAsync();

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

        var (ok, error, recipe) = await sut.CreateAsync(UserA, cb.Id, "Test", null, null, steps, CancellationToken.None);

        ok.Should().BeFalse();
        error.Should().NotBeNull();
        recipe.Should().BeNull();
    }
}