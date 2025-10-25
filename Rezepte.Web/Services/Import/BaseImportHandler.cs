using Rezepte.Web.Entities;
using System.Text.RegularExpressions;
using System.Globalization;

namespace Rezepte.Web.Services.Import;

public class BaseImportHandler
{
    public string UserId { protected get; set; }

    protected int ParseIsoDurationToMinutes(string isoDuration)
    {
        if (string.IsNullOrWhiteSpace(isoDuration))
            return 0;

        var pattern = @"^PT((\d+)H)?((\d+)M)?((\d+)S)?$";
        var match = Regex.Match(isoDuration, pattern);

        if (!match.Success)
            throw new FormatException($"Ungültiges ISO 8601 Zeitformat: {isoDuration}");

        int hours = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 0;
        int minutes = match.Groups[4].Success ? int.Parse(match.Groups[4].Value) : 0;
        int seconds = match.Groups[6].Success ? int.Parse(match.Groups[6].Value) : 0;

        return hours * 60 + minutes + (seconds >= 30 ? 1 : 0); // optional: runde Sekunden auf
    }
    protected void AssignUnitAndName(string rest, out string? parsedUnit, out string parsedName)
    {
        parsedUnit = null;
        parsedName = rest ?? string.Empty;
        if (string.IsNullOrWhiteSpace(rest))
            return;

        var parts = rest.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1)
        {
            // Ein einzelnes Token: eher Bezeichnung als Einheit -> setzen als Name
            parsedName = parts[0];
            parsedUnit = null;
        }
        else
        {
            // Zwei Teile: erst Teil als Einheit, zweiter als Name
            parsedUnit = parts[0];
            parsedName = parts[1];
        }
    }
    protected RecipeCreateIngredient ParseIngredientLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return new RecipeCreateIngredient(0m, null, string.Empty);

        var qty = line.Trim().TrimStart('*', '-', '•').Trim();

        // Unicode vulgar fractions map
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
        string name = qty;

        // Mixed number: "1 1/2 ..." or "1-1/2 ..."
        var m = System.Text.RegularExpressions.Regex.Match(qty, @"^\s*(\d+)[\s\-]+(\d+)\/(\d+)\s*(.*)$");
        if (m.Success)
        {
            var whole = int.Parse(m.Groups[1].Value);
            var num = int.Parse(m.Groups[2].Value);
            var den = int.Parse(m.Groups[3].Value);
            if (den != 0)
                amount = whole + (decimal)num / den;
            var rest = m.Groups[4].Value.Trim();
            AssignUnitAndName(rest, out unit, out name);
            return new RecipeCreateIngredient(amount, string.IsNullOrWhiteSpace(unit) ? null : unit, string.IsNullOrWhiteSpace(name) ? qty : name);
        }

        // Simple fraction: "1/2 ..."
        m = System.Text.RegularExpressions.Regex.Match(qty, @"^\s*(\d+)\/(\d+)\s*(.*)$");
        if (m.Success)
        {
            var num = int.Parse(m.Groups[1].Value);
            var den = int.Parse(m.Groups[2].Value);
            if (den != 0)
                amount = (decimal)num / den;
            var rest = m.Groups[3].Value.Trim();
            AssignUnitAndName(rest, out unit, out name);
            return new RecipeCreateIngredient(amount, string.IsNullOrWhiteSpace(unit) ? null : unit, string.IsNullOrWhiteSpace(name) ? qty : name);
        }

        // Decimal or integer: "1.5 g ..." or "1,5 g ..." or "2 g"
        m = System.Text.RegularExpressions.Regex.Match(qty, @"^\s*(\d+[.,]?\d*)\s*(.*)$");
        if (m.Success)
        {
            var numStr = m.Groups[1].Value.Replace(',', '.');
            if (decimal.TryParse(numStr, NumberStyles.Number, CultureInfo.InvariantCulture, out var val))
                amount = val;
            var rest = m.Groups[2].Value.Trim();
            AssignUnitAndName(rest, out unit, out name);
            return new RecipeCreateIngredient(amount, string.IsNullOrWhiteSpace(unit) ? null : unit, string.IsNullOrWhiteSpace(name) ? qty : name);
        }

        // Leading unicode vulgar fraction: "½ Zwiebel ..."
        if (qty.Length > 0 && vulgar.TryGetValue(qty[0], out var v))
        {
            amount = v;
            var rest = qty.Substring(1).Trim();
            AssignUnitAndName(rest, out unit, out name);
            return new RecipeCreateIngredient(amount, string.IsNullOrWhiteSpace(unit) ? null : unit, string.IsNullOrWhiteSpace(name) ? qty : name);
        }

        // Trailing quantity pattern: "Rahmspinat, tiefgefroren 400 g" or "Tomaten 2kg"
        m = System.Text.RegularExpressions.Regex.Match(qty, @"^\s*(.+?)\s+(\d+[.,]?\d*|\d+\/\d+|[½¼¾⅓⅔⅛])\s*([^\d].*)?$");
        if (m.Success)
        {
            var namePart = m.Groups[1].Value.Trim();
            var numPart = m.Groups[2].Value;
            var trailing = m.Groups[3].Success ? m.Groups[3].Value.Trim() : null;

            // parse number (fraction / vulgar / decimal)
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
                var normalized = numPart.Replace(',', '.');
                decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
            }

            // trailing may contain unit and/or extra name parts ("g", "g blanchiert")
            if (!string.IsNullOrWhiteSpace(trailing))
            {
                var tParts = trailing.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                unit = tParts.Length >= 1 ? tParts[0] : null;
                if (tParts.Length == 2)
                    name = $"{namePart} {tParts[1]}";
                else
                    name = namePart;
            }
            else
            {
                name = namePart;
            }

            return new RecipeCreateIngredient(amount, string.IsNullOrWhiteSpace(unit) ? null : unit, string.IsNullOrWhiteSpace(name) ? qty : name);
        }

        // No amount detected — treat whole line as name
        return new RecipeCreateIngredient(0m, null, qty);
    }
    protected RecipeIngredient ParseIngredient(string line)
    {
        RecipeCreateIngredient ing =  ParseIngredientLine(line);
        if (string.IsNullOrWhiteSpace(ing.Name))
            return null;
        return new RecipeIngredient()
        {
            Name = ing.Name,
            Amount = ing.Amount,
            Unit = ing.Unit
        };
    }

}
