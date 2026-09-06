using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Rezepte.Import.Plugins.AIFoto;
using Rezepte.Web.Services;
using Rezepte.Web.Services.Import;
using Xunit;

namespace Rezepte.Tests.Services.Import;

/// <summary>
/// Class representing the aifoto import plugin tests.
/// </summary>
public sealed class AIFotoImportPluginTests
{
    /// <summary>
    /// Check usability async should return usable when all global prerequisites met.
    /// </summary>
    [Fact]
    public async Task CheckUsabilityAsync_ShouldReturnUsable_WhenAllGlobalPrerequisitesMet()
    {
        var serviceProvider = CreateServiceProvider(
            globalAiEnabled: true,
            serviceAccountFileExists: true,
            hasApiKey: true,
            hasServiceAccount: false,
            globalGoogleVisionEnabled: true,
            globalGeminiEnabled: true);
        var sut = new AIFotoImportPlugin();

        var result = await sut.CheckUsabilityAsync(serviceProvider);

        result.IsUsable.Should().BeTrue();
        result.Issues.Should().BeEmpty();
    }

    /// <summary>
    /// Check usability async should report missing vision service account.
    /// </summary>
    [Fact]
    public async Task CheckUsabilityAsync_ShouldReportMissingVisionServiceAccount()
    {
        var serviceProvider = CreateServiceProvider(
            globalAiEnabled: true,
            serviceAccountFileExists: false,
            hasApiKey: true,
            hasServiceAccount: false,
            globalGoogleVisionEnabled: true,
            globalGeminiEnabled: true);
        var sut = new AIFotoImportPlugin();

        var result = await sut.CheckUsabilityAsync(serviceProvider);

        result.IsUsable.Should().BeFalse();
        result.Issues.Should().ContainSingle(i => i.Message == "Google Vision service account file is missing." && i.Hint != null);
    }

    /// <summary>
    /// Check usability async should report disabled global vision.
    /// </summary>
    [Fact]
    public async Task CheckUsabilityAsync_ShouldReportDisabledGlobalVision()
    {
        var serviceProvider = CreateServiceProvider(
            globalAiEnabled: true,
            serviceAccountFileExists: true,
            hasApiKey: true,
            hasServiceAccount: false,
            globalGoogleVisionEnabled: false,
            globalGeminiEnabled: true);
        var sut = new AIFotoImportPlugin();

        var result = await sut.CheckUsabilityAsync(serviceProvider);

        result.IsUsable.Should().BeFalse();
        result.Issues.Should().ContainSingle(i => i.Message == "Global Google Vision is disabled.");
    }

    private static IServiceProvider CreateServiceProvider(
        bool globalAiEnabled,
        bool serviceAccountFileExists,
        bool hasApiKey,
        bool hasServiceAccount,
        bool globalGoogleVisionEnabled,
        bool globalGeminiEnabled)
    {
        var settings = new Mock<ISettingsService>();
        settings.Setup(s => s.GetGlobalAiEnabledAsync(It.IsAny<CancellationToken>())).ReturnsAsync(globalAiEnabled);
        settings.Setup(s => s.GetGlobalGoogleVisionEnabledAsync(It.IsAny<CancellationToken>())).ReturnsAsync(globalGoogleVisionEnabled);
        settings.Setup(s => s.GetGlobalGeminiEnabledAsync(It.IsAny<CancellationToken>())).ReturnsAsync(globalGeminiEnabled);

        var geminiClient = new Mock<IGeminiClient>();
        geminiClient.Setup(c => c.HasApiKey()).Returns(hasApiKey);
        geminiClient.Setup(c => c.HasServiceAccount()).Returns(hasServiceAccount);

        var credentialsProvider = new Mock<IGoogleCredentialsProvider>();
        credentialsProvider.Setup(c => c.GetDiagnostics()).Returns(new GoogleCredentialsDiagnostics(
            ServiceAccountEnvironmentVariableSet: serviceAccountFileExists,
            ServiceAccountOptionsFallbackSet: false,
            ServiceAccountFilePath: "service-account.json",
            ServiceAccountFileExists: serviceAccountFileExists,
            GeminiApiKeyEnvironmentVariableSet: hasApiKey,
            GeminiApiKeyOptionsFallbackSet: false));

        var services = new ServiceCollection();
        services.AddSingleton(settings.Object);
        services.AddSingleton(geminiClient.Object);
        services.AddSingleton(credentialsProvider.Object);
        return services.BuildServiceProvider();
    }
}
