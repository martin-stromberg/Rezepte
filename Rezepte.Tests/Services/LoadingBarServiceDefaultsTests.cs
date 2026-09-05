using FluentAssertions;
using Rezepte.Tests.TestHelpers;
using Rezepte.Web.Configuration;
using Xunit;

namespace Rezepte.Tests.Services;

/// <summary>
/// Class representing the loading bar service defaults tests.
/// </summary>
public class LoadingBarServiceDefaultsTests
{
    /// <summary>
    /// Get settings with default options returns documented defaults.
    /// </summary>
    [Fact]
    public void GetSettings_WithDefaultOptions_ReturnsDocumentedDefaults()
    {
        var sut = LoadingBarServiceTestFactory.CreateService(new LoadingBarOptions());

        var result = sut.GetSettings();

        result.Enabled.Should().BeTrue();
        result.Height.Should().Be("3px");
        result.AnimationDuration.Should().Be("2s");
        result.HideDelayMilliseconds.Should().Be(300);
        result.MaxVisibleDurationMilliseconds.Should().Be(15000);
        result.Colors.Should().BeEquivalentTo(LoadingBarOptions.DefaultColors);
    }

    /// <summary>
    /// Get settings with enabled false returns disabled settings.
    /// </summary>
    [Fact]
    public void GetSettings_WithEnabledFalse_ReturnsDisabledSettings()
    {
        var sut = LoadingBarServiceTestFactory.CreateService(new LoadingBarOptions { Enabled = false });

        var result = sut.GetSettings();

        result.Enabled.Should().BeFalse();
    }

    /// <summary>
    /// Get settings called twice returns same instance.
    /// </summary>
    [Fact]
    public void GetSettings_CalledTwice_ReturnsSameInstance()
    {
        var sut = LoadingBarServiceTestFactory.CreateService(new LoadingBarOptions());

        var first = sut.GetSettings();
        var second = sut.GetSettings();

        second.Should().BeSameAs(first);
    }

    /// <summary>
    /// Get settings with valid custom options returns configured values.
    /// </summary>
    [Fact]
    public void GetSettings_WithValidCustomOptions_ReturnsConfiguredValues()
    {
        var options = new LoadingBarOptions
        {
            Enabled = true,
            Height = "5px",
            AnimationDuration = "3s",
            HideDelay = "500ms",
            MaxVisibleDuration = "20s",
            Colors = new[] { "#123456", "#abcdef" }
        };
        var sut = LoadingBarServiceTestFactory.CreateService(options);

        var result = sut.GetSettings();

        result.Height.Should().Be("5px");
        result.AnimationDuration.Should().Be("3s");
        result.HideDelayMilliseconds.Should().Be(500);
        result.MaxVisibleDurationMilliseconds.Should().Be(20000);
        result.Colors.Should().BeEquivalentTo(new[] { "#123456", "#abcdef" });
    }
}
