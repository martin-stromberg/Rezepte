using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;
using SdkParserBase = Rezepte.Import.PluginSdk.ImportParserBase;
using WebParserBase = Rezepte.Web.Services.Import.ImportParserBase;

namespace Rezepte.Tests.Services.Import;

/// <summary>
/// Shared expectations for the identical parser implementations in the plugin SDK and the web project.
/// </summary>
public static class ImportParserBaseTestData
{
    public static TheoryData<string?, decimal, string?, string> Ingredients => new()
    {
        { null, 0m, null, "" },
        { "   ", 0m, null, "" },
        { "Mehl", 0m, null, "Mehl" },
        { "2 Eier", 2m, null, "Eier" },
        { "500 g Mehl", 500m, "g", "Mehl" },
        { "1,5 kg Kartoffeln", 1.5m, "kg", "Kartoffeln" },
        { "* 250 ml Sahne", 250m, "ml", "Sahne" },
        { "- 1 Dose. Tomaten", 1m, "Dose.", "Tomaten" },
        { "1 1/2 EL Zucker", 1.5m, "EL", "Zucker" },
        { "3/4 l Milch", 0.75m, "l", "Milch" },
        { "0/0 Mehl", 0m, null, "Mehl" },
        { "500 g", 500m, "g", "500 g" },
        { "Salz 2 Prisen", 2m, null, "Salz Prisen" },
        { "Zucker 1/2", 1m, null, "Zucker /2" },
        { "Butter 100 weich", 100m, null, "Butter weich" }
    };

    public static TheoryData<string?, decimal, string?, string> VulgarIngredients => new()
    {
        { "½ TL Salz", 0.5m, "TL", "Salz" },
        { "¼ kg Butter", 0.25m, "kg", "Butter" },
        { "¾ l Wasser", 0.75m, "l", "Wasser" },
        { "⅛ TL Zimt", 0.125m, "TL", "Zimt" },
        { "Mehl ½", 0.5m, null, "Mehl" },
        { "Zucker ¼", 0.25m, null, "Zucker" }
    };

    public static TheoryData<string?, int> Durations => new()
    {
        { null, 0 },
        { "  ", 0 },
        { "PT", 0 },
        { "PT2H", 120 },
        { "PT30M", 30 },
        { "PT1H30M", 90 },
        { "PT45S", 1 },
        { "PT29S", 0 },
        { "PT1H15M30S", 76 }
    };

    public static TheoryData<string> InvalidDurations => new()
    {
        "1H30M",
        "PT1H30",
        "P1DT1H",
        "PT30M1H"
    };
}

public class SdkImportParserBaseTests
{
    private sealed class Probe : SdkParserBase
    {
        public new int ParseIsoDurationToMinutes(string? value) => base.ParseIsoDurationToMinutes(value);

        public (decimal Amount, string? Unit, string Name) Parse(string? line)
        {
            var parsed = ParseIngredientLine(line);
            return (parsed.Amount, parsed.Unit, parsed.Name);
        }
    }

    private readonly Probe _sut = new();

    [Theory]
    [MemberData(nameof(ImportParserBaseTestData.Ingredients), MemberType = typeof(ImportParserBaseTestData))]
    public void ParseIngredientLine_ShouldParseQuantityUnitAndName(string? line, decimal amount, string? unit, string name)
        => _sut.Parse(line).Should().Be((amount, unit, name));

    [Theory]
    [MemberData(nameof(ImportParserBaseTestData.VulgarIngredients), MemberType = typeof(ImportParserBaseTestData))]
    public void ParseIngredientLine_ShouldParseVulgarFractions(string? line, decimal amount, string? unit, string name)
        => _sut.Parse(line).Should().Be((amount, unit, name));

    [Fact]
    public void ParseIngredientLine_ShouldParseThirdsAsFraction()
    {
        var result = _sut.Parse("⅓ Tasse Reis");

        result.Amount.Should().BeApproximately(1m / 3m, 0.0001m);
        result.Unit.Should().BeNull();
        result.Name.Should().Be("Tasse Reis");
    }

    [Theory]
    [MemberData(nameof(ImportParserBaseTestData.Durations), MemberType = typeof(ImportParserBaseTestData))]
    public void ParseIsoDurationToMinutes_ShouldConvertDuration(string? value, int expected)
        => _sut.ParseIsoDurationToMinutes(value).Should().Be(expected);

    [Theory]
    [MemberData(nameof(ImportParserBaseTestData.InvalidDurations), MemberType = typeof(ImportParserBaseTestData))]
    public void ParseIsoDurationToMinutes_ShouldThrowForInvalidDuration(string value)
    {
        var act = () => _sut.ParseIsoDurationToMinutes(value);

        act.Should().Throw<FormatException>();
    }
}

public class WebImportParserBaseTests
{
    private sealed class Probe : WebParserBase
    {
        public new int ParseIsoDurationToMinutes(string? value) => base.ParseIsoDurationToMinutes(value);

        public (decimal Amount, string? Unit, string Name) Parse(string? line)
        {
            var parsed = ParseIngredientLine(line);
            return (parsed.Amount, parsed.Unit, parsed.Name);
        }
    }

    private readonly Probe _sut = new();

    [Theory]
    [MemberData(nameof(ImportParserBaseTestData.Ingredients), MemberType = typeof(ImportParserBaseTestData))]
    public void ParseIngredientLine_ShouldParseQuantityUnitAndName(string? line, decimal amount, string? unit, string name)
        => _sut.Parse(line).Should().Be((amount, unit, name));

    [Theory]
    [MemberData(nameof(ImportParserBaseTestData.VulgarIngredients), MemberType = typeof(ImportParserBaseTestData))]
    public void ParseIngredientLine_ShouldParseVulgarFractions(string? line, decimal amount, string? unit, string name)
        => _sut.Parse(line).Should().Be((amount, unit, name));

    [Theory]
    [MemberData(nameof(ImportParserBaseTestData.Durations), MemberType = typeof(ImportParserBaseTestData))]
    public void ParseIsoDurationToMinutes_ShouldConvertDuration(string? value, int expected)
        => _sut.ParseIsoDurationToMinutes(value).Should().Be(expected);

    [Theory]
    [MemberData(nameof(ImportParserBaseTestData.InvalidDurations), MemberType = typeof(ImportParserBaseTestData))]
    public void ParseIsoDurationToMinutes_ShouldThrowForInvalidDuration(string value)
    {
        var act = () => _sut.ParseIsoDurationToMinutes(value);

        act.Should().Throw<FormatException>();
    }
}
