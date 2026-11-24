using QuestPDF.Drawing;
using QuestPDF.Infrastructure;

namespace Rezepte.Web.Configuration;

/// <summary>
/// Central, idempotent QuestPDF configuration. Called once at application startup
/// (see Program.cs) and by PDF-related tests that run without the application host.
/// </summary>
public static class QuestPdfConfiguration
{
    /// <summary>
    /// Family name of the deployed emoji fallback font registered from the
    /// <c>Fonts/</c> directory next to the application.
    /// </summary>
    public const string EmojiFallbackFontFamily = "Noto Emoji";

    /// <summary>
    /// Configures QuestPDF licensing, font resolution and missing-glyph behavior.
    /// The bundled Lato font (shipped inside the QuestPDF package) is used as the base
    /// font; the deployed Noto Emoji font in <c>Fonts/</c> next to the application
    /// provides emoji glyphs. Safe to call multiple times.
    /// </summary>
    /// <returns>
    /// Diagnostics describing the font deployment state after configuration. Missing
    /// assets are reported through the result instead of throwing so tests that run
    /// without the deployed font still work; the caller (startup) logs the finding.
    /// </returns>
    public static QuestPdfFontDiagnostics Configure()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        QuestPDF.Settings.UseSystemFonts = false;
        QuestPDF.Settings.ThrowOnMissingFontFamilies = true;
        QuestPDF.Settings.ThrowOnMissingTextGlyphs = false;

        var fontsDirectory = Path.Combine(AppContext.BaseDirectory, "Fonts");
        var fontsDirectoryExists = Directory.Exists(fontsDirectory);
        if (fontsDirectoryExists)
        {
            FontManager.RegisterFontsFromDirectory(fontsDirectory);
        }

        var emojiFallbackRegistered = FontManager.GetRegisteredFonts()
            .Any(f => string.Equals(f.FamilyName, EmojiFallbackFontFamily, StringComparison.OrdinalIgnoreCase));

        return new QuestPdfFontDiagnostics(fontsDirectory, fontsDirectoryExists, emojiFallbackRegistered);
    }
}

/// <summary>
/// Result of <see cref="QuestPdfConfiguration.Configure"/> describing whether the
/// deployed fonts directory was found and whether the emoji fallback font family is
/// registered afterwards.
/// </summary>
/// <param name="FontsDirectory">The fonts directory expected next to the application.</param>
/// <param name="FontsDirectoryExists">Whether the fonts directory exists on disk.</param>
/// <param name="EmojiFallbackRegistered">
/// Whether <see cref="QuestPdfConfiguration.EmojiFallbackFontFamily"/> is registered.
/// </param>
/// <returns>The font diagnostics result.</returns>
public sealed record QuestPdfFontDiagnostics(
    string FontsDirectory,
    bool FontsDirectoryExists,
    bool EmojiFallbackRegistered);
