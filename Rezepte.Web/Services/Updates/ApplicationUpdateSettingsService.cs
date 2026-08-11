using msTools.Updater;

namespace Rezepte.Web.Services.Updates;

public interface IApplicationUpdateSettingsService
{
    ApplicationUpdateStatusItem GetStatus();
    Task<ApplicationUpdateCommandResult> CheckAsync(CancellationToken ct = default);
    Task<ApplicationUpdateCommandResult> DownloadAsync(CancellationToken ct = default);
    Task<ApplicationUpdateCommandResult> InstallAsync(CancellationToken ct = default);
}

public sealed class ApplicationUpdateSettingsService : IApplicationUpdateSettingsService
{
    private readonly IAutoUpdateStatusProvider _statusProvider;
    private readonly IAutoUpdateCommandHandler _commandHandler;
    private readonly AutoUpdateOptions _options;
    private readonly SemaphoreSlim _manualCheckGate = new(1, 1);

    public ApplicationUpdateSettingsService(
        IAutoUpdateStatusProvider statusProvider,
        IAutoUpdateCommandHandler commandHandler,
        AutoUpdateOptions options)
    {
        _statusProvider = statusProvider;
        _commandHandler = commandHandler;
        _options = options;
    }

    public ApplicationUpdateStatusItem GetStatus()
    {
        var snapshot = _statusProvider.GetSnapshot();
        return ApplicationUpdateStatusItem.FromSnapshot(snapshot);
    }

    public async Task<ApplicationUpdateCommandResult> CheckAsync(CancellationToken ct = default)
    {
        await _manualCheckGate.WaitAsync(ct).ConfigureAwait(false);
        var wasEnabled = _options.Enabled;

        try
        {
            _options.Enabled = true;
            return ApplicationUpdateCommandResult.FromResult(await _commandHandler.CheckAsync(ct).ConfigureAwait(false));
        }
        finally
        {
            _options.Enabled = wasEnabled;
            _manualCheckGate.Release();
        }
    }

    public async Task<ApplicationUpdateCommandResult> DownloadAsync(CancellationToken ct = default)
        => ApplicationUpdateCommandResult.FromResult(await _commandHandler.DownloadAsync(ct).ConfigureAwait(false));

    public async Task<ApplicationUpdateCommandResult> InstallAsync(CancellationToken ct = default)
        => ApplicationUpdateCommandResult.FromResult(await _commandHandler.InstallAsync(confirmDowntime: true, ct).ConfigureAwait(false));
}

public sealed record ApplicationUpdateStatusItem(
    string State,
    string? InstalledVersion,
    string? AvailableVersion,
    DateTimeOffset? LastCheckedAt,
    string? LastCheckSummary,
    string? LastDownloadSummary,
    string? LastInstallSummary,
    string? LastError,
    bool IsLocked,
    DateTimeOffset? LockCreatedAt)
{
    public static ApplicationUpdateStatusItem FromSnapshot(AutoUpdateStatusSnapshot snapshot)
        => new(
            LocalizeState(snapshot.State.ToString()),
            snapshot.InstalledVersion,
            snapshot.AvailableVersion,
            snapshot.LastCheckedAt,
            FormatCheck(snapshot.LastCheckResult),
            FormatDownload(snapshot.LastDownloadResult),
            FormatInstall(snapshot.LastInstallResult),
            ApplicationUpdateText.Localize(snapshot.LastError),
            snapshot.IsLocked,
            snapshot.LockCreatedAt);

    private static string? FormatCheck(AutoUpdateCheckResult? result)
    {
        if (result is null)
        {
            return null;
        }

        return result.AvailableVersion is null
            ? "Keine neue Version gefunden."
            : $"Version {result.AvailableVersion} gefunden.";
    }

    private static string LocalizeState(string state) => state switch
    {
        "Idle" => "Bereit",
        "Checking" => "Prüfung läuft",
        "UpdateAvailable" => "Update verfügbar",
        "Downloading" => "Download läuft",
        "ReadyToInstall" => "Installationsbereit",
        "Installing" => "Installation läuft",
        "Success" => "Erfolgreich",
        "Failed" => "Fehlgeschlagen",
        "Disabled" => "Deaktiviert",
        _ => state
    };

    private static string? FormatDownload(AutoUpdateDownloadResult? result)
    {
        if (result is null)
        {
            return null;
        }

        return $"{Path.GetFileName(result.LocalPath)} ({result.SizeBytes:N0} Bytes)";
    }

    private static string? FormatInstall(AutoUpdateInstallResult? result)
    {
        if (result is null)
        {
            return null;
        }

        return $"Version {result.Version}, Skript gestartet {result.StartedAt.LocalDateTime:g}";
    }
}

public sealed record ApplicationUpdateCommandResult(
    string Outcome,
    string State,
    string? Message,
    string? Error)
{
    public bool IsSuccess => Error is null && Outcome is not nameof(AutoUpdateOutcome.Failed) and not "Fehlgeschlagen";

    public static ApplicationUpdateCommandResult FromResult(AutoUpdateResult result)
        => new(
            ApplicationUpdateText.LocalizeOutcome(result.Outcome.ToString()),
            ApplicationUpdateText.LocalizeState(result.State.ToString()),
            ApplicationUpdateText.Localize(result.Message),
            ApplicationUpdateText.Localize(result.Error?.Message));
}

internal static class ApplicationUpdateText
{
    public static string? Localize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return value.Trim() switch
        {
            "Auto-update is disabled." => "Automatische Updates sind deaktiviert.",
            "Auto-update is disabled" => "Automatische Updates sind deaktiviert.",
            "No update available." => "Keine neue Version verfügbar.",
            "No update available" => "Keine neue Version verfügbar.",
            "No update." => "Keine neue Version verfügbar.",
            "No update" => "Keine neue Version verfügbar.",
            _ => value
        };
    }

    public static string LocalizeOutcome(string outcome) => outcome switch
    {
        nameof(AutoUpdateOutcome.Success) => "Erfolgreich",
        nameof(AutoUpdateOutcome.NoUpdate) => "Keine neue Version",
        nameof(AutoUpdateOutcome.Skipped) => "Übersprungen",
        nameof(AutoUpdateOutcome.Failed) => "Fehlgeschlagen",
        _ => outcome
    };

    public static string LocalizeState(string state) => state switch
    {
        "Idle" => "Bereit",
        "Checking" => "Prüfung läuft",
        "UpdateAvailable" => "Update verfügbar",
        "Downloading" => "Download läuft",
        "ReadyToInstall" => "Installationsbereit",
        "Installing" => "Installation läuft",
        "Success" => "Erfolgreich",
        "Failed" => "Fehlgeschlagen",
        "Disabled" => "Deaktiviert",
        _ => state
    };
}
