using System.Text.RegularExpressions;

namespace Rezepte.Web.Services.Import;

internal static class ImportExceptionHelper
{
    private static readonly Regex DetailRegex = new(@"Detail\s*=\s*""(?<d>.*?)""", RegexOptions.Singleline | RegexOptions.IgnoreCase);
    private static readonly Regex AltDetailRegex = new(@"(""detail""\s*[:=]\s*""(?<d>.*?)"")|detail\s*[:=]\s*(?<d>https?://[^\s]+|.+)", RegexOptions.Singleline | RegexOptions.IgnoreCase);

    public static string BeautifyExceptionMessage(Exception ex)
    {
        if (ex == null) return "Unbekannter Fehler";

        // unwrap to most relevant inner exception
        while (ex.InnerException is not null) ex = ex.InnerException;

        var raw = ex.Message ?? ex.ToString();

        // extract detail if present
        var m = DetailRegex.Match(raw);
        var detail = m.Success ? m.Groups["d"].Value : string.Empty;
        if (string.IsNullOrEmpty(detail))
        {
            var alt = AltDetailRegex.Match(raw);
            if (alt.Success) detail = alt.Groups["d"].Value;
        }

        string Shorten(string s, int max = 300) =>
            string.IsNullOrEmpty(s) ? s : (s.Length <= max ? s : s.Substring(0, max).TrimEnd() + "…");

        if (!string.IsNullOrEmpty(detail))
        {
            if (detail.IndexOf("billing", StringComparison.OrdinalIgnoreCase) >= 0)
                return $"Zugriff verweigert / Abrechnung erforderlich: {Shorten(detail)}";
            if (detail.IndexOf("permission", StringComparison.OrdinalIgnoreCase) >= 0 || detail.IndexOf("permissiondenied", StringComparison.OrdinalIgnoreCase) >= 0)
                return $"Zugriffsfehler: {Shorten(detail)}";

            return Shorten(detail);
        }

        var firstLine = raw.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? raw;
        return Shorten(firstLine);
    }
}