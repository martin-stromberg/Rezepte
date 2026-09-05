using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Rezepte.Import.Plugins.AIUrl;
using Rezepte.Web.Services;
using Rezepte.Web.Services.Import;
using Xunit;

namespace Rezepte.Tests.Services.Import;

/// <summary>
/// Class representing the aiurl import plugin tests.
/// </summary>
public sealed class AIUrlImportPluginTests
{
    /// <summary>
    /// Check usability async should return usable when all global prerequisites met.
    /// </summary>
    [Fact]
    public async Task CheckUsabilityAsync_ShouldReturnUsable_WhenAllGlobalPrerequisitesMet()
    {
        var serviceProvider = CreateServiceProvider(
            globalAiEnabled: true,
            hasApiKey: true,
            hasServiceAccount: false,
            globalGeminiEnabled: true);
        var sut = new AIUrlImportPlugin();

        var result = await sut.CheckUsabilityAsync(serviceProvider);

        result.IsUsable.Should().BeTrue();
        result.Issues.Should().BeEmpty();
    }

    /// <summary>
    /// Check usability async should report disabled global gemini.
    /// </summary>
    [Fact]
    public async Task CheckUsabilityAsync_ShouldReportDisabledGlobalGemini()
    {
        var serviceProvider = CreateServiceProvider(
            globalAiEnabled: true,
            hasApiKey: true,
            hasServiceAccount: false,
            globalGeminiEnabled: false);
        var sut = new AIUrlImportPlugin();

        var result = await sut.CheckUsabilityAsync(serviceProvider);

        result.IsUsable.Should().BeFalse();
        result.Issues.Should().ContainSingle(i => i.Message == "Global Gemini is disabled.");
    }

    /// <summary>
    /// Check usability async should report missing gemini authentication.
    /// </summary>
    [Fact]
    public async Task CheckUsabilityAsync_ShouldReportMissingGeminiAuthentication()
    {
        var serviceProvider = CreateServiceProvider(
            globalAiEnabled: true,
            hasApiKey: false,
            hasServiceAccount: false,
            globalGeminiEnabled: true);
        var sut = new AIUrlImportPlugin();

        var result = await sut.CheckUsabilityAsync(serviceProvider);

        result.IsUsable.Should().BeFalse();
        result.Issues.Should().ContainSingle(i => i.Message == "Gemini authentication is missing." && i.Hint != null);
    }

    private static IServiceProvider CreateServiceProvider(
        bool globalAiEnabled,
        bool hasApiKey,
        bool hasServiceAccount,
        bool globalGeminiEnabled)
    {
        var settings = new Mock<ISettingsService>();
        settings.Setup(s => s.GetGlobalAiEnabledAsync(It.IsAny<CancellationToken>())).ReturnsAsync(globalAiEnabled);
        settings.Setup(s => s.GetGlobalGeminiEnabledAsync(It.IsAny<CancellationToken>())).ReturnsAsync(globalGeminiEnabled);

        var geminiClient = new Mock<IGeminiClient>();
        geminiClient.Setup(c => c.HasApiKey()).Returns(hasApiKey);
        geminiClient.Setup(c => c.HasServiceAccount()).Returns(hasServiceAccount);

        var services = new ServiceCollection();
        services.AddSingleton(settings.Object);
        services.AddSingleton(geminiClient.Object);
        return services.BuildServiceProvider();
    }
}
