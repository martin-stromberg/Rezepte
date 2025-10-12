using Rezepte.Web.Components;
using Rezepte.Web.Services;
using Rezepte.Web.Middleware;
using Rezepte.Web.Data;
using Microsoft.EntityFrameworkCore;
using Rezepte.Web.ViewModels;
using Rezepte.Web;
using Rezepte.Web.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Configure logging (console errors + rolling file logs)
builder.ConfigureSerilog();

// Register all app services
builder.Services.AddRezepteServices(builder.Configuration, builder.Environment);

var app = builder.Build();

// Apply migrations / ensure database
await app.ApplyDatabaseMigrationsAsync();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseAuthentication();
app.UseAuthorization();
// Antiforgery nur für Nicht-API-Routen aktivieren
app.UseWhen(ctx => !ctx.Request.Path.StartsWithSegments("/api"), branch =>
{
    branch.UseAntiforgery();
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Map API controllers
app.MapControllers();

// Redirect to register/login depending on state
app.UseRedirectToRegisterWhenNoUsers();

app.Run();
