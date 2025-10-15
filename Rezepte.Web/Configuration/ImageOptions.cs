namespace Rezepte.Web.Configuration;

public sealed class ImageOptions
{
    /// <summary>
    /// Maximale Dateigröße in Bytes (z. B. 5 MB = 5_242_880).
    /// </summary>
    public long MaxSizeBytes { get; set; } = 5_242_880;

    /// <summary>
    /// Cache‑Dauer in Sekunden für Bild‑Responses (Cache‑Control: public, max-age=...).
    /// </summary>
    public int CacheMaxAgeSeconds { get; set; } = 3600;

    /// <summary>
    /// Erlaubte Content‑Types (einfache Whitelist). Erweiterbar.
    /// </summary>
    public string[] AllowedContentTypes { get; set; } = new[] { "image/jpeg", "image/png", "image/webp", "image/gif" };
}