using Rezepte.Web.Components;
using Rezepte.Web.Services;
using Rezepte.Web.Middleware;
using Rezepte.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Rezepte.Web.ViewModels;
using System.Net;
using System.Security.Cryptography;
using Rezepte.Web;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options =>
    {
        if (builder.Environment.IsDevelopment())
        {
            options.DetailedErrors = true; // bessere Fehlermeldungen bei Blazor-Circuit-Fehlern
        }
    });

// Add API controllers (Antiforgery für APIs global ignorieren)
builder.Services.AddControllers(options =>
{
    options.Filters.Add(new IgnoreAntiforgeryTokenAttribute());
});

// EF Core Sqlite
var connectionString = builder.Configuration.GetConnectionString("Default") ?? "Data Source=rezepte.db";
builder.Services.AddDbContext<RezepteDbContext>(options =>
    options.UseSqlite(connectionString));

// Auth cookie for website
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/login";
        options.Cookie.Name = "rezepte.auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);

        // WICHTIG: Keine HTML-Redirects für API-Endpunkte
        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = ctx =>
            {
                if (ctx.Request.Path.StartsWithSegments("/api"))
                {
                    ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                }
                ctx.Response.Redirect(ctx.RedirectUri);
                return Task.CompletedTask;
            },
            OnRedirectToAccessDenied = ctx =>
            {
                if (ctx.Request.Path.StartsWithSegments("/api"))
                {
                    ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                }
                ctx.Response.Redirect(ctx.RedirectUri);
                return Task.CompletedTask;
            }
        };
    })
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        var secret = builder.Configuration["Jwt:Key"] ?? "dev-super-secret-key-change";
        // Gleiches Verfahren wie im TokenService: SHA256 aus dem Secret bilden
        var raw = Encoding.UTF8.GetBytes(secret);
        var keyBytes = SHA256.HashData(raw);
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "rezepte",
            ValidateAudience = true,
            ValidAudience = "rezepte.api",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes)
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<ApiAuthHandler>();
builder.Services.AddTransient<AntiForgeryHandler>();

// Typed API-Client mit Auth-Handlern (Auth + AntiForgery)
builder.Services.AddHttpClient<ApiClient>()
    .AddHttpMessageHandler<ApiAuthHandler>()
    .AddHttpMessageHandler<AntiForgeryHandler>();

// Default HttpClient (ohne Auth-Header) bleibt für statische/öffentliche Calls verfügbar
builder.Services.AddScoped(sp =>
{
    var nav = sp.GetRequiredService<NavigationManager>();
    return new HttpClient { BaseAddress = new Uri(nav.BaseUri) };
});

// Register application services
builder.Services.AddScoped<IUserService, UserService>();

// ViewModels
builder.Services.AddScoped<SettingsViewModel>();
builder.Services.AddScoped<UserProfileViewModel>();

var app = builder.Build();

// Apply migrations / ensure database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RezepteDbContext>();
    var hasMigrations = db.Database.GetMigrations().Any();
    if (hasMigrations)
        await db.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage(); // Server-Fehler detailliert ausgeben/loggen
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
