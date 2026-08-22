using msTools.Updater;
using Microsoft.Extensions.Options;
using Rezepte.Web.Configuration;
using Rezepte.Web.Data;
using Rezepte.Web.Entities;

namespace Rezepte.Web.Services.Updates;

public interface IApplicationUpdateSettingsService
{
    ApplicationUpdateStatusItem GetStatus();
    Task<ApplicationUpdateSettingsItem> GetSettingsAsync(CancellationToken ct = default);
    Task<ApplicationUpdateSettingsItem> SetAllowPrereleaseUpdatesAsync(bool allowPrereleaseUpdates, CancellationToken ct = default);
    Task<ApplicationUpdateCommandResult> CheckAsync(CancellationToken ct = default);
    Task<ApplicationUpdateCommandResult> DownloadAsync(CancellationToken ct = default);
    Task<ApplicationUpdateCommandResult> InstallAsync(CancellationToken ct = default);
}

public sealed class ApplicationUpdateSettingsService : IApplicationUpdateSettingsService
{
    private const string AllowPrereleaseUpdatesKey = "ApplicationUpdates:AllowPrereleaseUpdates";

    private readonly IAutoUpdateStatusProvider _statusProvider;
    private readonly IAutoUpdateCommandHandler _commandHandler;
    private readonly AutoUpdateOptions _options;
    private readonly ApplicationUpdateOptions? _applicationOptions;
    private readonly RezepteDbContext? _db;
    private readonly SemaphoreSlim _manualCheckGate = new(1, 1);

    public ApplicationUpdateSettingsService(
        IAutoUpdateStatusProvider statusProvider,
        IAutoUpdateCommandHandler commandHandler,
        AutoUpdateOptions options,
        RezepteDbContext? db = null,
        IOptions<ApplicationUpdateOptions>? applicationOptions = null)
    {
        _statusProvider = statusProvider;
        _commandHandler = commandHandler;
        _options = options;
        _applicationOptions = applicationOptions?.Value;
        _db = db;
    }

    public ApplicationUpdateStatusItem GetStatus()
    {
        var snapshot = _statusProvider.GetSnapshot();
        return ApplicationUpdateStatusItem.FromSnapshot(snapshot);
    }

    public async Task<ApplicationUpdateSettingsItem> GetSettingsAsync(CancellationToken ct = default)
    {
        if (_db is null)
        {
            return new ApplicationUpdateSettingsItem(_options.AllowPrereleaseUpdates);
        }

        var setting = await _db.Set<AppSetting>().FindAsync([AllowPrereleaseUpdatesKey], ct).ConfigureAwait(false);
        if (setting is not null && bool.TryParse(setting.Value, out var allowPrereleaseUpdates))
        {
            ApplyPrereleaseSetting(allowPrereleaseUpdates);
        }

        return new ApplicationUpdateSettingsItem(_options.AllowPrereleaseUpdates);
    }

    public async Task<ApplicationUpdateSettingsItem> SetAllowPrereleaseUpdatesAsync(bool allowPrereleaseUpdates, CancellationToken ct = default)
    {
        ApplyPrereleaseSetting(allowPrereleaseUpdates);

        if (_db is not null)
        {
            var setting = await _db.Set<AppSetting>().FindAsync([AllowPrereleaseUpdatesKey], ct).ConfigureAwait(false);
            if (setting is null)
            {
                _db.Set<AppSetting>().Add(new AppSetting
                {
                    Key = AllowPrereleaseUpdatesKey,
                    Value = allowPrereleaseUpdates.ToString()
                });
            }
            else
            {
                setting.Value = allowPrereleaseUpdates.ToString();
            }

            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        return new ApplicationUpdateSettingsItem(_options.AllowPrereleaseUpdates);
    }

    private void ApplyPrereleaseSetting(bool allowPrereleaseUpdates)
    {
        _options.AllowPrereleaseUpdates = allowPrereleaseUpdates;

        if (!string.IsNullOrWhiteSpace(_applicationOptions?.RepositoryOwner) &&
            !string.IsNullOrWhiteSpace(_applicationOptions.RepositoryName))
        {
            _options.Source = AutoUpdateGithubSource.Create(
                _applicationOptions.RepositoryOwner,
                _applicationOptions.RepositoryName,
                _applicationOptions.ManifestAssetName,
                allowPrereleaseUpdates);
        }
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
        => ApplicationUpdateCommandResult.FromResult(await _commandHandler.InstallAsync(confirmDowntime: true, force: false, ct).ConfigureAwait(false));
}

public sealed record ApplicationUpdateSettingsItem(bool AllowPrereleaseUpdates);

public sealed record ApplicationUpdateStatusItem(
    string State,
    string? InstalledVersion,
    string? AvailableVersion,
    bool HasAvailablePackage,
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
            snapshot.LastCheckResult?.Package is not null,
            snapshot.LastCheckedAt,
            FormatCheck(snapshot.LastCheckResult),
            FormatDownload(snapshot.LastDownloadResult),
            FormatInstall(snapshot.LastInstallResult),
            ApplicationUpdateText.LocalizeError(snapshot.LastErrorCode, snapshot.LastError),
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
            : result.Package is null
                ? $"Version {result.AvailableVersion} gefunden, aber kein Paket für diese Plattform."
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
            ApplicationUpdateText.LocalizeResult(result.Code, result.Message),
            ApplicationUpdateText.LocalizeError(result.Error?.Code, result.Error?.Message));
}

internal static class ApplicationUpdateText
{
    public static string? LocalizeResult(AutoUpdateResultCode code, string? fallback) => code switch
    {
        AutoUpdateResultCode.Success => "Aktion erfolgreich abgeschlossen.",
        AutoUpdateResultCode.NoNewerUpdateAvailable => "Keine neuere Version verfügbar.",
        AutoUpdateResultCode.AutoUpdateDisabled => "Automatische Updates sind deaktiviert.",
        AutoUpdateResultCode.UpdateAvailable => "Update verfügbar.",
        AutoUpdateResultCode.DownloadCompleted => "Download abgeschlossen.",
        AutoUpdateResultCode.ReadyToInstall => "Update ist installationsbereit.",
        AutoUpdateResultCode.InstallationStarted => "Installation gestartet.",
        AutoUpdateResultCode.Skipped => "Aktion wurde übersprungen.",
        AutoUpdateResultCode.Canceled => "Aktion wurde abgebrochen.",
        AutoUpdateResultCode.Failed => Localize(fallback) ?? "Aktion fehlgeschlagen.",
        AutoUpdateResultCode.Unknown => Localize(fallback),
        _ => Localize(fallback)
    };

    public static string? LocalizeError(AutoUpdateErrorCode? code, string? fallback) => code switch
    {
        null or AutoUpdateErrorCode.None => Localize(fallback),
        AutoUpdateErrorCode.SourceUnavailable => "Die Update-Quelle ist nicht erreichbar.",
        AutoUpdateErrorCode.ManifestInvalid => "Das Update-Manifest ist ungültig.",
        AutoUpdateErrorCode.ManifestDownloadFailed => "Das Update-Manifest konnte nicht heruntergeladen werden.",
        AutoUpdateErrorCode.AssetNotFound => "Das benötigte Update-Paket wurde im Manifest nicht gefunden.",
        AutoUpdateErrorCode.AssetSizeMismatch => "Die Größe des Update-Pakets stimmt nicht mit dem Manifest überein.",
        AutoUpdateErrorCode.DownloadFailed => "Das Update-Paket konnte nicht heruntergeladen werden.",
        AutoUpdateErrorCode.HashMismatch => "Die Prüfsumme des Update-Pakets stimmt nicht mit dem Manifest überein.",
        AutoUpdateErrorCode.InstallationFailed => "Die Installation ist fehlgeschlagen.",
        AutoUpdateErrorCode.SourceNotConfigured => "Es ist keine Update-Quelle konfiguriert.",
        AutoUpdateErrorCode.ConfigurationInvalid => "Die Update-Konfiguration ist ungültig.",
        AutoUpdateErrorCode.UnsupportedPlatform => "Für diese Plattform ist kein passendes Update verfügbar.",
        AutoUpdateErrorCode.LockActive => "Eine andere Update-Aktion läuft bereits.",
        AutoUpdateErrorCode.NoPackageAvailable => "Für die aktuelle Plattform ist kein Update-Paket verfügbar.",
        AutoUpdateErrorCode.Canceled => "Die Aktion wurde abgebrochen.",
        AutoUpdateErrorCode.Unknown => Localize(fallback) ?? "Ein unbekannter Update-Fehler ist aufgetreten.",
        _ => Localize(fallback) ?? "Ein unbekannter Update-Fehler ist aufgetreten."
    };

    public static string? Localize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var trimmed = value.Trim();
        return Normalize(trimmed) switch
        {
            "auto-update is disabled" => "Automatische Updates sind deaktiviert.",
            "no update available" => "Keine neue Version verfügbar.",
            "no update" => "Keine neue Version verfügbar.",
            "no newer update is available" => "Keine neuere Version verfügbar.",
            "no update package is available to download" => "Für die aktuelle Plattform ist kein Update-Paket verfügbar.",
            "no update package is available" => "Für die aktuelle Plattform ist kein Update-Paket verfügbar.",
            _ => trimmed
        };
    }

    private static string Normalize(string value)
        => value.Trim().TrimEnd('.').ToLowerInvariant();

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
