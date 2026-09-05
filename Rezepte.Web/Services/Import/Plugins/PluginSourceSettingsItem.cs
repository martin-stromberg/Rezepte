namespace Rezepte.Web.Services.Import.Plugins;

/// <summary>
/// plugins the source settings item.
/// </summary>
/// <param name="Id">The id parameter.</param>
/// <param name="RepositoryUrl">The repository url parameter.</param>
/// <param name="Owner">The owner parameter.</param>
/// <param name="Repository">The repository parameter.</param>
/// <param name="IsPrivate">The is private parameter.</param>
/// <param name="Enabled">The enabled parameter.</param>
/// <param name="TrustConfirmed">The trust confirmed parameter.</param>
/// <param name="HasSecret">The has secret parameter.</param>
/// <param name="LastSuccessfulReleaseTag">The last successful release tag parameter.</param>
/// <param name="LastError">The last error parameter.</param>
/// <param name="LastCheckedAt">The last checked at parameter.</param>
/// <param name="LastErrorAt">The last error at parameter.</param>
/// <returns>The result.</returns>
public sealed record PluginSourceSettingsItem(
    string Id,
    string RepositoryUrl,
    string Owner,
    string Repository,
    bool IsPrivate,
    bool Enabled,
    bool TrustConfirmed,
    bool HasSecret,
    string? LastSuccessfulReleaseTag,
    string? LastError,
    DateTime? LastCheckedAt,
    DateTime? LastErrorAt);
