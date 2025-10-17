using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Rezepte.Web.Data;
using Rezepte.Web.Services;
using Rezepte.Web.Entities;
using Xunit;

namespace Rezepte.Tests.Services;

public class SettingsServiceTests
{
    private static RezepteDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<RezepteDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new RezepteDbContext(options);
    }

    [Fact]
    public async Task GetUserAiEnabledAsync_ShouldReturnTrueByDefault_WhenNoSettingExists()
    {
        using var db = CreateDb();
        var sut = new SettingsService(db);

        var result = await sut.GetUserAiEnabledAsync("user-1", CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task SetUserAiEnabledAsync_ShouldPersistValue_AndBeReadable()
    {
        using var db = CreateDb();
        var sut = new SettingsService(db);

        await sut.SetUserAiEnabledAsync("user-2", false, CancellationToken.None);

        var read = await sut.GetUserAiEnabledAsync("user-2", CancellationToken.None);
        read.Should().BeFalse();

        // update to true
        await sut.SetUserAiEnabledAsync("user-2", true, CancellationToken.None);
        var read2 = await sut.GetUserAiEnabledAsync("user-2", CancellationToken.None);
        read2.Should().BeTrue();
    }

    [Fact]
    public async Task GetGlobalAiEnabledAsync_ShouldReturnTrueByDefault_WhenNoSettingExists()
    {
        using var db = CreateDb();
        var sut = new SettingsService(db);

        var result = await sut.GetGlobalAiEnabledAsync(CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task SetGlobalAiEnabledAsync_ShouldPersistValue_AndBeReadable()
    {
        using var db = CreateDb();
        var sut = new SettingsService(db);

        await sut.SetGlobalAiEnabledAsync(false, CancellationToken.None);

        var read = await sut.GetGlobalAiEnabledAsync(CancellationToken.None);
        read.Should().BeFalse();

        // update to true
        await sut.SetGlobalAiEnabledAsync(true, CancellationToken.None);
        var read2 = await sut.GetGlobalAiEnabledAsync(CancellationToken.None);
        read2.Should().BeTrue();
    }

    [Fact]
    public async Task UserAndGlobalSettings_ShouldBeIndependent()
    {
        using var db = CreateDb();
        var sut = new SettingsService(db);

        // set global false, user true -> effective: user still stored true, but callers should check global separately
        await sut.SetGlobalAiEnabledAsync(false, CancellationToken.None);
        await sut.SetUserAiEnabledAsync("user-3", true, CancellationToken.None);

        var global = await sut.GetGlobalAiEnabledAsync(CancellationToken.None);
        var user = await sut.GetUserAiEnabledAsync("user-3", CancellationToken.None);

        global.Should().BeFalse();
        user.Should().BeTrue();
    }
}