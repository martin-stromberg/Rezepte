using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;

namespace Rezepte.Web;

public class ApiClient
{
    public HttpClient Http { get; }

    public ApiClient(HttpClient http, NavigationManager nav)
    {
        Http = http;
        if (Http.BaseAddress is null)
        {
            Http.BaseAddress = new Uri(nav.BaseUri);
        }
    }

    // Optional: bequeme Wrapper
    public Task<HttpResponseMessage> GetAsync(string uri, CancellationToken ct = default) => Http.GetAsync(uri, ct);
    public Task<HttpResponseMessage> PostAsJsonAsync<T>(string uri, T value, CancellationToken ct = default) => Http.PostAsJsonAsync(uri, value, ct);
    public Task<HttpResponseMessage> PutAsJsonAsync<T>(string uri, T value, CancellationToken ct = default) => Http.PutAsJsonAsync(uri, value, ct);
}