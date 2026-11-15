using FluentAssertions;
using Rezepte.Web.Services.Import.Plugins;
using Xunit;

namespace Rezepte.Tests.Services.Import;

/// <summary>
/// Tests for the process-wide serialization of <see cref="PluginUpdateService"/> checks.
/// </summary>
public sealed class PluginUpdateServiceTests_Serialization : PluginUpdateServiceTestsBase
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Check source for updates async should wait for running startup check.
    /// </summary>
    [Fact]
    public async Task CheckSourceForUpdatesAsync_ShouldWaitForRunningStartupCheck()
    {
        using var dbStartup = CreateDb();
        using var dbManual = CreateDb();
        AddSource(dbStartup);
        var manualSource = AddSource(dbManual);

        var blockingClient = new BlockingGitHubReleaseClient();
        var manualClient = new FakeGitHubReleaseClient(CreateRelease("v1"));
        var startupSut = CreateSut(dbStartup, blockingClient);
        var manualSut = CreateSut(dbManual, manualClient);

        var startupTask = startupSut.CheckForUpdatesAsync();
        try
        {
            await blockingClient.Entered.WaitAsync(Timeout);

            var manualTask = manualSut.CheckSourceForUpdatesAsync(manualSource.Id);
            await Task.Delay(250);

            manualTask.IsCompleted.Should().BeFalse();
            manualClient.LatestReleaseCalls.Should().Be(0);

            blockingClient.Release(CreateRelease("v1"));

            var result = await manualTask.WaitAsync(Timeout);
            result.Outcome.Should().Be(PluginUpdateCheckOutcome.Installed);
            manualClient.LatestReleaseCalls.Should().Be(1);
        }
        finally
        {
            blockingClient.Release(CreateRelease("v1"));
        }

        await startupTask.WaitAsync(Timeout);
    }

    /// <summary>
    /// Check for updates async should wait for running source check.
    /// </summary>
    [Fact]
    public async Task CheckForUpdatesAsync_ShouldWaitForRunningSourceCheck()
    {
        using var dbManual = CreateDb();
        using var dbStartup = CreateDb();
        var manualSource = AddSource(dbManual);
        AddSource(dbStartup);

        var blockingClient = new BlockingGitHubReleaseClient();
        var startupClient = new FakeGitHubReleaseClient(CreateRelease("v1"));
        var manualSut = CreateSut(dbManual, blockingClient);
        var startupSut = CreateSut(dbStartup, startupClient);

        var manualTask = manualSut.CheckSourceForUpdatesAsync(manualSource.Id);
        try
        {
            await blockingClient.Entered.WaitAsync(Timeout);

            var startupTask = startupSut.CheckForUpdatesAsync();
            await Task.Delay(250);

            startupTask.IsCompleted.Should().BeFalse();
            startupClient.LatestReleaseCalls.Should().Be(0);

            blockingClient.Release(CreateRelease("v1"));

            var result = await manualTask.WaitAsync(Timeout);
            result.Outcome.Should().Be(PluginUpdateCheckOutcome.Installed);
            await startupTask.WaitAsync(Timeout);
            startupClient.LatestReleaseCalls.Should().Be(1);
        }
        finally
        {
            blockingClient.Release(CreateRelease("v1"));
        }
    }

    /// <summary>
    /// Check source for updates async should release the gate after a failed check.
    /// </summary>
    [Fact]
    public async Task CheckSourceForUpdatesAsync_ShouldReleaseGateAfterFailedCheck()
    {
        using var dbFailed = CreateDb();
        using var dbSecond = CreateDb();
        var failedSource = AddSource(dbFailed);
        var secondSource = AddSource(dbSecond, id: "source-2");

        var failedSut = CreateSut(dbFailed, new ThrowingGitHubReleaseClient(new InvalidOperationException("github unavailable")));
        var secondClient = new FakeGitHubReleaseClient(CreateRelease("v1"));
        var secondSut = CreateSut(dbSecond, secondClient);

        var failed = await failedSut.CheckSourceForUpdatesAsync(failedSource.Id).WaitAsync(Timeout);
        failed.Outcome.Should().Be(PluginUpdateCheckOutcome.Failed);

        var result = await secondSut.CheckSourceForUpdatesAsync(secondSource.Id).WaitAsync(Timeout);

        result.Outcome.Should().Be(PluginUpdateCheckOutcome.Installed);
        secondClient.LatestReleaseCalls.Should().Be(1);
    }
}
