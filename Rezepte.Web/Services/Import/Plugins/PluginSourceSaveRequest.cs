namespace Rezepte.Web.Services.Import.Plugins;

public sealed record PluginSourceSaveRequest(
    string? Id,
    string RepositoryUrl,
    bool IsPrivate,
    bool Enabled,
    bool TrustConfirmed,
    string? PersonalAccessToken);
