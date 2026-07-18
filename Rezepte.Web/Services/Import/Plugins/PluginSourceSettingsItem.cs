namespace Rezepte.Web.Services.Import.Plugins;

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
