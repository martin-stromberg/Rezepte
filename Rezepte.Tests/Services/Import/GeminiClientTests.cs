using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Rezepte.Tests.TestHelpers;
using Rezepte.Web.Configuration;
using Rezepte.Web.Services;
using Rezepte.Web.Services.Import;
using Xunit;

namespace Rezepte.Tests.Services.Import;

/// <summary>
/// Class representing the gemini client tests.
/// </summary>
[Collection(GoogleCredentialsEnvironmentCollection.Name)]
public class GeminiClientTests
{
    /// <summary>
    /// Extract recipe from url async uses api key before service account.
    /// </summary>
    [Fact]
    public async Task ExtractRecipeFromUrlAsync_UsesApiKeyBeforeServiceAccount()
    {
        using var scope = new EnvironmentVariableScope();
        var serviceAccountPath = Path.GetTempFileName();
        var handler = new CapturingHttpMessageHandler();
        try
        {
            scope.Set(EnvironmentVariableScope.GeminiApiKeyEnvironmentVariable, "test-api-key");
            scope.Set(EnvironmentVariableScope.ServiceAccountEnvironmentVariable, serviceAccountPath);
            var client = CreateClient(handler, new GoogleCredentialsOptions());

            await client.ExtractRecipeFromUrlAsync("<html><body>Recipe</body></html>");

            handler.Request.Should().NotBeNull();
            handler.Request!.Headers.TryGetValues("x-goog-api-key", out var values).Should().BeTrue();
            values.Should().ContainSingle().Which.Should().Be("test-api-key");
            handler.Request.Headers.Authorization.Should().BeNull();
        }
        finally
        {
            File.Delete(serviceAccountPath);
        }
    }

    /// <summary>
    /// Extract recipe from url async throws secret free message when authentication is missing.
    /// </summary>
    [Fact]
    public async Task ExtractRecipeFromUrlAsync_ThrowsSecretFreeMessage_WhenAuthenticationIsMissing()
    {
        using var scope = new EnvironmentVariableScope();
        scope.Set(EnvironmentVariableScope.GeminiApiKeyEnvironmentVariable, null);
        scope.Set(EnvironmentVariableScope.ServiceAccountEnvironmentVariable, null);
        var client = CreateClient(new CapturingHttpMessageHandler(), new GoogleCredentialsOptions());

        var act = () => client.ExtractRecipeFromUrlAsync("<html><body>Recipe</body></html>");

        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.Which.Message.Should().Be("No Gemini API key configured and no service account path configured.");
        ex.Which.Message.Should().NotContain("GOOGLE_GEMINI_API_KEY");
        ex.Which.Message.Should().NotContain("GoogleCredentials:GeminiApiKey");
    }

    /// <summary>
    /// Extract recipe from url async throws path context when service account file is invalid.
    /// </summary>
    [Fact]
    public async Task ExtractRecipeFromUrlAsync_ThrowsPathContext_WhenServiceAccountFileIsInvalid()
    {
        using var scope = new EnvironmentVariableScope();
        var serviceAccountPath = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(serviceAccountPath, "not-json");
            scope.Set(EnvironmentVariableScope.GeminiApiKeyEnvironmentVariable, null);
            scope.Set(EnvironmentVariableScope.ServiceAccountEnvironmentVariable, serviceAccountPath);
            var client = CreateClient(new CapturingHttpMessageHandler(), new GoogleCredentialsOptions());

            var act = () => client.ExtractRecipeFromUrlAsync("<html><body>Recipe</body></html>");

            var ex = await act.Should().ThrowAsync<InvalidOperationException>();
            ex.Which.Message.Should().Be($"Failed to load Gemini service account file at '{serviceAccountPath}'.");
            ex.Which.InnerException.Should().NotBeNull();
        }
        finally
        {
            File.Delete(serviceAccountPath);
        }
    }

    private static GeminiClient CreateClient(HttpMessageHandler handler, GoogleCredentialsOptions options)
    {
        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient(handler));

        var optionsMonitor = new Mock<IOptionsMonitor<GoogleCredentialsOptions>>();
        optionsMonitor.Setup(m => m.CurrentValue).Returns(options);

        return new GeminiClient(
            httpClientFactory.Object,
            new GoogleCredentialsProvider(optionsMonitor.Object),
            NullLogger<GeminiClient>.Instance);
    }

    private sealed class CapturingHttpMessageHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    {
                      "candidates": [
                        {
                          "content": {
                            "parts": [
                              {
                                "text": "**Titel des Rezepts:** Test\n**Zutatenliste:**\nZutat\n**Zubereitungsschritte:**\nKochen"
                              }
                            ]
                          }
                        }
                      ]
                    }
                    """)
            });
        }
    }
}
