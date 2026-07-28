using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Rezepte.Web.Components.Layout;
using Rezepte.Web.Configuration;
using Rezepte.Web.Services;
using Xunit;

namespace Rezepte.Tests.Components;

public class LoadingBarRenderingTests : IDisposable
{
    private readonly TestContext _context = new();

    [Fact]
    public void Render_WhenEnabled_RendersHostElementWithIndicator()
    {
        var component = RenderLoadingBar(CreateSettings(enabled: true));

        var host = component.Find("#loading-bar");
        host.QuerySelector(".loading-bar-indicator").Should().NotBeNull();
    }

    [Fact]
    public void Render_WhenDisabled_RendersNoMarkup()
    {
        var component = RenderLoadingBar(CreateSettings(enabled: false));

        component.Markup.Should().BeEmpty();
    }

    [Fact]
    public void Render_WhenEnabled_WritesHeightAndDurationAsCssCustomProperties()
    {
        var component = RenderLoadingBar(CreateSettings(enabled: true, height: "5px", animationDuration: "3s"));

        var host = component.Find("#loading-bar");
        var style = host.GetAttribute("style");

        style.Should().Contain("--loading-bar-height: 5px");
        style.Should().Contain("--loading-bar-duration: 3s");
    }

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

    [Fact]
    public void Render_WhenEnabled_MarksBarAsDecorativeAndPermanent()
    {
        var component = RenderLoadingBar(CreateSettings(enabled: true));

        var host = component.Find("#loading-bar");

        host.GetAttribute("aria-hidden").Should().Be("true");
        host.HasAttribute("data-permanent").Should().BeTrue();
    }

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
