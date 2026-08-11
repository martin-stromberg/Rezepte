using FluentAssertions;
using msTools.Updater;
using Rezepte.Web.Services.Updates;
using Xunit;

namespace Rezepte.Tests.Services;

public sealed class ApplicationUpdateSettingsServiceTests
{
    [Fact]
    public void GetStatus_ShouldMapUpdaterSnapshot()
    {
        var checkedAt = DateTimeOffset.UtcNow.AddMinutes(-5);
        var statusProvider = new StubStatusProvider(new AutoUpdateStatusSnapshot(
            AutoUpdateState.UpdateAvailable,
            "1.0.0",
            "1.1.0",
            checkedAt,
            new AutoUpdateCheckResult("1.1.0", null, "Release notes", checkedAt),
            null,
            null,
            null,
            false,
            null));
        var sut = new ApplicationUpdateSettingsService(statusProvider, new RecordingCommandHandler());

        var status = sut.GetStatus();

        status.State.Should().Be("UpdateAvailable");
        status.InstalledVersion.Should().Be("1.0.0");
        status.AvailableVersion.Should().Be("1.1.0");
        status.LastCheckSummary.Should().Be("Version 1.1.0 gefunden.");
    }

    [Fact]
    public async Task InstallAsync_ShouldConfirmDowntime()
    {
        var commandHandler = new RecordingCommandHandler();
        var sut = new ApplicationUpdateSettingsService(
            new StubStatusProvider(AutoUpdateStatusSnapshot.Idle("1.0.0")),
            commandHandler);

        var result = await sut.InstallAsync();

        commandHandler.InstallCalled.Should().BeTrue();
        commandHandler.InstallConfirmDowntime.Should().BeTrue();
        result.Outcome.Should().Be(nameof(AutoUpdateOutcome.Success));
    }

    [Fact]
    public async Task CheckAsync_ShouldReturnFailedResult()
    {
        var commandHandler = new RecordingCommandHandler
        {
            CheckResult = new AutoUpdateResult(
                AutoUpdateOutcome.Failed,
                AutoUpdateState.Failed,
                "Quelle konnte nicht geprueft werden.",
                new InvalidOperationException("source failed"))
        };
        var sut = new ApplicationUpdateSettingsService(
            new StubStatusProvider(AutoUpdateStatusSnapshot.Idle("1.0.0")),
            commandHandler);

        var result = await sut.CheckAsync();

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("source failed");
    }

    private sealed class StubStatusProvider : IAutoUpdateStatusProvider
    {
        private readonly AutoUpdateStatusSnapshot _snapshot;

        public StubStatusProvider(AutoUpdateStatusSnapshot snapshot)
        {
            _snapshot = snapshot;
        }

        public AutoUpdateStatusSnapshot GetSnapshot() => _snapshot;
    }

    private sealed class RecordingCommandHandler : IAutoUpdateCommandHandler
    {
        public bool InstallCalled { get; private set; }
        public bool InstallConfirmDowntime { get; private set; }
        public AutoUpdateResult CheckResult { get; set; } = new(AutoUpdateOutcome.NoUpdate, AutoUpdateState.Idle, "Keine neue Version.", null);

        public Task<AutoUpdateResult> CheckAsync(CancellationToken ct = default) => Task.FromResult(CheckResult);

        public Task<AutoUpdateResult> DownloadAsync(CancellationToken ct = default)
            => Task.FromResult(new AutoUpdateResult(AutoUpdateOutcome.Success, AutoUpdateState.ReadyToInstall, "Download abgeschlossen.", null));

        public Task<AutoUpdateResult> InstallAsync(bool confirmDowntime, CancellationToken ct = default)
        {
            InstallCalled = true;
            InstallConfirmDowntime = confirmDowntime;
            return Task.FromResult(new AutoUpdateResult(AutoUpdateOutcome.Success, AutoUpdateState.Installing, "Installation gestartet.", null));
        }
    }
}
