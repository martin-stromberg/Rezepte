namespace Rezepte.Web.Dtos;

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
