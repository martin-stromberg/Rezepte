using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.IdentityModel.Tokens;
using Rezepte.Web.Configuration;
using Rezepte.Web.Data;
using Rezepte.Web.Security;
using Rezepte.Web.Services;
using Rezepte.Web.Services.BackgroundJobs;
using Rezepte.Web.Services.BackgroundJobs.Handlers;
using Rezepte.Web.Services.Import;
using Rezepte.Web.Services.Import.Plugins;
using Rezepte.Web.Services.Updates;
using Rezepte.Web.Services.Validation;
using Rezepte.Web.ViewModels;
using System.Net;
using System.Threading.RateLimiting;

namespace Rezepte.Web.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRezepteServices(this IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
    {
        // Bind configuration sections used by the app
        services.Configure<ImageOptions>(configuration.GetSection("Images"));
        services.Configure<AIOptions>(configuration.GetSection("AI"));
        services.Configure<PluginUpdateOptions>(configuration.GetSection("PluginUpdates"));
        services.Configure<UpdateBackupOptions>(configuration.GetSection("UpdateBackups"));
        services.Configure<ApplicationUpdateOptions>(configuration.GetSection("ApplicationUpdates"));
        services.Configure<GoogleCredentialsOptions>(configuration.GetSection("GoogleCredentials"));
        services.Configure<LoadingBarOptions>(configuration.GetSection("LoadingBar"));
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

        // JWT signing material (fails fast outside development when no secret is configured)
        var jwtSigningKeyProvider = new JwtSigningKeyProvider(configuration, env);
        services.AddSingleton<IJwtSigningKeyProvider>(jwtSigningKeyProvider);

        // Rate limiting for authentication endpoints
        var authenticationPermitLimit =
            configuration.GetValue<int?>("RateLimiting:Authentication:PermitLimit") ?? 10;
        var authenticationWindowSeconds =
            configuration.GetValue<int?>("RateLimiting:Authentication:WindowSeconds") ?? 60;

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(RateLimitPolicies.Authentication, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = authenticationPermitLimit,
                    Window = TimeSpan.FromSeconds(authenticationWindowSeconds),
                    QueueLimit = 0
                }));
        });

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
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtSigningKeyProvider.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtSigningKeyProvider.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(jwtSigningKeyProvider.Key)
                };
            });

        services.AddAuthorization();

        // Infrastructure
        services.AddMemoryCache();
        services.AddDataProtection();
        services.AddSingleton<ITokenService, TokenService>();
        services.AddSingleton<IGoogleCredentialsProvider, GoogleCredentialsProvider>();
        services.AddHttpContextAccessor();
        services.AddTransient<AntiForgeryHandler>();
        services.AddScoped<CircuitAuthHandler>();

        // Named client for the pooled base handler pipeline (AntiForgery only)
        services.AddHttpClient("ApiClient")
            .AddHttpMessageHandler<AntiForgeryHandler>();

        // Scoped ApiClient with a per-circuit/request auth handler
        services.AddScoped<ApiClient>(sp =>
        {
            var nav = sp.GetRequiredService<NavigationManager>();
            var handlerFactory = sp.GetRequiredService<IHttpMessageHandlerFactory>();
            var inner = handlerFactory.CreateHandler("ApiClient");
            var authHandler = sp.GetRequiredService<CircuitAuthHandler>();
            authHandler.InnerHandler = inner;
            var http = new HttpClient(authHandler, disposeHandler: true)
            {
                BaseAddress = new Uri(nav.BaseUri)
            };
            return new ApiClient(http, nav);
        });

        // Default HttpClient (no auth header) for static/public calls
        services.AddScoped(sp =>
        {
            var nav = sp.GetRequiredService<NavigationManager>();
            return new HttpClient(new HttpClientHandler()
            {
                UseCookies = true,
                CookieContainer = new CookieContainer()
            })
            { BaseAddress = new Uri(nav.BaseUri) };
        });

        // Application services
        services.AddSingleton<IUsernameValidator, UsernameValidator>();
        services.AddSingleton<ILoadingBarService, LoadingBarService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ICookbookService, CookbookService>();
        services.AddScoped<IRecipeService, RecipeService>();
        services.AddScoped<IExportService, ExportService>();
        services.AddScoped<IUpdateBackupService, UpdateBackupService>();
        services.AddScoped<IApplicationUpdateSettingsService, ApplicationUpdateSettingsService>();
        services.AddScoped<ExportJobFileStore>();
        services.AddScoped<IBackgroundJobHandler, ExportUserJobHandler>();
        services.AddScoped<IBackgroundJobHandler, ExportAllJobHandler>();
        services.AddScoped<IPdfGenerator, PdfGenerator>();
        services.AddScoped<IImportService, ImportService>();
        services.AddScoped<IImportedRecipePersister, ImportedRecipePersister>();
        services.AddSingleton<IPluginManager, PluginManager>();
        services.AddHostedService<PluginStartupService>();
        services.AddHostedService<PluginUpdateHostedService>();
        services.AddSingleton<IApplicationUpdatePreInstallHandler, ApplicationUpdatePreInstallHandler>();
        services.AddHostedService<ApplicationUpdateHostedService>();
        services.AddHttpClient<IGitHubReleaseClient, GitHubReleaseClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<PluginUpdateOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(Math.Max(1, options.TimeoutSeconds));
        });
        services.AddScoped<ISystemSecretStore, DataProtectionSystemSecretStore>();
        services.AddScoped<IPluginPackageValidator, PluginPackageValidator>();
        services.AddScoped<IPluginPackageInstaller, PluginPackageInstaller>();
        services.AddScoped<IPluginUpdateService, PluginUpdateService>();
        services.AddScoped<IPluginSettingsService, PluginSettingsService>();
        services.AddScoped<IAiUsageService, AiUsageService>();
        services.AddScoped<ISecurityTxtSettingsService, SecurityTxtSettingsService>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<ISecurityTxtRenderer, SecurityTxtRenderer>();
        services.AddScoped<ICalendarService, CalendarService>();
        services.AddScoped<IShoppingListService, ShoppingListService>();
        services.AddScoped<IGeminiClient, GeminiClient>();
        services.AddScoped<ITestRecipeImportService, TestRecipeImportService>();

        // ImportOrchestrator: singleton that creates scopes for handlers (handlers stay scoped)
        services.AddSingleton<ImportOrchestrator>();

        // ViewModels
        services.AddScoped<SettingsViewModel>();
        services.AddScoped<UserProfileViewModel>();
        services.AddScoped<UserAdminViewModel>();

        services.AddBackgroundJobQueue();

        return services;
    }
}
