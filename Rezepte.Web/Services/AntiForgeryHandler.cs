using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;

namespace Rezepte.Web.Services;

/// <summary>
/// Represents the anti forgery handler class.
/// </summary>
public class AntiForgeryHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="AntiForgeryHandler"/> class.
    /// </summary>
    /// <param name="httpContextAccessor">The http context accessor parameter.</param>
    public AntiForgeryHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Sends the async.
    /// </summary>
    /// <param name="request">The request parameter.</param>
    /// <param name="cancellationToken">The cancellation token parameter.</param>
    /// <returns>The result.</returns>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Nur für mutierende Methoden (nicht GET/HEAD)
        if (request.Method != HttpMethod.Get && request.Method != HttpMethod.Head)
        {
            var ctx = _httpContextAccessor.HttpContext;
            var cookies = ctx?.Request?.Cookies;
            if (cookies is not null)
            {
                // .NET 8/9 Standard: Cookie "RequestVerificationToken"
                if (cookies.TryGetValue("RequestVerificationToken", out var token) && !string.IsNullOrEmpty(token))
                {
                    request.Headers.TryAddWithoutValidation("RequestVerificationToken", token);
                }
                else
                {
                    // Fallback auf legacy .AspNetCore.Antiforgery.*
                    var legacy = cookies.Keys.FirstOrDefault(k => k.StartsWith(".AspNetCore.Antiforgery", StringComparison.Ordinal));
                    if (legacy is not null && cookies.TryGetValue(legacy, out var legacyToken) && !string.IsNullOrEmpty(legacyToken))
                    {
                        request.Headers.TryAddWithoutValidation("RequestVerificationToken", legacyToken);
                    }
                }
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
