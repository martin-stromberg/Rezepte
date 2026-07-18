using Microsoft.EntityFrameworkCore;
using Rezepte.Web.Data;
using Rezepte.Web.Entities;

namespace Rezepte.Web.Services.Import.Plugins;

public sealed class PluginUpdateService(
    RezepteDbContext db,
    IGitHubReleaseClient gitHubReleaseClient,
    ISystemSecretStore secretStore,
    IPluginPackageValidator packageValidator,
    IPluginPackageInstaller packageInstaller,
    ILogger<PluginUpdateService> logger) : IPluginUpdateService
{
    public async Task CheckForUpdatesAsync(CancellationToken ct = default)
    {
        var sources = await db.PluginSources
            .Where(s => s.Enabled && s.TrustConfirmed)
            .OrderBy(s => s.Owner)
            .ThenBy(s => s.Repository)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var source in sources)
        {
            await ProcessSourceAsync(source, ct).ConfigureAwait(false);
        }
    }

    private async Task ProcessSourceAsync(PluginSource source, CancellationToken ct)
    {
        source.LastCheckedAt = DateTime.UtcNow;
        source.LastError = null;
        source.LastErrorAt = null;
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        try
        {
            var repository = new GitHubRepository(source.Owner, source.Repository, source.RepositoryUrl);
            var token = source.IsPrivate && !string.IsNullOrWhiteSpace(source.SecretName)
                ? await secretStore.GetAsync(source.SecretName, ct).ConfigureAwait(false)
                : null;
            if (source.IsPrivate && string.IsNullOrWhiteSpace(token))
            {
                await MarkSourceFailureAsync(source, "Private source has no stored PAT.", ct).ConfigureAwait(false);
                return;
            }

            var release = await gitHubReleaseClient.GetLatestReleaseAsync(repository, token, ct).ConfigureAwait(false);
            if (release is null || string.IsNullOrWhiteSpace(release.TagName))
            {
                await MarkSourceFailureAsync(source, "No published GitHub release was found.", ct).ConfigureAwait(false);
                return;
            }

            var asset = release.FindZipAsset();
            if (asset is null)
            {
                await UpsertReleaseFailureAsync(source, release, 0, string.Empty, PluginSourceReleaseStatus.ValidationFailed, "Latest release has no ZIP asset.", ct).ConfigureAwait(false);
                return;
            }

            var existing = await db.PluginSourceReleases
                .FirstOrDefaultAsync(r => r.PluginSourceId == source.Id && r.ReleaseTag == release.TagName && r.AssetId == asset.Id, ct)
                .ConfigureAwait(false);
            if (existing is not null && existing.Status == PluginSourceReleaseStatus.Installed)
            {
                logger.LogInformation("Skipping plugin source {Owner}/{Repository} release {ReleaseTag} because status is {Status}", source.Owner, source.Repository, release.TagName, existing.Status);
                return;
            }

            var record = existing ?? new PluginSourceRelease
            {
                PluginSourceId = source.Id,
                ReleaseTag = release.TagName,
                GitHubReleaseId = release.Id,
                AssetId = asset.Id,
                AssetName = asset.Name,
                Status = PluginSourceReleaseStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };
            if (existing is null)
            {
                db.PluginSourceReleases.Add(record);
            }
            else if (existing.Status is PluginSourceReleaseStatus.ValidationFailed or PluginSourceReleaseStatus.DownloadFailed or PluginSourceReleaseStatus.InstallFailed)
            {
                logger.LogInformation("Retrying previously failed plugin source {Owner}/{Repository} release {ReleaseTag} with status {Status}", source.Owner, source.Repository, release.TagName, existing.Status);
                existing.Status = PluginSourceReleaseStatus.Pending;
                existing.Error = null;
            }

            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            await DownloadValidateInstallAsync(source, record, asset, token, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Plugin source {Owner}/{Repository} failed", source.Owner, source.Repository);
            await MarkSourceFailureAsync(source, ex.Message, CancellationToken.None).ConfigureAwait(false);
        }
    }

    private async Task DownloadValidateInstallAsync(PluginSource source, PluginSourceRelease record, GitHubReleaseAsset asset, string? token, CancellationToken ct)
    {
        var workRoot = Path.Combine(Path.GetTempPath(), "rezepte-plugin-download", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workRoot);
        var zipPath = Path.Combine(workRoot, asset.Name);
        PluginPackageValidationResult? validation = null;
        try
        {
            record.Status = PluginSourceReleaseStatus.Downloading;
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            await gitHubReleaseClient.DownloadAssetAsync(asset, zipPath, token, ct).ConfigureAwait(false);
            record.DownloadedAt = DateTime.UtcNow;

            record.Status = PluginSourceReleaseStatus.Validating;
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            validation = await packageValidator.ValidateAsync(zipPath, ct).ConfigureAwait(false);
            if (!validation.Success)
            {
                record.Status = PluginSourceReleaseStatus.ValidationFailed;
                record.Error = validation.Error;
                await MarkSourceFailureAsync(source, validation.Error ?? "Plugin package validation failed.", ct).ConfigureAwait(false);
                return;
            }

            record.ValidatedAt = DateTime.UtcNow;
            record.Status = PluginSourceReleaseStatus.Installing;
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            await packageInstaller.InstallAsync(validation.PluginDirectories, ct).ConfigureAwait(false);

            record.Status = PluginSourceReleaseStatus.Installed;
            record.Error = null;
            record.InstalledAt = DateTime.UtcNow;
            source.LastSuccessfulReleaseTag = record.ReleaseTag;
            source.LastError = null;
            source.LastErrorAt = null;
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            record.Status = record.Status switch
            {
                PluginSourceReleaseStatus.Downloading => PluginSourceReleaseStatus.DownloadFailed,
                PluginSourceReleaseStatus.Validating => PluginSourceReleaseStatus.ValidationFailed,
                _ => PluginSourceReleaseStatus.InstallFailed
            };
            record.Error = ex.Message;
            await MarkSourceFailureAsync(source, ex.Message, CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            if (validation is not null && Directory.Exists(validation.ExtractedRoot))
            {
                TryDeleteDirectory(validation.ExtractedRoot);
            }

            if (Directory.Exists(workRoot))
            {
                TryDeleteDirectory(workRoot);
            }
        }
    }

    private async Task UpsertReleaseFailureAsync(PluginSource source, GitHubReleaseInfo release, long assetId, string assetName, string status, string error, CancellationToken ct)
    {
        var record = await db.PluginSourceReleases
            .FirstOrDefaultAsync(r => r.PluginSourceId == source.Id && r.ReleaseTag == release.TagName && r.AssetId == assetId, ct)
            .ConfigureAwait(false);
        if (record is null)
        {
            db.PluginSourceReleases.Add(new PluginSourceRelease
            {
                PluginSourceId = source.Id,
                ReleaseTag = release.TagName,
                GitHubReleaseId = release.Id,
                AssetId = assetId,
                AssetName = assetName,
                Status = status,
                Error = error,
                CreatedAt = DateTime.UtcNow
            });
        }
        else
        {
            record.Status = status;
            record.Error = error;
        }

        await MarkSourceFailureAsync(source, error, ct).ConfigureAwait(false);
    }

    private async Task MarkSourceFailureAsync(PluginSource source, string error, CancellationToken ct)
    {
        source.LastError = error;
        source.LastErrorAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private static void TryDeleteDirectory(string directory)
    {
        try
        {
            Directory.Delete(directory, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
