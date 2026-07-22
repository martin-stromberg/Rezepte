using Microsoft.Extensions.Options;
using Rezepte.Web.Configuration;

namespace Rezepte.Web.Services;

public sealed class GoogleCredentialsProvider : IGoogleCredentialsProvider
{
    private const string ServiceAccountEnvironmentVariable = "GOOGLE_APPLICATION_CREDENTIALS";
    private const string GeminiApiKeyEnvironmentVariable = "GOOGLE_GEMINI_API_KEY";

    private readonly IOptionsMonitor<GoogleCredentialsOptions> _options;

    public GoogleCredentialsProvider(IOptionsMonitor<GoogleCredentialsOptions> options)
    {
        _options = options;
    }

    public string GetServiceAccountFilePath()
    {
        return ResolveValue(ServiceAccountEnvironmentVariable, _options.CurrentValue.ServiceAccountFilePath);
    }

    public bool ServiceAccountFileExists()
    {
        var path = GetServiceAccountFilePath();
        if (string.IsNullOrWhiteSpace(path))
            return false;
        return File.Exists(path);
    }

    public string GetGeminiApiKey()
    {
        return ResolveValue(GeminiApiKeyEnvironmentVariable, _options.CurrentValue.GeminiApiKey);
    }

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
