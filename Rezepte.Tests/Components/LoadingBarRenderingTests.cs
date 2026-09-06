using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Rezepte.Web.Components.Layout;
using Rezepte.Web.Configuration;
using Rezepte.Web.Services;
using Xunit;

namespace Rezepte.Tests.Components;

/// <summary>
/// Class representing the loading bar rendering tests.
/// </summary>
public class LoadingBarRenderingTests : IDisposable
{
    private readonly TestContext _context = new();

    /// <summary>
    /// Render when enabled renders host element with indicator.
    /// </summary>
    [Fact]
    public void Render_WhenEnabled_RendersHostElementWithIndicator()
    {
        var component = RenderLoadingBar(CreateSettings(enabled: true));

        var host = component.Find("#loading-bar");
        host.QuerySelector(".loading-bar-indicator").Should().NotBeNull();
    }

    /// <summary>
    /// Render when disabled renders no markup.
    /// </summary>
    [Fact]
    public void Render_WhenDisabled_RendersNoMarkup()
    {
        var component = RenderLoadingBar(CreateSettings(enabled: false));

        component.Markup.Should().BeEmpty();
    }

    /// <summary>
    /// Render when enabled writes height and duration as css custom properties.
    /// </summary>
    [Fact]
    public void Render_WhenEnabled_WritesHeightAndDurationAsCssCustomProperties()
    {
        var component = RenderLoadingBar(CreateSettings(enabled: true, height: "5px", animationDuration: "3s"));

        var host = component.Find("#loading-bar");
        var style = host.GetAttribute("style");

        style.Should().Contain("--loading-bar-height: 5px");
        style.Should().Contain("--loading-bar-duration: 3s");
    }

    /// <summary>
    /// Render when enabled writes colors and timings as data attributes.
    /// </summary>
    [Fact]
    public void Render_WhenEnabled_WritesColorsAndTimingsAsDataAttributes()
    {
        var component = RenderLoadingBar(CreateSettings(
            enabled: true,
            colors: new[] { "#FF6B6B", "#4ECDC4" },
            hideDelayMilliseconds: 300,
            maxVisibleDurationMilliseconds: 15000));

        var host = component.Find("#loading-bar");

        host.GetAttribute("data-colors").Should().Be("#FF6B6B,#4ECDC4");
        host.GetAttribute("data-hide-delay").Should().Be("300");
        host.GetAttribute("data-max-visible-duration").Should().Be("15000");
    }

    /// <summary>
    /// Render when enabled marks bar as decorative and permanent.
    /// </summary>
    [Fact]
    public void Render_WhenEnabled_MarksBarAsDecorativeAndPermanent()
    {
        var component = RenderLoadingBar(CreateSettings(enabled: true));

        var host = component.Find("#loading-bar");

        host.GetAttribute("aria-hidden").Should().Be("true");
        host.HasAttribute("data-permanent").Should().BeTrue();
    }

    /// <summary>
    /// Dispose.
    /// </summary>
    public void Dispose()
    {
        _context.Dispose();
    }

    private IRenderedComponent<LoadingBar> RenderLoadingBar(LoadingBarSettings settings)
    {
        var serviceMock = new Mock<ILoadingBarService>();
        serviceMock.Setup(service => service.GetSettings()).Returns(settings);
        _context.Services.AddSingleton(serviceMock.Object);

        return _context.RenderComponent<LoadingBar>();
    }

    private static LoadingBarSettings CreateSettings(
        bool enabled,
        string height = "3px",
        string animationDuration = "2s",
        string[]? colors = null,
        int hideDelayMilliseconds = 300,
        int maxVisibleDurationMilliseconds = 15000)
    {
        return new LoadingBarSettings(
            enabled,
            height,
            animationDuration,
            colors ?? LoadingBarOptions.DefaultColors,
            hideDelayMilliseconds,
            maxVisibleDurationMilliseconds);
    }
}
