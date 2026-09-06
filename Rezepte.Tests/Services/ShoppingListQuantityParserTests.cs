using FluentAssertions;
using Rezepte.Web.Services;
using Xunit;

namespace Rezepte.Tests.Services;

/// <summary>
/// Class representing the shopping list quantity parser tests.
/// </summary>
public class ShoppingListQuantityParserTests
{
    /// <summary>
    /// Quantity examples.
    /// </summary>
    /// <returns>The result.</returns>
    public static TheoryData<string?, decimal, string?> QuantityExamples => new()
    {
        { "2 kg", 2, "kg" },
        { "0.5 l", 0.5m, "l" },
        { "2,5 kg", 2.5m, "kg" },
        { "2", 2, null },
        { string.Empty, 0, null },
        { "Mehl", 0, "Mehl" },
        { "-1 kg", 0, "kg" }
    };

    /// <summary>
    /// Parse quantity should split amount and unit.
    /// </summary>
    /// <param name="input">The input parameter.</param>
    /// <param name="expectedAmount">The expected amount parameter.</param>
    /// <param name="expectedUnit">The expected unit parameter.</param>
    [Theory]
    [MemberData(nameof(QuantityExamples))]
    public void ParseQuantity_ShouldSplitAmountAndUnit(string? input, decimal expectedAmount, string? expectedUnit)
    {
        var result = ShoppingListQuantityParser.ParseQuantity(input);

        result.Amount.Should().Be(expectedAmount);
        result.Unit.Should().Be(expectedUnit);
    }

    /// <summary>
    /// Parse amount should clamp negative numbers to zero.
    /// </summary>
    [Fact]
    public void ParseAmount_ShouldClampNegativeNumbersToZero()
    {
        var result = ShoppingListQuantityParser.ParseAmount("-2,5");

        result.Should().Be(0);
    }
}
