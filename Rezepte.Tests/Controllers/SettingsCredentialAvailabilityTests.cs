using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using Rezepte.Tests.TestHelpers;
using Rezepte.Web.Configuration;
using Rezepte.Web.Controllers;
using Rezepte.Web.Services;
using System.Security.Claims;
using Xunit;

namespace Rezepte.Tests.Controllers;

/// <summary>
/// Class representing the settings credential availability tests.
/// </summary>
[Collection(GoogleCredentialsEnvironmentCollection.Name)]
public class SettingsCredentialAvailabilityTests
{
    private const string ServiceAccountEnvironmentVariable = EnvironmentVariableScope.ServiceAccountEnvironmentVariable;
    private const string GeminiApiKeyEnvironmentVariable = EnvironmentVariableScope.GeminiApiKeyEnvironmentVariable;

    /// <summary>
    /// Get my settings reports credentials available when provided via environment variable.
    /// </summary>
    [Fact]
    public async Task GetMySettings_ReportsCredentialsAvailable_WhenProvidedViaEnvironmentVariable()
    {
        using var scope = new EnvironmentVariableScope();
        var serviceAccountPath = Path.GetTempFileName();
        try
        {
            scope.Set(ServiceAccountEnvironmentVariable, serviceAccountPath);
            scope.Set(GeminiApiKeyEnvironmentVariable, "test-api-key");

            var controller = CreateController(new GoogleCredentialsOptions());

            var result = await controller.GetMySettings(CancellationToken.None);

            var value = ((OkObjectResult)result).Value!;
            GetProperty<bool>(value, "GoogleServiceAccountFileAvailable").Should().BeTrue();
            GetProperty<bool>(value, "GeminiApiKeyAvailable").Should().BeTrue();
        }
        finally
        {
            File.Delete(serviceAccountPath);
        }
    }

    /// <summary>
    /// Get my settings reports credentials unavailable when nothing configured.
    /// </summary>
    [Fact]
    public async Task GetMySettings_ReportsCredentialsUnavailable_WhenNothingConfigured()
    {
        using var scope = new EnvironmentVariableScope();
        scope.Set(ServiceAccountEnvironmentVariable, null);
        scope.Set(GeminiApiKeyEnvironmentVariable, null);

        var controller = CreateController(new GoogleCredentialsOptions { ServiceAccountFilePath = "", GeminiApiKey = "" });

        var result = await controller.GetMySettings(CancellationToken.None);

        var value = ((OkObjectResult)result).Value!;
        GetProperty<bool>(value, "GoogleServiceAccountFileAvailable").Should().BeFalse();
        GetProperty<bool>(value, "GeminiApiKeyAvailable").Should().BeFalse();
    }

    private static SettingsController CreateController(GoogleCredentialsOptions options)
    {
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(s => s.GetUserAiEnabledAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        settingsService.Setup(s => s.GetUserGoogleVisionEnabledAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        settingsService.Setup(s => s.GetUserGeminiEnabledAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        settingsService.Setup(s => s.GetUserRequireAiConfirmationAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        settingsService.Setup(s => s.GetGlobalAiEnabledAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        settingsService.Setup(s => s.GetGlobalGoogleVisionEnabledAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        settingsService.Setup(s => s.GetGlobalGeminiEnabledAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        settingsService.Setup(s => s.GetGlobalMaxRequestsPerHourAsync(It.IsAny<CancellationToken>())).ReturnsAsync((int?)null);
        settingsService.Setup(s => s.GetGlobalMaxRequestsPerDayAsync(It.IsAny<CancellationToken>())).ReturnsAsync((int?)null);
        settingsService.Setup(s => s.GetGlobalDisableOnLimitReachedAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var monitor = new Mock<IOptionsMonitor<GoogleCredentialsOptions>>();
        monitor.Setup(m => m.CurrentValue).Returns(options);
        var credentialsProvider = new GoogleCredentialsProvider(monitor.Object);

        var claims = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "user-1") }, "TestAuth");

        return new SettingsController(settingsService.Object, credentialsProvider)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(claims)
                }
            }
        };
    }

    private static T GetProperty<T>(object value, string propertyName)
    {
        var property = value.GetType().GetProperty(propertyName)
            ?? throw new InvalidOperationException($"Property '{propertyName}' not found on response.");
        return (T)property.GetValue(value)!;
    }
}
