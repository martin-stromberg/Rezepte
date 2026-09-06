using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;

namespace Rezepte.Web.Services;

/// <summary>
/// Represents the circuit auth handler class.
/// </summary>
public class CircuitAuthHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly ITokenService _tokenService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CircuitAuthHandler"/> class.
    /// </summary>
    /// <param name="httpContextAccessor">The http context accessor parameter.</param>
    /// <param name="authenticationStateProvider">The authentication state provider parameter.</param>
    /// <param name="tokenService">The token service parameter.</param>
    public CircuitAuthHandler(IHttpContextAccessor httpContextAccessor, AuthenticationStateProvider authenticationStateProvider, ITokenService tokenService)
    {
        _httpContextAccessor = httpContextAccessor;
        _authenticationStateProvider = authenticationStateProvider;
        _tokenService = tokenService;
    }

    /// <summary>
    /// Sends the async.
    /// </summary>
    /// <param name="request">The request parameter.</param>
    /// <param name="cancellationToken">The cancellation token parameter.</param>
    /// <returns>The result.</returns>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
            user = authState.User;
        }

        if (user?.Identity?.IsAuthenticated == true)
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                var username = user.Identity?.Name ?? user.FindFirstValue(ClaimTypes.Name) ?? userId;
                var isAdmin = user.IsInRole("Admin");

                var token = _tokenService.GetToken(userId);
                if (string.IsNullOrEmpty(token))
                {
                    token = _tokenService.CreateToken(userId, username, isAdmin);
                }

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
