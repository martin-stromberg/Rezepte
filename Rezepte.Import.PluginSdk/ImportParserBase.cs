using System.Globalization;
using System.Text.RegularExpressions;

namespace Rezepte.Import.PluginSdk;

public abstract class ImportParserBase
{
    protected int ParseIsoDurationToMinutes(string? isoDuration)
    {
        if (string.IsNullOrWhiteSpace(isoDuration))
            return 0;

        var match = Regex.Match(isoDuration, @"^PT((\d+)H)?((\d+)M)?((\d+)S)?$");
        if (!match.Success)
            throw new FormatException($"UngÃ¼ltiges ISO 8601 Zeitformat: {isoDuration}");

        var hours = match.Groups[2].Success ? int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture) : 0;
        var minutes = match.Groups[4].Success ? int.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture) : 0;
        var seconds = match.Groups[6].Success ? int.Parse(match.Groups[6].Value, CultureInfo.InvariantCulture) : 0;

        return hours * 60 + minutes + (seconds >= 30 ? 1 : 0);
    }

    protected ParsedIngredient ParseIngredientLine(string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return new ParsedIngredient(0m, null, string.Empty);

        var qty = line.Trim().TrimStart('*', '-', '•').Trim();
        var vulgar = new Dictionary<char, decimal>
        {
            ['½'] = 0.5m,
            ['⅓'] = 1m / 3m,
            ['⅔'] = 2m / 3m,
            ['¼'] = 0.25m,
            ['¾'] = 0.75m,
            ['⅛'] = 0.125m
        };

        decimal amount = 0m;
        string? unit = null;
        var name = qty;

        var match = Regex.Match(qty, @"^\s*(\d+)[\s\-]+(\d+)\/(\d+)\s*(.*)$");
        if (match.Success)
        {
            var whole = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            var num = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
            var den = int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
            if (den != 0)
                amount = whole + (decimal)num / den;
            DetectUnitAndName(match.Groups[4].Value.Trim(), out unit, out name);
            return new ParsedIngredient(amount, string.IsNullOrWhiteSpace(unit) ? null : unit, string.IsNullOrWhiteSpace(name) ? qty : name);
        }

        match = Regex.Match(qty, @"^\s*(\d+)\/(\d+)\s*(.*)$");
        if (match.Success)
        {
            var num = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            var den = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
            if (den != 0)
                amount = (decimal)num / den;
            DetectUnitAndName(match.Groups[3].Value.Trim(), out unit, out name);
            return new ParsedIngredient(amount, string.IsNullOrWhiteSpace(unit) ? null : unit, string.IsNullOrWhiteSpace(name) ? qty : name);
        }

        match = Regex.Match(qty, @"^\s*(\d+[.,]?\d*)\s*(.*)$");
        if (match.Success)
        {
            if (decimal.TryParse(match.Groups[1].Value.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var val))
                amount = val;
            DetectUnitAndName(match.Groups[2].Value.Trim(), out unit, out name);
            return new ParsedIngredient(amount, string.IsNullOrWhiteSpace(unit) ? null : unit, string.IsNullOrWhiteSpace(name) ? qty : name);
        }

        if (qty.Length > 0 && vulgar.TryGetValue(qty[0], out var v))
        {
            amount = v;
            DetectUnitAndName(qty[1..].Trim(), out unit, out name);
            return new ParsedIngredient(amount, string.IsNullOrWhiteSpace(unit) ? null : unit, string.IsNullOrWhiteSpace(name) ? qty : name);
        }

        match = Regex.Match(qty, @"^\s*(.+?)\s+(\d+[.,]?\d*|\d+\/\d+|[½¼¾⅓⅔⅛])\s*([^\d].*)?$");
        if (match.Success)
        {
            var namePart = match.Groups[1].Value.Trim();
            var numPart = match.Groups[2].Value;
            var trailing = match.Groups[3].Success ? match.Groups[3].Value.Trim() : null;

            if (numPart.Contains('/'))
            {
                var parts = numPart.Split('/');
                if (parts.Length == 2 && int.TryParse(parts[0], out var n) && int.TryParse(parts[1], out var d) && d != 0)
                    amount = (decimal)n / d;
            }
            else if (numPart.Length == 1 && vulgar.TryGetValue(numPart[0], out var uv))
            {
                amount = uv;
            }
            else
            {
                decimal.TryParse(numPart.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
            }

            if (!string.IsNullOrWhiteSpace(trailing))
            {
                DetectUnitAndName(trailing, out unit, out var extraName);
                name = string.IsNullOrWhiteSpace(extraName) ? namePart : $"{namePart} {extraName}";
            }
            else
            {
                name = namePart;
            }

            return new ParsedIngredient(amount, string.IsNullOrWhiteSpace(unit) ? null : unit, string.IsNullOrWhiteSpace(name) ? qty : name);
        }

        return new ParsedIngredient(0m, null, qty);
    }

    private static void DetectUnitAndName(string rest, out string? parsedUnit, out string parsedName)
    {
        parsedUnit = null;
        parsedName = rest ?? string.Empty;
        if (string.IsNullOrWhiteSpace(rest))
            return;

        var knownUnits = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "g", "gr", "gramm", "kg", "ml", "l", "cl", "tsp", "teelÃ¶ffel", "tl", "el",
            "esslÃ¶ffel", "essl", "st", "stÃ¼ck", "stÃ¼cke", "stk", "prise", "dose",
            "dosen", "becher", "bund", "paket", "packung", "pkg", "glas", "pz", "pkt", "spritzer"
        };

        static string NormalizeToken(string token) => token.Trim().TrimEnd('.', ',', ';').ToLowerInvariant();

        var parts = rest.Split([' '], 2, StringSplitOptions.RemoveEmptyEntries);
        var first = NormalizeToken(parts[0]);
        if (knownUnits.Contains(first) || Regex.IsMatch(first, @"^(?:\d*(?:g|kg|ml|l|tsp|tl|el|st|stk|dose|packung|paket|pkg))$", RegexOptions.IgnoreCase))
        {
            parsedUnit = parts[0];
            parsedName = parts.Length > 1 ? parts[1] : string.Empty;
        }
        else
        {
            parsedName = rest;
        }
    }
}
