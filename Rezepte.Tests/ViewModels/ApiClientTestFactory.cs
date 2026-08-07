using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Rezepte.Web;

namespace Rezepte.Tests.ViewModels;

/// <summary>
/// Builds an <see cref="ApiClient"/> whose responses are provided by the test instead of a running server.
/// </summary>
internal sealed class ApiClientTestFactory
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

    public ApiClientTestFactory(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
    {
        _responseFactory = responseFactory;
    }

    public List<HttpRequestMessage> Requests { get; } = [];

    public ApiClient Create()
    {
        var httpClient = new HttpClient(new DelegateHandler(this))
        {
            BaseAddress = new Uri("http://localhost/")
        };
        return new ApiClient(httpClient, new TestNavigationManager());
    }

    public static HttpResponseMessage Json(HttpStatusCode status, string json)
        => new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    public static HttpResponseMessage Error(HttpStatusCode status, string? message = null)
        => message is null
            ? new HttpResponseMessage(status)
            : Json(status, $"{{\"message\":\"{message}\"}}");

    private sealed class DelegateHandler(ApiClientTestFactory owner) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            owner.Requests.Add(request);
            return Task.FromResult(owner._responseFactory(request));
        }
    }

    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager() => Initialize("http://localhost/", "http://localhost/");
    }
}
