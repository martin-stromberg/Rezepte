using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Rezepte.Web.Data;
using Rezepte.Web.Entities;
using Rezepte.Web.Services.Import.Plugins;
using System.Security.Claims;
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

    [Fact]
    public async Task SaveSourceAsync_ShouldCanonicalizeGlobalSourceAndStorePatOnlyInSecretStore()
    {
        using var db = CreateDb();
        var secrets = new FakeSecretStore();
        var sut = new PluginSettingsService(db, CreateHttpContextAccessor(isAdmin: true), secrets);

        await sut.SaveSourceAsync(new PluginSourceSaveRequest(
            null,
            "https://github.com/Owner/Repo.git",
            IsPrivate: true,
            Enabled: true,
            TrustConfirmed: true,
            PersonalAccessToken: "ghp_secret"));

        var source = await db.PluginSources.SingleAsync();
        source.RepositoryUrl.Should().Be("https://github.com/Owner/Repo");
        source.Owner.Should().Be("Owner");
        source.Repository.Should().Be("Repo");
        source.SecretName.Should().NotBeNullOrWhiteSpace();
        source.SecretName.Should().NotContain("ghp_secret");
        secrets.Values[source.SecretName!].Should().Be("ghp_secret");

        var item = (await sut.GetSourcesAsync()).Single();
        item.HasSecret.Should().BeTrue();
        item.ToString().Should().NotContain("ghp_secret");
    }

    [Fact]
    public async Task SaveSourceAsync_ShouldRequireTrustConfirmationForNewSource()
    {
        using var db = CreateDb();
        var sut = new PluginSettingsService(db, CreateHttpContextAccessor(isAdmin: true), new FakeSecretStore());

        var act = () => sut.SaveSourceAsync(new PluginSourceSaveRequest(
            null,
            "https://github.com/owner/repo",
            IsPrivate: false,
            Enabled: true,
            TrustConfirmed: false,
            PersonalAccessToken: null));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task SaveSourceAsync_ShouldRejectNonAdminUsers()
    {
        using var db = CreateDb();
        var sut = new PluginSettingsService(db, CreateHttpContextAccessor(isAdmin: false), new FakeSecretStore());

        var act = () => sut.SaveSourceAsync(new PluginSourceSaveRequest(
            null,
            "https://github.com/owner/repo",
            IsPrivate: false,
            Enabled: true,
            TrustConfirmed: true,
            PersonalAccessToken: null));

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task SaveSourceAsync_ShouldRejectMissingHttpContext()
    {
        using var db = CreateDb();
        var sut = new PluginSettingsService(db, new HttpContextAccessor(), new FakeSecretStore());

        var act = () => sut.SaveSourceAsync(new PluginSourceSaveRequest(
            null,
            "https://github.com/owner/repo",
            IsPrivate: false,
            Enabled: true,
            TrustConfirmed: true,
            PersonalAccessToken: null));

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
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

    private static IHttpContextAccessor CreateHttpContextAccessor(bool isAdmin)
    {
        var claims = isAdmin
            ? new[] { new Claim(ClaimTypes.Role, "Admin") }
            : Array.Empty<Claim>();
        return new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
            }
        };
    }

    private sealed class FakeSecretStore : ISystemSecretStore
    {
        public Dictionary<string, string> Values { get; } = [];

        public Task StoreAsync(string name, string secret, CancellationToken ct = default)
        {
            Values[name] = secret;
            return Task.CompletedTask;
        }

        public Task<string?> GetAsync(string name, CancellationToken ct = default)
            => Task.FromResult(Values.GetValueOrDefault(name));

        public Task DeleteAsync(string name, CancellationToken ct = default)
        {
            Values.Remove(name);
            return Task.CompletedTask;
        }
    }
}
