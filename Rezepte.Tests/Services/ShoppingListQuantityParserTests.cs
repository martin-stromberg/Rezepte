using FluentAssertions;
using Rezepte.Web.Services;
using Xunit;

namespace Rezepte.Tests.Services;

public class ShoppingListQuantityParserTests
{
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

    [Theory]
    [MemberData(nameof(QuantityExamples))]
    public void ParseQuantity_ShouldSplitAmountAndUnit(string? input, decimal expectedAmount, string? expectedUnit)
    {
        var result = ShoppingListQuantityParser.ParseQuantity(input);

        result.Amount.Should().Be(expectedAmount);
        result.Unit.Should().Be(expectedUnit);
    }

    [Fact]
    public void ParseAmount_ShouldClampNegativeNumbersToZero()
    {
        var result = ShoppingListQuantityParser.ParseAmount("-2,5");

        result.Should().Be(0);
    }
}
