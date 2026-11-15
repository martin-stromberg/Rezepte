using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rezepte.Import.Abstractions;
using Rezepte.Web.Data;
using Rezepte.Web.Entities;
using Rezepte.Web.Services.Import.Plugins;
using System.Security.Claims;

namespace Rezepte.Tests.Services.Import;

/// <summary>
/// Base class for <see cref="PluginSettingsService"/> tests providing an in-memory database,
/// http context fixtures and fake collaborators.
/// </summary>
public abstract class PluginSettingsServiceTestsBase
{
    /// <summary>
    /// Creates a new in-memory database context.
    /// </summary>
    /// <returns>The result.</returns>
    protected static RezepteDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<RezepteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new RezepteDbContext(options);
    }

    /// <summary>
    /// Creates a plugin setting fixture.
    /// </summary>
    /// <param name="pluginId">The plugin id parameter.</param>
    /// <param name="orderIndex">The order index parameter.</param>
    /// <param name="enabled">The enabled parameter.</param>
    /// <returns>The result.</returns>
    protected static PluginSetting CreateSetting(string pluginId, int orderIndex, bool enabled)
    {
        return new PluginSetting
        {
            PluginId = pluginId,
            DisplayName = pluginId,
            AssemblyName = "TestAssembly",
            TypeName = "TestHandler",
            Enabled = enabled,
            OrderIndex = orderIndex,
            Status = PluginStatus.Loaded,
            DiscoveredAt = DateTime.UtcNow,
            LastSeenAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates an http context accessor with an authenticated user.
    /// </summary>
    /// <param name="isAdmin">The is admin parameter.</param>
    /// <returns>The result.</returns>
    protected static IHttpContextAccessor CreateHttpContextAccessor(bool isAdmin)
    {
        var claims = isAdmin
            ? new[] { new Claim(ClaimTypes.Role, "Admin") }
            : Array.Empty<Claim>();
        return new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
            }
        };
    }

    /// <summary>
    /// Creates an empty service provider.
    /// </summary>
    /// <returns>The result.</returns>
    protected static IServiceProvider CreateServiceProvider() => new ServiceCollection().BuildServiceProvider();

    /// <summary>
    /// Secret store backed by an in-memory dictionary.
    /// </summary>
    protected sealed class FakeSecretStore : ISystemSecretStore
    {
        /// <summary>
        /// Gets the stored secrets.
        /// </summary>
        /// <returns>The result.</returns>
        public Dictionary<string, string> Values { get; } = [];

        /// <inheritdoc />
        public Task StoreAsync(string name, string secret, CancellationToken ct = default)
        {
            Values[name] = secret;
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<string?> GetAsync(string name, CancellationToken ct = default)
            => Task.FromResult(Values.GetValueOrDefault(name));

        /// <inheritdoc />
        public Task DeleteAsync(string name, CancellationToken ct = default)
        {
            Values.Remove(name);
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Plugin manager stub that returns a configurable usability map.
    /// </summary>
    /// <param name="usability">The usability parameter.</param>
    /// <returns>The result.</returns>
    protected class FakePluginManager(IReadOnlyDictionary<string, PluginUsabilityResult>? usability = null) : IPluginManager
    {
        /// <inheritdoc />
        public Task InitializeAsync(CancellationToken ct = default) => Task.CompletedTask;

        /// <inheritdoc />
        public Task<IReadOnlyList<PluginImportHandler>> GetActiveHandlersAsync(IServiceProvider serviceProvider, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<PluginImportHandler>>([]);

        /// <inheritdoc />
        public Task<IReadOnlyDictionary<string, PluginUsabilityResult>> GetPluginsUsabilityAsync(IServiceProvider serviceProvider, CancellationToken ct = default)
            => Task.FromResult(usability ?? new Dictionary<string, PluginUsabilityResult>());
    }

    /// <summary>
    /// Plugin update service stub that captures the requested source id and returns a configurable result.
    /// </summary>
    /// <param name="result">The result parameter.</param>
    /// <returns>The result.</returns>
    protected sealed class FakePluginUpdateService(PluginUpdateCheckResult? result = null) : IPluginUpdateService
    {
        /// <summary>
        /// Gets the result that is returned for source checks.
        /// </summary>
        /// <returns>The result.</returns>
        public PluginUpdateCheckResult Result { get; } = result ?? PluginUpdateCheckResult.NoUpdate();

        /// <summary>
        /// Gets the source id of the last source check call.
        /// </summary>
        /// <returns>The result.</returns>
        public string? LastSourceId { get; private set; }

        /// <summary>
        /// Gets the number of source check calls.
        /// </summary>
        /// <returns>The result.</returns>
        public int CheckSourceCalls { get; private set; }

        /// <inheritdoc />
        public Task CheckForUpdatesAsync(CancellationToken ct = default) => Task.CompletedTask;

        /// <inheritdoc />
        public Task<PluginUpdateCheckResult> CheckSourceForUpdatesAsync(string sourceId, CancellationToken ct = default)
        {
            CheckSourceCalls++;
            LastSourceId = sourceId;
            return Task.FromResult(Result);
        }
    }
}
