namespace Rezepte.Web.Services.Import.Plugins;

/// <summary>
/// plugins the source save request.
/// </summary>
/// <param name="Id">The id parameter.</param>
/// <param name="RepositoryUrl">The repository url parameter.</param>
/// <param name="IsPrivate">The is private parameter.</param>
/// <param name="Enabled">The enabled parameter.</param>
/// <param name="TrustConfirmed">The trust confirmed parameter.</param>
/// <param name="PersonalAccessToken">The personal access token parameter.</param>
/// <returns>The result.</returns>
public sealed record PluginSourceSaveRequest(
    string? Id,
    string RepositoryUrl,
    bool IsPrivate,
    bool Enabled,
    bool TrustConfirmed,
    string? PersonalAccessToken);
