using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Rezepte.Web.Configuration;
using Rezepte.Web.Services;

namespace Rezepte.Tests.TestHelpers;

/// <summary>
/// Class representing the loading bar service test factory.
/// </summary>
public static class LoadingBarServiceTestFactory
{
    /// <summary>
    /// Create service.
    /// </summary>
    /// <param name="options">The options parameter.</param>
    /// <returns>The result.</returns>
    public static LoadingBarService CreateService(LoadingBarOptions options)
        => CreateService(options, NullLogger<LoadingBarService>.Instance);

    /// <summary>
    /// Create service.
    /// </summary>
    /// <param name="options">The options parameter.</param>
    /// <param name="logger">The logger parameter.</param>
    /// <returns>The result.</returns>
    public static LoadingBarService CreateService(LoadingBarOptions options, ILogger<LoadingBarService> logger)
        => new(Options.Create(options), logger);
}
