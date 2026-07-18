using System.Globalization;

namespace Rezepte.Web.Services;

public static class ShoppingListQuantityParser
{
    public static decimal ParseAmount(string? value)
    {
        var text = value?.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        if (text.Contains(',') &&
            !text.Contains('.') &&
            decimal.TryParse(text.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var commaAmount))
        {
            return Math.Max(0, commaAmount);
        }

        if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount) ||
            decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out amount))
        {
            return Math.Max(0, amount);
        }

        return 0;
    }

    public static (decimal Amount, string? Unit) ParseQuantity(string? value)
    {
        var text = value?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
        {
            return (0, null);
        }

        var firstWhitespace = text.IndexOfAny(new[] { ' ', '\t', '\r', '\n' });
        var amountText = firstWhitespace < 0 ? text : text[..firstWhitespace];
        var unit = firstWhitespace < 0 ? string.Empty : text[firstWhitespace..].Trim();

        if (!CanParseAmountToken(amountText))
        {
            return (0, text);
        }

        return (ParseAmount(amountText), string.IsNullOrWhiteSpace(unit) ? null : unit);
    }

    private static bool CanParseAmountToken(string value)
    {
        var text = value.Trim();
        if (text.Contains(',') && !text.Contains('.'))
        {
            text = text.Replace(',', '.');
        }

        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out _) ||
            decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out _);
    }
}
