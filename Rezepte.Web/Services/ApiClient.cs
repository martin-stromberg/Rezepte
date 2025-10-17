using Microsoft.AspNetCore.Components;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

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
    public async Task<T> GetAsync<T>(string uri, CancellationToken ct = default) {
        var response = await Http.GetAsync(uri, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(ct);
    }
    public Task<HttpResponseMessage> PostAsJsonAsync<T>(string uri, T value, CancellationToken ct = default) => Http.PostAsJsonAsync(uri, value, ct);
    public Task<HttpResponseMessage> PutAsJsonAsync<T>(string uri, T value, CancellationToken ct = default) => Http.PutAsJsonAsync(uri, value, ct);
}