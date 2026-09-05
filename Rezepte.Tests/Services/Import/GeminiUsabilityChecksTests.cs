using FluentAssertions;
using Moq;
using Rezepte.Web.Services;
using Rezepte.Web.Services.Import;
using Xunit;

namespace Rezepte.Tests.Services.Import;

/// <summary>
/// Class representing the gemini usability checks tests.
/// </summary>
public sealed class GeminiUsabilityChecksTests
{
    /// <summary>
    /// Collect async should return no issues when all prerequisites met.
    /// </summary>
    [Fact]
    public async Task CollectAsync_ShouldReturnNoIssues_WhenAllPrerequisitesMet()
    {
        var settings = CreateSettings(globalAiEnabled: true, globalGeminiEnabled: true);
        var geminiClient = CreateGeminiClient(hasApiKey: true, hasServiceAccount: false);

        var issues = await GeminiUsabilityChecks.CollectAsync(settings, geminiClient);

        issues.Should().BeEmpty();
    }

    /// <summary>
    /// Collect async should report disabled global ai.
    /// </summary>
    [Fact]
    public async Task CollectAsync_ShouldReportDisabledGlobalAi()
    {
        var settings = CreateSettings(globalAiEnabled: false, globalGeminiEnabled: true);
        var geminiClient = CreateGeminiClient(hasApiKey: true, hasServiceAccount: false);

        var issues = await GeminiUsabilityChecks.CollectAsync(settings, geminiClient);

        issues.Should().ContainSingle(i => i.Message == "Global AI is disabled." && i.Hint != null);
    }

    /// <summary>
    /// Collect async should report missing gemini authentication.
    /// </summary>
    [Fact]
    public async Task CollectAsync_ShouldReportMissingGeminiAuthentication()
    {
        var settings = CreateSettings(globalAiEnabled: true, globalGeminiEnabled: true);
        var geminiClient = CreateGeminiClient(hasApiKey: false, hasServiceAccount: false);

        var issues = await GeminiUsabilityChecks.CollectAsync(settings, geminiClient);

        issues.Should().ContainSingle(i => i.Message == "Gemini authentication is missing." && i.Hint != null);
    }

    /// <summary>
    /// Collect async should report disabled global gemini.
    /// </summary>
    [Fact]
    public async Task CollectAsync_ShouldReportDisabledGlobalGemini()
    {
        var settings = CreateSettings(globalAiEnabled: true, globalGeminiEnabled: false);
        var geminiClient = CreateGeminiClient(hasApiKey: true, hasServiceAccount: false);

        var issues = await GeminiUsabilityChecks.CollectAsync(settings, geminiClient);

        issues.Should().ContainSingle(i => i.Message == "Global Gemini is disabled." && i.Hint != null);
    }

    private static ISettingsService CreateSettings(bool globalAiEnabled, bool globalGeminiEnabled)
    {
        var settings = new Mock<ISettingsService>();
        settings.Setup(s => s.GetGlobalAiEnabledAsync(It.IsAny<CancellationToken>())).ReturnsAsync(globalAiEnabled);
        settings.Setup(s => s.GetGlobalGeminiEnabledAsync(It.IsAny<CancellationToken>())).ReturnsAsync(globalGeminiEnabled);
        return settings.Object;
    }

    private static IGeminiClient CreateGeminiClient(bool hasApiKey, bool hasServiceAccount)
    {
        var geminiClient = new Mock<IGeminiClient>();
        geminiClient.Setup(c => c.HasApiKey()).Returns(hasApiKey);
        geminiClient.Setup(c => c.HasServiceAccount()).Returns(hasServiceAccount);
        return geminiClient.Object;
    }
}
