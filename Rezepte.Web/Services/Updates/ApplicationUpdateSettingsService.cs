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

    public ApplicationUpdateSettingsService(
        IAutoUpdateStatusProvider statusProvider,
        IAutoUpdateCommandHandler commandHandler)
    {
        _statusProvider = statusProvider;
        _commandHandler = commandHandler;
    }

    public ApplicationUpdateStatusItem GetStatus()
    {
        var snapshot = _statusProvider.GetSnapshot();
        return ApplicationUpdateStatusItem.FromSnapshot(snapshot);
    }

    public async Task<ApplicationUpdateCommandResult> CheckAsync(CancellationToken ct = default)
        => ApplicationUpdateCommandResult.FromResult(await _commandHandler.CheckAsync(ct).ConfigureAwait(false));

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
            snapshot.State.ToString(),
            snapshot.InstalledVersion,
            snapshot.AvailableVersion,
            snapshot.LastCheckedAt,
            FormatCheck(snapshot.LastCheckResult),
            FormatDownload(snapshot.LastDownloadResult),
            FormatInstall(snapshot.LastInstallResult),
            snapshot.LastError,
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
    public bool IsSuccess => Error is null && Outcome is not nameof(AutoUpdateOutcome.Failed);

    public static ApplicationUpdateCommandResult FromResult(AutoUpdateResult result)
        => new(
            result.Outcome.ToString(),
            result.State.ToString(),
            result.Message,
            result.Error?.Message);
}
