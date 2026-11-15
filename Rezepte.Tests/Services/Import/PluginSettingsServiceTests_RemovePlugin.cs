using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Rezepte.Web.Services.Import.Plugins;
using Xunit;

namespace Rezepte.Tests.Services.Import;

/// <summary>
/// Tests for <see cref="PluginSettingsService.RemovePluginAsync"/> admin guard, status guard and removal.
/// </summary>
public sealed class PluginSettingsServiceTests_RemovePlugin : PluginSettingsServiceTestsBase
{
    /// <summary>
    /// Remove plugin async should remove missing plugin.
    /// </summary>
    [Fact]
    public async Task RemovePluginAsync_ShouldRemoveMissingPlugin()
    {
        using var db = CreateDb();
        db.PluginSettings.Add(CreateSetting("plugin-a", 0, true));
        var missing = CreateSetting("plugin-b", 1, false);
        missing.Status = PluginStatus.Missing;
        db.PluginSettings.Add(missing);
        await db.SaveChangesAsync();
        var sut = new PluginSettingsService(db, new FakePluginManager(), CreateServiceProvider(), new FakePluginUpdateService(), CreateHttpContextAccessor(isAdmin: true));

        await sut.RemovePluginAsync("plugin-b");

        (await db.PluginSettings.FindAsync("plugin-b")).Should().BeNull();
        (await db.PluginSettings.FindAsync("plugin-a")).Should().NotBeNull();
        var items = await sut.GetPluginsAsync();
        items.Select(p => p.PluginId).Should().NotContain("plugin-b");
    }

    /// <summary>
    /// Remove plugin async should reject non admin users.
    /// </summary>
    [Fact]
    public async Task RemovePluginAsync_ShouldRejectNonAdminUsers()
    {
        using var db = CreateDb();
        var missing = CreateSetting("plugin-a", 0, false);
        missing.Status = PluginStatus.Missing;
        db.PluginSettings.Add(missing);
        await db.SaveChangesAsync();
        var sut = new PluginSettingsService(db, new FakePluginManager(), CreateServiceProvider(), new FakePluginUpdateService(), CreateHttpContextAccessor(isAdmin: false));

        var act = () => sut.RemovePluginAsync("plugin-a");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        (await db.PluginSettings.FindAsync("plugin-a")).Should().NotBeNull();
    }

    /// <summary>
    /// Remove plugin async should reject missing http context.
    /// </summary>
    [Fact]
    public async Task RemovePluginAsync_ShouldRejectMissingHttpContext()
    {
        using var db = CreateDb();
        var missing = CreateSetting("plugin-a", 0, false);
        missing.Status = PluginStatus.Missing;
        db.PluginSettings.Add(missing);
        await db.SaveChangesAsync();
        var sut = new PluginSettingsService(db, new FakePluginManager(), CreateServiceProvider(), new FakePluginUpdateService(), new HttpContextAccessor());

        var act = () => sut.RemovePluginAsync("plugin-a");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        (await db.PluginSettings.FindAsync("plugin-a")).Should().NotBeNull();
    }

    /// <summary>
    /// Remove plugin async should reject non missing status.
    /// </summary>
    /// <param name="status">The status parameter.</param>
    [Theory]
    [InlineData(PluginStatus.Loaded)]
    [InlineData(PluginStatus.Incompatible)]
    [InlineData(PluginStatus.LoadFailed)]
    [InlineData(PluginStatus.RuntimeFailed)]
    public async Task RemovePluginAsync_ShouldRejectNonMissingStatus(string status)
    {
        using var db = CreateDb();
        var plugin = CreateSetting("plugin-a", 0, true);
        plugin.Status = status;
        db.PluginSettings.Add(plugin);
        await db.SaveChangesAsync();
        var sut = new PluginSettingsService(db, new FakePluginManager(), CreateServiceProvider(), new FakePluginUpdateService(), CreateHttpContextAccessor(isAdmin: true));

        var act = () => sut.RemovePluginAsync("plugin-a");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*'plugin-a'*'{status}'*");
        (await db.PluginSettings.FindAsync("plugin-a")).Should().NotBeNull();
    }

    /// <summary>
    /// Remove plugin async should reject unknown plugin id.
    /// </summary>
    [Fact]
    public async Task RemovePluginAsync_ShouldRejectUnknownPluginId()
    {
        using var db = CreateDb();
        var sut = new PluginSettingsService(db, new FakePluginManager(), CreateServiceProvider(), new FakePluginUpdateService(), CreateHttpContextAccessor(isAdmin: true));

        var act = () => sut.RemovePluginAsync("unknown-plugin");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*'unknown-plugin'*not found*");
    }

    /// <summary>
    /// Remove plugin async should remove missing enabled plugin.
    /// </summary>
    [Fact]
    public async Task RemovePluginAsync_ShouldRemoveMissingEnabledPlugin()
    {
        using var db = CreateDb();
        var missing = CreateSetting("plugin-a", 0, true);
        missing.Status = PluginStatus.Missing;
        db.PluginSettings.Add(missing);
        await db.SaveChangesAsync();
        var sut = new PluginSettingsService(db, new FakePluginManager(), CreateServiceProvider(), new FakePluginUpdateService(), CreateHttpContextAccessor(isAdmin: true));

        await sut.RemovePluginAsync("plugin-a");

        (await db.PluginSettings.FindAsync("plugin-a")).Should().BeNull();
    }
}
