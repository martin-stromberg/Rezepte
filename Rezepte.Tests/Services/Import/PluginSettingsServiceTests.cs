using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Rezepte.Web.Data;
using Rezepte.Web.Entities;
using Rezepte.Web.Services.Import.Plugins;
using Xunit;

namespace Rezepte.Tests.Services.Import;

public class PluginSettingsServiceTests
{
    private static RezepteDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<RezepteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new RezepteDbContext(options);
    }

    [Fact]
    public async Task SetEnabledAsync_ShouldPersistActivation()
    {
        using var db = CreateDb();
        db.PluginSettings.Add(CreateSetting("plugin-a", 0, true));
        await db.SaveChangesAsync();
        var sut = new PluginSettingsService(db);

        await sut.SetEnabledAsync("plugin-a", false);

        var plugin = await db.PluginSettings.FindAsync("plugin-a");
        plugin!.Enabled.Should().BeFalse();
    }

    [Fact]
    public async Task MoveAsync_ShouldSwapOrderWithNeighbor()
    {
        using var db = CreateDb();
        db.PluginSettings.Add(CreateSetting("plugin-a", 0, true));
        db.PluginSettings.Add(CreateSetting("plugin-b", 1, true));
        await db.SaveChangesAsync();
        var sut = new PluginSettingsService(db);

        await sut.MoveAsync("plugin-b", -1);

        var ordered = await sut.GetPluginsAsync();
        ordered.Select(p => p.PluginId).Should().Equal("plugin-b", "plugin-a");
    }

    private static PluginSetting CreateSetting(string pluginId, int orderIndex, bool enabled)
    {
        return new PluginSetting
        {
            PluginId = pluginId,
            DisplayName = pluginId,
            AssemblyName = "TestAssembly",
            TypeName = "TestHandler",
            Enabled = enabled,
            OrderIndex = orderIndex,
            Status = PluginStatus.Loaded,
            DiscoveredAt = DateTime.UtcNow,
            LastSeenAt = DateTime.UtcNow
        };
    }
}
