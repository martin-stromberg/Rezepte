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

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleQuotaClient"/> class.
    /// </summary>
    /// <param name="serviceAccountJsonPath">The service account json path parameter.</param>
    public GoogleQuotaClient(string serviceAccountJsonPath)
    {
        _serviceAccountJsonPath = serviceAccountJsonPath;
        _httpClient = new HttpClient();
    }

    /// <summary>
    /// Gets the quota async.
    /// </summary>
    /// <param name="serviceName">The service name parameter.</param>
    /// <param name="projectId">The project id parameter.</param>
    /// <returns>The result.</returns>
    public async Task<string> GetQuotaAsync(string serviceName, string projectId)
    {
        // GoogleCredential.FromFile(string) is obsolete (potential security risk loading an
        // unvalidated credential configuration); CredentialFactory.FromFile<T> loads a
        // specifically-typed credential instead, which is then converted back to a
        // GoogleCredential for CreateScoped.
        var credential = CredentialFactory
            .FromFile<ServiceAccountCredential>(_serviceAccountJsonPath)
            .ToGoogleCredential()
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
