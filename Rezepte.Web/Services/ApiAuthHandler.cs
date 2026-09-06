using System.Net.Http.Headers;
using System.Security.Claims;

namespace Rezepte.Web.Services;

/// <summary>
/// Represents the api auth handler class.
/// </summary>
public class ApiAuthHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITokenService _tokenService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiAuthHandler"/> class.
    /// </summary>
    /// <param name="httpContextAccessor">The http context accessor parameter.</param>
    /// <param name="tokenService">The token service parameter.</param>
    public ApiAuthHandler(IHttpContextAccessor httpContextAccessor, ITokenService tokenService)
    {
        _httpContextAccessor = httpContextAccessor;
        _tokenService = tokenService;
    }

    /// <summary>
    /// Sends the async.
    /// </summary>
    /// <param name="request">The request parameter.</param>
    /// <param name="cancellationToken">The cancellation token parameter.</param>
    /// <returns>The result.</returns>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        var userId = user?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userId))
        {
            var token = _tokenService.GetToken(userId);
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }
        return base.SendAsync(request, cancellationToken);
    }
}
