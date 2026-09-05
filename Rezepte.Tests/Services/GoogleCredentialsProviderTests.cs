using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Rezepte.Tests.TestHelpers;
using Rezepte.Web.Configuration;
using Rezepte.Web.Services;
using Xunit;

namespace Rezepte.Tests.Services;

/// <summary>
/// Class representing the google credentials provider tests.
/// </summary>
[Collection(GoogleCredentialsEnvironmentCollection.Name)]
public class GoogleCredentialsProviderTests
{
    private const string ServiceAccountEnvironmentVariable = EnvironmentVariableScope.ServiceAccountEnvironmentVariable;
    private const string GeminiApiKeyEnvironmentVariable = EnvironmentVariableScope.GeminiApiKeyEnvironmentVariable;

    /// <summary>
    /// Get service account file path returns path from environment variable.
    /// </summary>
    [Fact]
    public void GetServiceAccountFilePath_ReturnsPath_FromEnvironmentVariable()
    {
        using var scope = new EnvironmentVariableScope();
        scope.Set(ServiceAccountEnvironmentVariable, "C:/secrets/google.application-credentials.json");
        var provider = CreateProvider(new GoogleCredentialsOptions { ServiceAccountFilePath = "C:/other/path.json" });

        var result = provider.GetServiceAccountFilePath();

        result.Should().Be("C:/secrets/google.application-credentials.json");
    }

    /// <summary>
    /// Get service account file path returns path from options when env not set.
    /// </summary>
    [Fact]
    public void GetServiceAccountFilePath_ReturnsPath_FromOptions_WhenEnvNotSet()
    {
        using var scope = new EnvironmentVariableScope();
        scope.Set(ServiceAccountEnvironmentVariable, null);
        var provider = CreateProvider(new GoogleCredentialsOptions { ServiceAccountFilePath = "C:/secrets/google.application-credentials.json" });

        var result = provider.GetServiceAccountFilePath();

        result.Should().Be("C:/secrets/google.application-credentials.json");
    }

    /// <summary>
    /// Get service account file path does not mutate environment variable when resolved from options.
    /// </summary>
    [Fact]
    public void GetServiceAccountFilePath_DoesNotMutateEnvironmentVariable_WhenResolvedFromOptions()
    {
        using var scope = new EnvironmentVariableScope();
        scope.Set(ServiceAccountEnvironmentVariable, null);
        var provider = CreateProvider(new GoogleCredentialsOptions { ServiceAccountFilePath = "C:/secrets/google.application-credentials.json" });

        provider.GetServiceAccountFilePath();

        Environment.GetEnvironmentVariable(ServiceAccountEnvironmentVariable).Should().BeNull();
    }

    /// <summary>
    /// Get service account file path reflects options monitor changes without caching.
    /// </summary>
    [Fact]
    public void GetServiceAccountFilePath_ReflectsOptionsMonitorChanges_WithoutCaching()
    {
        using var scope = new EnvironmentVariableScope();
        scope.Set(ServiceAccountEnvironmentVariable, null);
        var options = new GoogleCredentialsOptions { ServiceAccountFilePath = "C:/first/path.json" };
        var provider = CreateProvider(options);

        var firstResult = provider.GetServiceAccountFilePath();
        options.ServiceAccountFilePath = "C:/second/path.json";
        var secondResult = provider.GetServiceAccountFilePath();

        firstResult.Should().Be("C:/first/path.json");
        secondResult.Should().Be("C:/second/path.json");
    }

    /// <summary>
    /// Get service account file path returns empty when nothing configured.
    /// </summary>
    [Fact]
    public void GetServiceAccountFilePath_ReturnsEmpty_WhenNothingConfigured()
    {
        using var scope = new EnvironmentVariableScope();
        scope.Set(ServiceAccountEnvironmentVariable, null);
        var provider = CreateProvider(new GoogleCredentialsOptions { ServiceAccountFilePath = "" });

        var result = provider.GetServiceAccountFilePath();

        result.Should().BeEmpty();
    }

    /// <summary>
    /// Get service account file path returns empty when options value is whitespace.
    /// </summary>
    [Fact]
    public void GetServiceAccountFilePath_ReturnsEmpty_WhenOptionsValueIsWhitespace()
    {
        using var scope = new EnvironmentVariableScope();
        scope.Set(ServiceAccountEnvironmentVariable, null);
        var provider = CreateProvider(new GoogleCredentialsOptions { ServiceAccountFilePath = "   " });

        var result = provider.GetServiceAccountFilePath();

        result.Should().BeEmpty();
    }

    /// <summary>
    /// Service account file exists returns false when path missing.
    /// </summary>
    [Fact]
    public void ServiceAccountFileExists_ReturnsFalse_WhenPathMissing()
    {
        using var scope = new EnvironmentVariableScope();
        scope.Set(ServiceAccountEnvironmentVariable, null);
        var provider = CreateProvider(new GoogleCredentialsOptions { ServiceAccountFilePath = "" });

        var result = provider.ServiceAccountFileExists();

        result.Should().BeFalse();
    }

    /// <summary>
    /// Service account file exists returns true when path is set and file exists.
    /// </summary>
    [Fact]
    public void ServiceAccountFileExists_ReturnsTrue_WhenPathIsSetAndFileExists()
    {
        using var scope = new EnvironmentVariableScope();
        var tempFilePath = Path.GetTempFileName();
        try
        {
            scope.Set(ServiceAccountEnvironmentVariable, tempFilePath);
            var provider = CreateProvider(new GoogleCredentialsOptions { ServiceAccountFilePath = "" });

            var result = provider.ServiceAccountFileExists();

            result.Should().BeTrue();
        }
        finally
        {
            File.Delete(tempFilePath);
        }
    }

    /// <summary>
    /// Get gemini api key returns key from environment variable.
    /// </summary>
    [Fact]
    public void GetGeminiApiKey_ReturnsKey_FromEnvironmentVariable()
    {
        using var scope = new EnvironmentVariableScope();
        scope.Set(GeminiApiKeyEnvironmentVariable, "env-api-key");
        var provider = CreateProvider(new GoogleCredentialsOptions { GeminiApiKey = "options-api-key" });

        var result = provider.GetGeminiApiKey();

        result.Should().Be("env-api-key");
    }

    /// <summary>
    /// Get gemini api key returns key from options when env not set.
    /// </summary>
    [Fact]
    public void GetGeminiApiKey_ReturnsKey_FromOptions_WhenEnvNotSet()
    {
        using var scope = new EnvironmentVariableScope();
        scope.Set(GeminiApiKeyEnvironmentVariable, null);
        var provider = CreateProvider(new GoogleCredentialsOptions { GeminiApiKey = "options-api-key" });

        var result = provider.GetGeminiApiKey();

        result.Should().Be("options-api-key");
    }

    /// <summary>
    /// Get gemini api key returns empty when nothing configured.
    /// </summary>
    [Fact]
    public void GetGeminiApiKey_ReturnsEmpty_WhenNothingConfigured()
    {
        using var scope = new EnvironmentVariableScope();
        scope.Set(GeminiApiKeyEnvironmentVariable, null);
        var provider = CreateProvider(new GoogleCredentialsOptions { GeminiApiKey = "" });

        var result = provider.GetGeminiApiKey();

        result.Should().BeEmpty();
    }

    /// <summary>
    /// Get gemini api key returns empty when options value is whitespace.
    /// </summary>
    [Fact]
    public void GetGeminiApiKey_ReturnsEmpty_WhenOptionsValueIsWhitespace()
    {
        using var scope = new EnvironmentVariableScope();
        scope.Set(GeminiApiKeyEnvironmentVariable, null);
        var provider = CreateProvider(new GoogleCredentialsOptions { GeminiApiKey = "   " });

        var result = provider.GetGeminiApiKey();

        result.Should().BeEmpty();
    }

    /// <summary>
    /// Get diagnostics reports environment sources without api key value.
    /// </summary>
    [Fact]
    public void GetDiagnostics_ReportsEnvironmentSources_WithoutApiKeyValue()
    {
        using var scope = new EnvironmentVariableScope();
        var tempFilePath = Path.GetTempFileName();
        try
        {
            scope.Set(ServiceAccountEnvironmentVariable, tempFilePath);
            scope.Set(GeminiApiKeyEnvironmentVariable, "env-api-key");
            var provider = CreateProvider(new GoogleCredentialsOptions
            {
                ServiceAccountFilePath = "C:/options/service-account.json",
                GeminiApiKey = "options-api-key"
            });

            var result = provider.GetDiagnostics();

            result.ServiceAccountEnvironmentVariableSet.Should().BeTrue();
            result.ServiceAccountOptionsFallbackSet.Should().BeFalse();
            result.ServiceAccountFilePath.Should().Be(tempFilePath);
            result.ServiceAccountFileExists.Should().BeTrue();
            result.ServiceAccountSource.Should().Be("environment");
            result.GeminiApiKeyEnvironmentVariableSet.Should().BeTrue();
            result.GeminiApiKeyOptionsFallbackSet.Should().BeFalse();
            result.GeminiApiKeyConfigured.Should().BeTrue();
            result.GeminiApiKeySource.Should().Be("environment");
            result.ToString().Should().NotContain("env-api-key");
        }
        finally
        {
            File.Delete(tempFilePath);
        }
    }

    /// <summary>
    /// Get diagnostics reports options fallback when environment variables missing.
    /// </summary>
    [Fact]
    public void GetDiagnostics_ReportsOptionsFallback_WhenEnvironmentVariablesMissing()
    {
        using var scope = new EnvironmentVariableScope();
        scope.Set(ServiceAccountEnvironmentVariable, null);
        scope.Set(GeminiApiKeyEnvironmentVariable, null);
        var provider = CreateProvider(new GoogleCredentialsOptions
        {
            ServiceAccountFilePath = "C:/options/service-account.json",
            GeminiApiKey = "options-api-key"
        });

        var result = provider.GetDiagnostics();

        result.ServiceAccountEnvironmentVariableSet.Should().BeFalse();
        result.ServiceAccountOptionsFallbackSet.Should().BeTrue();
        result.ServiceAccountFilePath.Should().Be("C:/options/service-account.json");
        result.ServiceAccountFileExists.Should().BeFalse();
        result.ServiceAccountSource.Should().Be("options");
        result.GeminiApiKeyEnvironmentVariableSet.Should().BeFalse();
        result.GeminiApiKeyOptionsFallbackSet.Should().BeTrue();
        result.GeminiApiKeyConfigured.Should().BeTrue();
        result.GeminiApiKeySource.Should().Be("options");
        result.ToString().Should().NotContain("options-api-key");
    }

    private static GoogleCredentialsProvider CreateProvider(GoogleCredentialsOptions options)
    {
        var monitor = new Mock<IOptionsMonitor<GoogleCredentialsOptions>>();
        monitor.Setup(m => m.CurrentValue).Returns(options);
        return new GoogleCredentialsProvider(monitor.Object);
    }
}
