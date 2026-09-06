using Microsoft.Extensions.Options;
using Rezepte.Web.Configuration;

namespace Rezepte.Web.Services;

/// <summary>
/// Represents the google credentials provider class.
/// </summary>
public sealed class GoogleCredentialsProvider : IGoogleCredentialsProvider
{
    private const string ServiceAccountEnvironmentVariable = "GOOGLE_APPLICATION_CREDENTIALS";
    private const string GeminiApiKeyEnvironmentVariable = "GOOGLE_GEMINI_API_KEY";

    private readonly IOptionsMonitor<GoogleCredentialsOptions> _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleCredentialsProvider"/> class.
    /// </summary>
    /// <param name="options">The options parameter.</param>
    public GoogleCredentialsProvider(IOptionsMonitor<GoogleCredentialsOptions> options)
    {
        _options = options;
    }

    /// <summary>
    /// Gets the service account file path.
    /// </summary>
    /// <returns>The result.</returns>
    public string GetServiceAccountFilePath()
    {
        return ResolveValue(ServiceAccountEnvironmentVariable, _options.CurrentValue.ServiceAccountFilePath);
    }

    /// <summary>
    /// services the account file exists.
    /// </summary>
    /// <returns>The result.</returns>
    public bool ServiceAccountFileExists()
    {
        var path = GetServiceAccountFilePath();
        if (string.IsNullOrWhiteSpace(path))
            return false;
        return File.Exists(path);
    }

    /// <summary>
    /// Gets the gemini api key.
    /// </summary>
    /// <returns>The result.</returns>
    public string GetGeminiApiKey()
    {
        return ResolveValue(GeminiApiKeyEnvironmentVariable, _options.CurrentValue.GeminiApiKey);
    }

    /// <summary>
    /// Gets the diagnostics.
    /// </summary>
    /// <returns>The result.</returns>
    public GoogleCredentialsDiagnostics GetDiagnostics()
    {
        var options = _options.CurrentValue;
        var serviceAccountEnvironmentValue = Environment.GetEnvironmentVariable(ServiceAccountEnvironmentVariable);
        var geminiApiKeyEnvironmentValue = Environment.GetEnvironmentVariable(GeminiApiKeyEnvironmentVariable);

        var serviceAccountEnvironmentVariableSet = !string.IsNullOrWhiteSpace(serviceAccountEnvironmentValue);
        var serviceAccountOptionsFallbackSet = !serviceAccountEnvironmentVariableSet && !string.IsNullOrWhiteSpace(options.ServiceAccountFilePath);
        var serviceAccountFilePath = serviceAccountEnvironmentVariableSet
            ? serviceAccountEnvironmentValue!
            : serviceAccountOptionsFallbackSet
                ? options.ServiceAccountFilePath!
                : string.Empty;

        var geminiApiKeyEnvironmentVariableSet = !string.IsNullOrWhiteSpace(geminiApiKeyEnvironmentValue);
        var geminiApiKeyOptionsFallbackSet = !geminiApiKeyEnvironmentVariableSet && !string.IsNullOrWhiteSpace(options.GeminiApiKey);

        return new GoogleCredentialsDiagnostics(
            serviceAccountEnvironmentVariableSet,
            serviceAccountOptionsFallbackSet,
            serviceAccountFilePath,
            !string.IsNullOrWhiteSpace(serviceAccountFilePath) && File.Exists(serviceAccountFilePath),
            geminiApiKeyEnvironmentVariableSet,
            geminiApiKeyOptionsFallbackSet);
    }

    private static string ResolveValue(string environmentVariableName, string? configuredValue)
    {
        var environmentValue = Environment.GetEnvironmentVariable(environmentVariableName);
        if (!string.IsNullOrWhiteSpace(environmentValue))
            return environmentValue;

        if (!string.IsNullOrWhiteSpace(configuredValue))
            return configuredValue;

        return string.Empty;
    }
}
