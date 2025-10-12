using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Rezepte.Web.Data;
using Rezepte.Web.Entities;
using Rezepte.Web.Services;
using Xunit;

namespace Rezepte.Tests.Services;

public class CookbookServiceTests
{
    private static RezepteDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<RezepteDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new RezepteDbContext(options);
    }

    private const string UserA = "user-a";
    private const string UserB = "user-b";

    [Fact]
    public async Task CreateAsync_ShouldCreate_ForUser()
    {
        using var db = CreateDb();
        var sut = new CookbookService(db);
        var (ok, error, book) = await sut.CreateAsync(UserA, "Mein Buch", null, CancellationToken.None);
        ok.Should().BeTrue();
        error.Should().BeNull();
        book.Should().NotBeNull();
        book!.UserId.Should().Be(UserA);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnOnlyUserCookbooks()
    {
        using var db = CreateDb();
        var sut = new CookbookService(db);
        await sut.CreateAsync(UserA, "A01", null, CancellationToken.None);
        await sut.CreateAsync(UserA, "A02", null, CancellationToken.None);
        await sut.CreateAsync(UserB, "B01", null, CancellationToken.None);

        var listA = await sut.GetAllAsync(UserA, CancellationToken.None);
        var listB = await sut.GetAllAsync(UserB, CancellationToken.None);

        listA.Should().HaveCount(2);
        listA.All(c => c.UserId == UserA).Should().BeTrue();
        listB.Should().HaveCount(1);
        listB.Single().UserId.Should().Be(UserB);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnOnlyUserCookbook()
    {
        using var db = CreateDb();
        var sut = new CookbookService(db);
        var (okA, errorA, a) = await sut.CreateAsync(UserA, "A01", null, CancellationToken.None);
        okA.Should().BeTrue($"CreateAsync for UserA failed: {errorA}");
        a.Should().NotBeNull();
        var (okB, errorB, b) = await sut.CreateAsync(UserB, "B01", null, CancellationToken.None);
        okB.Should().BeTrue($"CreateAsync for UserB failed: {errorB}");
        b.Should().NotBeNull();

        var foundA = await sut.GetByIdAsync(UserA, a!.Id, CancellationToken.None);
        var foundB = await sut.GetByIdAsync(UserA, b!.Id, CancellationToken.None);

        foundA.Should().NotBeNull();
        foundA!.UserId.Should().Be(UserA);
        foundB.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateOnlyUserCookbook()
    {
        using var db = CreateDb();
        var sut = new CookbookService(db);
        var (okA, errorA, a) = await sut.CreateAsync(UserA, "A01", null, CancellationToken.None);
        okA.Should().BeTrue($"CreateAsync for UserA failed: {errorA}");
        a.Should().NotBeNull();
        var (okB, errorB, b) = await sut.CreateAsync(UserB, "B01", null, CancellationToken.None);
        okB.Should().BeTrue($"CreateAsync for UserB failed: {errorB}");
        b.Should().NotBeNull();

        var (ok, err) = await sut.UpdateAsync(UserA, a!.Id, "A01-Edit", "Desc", CancellationToken.None);
        ok.Should().BeTrue();
        err.Should().BeNull();
        var updated = await sut.GetByIdAsync(UserA, a.Id, CancellationToken.None);
        updated!.Name.Should().Be("A01-Edit");
        updated.Description.Should().Be("Desc");

        var (fail, _) = await sut.UpdateAsync(UserA, b!.Id, "XXX", null, CancellationToken.None);
        fail.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteOnlyUserCookbook()
    {
        using var db = CreateDb();
        var sut = new CookbookService(db);
        var (okA, errorA, a) = await sut.CreateAsync(UserA, "A01", null, CancellationToken.None);
        okA.Should().BeTrue($"CreateAsync for UserA failed: {errorA}");
        a.Should().NotBeNull();
        var (okB, errorB, b) = await sut.CreateAsync(UserB, "B01", null, CancellationToken.None);
        okB.Should().BeTrue($"CreateAsync for UserB failed: {errorB}");
        b.Should().NotBeNull();

        var (ok, err) = await sut.DeleteAsync(UserA, a!.Id, CancellationToken.None);
        ok.Should().BeTrue();
        err.Should().BeNull();
        (await sut.GetByIdAsync(UserA, a.Id, CancellationToken.None)).Should().BeNull();
        (await sut.GetByIdAsync(UserB, b!.Id, CancellationToken.None)).Should().NotBeNull();
    }
}
