using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Rezepte.Import.Plugins.AIFoto;
using Rezepte.Import.Plugins.AIUrl;
using Rezepte.Tests.TestHelpers;
using Rezepte.Web.Configuration;
using Rezepte.Web.Services;
using Rezepte.Web.Services.Import;
using Xunit;

namespace Rezepte.Tests.Services.Import;

[Collection(GoogleCredentialsEnvironmentCollection.Name)]
public class AIImportHandlerActivationTests
{
    [Fact]
    public async Task AIUrl_IsActive_WhenOnlyGeminiApiKeyExists()
    {
        using var scope = new EnvironmentVariableScope();
        scope.Set(EnvironmentVariableScope.ServiceAccountEnvironmentVariable, null);
        scope.Set(EnvironmentVariableScope.GeminiApiKeyEnvironmentVariable, "test-api-key");

        var handler = CreateUrlHandler(hasApiKey: true, hasServiceAccount: false);

        var result = await handler.CanHandleAsync(HtmlStream(), "recipe.html");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task AIUrl_IsInactive_WhenGeminiAuthenticationIsMissing()
    {
        using var scope = new EnvironmentVariableScope();
        scope.Set(EnvironmentVariableScope.ServiceAccountEnvironmentVariable, null);
        scope.Set(EnvironmentVariableScope.GeminiApiKeyEnvironmentVariable, null);

        var handler = CreateUrlHandler(hasApiKey: false, hasServiceAccount: false);

        var result = await handler.CanHandleAsync(HtmlStream(), "recipe.html");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task AIFoto_IsActive_WhenVisionServiceAccountAndGeminiAuthenticationExist()
    {
        using var scope = new EnvironmentVariableScope();
        var serviceAccountPath = Path.GetTempFileName();
        try
        {
            scope.Set(EnvironmentVariableScope.ServiceAccountEnvironmentVariable, serviceAccountPath);
            scope.Set(EnvironmentVariableScope.GeminiApiKeyEnvironmentVariable, "test-api-key");

            var handler = CreateFotoHandler(hasApiKey: true, hasServiceAccount: true);

            var result = await handler.CanHandleAsync(ImageStream(), "recipe.jpg");

            result.Should().BeTrue();
        }
        finally
        {
            File.Delete(serviceAccountPath);
        }
    }

    [Fact]
    public async Task AIFoto_IsInactive_WhenOnlyGeminiApiKeyExists()
    {
        using var scope = new EnvironmentVariableScope();
        scope.Set(EnvironmentVariableScope.ServiceAccountEnvironmentVariable, null);
        scope.Set(EnvironmentVariableScope.GeminiApiKeyEnvironmentVariable, "test-api-key");

        var handler = CreateFotoHandler(hasApiKey: true, hasServiceAccount: false);

        var result = await handler.CanHandleAsync(ImageStream(), "recipe.jpg");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task AIFoto_IsInactive_WhenServiceAccountPathDoesNotExist()
    {
        using var scope = new EnvironmentVariableScope();
        var missingPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
        scope.Set(EnvironmentVariableScope.ServiceAccountEnvironmentVariable, missingPath);
        scope.Set(EnvironmentVariableScope.GeminiApiKeyEnvironmentVariable, "test-api-key");

        var handler = CreateFotoHandler(hasApiKey: true, hasServiceAccount: false);

        var result = await handler.CanHandleAsync(ImageStream(), "recipe.jpg");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task BaseActivation_DoesNotRequireServiceAccount_WhenGeminiApiKeyExists()
    {
        using var scope = new EnvironmentVariableScope();
        scope.Set(EnvironmentVariableScope.ServiceAccountEnvironmentVariable, null);
        scope.Set(EnvironmentVariableScope.GeminiApiKeyEnvironmentVariable, "test-api-key");

        var handler = CreateUrlHandler(hasApiKey: true, hasServiceAccount: false);

        var result = await handler.CanHandleAsync(HtmlStream(), "recipe.html");

        result.Should().BeTrue();
    }

    private static AIUrlImportHandler CreateUrlHandler(bool hasApiKey, bool hasServiceAccount)
    {
        var handler = new AIUrlImportHandler(
            CreateOptionsMonitor(),
            Mock.Of<IRecipeService>(),
            NullLogger<AIUrlImportHandler>.Instance,
            Mock.Of<IAiUsageService>(),
            CreateGeminiClient(hasApiKey, hasServiceAccount),
            CreateEnabledSettings());
        handler.UserId = "user-1";
        return handler;
    }

    private static AIFotoImportHandler CreateFotoHandler(bool hasApiKey, bool hasServiceAccount)
    {
        var handler = new AIFotoImportHandler(
            Mock.Of<IRecipeService>(),
            CreateOptionsMonitor(),
            Mock.Of<IAiUsageService>(),
            new MemoryCache(new MemoryCacheOptions()),
            CreateGeminiClient(hasApiKey, hasServiceAccount),
            CreateCredentialsProvider(),
            NullLogger<AIFotoImportHandler>.Instance,
            CreateEnabledSettings());
        handler.UserId = "user-1";
        return handler;
    }

    private static IOptionsMonitor<AIOptions> CreateOptionsMonitor()
    {
        var monitor = new Mock<IOptionsMonitor<AIOptions>>();
        monitor.Setup(m => m.CurrentValue).Returns(new AIOptions());
        return monitor.Object;
    }

    private static IGeminiClient CreateGeminiClient(bool hasApiKey, bool hasServiceAccount)
    {
        var geminiClient = new Mock<IGeminiClient>();
        geminiClient.Setup(c => c.HasApiKey()).Returns(hasApiKey);
        geminiClient.Setup(c => c.HasServiceAccount()).Returns(hasServiceAccount);
        return geminiClient.Object;
    }

    private static IGoogleCredentialsProvider CreateCredentialsProvider()
    {
        var monitor = new Mock<IOptionsMonitor<GoogleCredentialsOptions>>();
        monitor.Setup(m => m.CurrentValue).Returns(new GoogleCredentialsOptions());
        return new GoogleCredentialsProvider(monitor.Object);
    }

    private static ISettingsService CreateEnabledSettings()
    {
        var settings = new Mock<ISettingsService>();
        settings.Setup(s => s.GetGlobalAiEnabledAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        settings.Setup(s => s.GetUserAiEnabledAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        settings.Setup(s => s.GetGlobalGoogleVisionEnabledAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        settings.Setup(s => s.GetUserGoogleVisionEnabledAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        settings.Setup(s => s.GetGlobalGeminiEnabledAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        settings.Setup(s => s.GetUserGeminiEnabledAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        return settings.Object;
    }

    private static MemoryStream HtmlStream()
    {
        return new MemoryStream(Encoding.UTF8.GetBytes("<html><body><h1>Recipe</h1></body></html>"));
    }

    private static MemoryStream ImageStream()
    {
        return new MemoryStream([1, 2, 3]);
    }
}
