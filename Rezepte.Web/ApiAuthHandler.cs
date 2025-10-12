using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

public class ApiAuthHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly Rezepte.Web.Services.ITokenService _tokens;

    public ApiAuthHandler(IHttpContextAccessor httpContextAccessor, Rezepte.Web.Services.ITokenService tokens)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var user = httpContext?.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                var username = user.Identity?.Name ?? user.FindFirstValue(ClaimTypes.Name) ?? userId;
                var token = _tokens.GetToken(userId);
                if (string.IsNullOrEmpty(token))
                {
                    var isAdmin = user.IsInRole("Admin");
                    token = _tokens.CreateToken(userId, username, isAdmin);
                }
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}