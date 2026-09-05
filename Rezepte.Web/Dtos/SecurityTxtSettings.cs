namespace Rezepte.Web.Dtos;

/// <summary>
/// securitys the txt settings.
/// </summary>
/// <param name="Enabled">The enabled parameter.</param>
/// <param name="Contact">The contact parameter.</param>
/// <param name="Expires">The expires parameter.</param>
/// <param name="Encryption">The encryption parameter.</param>
/// <param name="Acknowledgments">The acknowledgments parameter.</param>
/// <param name="PreferredLanguages">The preferred languages parameter.</param>
/// <param name="Canonical">The canonical parameter.</param>
/// <param name="Policy">The policy parameter.</param>
/// <param name="Hiring">The hiring parameter.</param>
/// <returns>The result.</returns>
public sealed record SecurityTxtSettings(
    bool Enabled,
    string? Contact,
    DateTimeOffset? Expires,
    string? Encryption,
    string? Acknowledgments,
    string? PreferredLanguages,
    string? Canonical,
    string? Policy,
    string? Hiring);
