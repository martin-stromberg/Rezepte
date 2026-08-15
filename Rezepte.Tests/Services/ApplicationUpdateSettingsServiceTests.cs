using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using msTools.Updater;
using Rezepte.Web.Configuration;
using Rezepte.Web.Data;
using Rezepte.Web.Entities;
using Rezepte.Web.Services.Updates;
using Xunit;

namespace Rezepte.Tests.Services;

public sealed class ApplicationUpdateSettingsServiceTests
{
    [Fact]
    public void GetStatus_ShouldMapUpdaterSnapshot()
    {
        var checkedAt = DateTimeOffset.UtcNow.AddMinutes(-5);
        var package = new AutoUpdatePackageDescriptor(
            "1.1.0",
            "linux",
            "linux-x64",
            "release.zip",
            new Uri("https://example.invalid/release.zip"),
            new string('a', 64),
            42);
        var statusProvider = new StubStatusProvider(new AutoUpdateStatusSnapshot(
            AutoUpdateState.UpdateAvailable,
            "1.0.0",
            "1.1.0",
            checkedAt,
            new AutoUpdateCheckResult("1.1.0", package, "Release notes", checkedAt),
            null,
            null,
            null,
            null,
            false,
            null));
        var sut = new ApplicationUpdateSettingsService(statusProvider, new RecordingCommandHandler(), new AutoUpdateOptions());

        var status = sut.GetStatus();

        status.State.Should().Be("Update verfügbar");
        status.InstalledVersion.Should().Be("1.0.0");
        status.AvailableVersion.Should().Be("1.1.0");
        status.LastCheckSummary.Should().Be("Version 1.1.0 gefunden.");
    }

    [Fact]
    public void GetStatus_ShouldReportAvailableVersionWithoutMatchingPackage()
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
            null,
            false,
            null));
        var sut = new ApplicationUpdateSettingsService(statusProvider, new RecordingCommandHandler(), new AutoUpdateOptions());

        var status = sut.GetStatus();

        status.HasAvailablePackage.Should().BeFalse();
        status.LastCheckSummary.Should().Be("Version 1.1.0 gefunden, aber kein Paket für diese Plattform.");
    }

    [Fact]
    public void GetStatus_ShouldReportMatchingPackage()
    {
        var checkedAt = DateTimeOffset.UtcNow.AddMinutes(-5);
        var package = new AutoUpdatePackageDescriptor(
            "1.1.0",
            "linux",
            "linux-x64",
            "release.zip",
            new Uri("https://example.invalid/release.zip"),
            new string('a', 64),
            42);
        var statusProvider = new StubStatusProvider(new AutoUpdateStatusSnapshot(
            AutoUpdateState.UpdateAvailable,
            "1.0.0",
            "1.1.0",
            checkedAt,
            new AutoUpdateCheckResult("1.1.0", package, "Release notes", checkedAt),
            null,
            null,
            null,
            null,
            false,
            null));
        var sut = new ApplicationUpdateSettingsService(statusProvider, new RecordingCommandHandler(), new AutoUpdateOptions());

        var status = sut.GetStatus();

        status.HasAvailablePackage.Should().BeTrue();
        status.LastCheckSummary.Should().Be("Version 1.1.0 gefunden.");
    }

    [Fact]
    public async Task InstallAsync_ShouldConfirmDowntime()
    {
        var commandHandler = new RecordingCommandHandler();
        var sut = new ApplicationUpdateSettingsService(
            new StubStatusProvider(AutoUpdateStatusSnapshot.Idle("1.0.0")),
            commandHandler,
            new AutoUpdateOptions());

        var result = await sut.InstallAsync();

        commandHandler.InstallCalled.Should().BeTrue();
        commandHandler.InstallConfirmDowntime.Should().BeTrue();
        result.Outcome.Should().Be("Erfolgreich");
    }

    [Fact]
    public async Task CheckAsync_ShouldReturnFailedResult()
    {
        var commandHandler = new RecordingCommandHandler
        {
            CheckResult = new AutoUpdateResult(
                AutoUpdateOutcome.Failed,
                AutoUpdateState.Failed,
                AutoUpdateResultCode.Failed,
                "Quelle konnte nicht geprüft werden.",
                new AutoUpdateError(AutoUpdateErrorCode.SourceUnavailable, "source failed", null))
        };
        var sut = new ApplicationUpdateSettingsService(
            new StubStatusProvider(AutoUpdateStatusSnapshot.Idle("1.0.0")),
            commandHandler,
            new AutoUpdateOptions());

        var result = await sut.CheckAsync();

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Die Update-Quelle ist nicht erreichbar.");
    }

    [Fact]
    public async Task CheckAsync_ShouldAllowManualCheckWhenAutomaticUpdatesAreDisabled()
    {
        var options = new AutoUpdateOptions { Enabled = false };
        var commandHandler = new RecordingCommandHandler
        {
            OnCheck = () => options.Enabled.Should().BeTrue()
        };
        var sut = new ApplicationUpdateSettingsService(
            new StubStatusProvider(AutoUpdateStatusSnapshot.Idle("1.0.0")),
            commandHandler,
            options);

        var result = await sut.CheckAsync();

        result.IsSuccess.Should().BeTrue();
        commandHandler.CheckCalled.Should().BeTrue();
        options.Enabled.Should().BeFalse();
    }

    [Fact]
    public async Task CheckAsync_ShouldTranslateDisabledUpdaterMessage()
    {
        var commandHandler = new RecordingCommandHandler
        {
            CheckResult = new AutoUpdateResult(
                AutoUpdateOutcome.Skipped,
                AutoUpdateState.Disabled,
                AutoUpdateResultCode.AutoUpdateDisabled,
                "Auto-update is disabled.",
                null)
        };
        var sut = new ApplicationUpdateSettingsService(
            new StubStatusProvider(AutoUpdateStatusSnapshot.Idle("1.0.0")),
            commandHandler,
            new AutoUpdateOptions());

        var result = await sut.CheckAsync();

        result.Message.Should().Be("Automatische Updates sind deaktiviert.");
        result.Outcome.Should().Be("Übersprungen");
        result.State.Should().Be("Deaktiviert");
    }

    [Fact]
    public async Task CheckAsync_ShouldTranslateNoNewerUpdateMessage()
    {
        var commandHandler = new RecordingCommandHandler
        {
            CheckResult = new AutoUpdateResult(
                AutoUpdateOutcome.NoUpdate,
                AutoUpdateState.Idle,
                AutoUpdateResultCode.NoNewerUpdateAvailable,
                "No newer update is available.",
                null)
        };
        var sut = new ApplicationUpdateSettingsService(
            new StubStatusProvider(AutoUpdateStatusSnapshot.Idle("1.0.0")),
            commandHandler,
            new AutoUpdateOptions());

        var result = await sut.CheckAsync();

        result.Message.Should().Be("Keine neuere Version verfügbar.");
        result.Outcome.Should().Be("Keine neue Version");
        result.State.Should().Be("Bereit");
    }

    [Fact]
    public async Task GetSettingsAsync_ShouldMapPrereleaseSetting()
    {
        var sut = new ApplicationUpdateSettingsService(
            new StubStatusProvider(AutoUpdateStatusSnapshot.Idle("1.0.0")),
            new RecordingCommandHandler(),
            new AutoUpdateOptions { AllowPrereleaseUpdates = true });

        var settings = await sut.GetSettingsAsync();

        settings.AllowPrereleaseUpdates.Should().BeTrue();
    }

    [Fact]
    public async Task GetSettingsAsync_ShouldPreferPersistedPrereleaseSetting()
    {
        await using var db = CreateDbContext();
        db.AppSettings.Add(new AppSetting
        {
            Key = "ApplicationUpdates:AllowPrereleaseUpdates",
            Value = bool.TrueString
        });
        await db.SaveChangesAsync();
        var options = new AutoUpdateOptions { AllowPrereleaseUpdates = false };
        var sut = new ApplicationUpdateSettingsService(
            new StubStatusProvider(AutoUpdateStatusSnapshot.Idle("1.0.0")),
            new RecordingCommandHandler(),
            options,
            db);

        var settings = await sut.GetSettingsAsync();

        settings.AllowPrereleaseUpdates.Should().BeTrue();
        options.AllowPrereleaseUpdates.Should().BeTrue();
    }

    [Fact]
    public async Task SetAllowPrereleaseUpdatesAsync_ShouldUpdateRuntimeUpdaterOptions()
    {
        var options = new AutoUpdateOptions();
        var sut = new ApplicationUpdateSettingsService(
            new StubStatusProvider(AutoUpdateStatusSnapshot.Idle("1.0.0")),
            new RecordingCommandHandler(),
            options);

        var settings = await sut.SetAllowPrereleaseUpdatesAsync(true);

        settings.AllowPrereleaseUpdates.Should().BeTrue();
        options.AllowPrereleaseUpdates.Should().BeTrue();
    }

    [Fact]
    public async Task SetAllowPrereleaseUpdatesAsync_ShouldPersistPrereleaseSetting()
    {
        await using var db = CreateDbContext();
        var sut = new ApplicationUpdateSettingsService(
            new StubStatusProvider(AutoUpdateStatusSnapshot.Idle("1.0.0")),
            new RecordingCommandHandler(),
            new AutoUpdateOptions(),
            db);

        await sut.SetAllowPrereleaseUpdatesAsync(true);

        var setting = await db.AppSettings.FindAsync("ApplicationUpdates:AllowPrereleaseUpdates");
        setting.Should().NotBeNull();
        setting!.Value.Should().Be(bool.TrueString);
    }

    [Fact]
    public async Task SetAllowPrereleaseUpdatesAsync_ShouldRecreateGithubSourceWithPrereleaseSetting()
    {
        var options = new AutoUpdateOptions
        {
            Source = AutoUpdateGithubSource.Create("owner", "repo", null, includePrereleases: false)
        };
        var sut = new ApplicationUpdateSettingsService(
            new StubStatusProvider(AutoUpdateStatusSnapshot.Idle("1.0.0")),
            new RecordingCommandHandler(),
            options,
            applicationOptions: Options.Create(new ApplicationUpdateOptions
            {
                RepositoryOwner = "owner",
                RepositoryName = "repo"
            }));

        var previousSource = options.Source;

        await sut.SetAllowPrereleaseUpdatesAsync(true);

        options.AllowPrereleaseUpdates.Should().BeTrue();
        options.Source.Should().BeOfType<AutoUpdateGithubSource>();
        options.Source.Should().NotBeSameAs(previousSource);
    }

    private static RezepteDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<RezepteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new RezepteDbContext(options);
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
        public bool CheckCalled { get; private set; }
        public bool InstallCalled { get; private set; }
        public bool InstallConfirmDowntime { get; private set; }
        public Action? OnCheck { get; set; }
        public AutoUpdateResult CheckResult { get; set; } = new(
            AutoUpdateOutcome.NoUpdate,
            AutoUpdateState.Idle,
            AutoUpdateResultCode.NoNewerUpdateAvailable,
            "Keine neue Version.",
            null);

        public Task<AutoUpdateResult> CheckAsync(CancellationToken ct = default)
        {
            CheckCalled = true;
            OnCheck?.Invoke();
            return Task.FromResult(CheckResult);
        }

        public Task<AutoUpdateResult> DownloadAsync(CancellationToken ct = default)
            => Task.FromResult(new AutoUpdateResult(
                AutoUpdateOutcome.Success,
                AutoUpdateState.ReadyToInstall,
                AutoUpdateResultCode.DownloadCompleted,
                "Download abgeschlossen.",
                null));

        public Task<AutoUpdateResult> InstallAsync(bool confirmDowntime, CancellationToken ct = default)
        {
            InstallCalled = true;
            InstallConfirmDowntime = confirmDowntime;
            return Task.FromResult(new AutoUpdateResult(
                AutoUpdateOutcome.Success,
                AutoUpdateState.Installing,
                AutoUpdateResultCode.InstallationStarted,
                "Installation gestartet.",
                null));
        }
    }
}
