using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Rezepte.Web.Services.Import.Plugins;
using Xunit;

namespace Rezepte.Tests.Services.Import;

/// <summary>
/// Tests for <see cref="PluginSettingsService.CheckSourceForUpdatesAsync"/> admin guard and delegation.
/// </summary>
public sealed class PluginSettingsServiceTests_CheckSource : PluginSettingsServiceTestsBase
{
    /// <summary>
    /// Check source for updates async should reject non admin users.
    /// </summary>
    [Fact]
    public async Task CheckSourceForUpdatesAsync_ShouldRejectNonAdminUsers()
    {
        using var db = CreateDb();
        var updateService = new FakePluginUpdateService();
        var sut = new PluginSettingsService(db, new FakePluginManager(), CreateServiceProvider(), updateService, CreateHttpContextAccessor(isAdmin: false));

        var act = () => sut.CheckSourceForUpdatesAsync("source-1");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        updateService.CheckSourceCalls.Should().Be(0);
    }

    /// <summary>
    /// Check source for updates async should reject missing http context.
    /// </summary>
    [Fact]
    public async Task CheckSourceForUpdatesAsync_ShouldRejectMissingHttpContext()
    {
        using var db = CreateDb();
        var updateService = new FakePluginUpdateService();
        var sut = new PluginSettingsService(db, new FakePluginManager(), CreateServiceProvider(), updateService, new HttpContextAccessor());

        var act = () => sut.CheckSourceForUpdatesAsync("source-1");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        updateService.CheckSourceCalls.Should().Be(0);
    }

    /// <summary>
    /// Check source for updates async should delegate to update service.
    /// </summary>
    [Fact]
    public async Task CheckSourceForUpdatesAsync_ShouldDelegateToUpdateService()
    {
        using var db = CreateDb();
        var expected = PluginUpdateCheckResult.Installed("v1");
        var updateService = new FakePluginUpdateService(expected);
        var sut = new PluginSettingsService(db, new FakePluginManager(), CreateServiceProvider(), updateService, CreateHttpContextAccessor(isAdmin: true));

        var result = await sut.CheckSourceForUpdatesAsync("source-1");

        result.Should().BeSameAs(expected);
        updateService.CheckSourceCalls.Should().Be(1);
        updateService.LastSourceId.Should().Be("source-1");
    }
}
