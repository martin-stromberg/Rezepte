using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using Rezepte.Tests.TestHelpers;
using Rezepte.Web.Configuration;
using Rezepte.Web.Extensions;
using Rezepte.Web.Services;
using System.Text.RegularExpressions;
using Xunit;

namespace Rezepte.Tests.Deployment;

/// <summary>
/// Class representing the loading bar wiring tests.
/// </summary>
public class LoadingBarWiringTests
{
    private static readonly Regex LoadingBarElementPattern = new(@"<LoadingBar\s*/>", RegexOptions.Compiled);
    private static readonly Regex MainElementPattern = new(@"<main[\s>]", RegexOptions.Compiled);

    /// <summary>
    /// Layout should place loading bar between navigation and main content.
    /// </summary>
    [Fact]
    public void Layout_ShouldPlaceLoadingBarBetweenNavigationAndMainContent()
    {
        var markup = RepositoryPaths.ReadRepositoryFile("Rezepte.Web", "Components", "Layout", "MainLayout.razor");

        var navCloseIndex = markup.IndexOf("</nav>", StringComparison.Ordinal);
        var loadingBarMatch = LoadingBarElementPattern.Match(markup);
        var mainMatch = MainElementPattern.Match(markup);

        navCloseIndex.Should().BeGreaterThan(-1);
        loadingBarMatch.Success.Should().BeTrue();
        loadingBarMatch.Index.Should().BeGreaterThan(navCloseIndex);
        mainMatch.Success.Should().BeTrue();
        mainMatch.Index.Should().BeGreaterThan(loadingBarMatch.Index);
    }

    /// <summary>
    /// App should load loading bar script after blazor script.
    /// </summary>
    [Fact]
    public void App_ShouldLoadLoadingBarScriptAfterBlazorScript()
    {
        var markup = RepositoryPaths.ReadRepositoryFile("Rezepte.Web", "Components", "App.razor");

        var blazorScriptIndex = markup.IndexOf("_framework/blazor.web.js", StringComparison.Ordinal);
        var loadingBarScriptIndex = markup.IndexOf("js/loadingBar.js", StringComparison.Ordinal);

        blazorScriptIndex.Should().BeGreaterThan(-1);
        loadingBarScriptIndex.Should().BeGreaterThan(blazorScriptIndex);
    }

    /// <summary>
    /// Configuration loading bar section matches documented defaults.
    /// </summary>
    [Fact]
    public void Configuration_LoadingBarSection_MatchesDocumentedDefaults()
    {
        var repositoryRoot = RepositoryPaths.FindRepositoryRoot();
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(repositoryRoot.FullName, "Rezepte.Web"))
            .AddJsonFile("appsettings.json")
            .Build();

        var options = new LoadingBarOptions();
        configuration.GetSection("LoadingBar").Bind(options);

        var defaults = new LoadingBarOptions();
        options.Enabled.Should().Be(defaults.Enabled);
        options.Height.Should().Be(defaults.Height);
        options.AnimationDuration.Should().Be(defaults.AnimationDuration);
        options.HideDelay.Should().Be(defaults.HideDelay);
        options.MaxVisibleDuration.Should().Be(defaults.MaxVisibleDuration);
        options.Colors.Should().BeEquivalentTo(LoadingBarOptions.DefaultColors);
    }

    /// <summary>
    /// Service collection extensions should bind loading bar options section.
    /// </summary>
    [Fact]
    public void ServiceCollectionExtensions_ShouldBindLoadingBarOptionsSection()
    {
        using var provider = BuildServiceProvider(new Dictionary<string, string?>
        {
            ["LoadingBar:Height"] = "5px",
            ["ConnectionStrings:Default"] = "Data Source=:memory:"
        });

        provider.GetRequiredService<IOptions<LoadingBarOptions>>().Value.Height.Should().Be("5px");
    }

    /// <summary>
    /// Service collection extensions should register loading bar service.
    /// </summary>
    [Fact]
    public void ServiceCollectionExtensions_ShouldRegisterLoadingBarService()
    {
        using var provider = BuildServiceProvider(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = "Data Source=:memory:"
        });

        provider.GetRequiredService<ILoadingBarService>().Should().BeOfType<LoadingBarService>();
    }

    private static ServiceProvider BuildServiceProvider(IReadOnlyDictionary<string, string?> configurationOverrides)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationOverrides)
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        var environmentMock = new Mock<IHostEnvironment>();
        environmentMock.Setup(env => env.EnvironmentName).Returns("Development");

        services.AddRezepteServices(configuration, environmentMock.Object);

        return services.BuildServiceProvider();
    }
}
