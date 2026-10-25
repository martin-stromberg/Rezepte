using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Rezepte.Web.Data;
using Rezepte.Web.Entities;
using Rezepte.Web.Services;
using Xunit;

namespace Rezepte.Tests.Services;

/// <summary>
/// Class representing the recipe service search tests on the production SQLite provider.
/// </summary>
public class RecipeServiceSearchSqliteTests : IDisposable
{
    private const string UserA = "user-a";
    private const string UserB = "user-b";

    private SqliteConnection? _connection;

    /// <summary>
    /// Disposes the SQLite connection that keeps the in-memory database alive.
    /// </summary>
    public void Dispose()
    {
        _connection?.Dispose();
    }

    private RezepteDbContext CreateSqliteDb()
    {
        // the connection is held in a field because EF Core does not dispose externally
        // supplied connections; it stays open for the lifetime of the test instance
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<RezepteDbContext>()
            .UseSqlite(_connection)
            .Options;

        var db = new RezepteDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private static RecipeService CreateService(RezepteDbContext db)
    {
        var env = new Mock<IWebHostEnvironment>();
        env.SetupGet(e => e.WebRootPath).Returns("wwwroot");

        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        return new RecipeService(db, env.Object, httpContextAccessor.Object);
    }

    /// <summary>
    /// Search async should find a recipe by an exact substring of its title on SQLite.
    /// </summary>
    [Fact]
    public async Task SearchAsync_ShouldFindByTitle_OnSqlite()
    {
        await using var db = CreateSqliteDb();
        db.Recipes.Add(new Recipe { UserId = UserA, Title = "Honig - Senf - Sojamarinade" });
        db.Recipes.Add(new Recipe { UserId = UserA, Title = "Tomatensalat" });
        await db.SaveChangesAsync();
        var sut = CreateService(db);

        var result = await sut.SearchAsync(UserA, "Honig", tags: null, cookbookId: null, page: 1, pageSize: 10, sort: "relevance", ct: CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Select(i => i.Title).Should().ContainSingle()
            .Which.Should().Be("Honig - Senf - Sojamarinade");
    }

    /// <summary>
    /// Search async should be case-insensitive for ASCII characters on SQLite.
    /// </summary>
    [Fact]
    public async Task SearchAsync_ShouldBeCaseInsensitiveAscii_OnSqlite()
    {
        await using var db = CreateSqliteDb();
        db.Recipes.Add(new Recipe { UserId = UserA, Title = "Honigbrot" });
        await db.SaveChangesAsync();
        var sut = CreateService(db);

        var result = await sut.SearchAsync(UserA, "honig", tags: null, cookbookId: null, page: 1, pageSize: 10, sort: "relevance", ct: CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Select(i => i.Title).Should().ContainSingle()
            .Which.Should().Be("Honigbrot");
    }

    /// <summary>
    /// Search async should find a recipe through a step description on SQLite.
    /// </summary>
    [Fact]
    public async Task SearchAsync_ShouldFindByStepDescription_OnSqlite()
    {
        await using var db = CreateSqliteDb();
        db.Recipes.Add(new Recipe
        {
            UserId = UserA,
            Title = "Marinade",
            Steps = new List<RecipeStep>
            {
                new() { StepIndex = 0, Description = "Mit Senf abschmecken" }
            }
        });
        await db.SaveChangesAsync();
        var sut = CreateService(db);

        var result = await sut.SearchAsync(UserA, "abschmecken", tags: null, cookbookId: null, page: 1, pageSize: 10, sort: "relevance", ct: CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Select(i => i.Title).Should().ContainSingle()
            .Which.Should().Be("Marinade");
    }

    /// <summary>
    /// Search async should find a recipe through an ingredient name on SQLite.
    /// </summary>
    [Fact]
    public async Task SearchAsync_ShouldFindByIngredientName_OnSqlite()
    {
        await using var db = CreateSqliteDb();
        db.Recipes.Add(new Recipe
        {
            UserId = UserA,
            Title = "Dressing",
            Steps = new List<RecipeStep>
            {
                new()
                {
                    StepIndex = 0,
                    Description = "Verrühren",
                    Ingredients = new List<RecipeIngredient>
                    {
                        new() { Amount = 1, Unit = "TL", Name = "Pfefferkörner" }
                    }
                }
            }
        });
        await db.SaveChangesAsync();
        var sut = CreateService(db);

        var result = await sut.SearchAsync(UserA, "Pfeffer", tags: null, cookbookId: null, page: 1, pageSize: 10, sort: "relevance", ct: CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Select(i => i.Title).Should().ContainSingle()
            .Which.Should().Be("Dressing");
    }

    /// <summary>
    /// Search async should not return recipes of a foreign user on SQLite.
    /// </summary>
    [Fact]
    public async Task SearchAsync_ShouldNotReturnForeignUserRecipes_OnSqlite()
    {
        await using var db = CreateSqliteDb();
        db.Recipes.Add(new Recipe { UserId = UserA, Title = "Honig - Senf - Sojamarinade" });
        await db.SaveChangesAsync();
        var sut = CreateService(db);

        var result = await sut.SearchAsync(UserB, "Honig", tags: null, cookbookId: null, page: 1, pageSize: 10, sort: "relevance", ct: CancellationToken.None);

        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    /// <summary>
    /// Search async should report the total count before paging and return only the requested page on SQLite.
    /// </summary>
    [Fact]
    public async Task SearchAsync_ShouldPageAndCount_OnSqlite()
    {
        await using var db = CreateSqliteDb();
        db.Recipes.Add(new Recipe { UserId = UserA, Title = "Eintrag A" });
        db.Recipes.Add(new Recipe { UserId = UserA, Title = "Eintrag B" });
        db.Recipes.Add(new Recipe { UserId = UserA, Title = "Eintrag C" });
        await db.SaveChangesAsync();
        var sut = CreateService(db);

        var result = await sut.SearchAsync(UserA, "Eintrag", tags: null, cookbookId: null, page: 2, pageSize: 2, sort: "title", ct: CancellationToken.None);

        result.TotalCount.Should().Be(3);
        result.Items.Should().ContainSingle()
            .Which.Title.Should().Be("Eintrag C");
    }

    /// <summary>
    /// Search async should clamp out-of-range paging values so all callers share the
    /// same bound as the API endpoint.
    /// </summary>
    [Fact]
    public async Task SearchAsync_ShouldClampOutOfRangePaging_OnSqlite()
    {
        await using var db = CreateSqliteDb();
        db.Recipes.Add(new Recipe { UserId = UserA, Title = "Eintrag A" });
        db.Recipes.Add(new Recipe { UserId = UserA, Title = "Eintrag B" });
        await db.SaveChangesAsync();
        var sut = CreateService(db);

        var result = await sut.SearchAsync(UserA, "Eintrag", tags: null, cookbookId: null, page: 0, pageSize: 0, sort: "title", ct: CancellationToken.None);

        result.TotalCount.Should().Be(2);
        result.Items.Should().ContainSingle()
            .Which.Title.Should().Be("Eintrag A", "page 0 is clamped to 1 and pageSize 0 is clamped to 1");
    }

    /// <summary>
    /// Search async should provide the formatted published label so UI and API render
    /// the same value.
    /// </summary>
    [Fact]
    public async Task SearchAsync_ShouldPopulatePublishedLabel_OnSqlite()
    {
        await using var db = CreateSqliteDb();
        db.Recipes.Add(new Recipe { UserId = UserA, Title = "Honigbrot", CreatedAt = new DateTime(2024, 3, 5) });
        await db.SaveChangesAsync();
        var sut = CreateService(db);

        var result = await sut.SearchAsync(UserA, "Honig", tags: null, cookbookId: null, page: 1, pageSize: 10, sort: "relevance", ct: CancellationToken.None);

        result.Items.Should().ContainSingle()
            .Which.PublishedLabel.Should().Be("2024-03-05");
    }
}
