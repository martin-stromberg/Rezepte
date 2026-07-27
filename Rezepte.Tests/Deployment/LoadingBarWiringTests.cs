using System;
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
using Xunit;

namespace Rezepte.Tests.Deployment;

public class LoadingBarWiringTests
{
    [Fact]
    public void Layout_ShouldPlaceLoadingBarDirectlyBelowNavigation()
    {
        var markup = RepositoryPaths.ReadRepositoryFile("Rezepte.Web", "Components", "Layout", "MainLayout.razor");

        var navCloseIndex = markup.IndexOf("</nav>", StringComparison.Ordinal);
        var loadingBarIndex = markup.IndexOf("<LoadingBar />", StringComparison.Ordinal);
        var mainIndex = markup.IndexOf("<main class=\"container py-4\">", StringComparison.Ordinal);

        navCloseIndex.Should().BeGreaterThan(-1);
        loadingBarIndex.Should().BeGreaterThan(navCloseIndex);
        mainIndex.Should().BeGreaterThan(loadingBarIndex);
    }

    [Fact]
    public void App_ShouldLoadLoadingBarScriptAfterBlazorScript()
    {
        var markup = RepositoryPaths.ReadRepositoryFile("Rezepte.Web", "Components", "App.razor");

        var blazorScriptIndex = markup.IndexOf("_framework/blazor.web.js", StringComparison.Ordinal);
        var loadingBarScriptIndex = markup.IndexOf("js/loadingBar.js", StringComparison.Ordinal);

        blazorScriptIndex.Should().BeGreaterThan(-1);
        loadingBarScriptIndex.Should().BeGreaterThan(blazorScriptIndex);
    }

    [Fact]
    public void Configuration_LoadingBarSection_MatchesDocumentedDefaults()
    {
        var repositoryRoot = RepositoryPaths.FindRepositoryRoot();
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(repositoryRoot.FullName, "Rezepte.Web"))
            .AddJsonFile("appsettings.json")
            .Build();

        // Colors is a fixed-size array; binding into a pre-populated default array appends
        // rather than overwrites, so it must start out empty for the comparison to be meaningful.
        var options = new LoadingBarOptions { Colors = Array.Empty<string>() };
        configuration.GetSection("LoadingBar").Bind(options);

        var defaults = new LoadingBarOptions();
        options.Enabled.Should().Be(defaults.Enabled);
        options.Height.Should().Be(defaults.Height);
        options.AnimationDuration.Should().Be(defaults.AnimationDuration);
        options.HideDelay.Should().Be(defaults.HideDelay);
        options.MaxVisibleDuration.Should().Be(defaults.MaxVisibleDuration);
        options.Colors.Should().BeEquivalentTo(defaults.Colors);
    }

    [Fact]
    public void ServiceCollectionExtensions_ShouldRegisterLoadingBarOptionsAndService()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["LoadingBar:Height"] = "5px",
                ["ConnectionStrings:Default"] = "Data Source=:memory:"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        var environmentMock = new Mock<IHostEnvironment>();
        environmentMock.Setup(env => env.EnvironmentName).Returns("Development");

        services.AddRezepteServices(configuration, environmentMock.Object);

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IOptions<LoadingBarOptions>>().Value.Height.Should().Be("5px");
        provider.GetRequiredService<ILoadingBarService>().Should().BeOfType<LoadingBarService>();
    }
}
