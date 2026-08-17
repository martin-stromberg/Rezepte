using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;

namespace Rezepte.Web.Services;

public class CircuitAuthHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly ITokenService _tokenService;

    public CircuitAuthHandler(IHttpContextAccessor httpContextAccessor, AuthenticationStateProvider authenticationStateProvider, ITokenService tokenService)
    {
        _httpContextAccessor = httpContextAccessor;
        _authenticationStateProvider = authenticationStateProvider;
        _tokenService = tokenService;
    }

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
