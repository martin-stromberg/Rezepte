using System.Net.Http.Headers;
using System.Security.Claims;

public class ApiAuthHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly Rezepte.Web.Services.ITokenService _tokens;

    public ApiAuthHandler(IHttpContextAccessor httpContextAccessor, Rezepte.Web.Services.ITokenService tokens)
    {
        _httpContextAccessor = httpContextAccessor;
        _tokens = tokens;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var user = httpContext?.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var username = user.Identity?.Name ?? user.FindFirstValue(ClaimTypes.Name) ?? userId;
            if (!string.IsNullOrEmpty(userId))
            {
                var token = _tokens.GetToken(userId) ?? _tokens.CreateToken(userId!, username ?? userId!);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }
        return await base.SendAsync(request, cancellationToken);
    }
}