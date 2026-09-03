using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Moq;
using Rezepte.Web.Services;
using Xunit;

namespace Rezepte.Tests.Services;

public class CircuitAuthHandlerTests
{
    private static ClaimsPrincipal AuthenticatedUser(string userId, string username, bool isAdmin = false)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, username)
        };
        if (isAdmin)
        {
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));
        }
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    private static CircuitAuthHandler CreateHandler(
        IHttpContextAccessor? httpContextAccessor = null,
        AuthenticationStateProvider? authProvider = null,
        ITokenService? tokenService = null,
        HttpMessageHandler? inner = null)
    {
        var handler = new CircuitAuthHandler(
            httpContextAccessor ?? new HttpContextAccessor(),
            authProvider ?? new FakeAuthStateProvider(new AuthenticationState(new ClaimsPrincipal())),
            tokenService ?? Mock.Of<ITokenService>());
        handler.InnerHandler = inner ?? new TestHandler();
        return handler;
    }

    [Fact]
    public async Task Sets_authorization_header_from_http_context()
    {
        var user = AuthenticatedUser("u1", "Max");
        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        var tokenService = new Mock<ITokenService>();
        tokenService.Setup(s => s.GetToken("u1")).Returns("cached-token");

        var handler = CreateHandler(httpContextAccessor, tokenService: tokenService.Object);
        var client = new HttpClient(handler);

        var response = await client.GetAsync("http://localhost/api/users/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var captured = ((TestHandler)handler.InnerHandler!).LastRequest;
        captured!.Headers.Authorization.Should().Be(new AuthenticationHeaderValue("Bearer", "cached-token"));
    }

    [Fact]
    public async Task Falls_back_to_authentication_state_provider_when_http_context_is_null()
    {
        var user = AuthenticatedUser("u2", "Maria");
        var authProvider = new FakeAuthStateProvider(new AuthenticationState(user));

        var tokenService = new Mock<ITokenService>();
        tokenService.Setup(s => s.GetToken("u2")).Returns<string?>(_ => null);
        tokenService.Setup(s => s.CreateToken("u2", "Maria", false)).Returns("new-token");

        var handler = CreateHandler(authProvider: authProvider, tokenService: tokenService.Object);
        var client = new HttpClient(handler);

        var response = await client.GetAsync("http://localhost/api/users/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var captured = ((TestHandler)handler.InnerHandler!).LastRequest;
        captured!.Headers.Authorization.Should().Be(new AuthenticationHeaderValue("Bearer", "new-token"));
    }

    [Fact]
    public async Task Does_not_set_authorization_for_anonymous_user()
    {
        var handler = CreateHandler();
        var client = new HttpClient(handler);

        var response = await client.GetAsync("http://localhost/api/users/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var captured = ((TestHandler)handler.InnerHandler!).LastRequest;
        captured!.Headers.Authorization.Should().BeNull();
    }

    [Fact]
    public async Task Caches_new_token_for_authenticated_user()
    {
        var user = AuthenticatedUser("u3", "Anna");
        var authProvider = new FakeAuthStateProvider(new AuthenticationState(user));

        var tokenService = new Mock<ITokenService>();
        tokenService.Setup(s => s.GetToken("u3")).Returns<string?>(_ => null);
        tokenService.Setup(s => s.CreateToken("u3", "Anna", false)).Returns("created");

        var handler = CreateHandler(authProvider: authProvider, tokenService: tokenService.Object);
        var client = new HttpClient(handler);

        await client.GetAsync("http://localhost/api/users/me");

        tokenService.Verify(s => s.CreateToken("u3", "Anna", false), Times.Once);
    }

    private class FakeAuthStateProvider : AuthenticationStateProvider
    {
        private readonly AuthenticationState _state;

        public FakeAuthStateProvider(AuthenticationState state)
        {
            _state = state;
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(_state);
    }

    private class TestHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
