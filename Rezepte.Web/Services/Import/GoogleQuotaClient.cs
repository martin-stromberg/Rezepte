using Google.Apis.Auth.OAuth2;
using System.Net.Http.Headers;

namespace Rezepte.Web.Services.Import;

/// <summary>
/// Provides functionality to retrieve quota information for Google Cloud services using a service account for
/// authentication.
/// </summary>
/// <remarks>This client is designed to interact with the Google Service Usage API to fetch consumer quota metrics
/// for a specified service and project. It requires a valid service account JSON file for authentication and
/// authorization.</remarks>
public class GoogleQuotaClient
{
    private readonly string _serviceAccountJsonPath;
    private readonly HttpClient _httpClient;

    public GoogleQuotaClient(string serviceAccountJsonPath)
    {
        _serviceAccountJsonPath = serviceAccountJsonPath;
        _httpClient = new HttpClient();
    }

    public async Task<string> GetQuotaAsync(string serviceName, string projectId)
    {
        var credential = GoogleCredential
            .FromFile(_serviceAccountJsonPath)
            .CreateScoped("https://www.googleapis.com/auth/cloud-platform");

        var token = await credential.UnderlyingCredential.GetAccessTokenForRequestAsync();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        string url = $"https://serviceusage.googleapis.com/v1beta1/projects/{projectId}/services/{serviceName}/consumerQuotaMetrics";
        var response = await _httpClient.GetAsync(url);
        var result = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        return result;
    }
}
