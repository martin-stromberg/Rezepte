using System.Net;
using System.Net.Http.Json;
using Rezepte.Web;

namespace Rezepte.Tests.Components;

/// <summary>
/// Configurable <see cref="HttpMessageHandler"/> stub that returns a prepared JSON response
/// or throws a prepared exception for the requests of an <see cref="ApiClient"/> <see cref="HttpClient"/>.
/// </summary>
public sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _responder;

    /// <summary>
    /// Initializes a new instance of the <see cref="StubHttpMessageHandler"/> class.
    /// </summary>
    /// <param name="responder">The responder invoked for each request.</param>
    public StubHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder)
    {
        _responder = responder;
    }

    /// <summary>
    /// Gets the last request the handler has seen.
    /// </summary>
    public HttpRequestMessage? LastRequest { get; private set; }

    /// <summary>
    /// Creates a handler that responds with the supplied payload serialized as JSON.
    /// </summary>
    /// <typeparam name="T">The payload type.</typeparam>
    /// <param name="payload">The payload to serialize into the response body.</param>
    /// <param name="statusCode">The HTTP status code of the response.</param>
    /// <returns>The configured handler.</returns>
    public static StubHttpMessageHandler WithJson<T>(T payload, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new StubHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(statusCode)
        {
            Content = JsonContent.Create(payload)
        }));
    }

    /// <summary>
    /// Creates a handler that throws the supplied exception for every request.
    /// </summary>
    /// <param name="exception">The exception to throw.</param>
    /// <returns>The configured handler.</returns>
    public static StubHttpMessageHandler Throwing(Exception exception)
    {
        return new StubHttpMessageHandler((_, _) => Task.FromException<HttpResponseMessage>(exception));
    }

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        return _responder(request, cancellationToken);
    }
}
