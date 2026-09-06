using Microsoft.AspNetCore.Components;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Rezepte.Web;

/// <summary>
/// Represents the api client class.
/// </summary>
public class ApiClient : IDisposable
{
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public HttpClient Http { get; }
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiClient"/> class.
    /// </summary>
    /// <param name="http">The http parameter.</param>
    /// <param name="nav">The nav parameter.</param>
    public ApiClient(HttpClient http, NavigationManager nav)
    {
        Http = http;
        if (Http.BaseAddress is null)
        {
            Http.BaseAddress = new Uri(nav.BaseUri);
        }
    }

    /// <summary>
    /// Gets the async.
    /// </summary>
    /// <param name="uri">The uri parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    public Task<HttpResponseMessage> GetAsync(string uri, CancellationToken ct = default) => Http.GetAsync(uri, ct);
    /// <summary>
    /// Gets the async.
    /// </summary>
    /// <typeparam name="T">The t type parameter.</typeparam>
    /// <param name="uri">The uri parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    public async Task<T> GetAsync<T>(string uri, CancellationToken ct = default)
    {
        var response = await Http.GetAsync(uri, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<T>(ct);
        return result ?? throw new InvalidOperationException($"The response body for '{uri}' deserialized to null.");
    }
    /// <summary>
    /// posts the as json async.
    /// </summary>
    /// <typeparam name="T">The t type parameter.</typeparam>
    /// <param name="uri">The uri parameter.</param>
    /// <param name="value">The value parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    public Task<HttpResponseMessage> PostAsJsonAsync<T>(string uri, T value, CancellationToken ct = default) => Http.PostAsJsonAsync(uri, value, ct);
    /// <summary>
    /// puts the as json async.
    /// </summary>
    /// <typeparam name="T">The t type parameter.</typeparam>
    /// <param name="uri">The uri parameter.</param>
    /// <param name="value">The value parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    public Task<HttpResponseMessage> PutAsJsonAsync<T>(string uri, T value, CancellationToken ct = default) => Http.PutAsJsonAsync(uri, value, ct);

    /// <summary>
    /// disposes the value.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            Http.Dispose();
        }
    }
}
