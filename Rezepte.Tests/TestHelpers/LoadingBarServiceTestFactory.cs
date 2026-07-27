using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Rezepte.Web.Configuration;
using Rezepte.Web.Services;

namespace Rezepte.Tests.TestHelpers;

public static class LoadingBarServiceTestFactory
{
    public static LoadingBarService CreateService(LoadingBarOptions options)
        => CreateService(options, NullLogger<LoadingBarService>.Instance);

    public static LoadingBarService CreateService(LoadingBarOptions options, ILogger<LoadingBarService> logger)
        => new(Options.Create(options), logger);
}
