using System.Globalization;

namespace Rezepte.Web.Services;

/// <summary>
/// Represents the shopping list quantity parser class.
/// </summary>
public static class ShoppingListQuantityParser
{
    /// <summary>
    /// Parses the amount.
    /// </summary>
    /// <param name="value">The value parameter.</param>
    /// <returns>The result.</returns>
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

    /// <summary>
    /// Represents the public class.
    /// </summary>
    /// <param name="Amount">The amount parameter.</param>
    /// <param name="value">The value parameter.</param>
    /// <param>...</param>
    /// <param>...</param>
    /// <param>...</param>
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
