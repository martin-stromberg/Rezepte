using Google.Apis.Auth.OAuth2;
using System.Text.Json;

namespace Rezepte.Web.Services;

public sealed class GoogleCredentialsProvider : IGoogleCredentialsProvider
{
    private const string ServiceAccountFileName = "google.application-credentials.json";
    private const string GeminiApiKeyFileName = "google.gemini.api-key.json";
    private const string accountfile_type_service_account = "service_account";
    private const string apikeyfile_type_api_key = "api_key";
    private struct AccountFile
    {
        public string project_id { get; set; }
        public string private_key_id { get; set; }
        public string private_key { get; set; }
        public string client_email { get; set; }
        public string client_id { get; set; }
        public string auth_uri { get; set; }
        public string token_uri { get; set; }
        public string auth_provider_x509_cert_url { get; set; }
        public string client_x509_cert_url { get; set; }
        public string universe_domain { get; set; }
    }
    private struct ApiKeyFile
    {
        public string type { get; set; }
        public string api_key { get; set; }
    }

    public string GetServiceAccountFilePath()
    {
        var jsonPath = Path.Combine(AppContext.BaseDirectory ?? Environment.CurrentDirectory, ServiceAccountFileName);

        if (File.Exists(jsonPath))
        {
            // Nur setzen, wenn vorhanden
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", jsonPath);
        }

        return jsonPath;
    }

    public bool ServiceAccountFileExists()
    {
        var path = GetServiceAccountFilePath();
        return File.Exists(path);
    }

    public string GetGeminiApiKey()
    {
        var apiKeyPath = Path.Combine(AppContext.BaseDirectory ?? Environment.CurrentDirectory, GeminiApiKeyFileName);
        if (!File.Exists(apiKeyPath))
            return string.Empty;
        var fileContent = File.ReadAllText(apiKeyPath).Trim();
        var accountFile = JsonSerializer.Deserialize<ApiKeyFile>(System.IO.File.ReadAllText(apiKeyPath));
        if (accountFile.type == apikeyfile_type_api_key)
            return accountFile.api_key;
        else
            return string.Empty;
    }
}