using System.Globalization;

namespace Rezepte.Import.Abstractions;

public static class StringParsingExtensions
{
    public static int ToInt32Invariant(this string? value, int defaultValue = 0)
    {
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;

        var digits = new string(value.Where(c => char.IsDigit(c) || c == '-' || c == '+').ToArray());
        return int.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
            ? result
            : defaultValue;
    }
}
