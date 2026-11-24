using Rezepte.Web.Components;
using Rezepte.Web.Services;
using Rezepte.Web.Middleware;
using Rezepte.Web.Data;
using Rezepte.Web.ViewModels;
using Rezepte.Web;
using Rezepte.Web.Configuration;
using Rezepte.Web.Extensions;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using msTools.Updater;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure logging (Serilog via extension)
builder.ConfigureSerilog();

// Central QuestPDF setup (license, deterministic bundled font chain, deployed emoji fallback)
var questPdfFonts = QuestPdfConfiguration.Configure();
if (!questPdfFonts.EmojiFallbackRegistered)
{
    // A missing Fonts deployment (e.g. a packaging error) would otherwise only surface
    // as failed or tofu-rendered PDF exports; log the root cause at startup.
    Log.Warning(
        questPdfFonts.FontsDirectoryExists
            ? "QuestPDF fonts directory {FontsDirectory} exists, but the emoji fallback font '{EmojiFallbackFontFamily}' is not registered; emoji in PDF exports may render as placeholder glyphs."
            : "QuestPDF fonts directory {FontsDirectory} is missing; the emoji fallback font '{EmojiFallbackFontFamily}' was not registered and PDF exports may fail or render emoji as placeholder glyphs.",
        questPdfFonts.FontsDirectory,
        QuestPdfConfiguration.EmojiFallbackFontFamily);
}

builder.UseAutoUpdate(autoUpdate =>
{
    autoUpdate.BindConfiguration("ApplicationUpdates");

    var updateOptions = builder.Configuration
        .GetSection("ApplicationUpdates")
        .Get<ApplicationUpdateOptions>() ?? new ApplicationUpdateOptions();

    if (updateOptions.AllowPrereleaseUpdates)
    {
        autoUpdate.EnablePrereleaseUpdates();
    }

    if (!string.IsNullOrWhiteSpace(updateOptions.RepositoryOwner) &&
        !string.IsNullOrWhiteSpace(updateOptions.RepositoryName))
    {
        autoUpdate.UseGithubSource(
            updateOptions.RepositoryOwner,
            updateOptions.RepositoryName,
            updateOptions.ManifestAssetName);
    }
    else if (!string.IsNullOrWhiteSpace(updateOptions.LocalSourceDirectory))
    {
        autoUpdate.UseLocalFolderSource(updateOptions.LocalSourceDirectory);
    }

    if (!string.IsNullOrWhiteSpace(updateOptions.AppPoolName))
    {
        autoUpdate.WithIisApplicationPool(updateOptions.AppPoolName, updateOptions.SiteName ?? string.Empty);
    }

    if (!string.IsNullOrWhiteSpace(updateOptions.UpdateUnitName))
    {
        autoUpdate.WithUpdateUnitName(updateOptions.UpdateUnitName);
    }
});

// Localization (IStringLocalizer for shared UI strings)
builder.Services.AddLocalization();

// Register all application services via project extension (DbContext, auth, DI, controllers, etc.)
builder.Services.AddRezepteServices(builder.Configuration, builder.Environment);

var app = builder.Build();

Log.Information("Application starting. Version: {Version}", ApplicationVersion.Current);

// Apply migrations / ensure database (extension handles logging/errors)
await app.ApplyDatabaseMigrationsAsync();

// Optional PathBase support for deployments that put this application behind a reverse
// proxy under a URL sub-path (e.g. https://example.com/rezepte/) instead of its own
// domain/subdomain root. Configured via the "PathBase" configuration key; defaults to
// unset, which is a no-op and keeps the existing domain-root deployment unchanged.
// Registered first so that routing/endpoint matching further down the pipeline already
// sees the stripped path.
var pathBase = app.Configuration["PathBase"];
if (!string.IsNullOrWhiteSpace(pathBase))
{
    app.UsePathBase(pathBase);
}

// Reads X-Forwarded-Proto/-For from the reverse proxy so Request.Scheme/RemoteIp reflect
// the real client instead of the proxy's own loopback connection. Trust is limited to
// loopback: the reverse proxy runs on the same host, and this application's port is also
// reachable directly (not only through the proxy), so trusting more than loopback here
// would let anyone with direct network access to this port spoof these headers.
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor
};
forwardedHeadersOptions.KnownProxies.Add(System.Net.IPAddress.Loopback);
forwardedHeadersOptions.KnownProxies.Add(System.Net.IPAddress.IPv6Loopback);
app.UseForwardedHeaders(forwardedHeadersOptions);

// Error handling
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

// Request logging
app.UseRequestLogging();

// Static files (wwwroot)
app.UseStaticFiles();

// Authentication/Authorization
app.UseAuthentication();
app.UseAuthorization();

// Rate limiting (policies are applied per endpoint)
app.UseRateLimiter();

// Antiforgery only for non-API routes
app.UseWhen(ctx => !ctx.Request.Path.StartsWithSegments("/api"), branch =>
{
    branch.UseAntiforgery();
});

// Map static assets (project extension may map embedded/static resources)
app.MapStaticAssets();

// Blazor Razor components (interactive server render mode)
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Map API controllers
app.MapControllers();

// Redirect middleware to /register or /login depending on user state
app.UseRedirectToRegisterWhenNoUsers();

// Run
app.Run();

/// <summary>
/// Application entry point class (top-level statements generate the implicit Program implementation).
/// </summary>
public partial class Program;
