using Microsoft.AspNetCore.Http;
using Rezepte.Web.Services;

namespace Rezepte.Web.Middleware;

public class RedirectToRegisterMiddleware
{
    private static readonly string[] StaticExtensions = [".css", ".js", ".map", ".png", ".jpg", ".jpeg", ".gif", ".svg", ".ico", ".woff2", ".woff", ".ttf", ".eot", ".webmanifest"];

    private readonly RequestDelegate _next;

    public RedirectToRegisterMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Ausnahmen: API, Blazor-Hub, statische Inhalte und Auth-Seite
        if (IsExcluded(path))
        {
            await _next(context);
            return;
        }

        var userService = context.RequestServices.GetRequiredService<IUserService>();
        var hasAnyUsers = await userService.HasAnyUsersAsync(context.RequestAborted);
        if (await RedirectToRegistration(context, path, hasAnyUsers))
            return;
        if (await RedirectToLogin(context, path, hasAnyUsers))
            return;
        await _next(context);
    }

    private static Task<bool> RedirectToRegistration(HttpContext context, string path, bool hasAnyUsers)
    {
        if (!hasAnyUsers)
        {
            if (!path.Equals("/register", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.Redirect("/register");
                return Task.FromResult(true);
            }
        }
        return Task.FromResult(false);
    }

    private static Task<bool> RedirectToLogin(HttpContext context, string path, bool hasAnyUsers)
    {
        // Wenn keine Benutzer vorhanden sind, nie zur Login-Seite umleiten (Registrierung soll möglich sein)
        if (!hasAnyUsers)
            return Task.FromResult(false);

        var isAuthenticated = context.User?.Identity?.IsAuthenticated == true;
        if (!isAuthenticated && !path.Equals("/login", StringComparison.OrdinalIgnoreCase))
        {
            // Direkter Zugriff auf /register ist nicht erlaubt, wenn Benutzer existieren
            context.Response.Redirect("/login");
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    private static bool IsExcluded(string path)
    {
        if (string.IsNullOrEmpty(path)) return true;

        // Allow only login as auth route (Register wird über Middleware gesteuert)
        if (path.Equals("/login", StringComparison.OrdinalIgnoreCase))
            return true;

        // Allow access to security.txt endpoints without authentication
        if (path.Equals("/security.txt", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/.well-known/security.txt", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/.well-known/security.md", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/.well-known/security.html", StringComparison.OrdinalIgnoreCase))
            return true;

        // Exclude APIs and framework/static assets
        if (path.StartsWith("/api", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/_blazor", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/_framework", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/_content", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/_vs", StringComparison.OrdinalIgnoreCase))
            return true;

        // Exclude typical static file extensions regardless of path (hashed filenames etc.)
        var hasStaticExt = StaticExtensions.Any(ext => path.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
        if (hasStaticExt) return true;

        return false;
    }
}

public static class RedirectToRegisterMiddlewareExtensions
{
    public static IApplicationBuilder UseRedirectToRegisterWhenNoUsers(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RedirectToRegisterMiddleware>();
    }
}
