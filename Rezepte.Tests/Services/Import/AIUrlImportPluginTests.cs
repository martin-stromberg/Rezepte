using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Rezepte.Import.Plugins.AIUrl;
using Rezepte.Web.Services;
using Rezepte.Web.Services.Import;
using Xunit;

namespace Rezepte.Tests.Services.Import;

public sealed class AIUrlImportPluginTests
{
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
