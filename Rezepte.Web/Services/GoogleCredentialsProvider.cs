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
        var environmentPath = Environment.GetEnvironmentVariable(ServiceAccountEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(environmentPath))
            return environmentPath;

        var configuredPath = _options.CurrentValue.ServiceAccountFilePath;
        if (!string.IsNullOrWhiteSpace(configuredPath))
            return configuredPath;

        return string.Empty;
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
        var environmentKey = Environment.GetEnvironmentVariable(GeminiApiKeyEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(environmentKey))
            return environmentKey;

        var configuredKey = _options.CurrentValue.GeminiApiKey;
        if (!string.IsNullOrWhiteSpace(configuredKey))
            return configuredKey;

        return string.Empty;
    }
}
