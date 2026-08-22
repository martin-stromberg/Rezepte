using Rezepte.Web.Components;
using Rezepte.Web.Services;
using Rezepte.Web.Middleware;
using Rezepte.Web.Data;
using Rezepte.Web.ViewModels;
using Rezepte.Web;
using Rezepte.Web.Configuration;
using Rezepte.Web.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using msTools.Updater;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure logging (Serilog via extension)
builder.ConfigureSerilog();

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

// Register all application services via project extension (DbContext, auth, DI, controllers, etc.)
builder.Services.AddRezepteServices(builder.Configuration, builder.Environment);

var app = builder.Build();

Log.Information("Application starting. Version: {Version}", ApplicationVersion.Current);

// Apply migrations / ensure database (extension handles logging/errors)
await app.ApplyDatabaseMigrationsAsync();

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

public partial class Program;
