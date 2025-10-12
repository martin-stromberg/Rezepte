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

// Add API controllers
builder.Services.AddControllers();

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
    })
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        var key = builder.Configuration["Jwt:Key"] ?? "dev-super-secret-key-change";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "rezepte",
            ValidateAudience = true,
            ValidAudience = "rezepte.api",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
        };
    });

builder.Services.AddAuthorization();

// HttpClient for Blazor Server components with API auth delegating handler
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<ApiAuthHandler>();
builder.Services.AddHttpClient("ApiClient").AddHttpMessageHandler<ApiAuthHandler>();

builder.Services.AddScoped(sp =>
{
    // Default HttpClient for same-origin calls (no auth header)
    var nav = sp.GetRequiredService<NavigationManager>();
    return new HttpClient { BaseAddress = new Uri(nav.BaseUri) };
});

// Register application services
builder.Services.AddScoped<IUserService, UserService>();

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

app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Map API controllers
app.MapControllers();

// Redirect to register/login depending on state
app.UseRedirectToRegisterWhenNoUsers();

app.Run();
