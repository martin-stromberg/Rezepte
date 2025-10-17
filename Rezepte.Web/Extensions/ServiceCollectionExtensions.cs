using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Rezepte.Web.Configuration;
using Rezepte.Web.Data;
using Rezepte.Web.Services;
using Rezepte.Web.Services.BackgroundJobs;
using Rezepte.Web.Services.BackgroundJobs.Handlers;
using Rezepte.Web.Services.Import;
using Rezepte.Web.ViewModels;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Rezepte.Web.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRezepteServices(this IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
    {
        // Bind configuration sections used by the app
        services.Configure<ImageOptions>(configuration.GetSection("Images"));
        services.Configure<AIOptions>(configuration.GetSection("AI"));
        // Razor Components
        services.AddRazorComponents()
            .AddInteractiveServerComponents(options =>
            {
                if (env.IsDevelopment())
                {
                    options.DetailedErrors = true;
                }
            });

        // Controllers (disable antiforgery for APIs globally)
        services.AddControllers(options =>
        {
            options.Filters.Add(new IgnoreAntiforgeryTokenAttribute());
        });

        // EF Core Sqlite
        var connectionString = configuration.GetConnectionString("Default") ?? "Data Source=rezepte.db";
        services.AddDbContext<RezepteDbContext>(options => options.UseSqlite(connectionString));

        // Authentication (Cookie + JWT)
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
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
                var secret = configuration["Jwt:Key"] ?? "dev-super-secret-key-change";
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

        services.AddAuthorization();

        // Infrastructure
        services.AddMemoryCache();
        services.AddSingleton<ITokenService, TokenService>();
        services.AddSingleton<IGoogleServiceAccountProvider, GoogleServiceAccountProvider>();
        services.AddHttpContextAccessor();
        services.AddTransient<ApiAuthHandler>();
        services.AddTransient<AntiForgeryHandler>();

        // Typed API client with handlers
        services.AddHttpClient<ApiClient>()
            .AddHttpMessageHandler<ApiAuthHandler>()
            .AddHttpMessageHandler<AntiForgeryHandler>();

        // Default HttpClient (no auth header) for static/public calls
        services.AddScoped(sp =>
        {
            var nav = sp.GetRequiredService<NavigationManager>();
            return new HttpClient(new HttpClientHandler()
            {
                UseCookies = true,
                CookieContainer = new CookieContainer()
            }) { BaseAddress = new Uri(nav.BaseUri) };
        });

        // Application services
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ICookbookService, CookbookService>();
        services.AddScoped<IRecipeService, RecipeService>();
        services.AddScoped<IExportService, ExportService>();
        services.AddScoped<IBackgroundJobHandler, ExportUserJobHandler>();
        services.AddScoped<IPdfGenerator, PdfGenerator>();
        services.AddScoped<IImportService, ImportService>();
        services.AddScoped<IImportHandler, BackupImportHandler>();
        services.AddScoped<IImportHandler, UrlReceiptImportHandler>();
        services.AddScoped<IImportHandler, AIFotoImportHandler>();
        services.AddScoped<IImportHandler, AIUrlImportHandler>();
        services.AddScoped<IAiUsageService, AiUsageService>();
        services.AddScoped<ISettingsService, SettingsService>();

        // ViewModels
        services.AddScoped<SettingsViewModel>();
        services.AddScoped<UserProfileViewModel>();
        services.AddScoped<UserAdminViewModel>();

        services.AddBackgroundJobQueue();

        return services;
    }
}
