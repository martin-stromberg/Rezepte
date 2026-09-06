using FluentAssertions;
using Rezepte.Tests.TestHelpers;
using Rezepte.Web.Configuration;
using Xunit;

namespace Rezepte.Tests.Services;

/// <summary>
/// Class representing the loading bar service duration parsing tests.
/// </summary>
public class LoadingBarServiceDurationParsingTests
{
    /// <summary>
    /// Get settings with hide delay in milliseconds converts to milliseconds.
    /// </summary>
    [Fact]
    public void GetSettings_WithHideDelayInMilliseconds_ConvertsToMilliseconds()
    {
        var sut = LoadingBarServiceTestFactory.CreateService(new LoadingBarOptions { HideDelay = "250ms" });

        var result = sut.GetSettings();

        result.HideDelayMilliseconds.Should().Be(250);
    }

    /// <summary>
    /// Get settings with hide delay in seconds converts to milliseconds.
    /// </summary>
    [Fact]
    public void GetSettings_WithHideDelayInSeconds_ConvertsToMilliseconds()
    {
        var sut = LoadingBarServiceTestFactory.CreateService(new LoadingBarOptions { HideDelay = "0.5s" });

        var result = sut.GetSettings();

        result.HideDelayMilliseconds.Should().Be(500);
    }

    /// <summary>
    /// Get settings with max visible duration in seconds converts to milliseconds.
    /// </summary>
    [Fact]
    public void GetSettings_WithMaxVisibleDurationInSeconds_ConvertsToMilliseconds()
    {
        var sut = LoadingBarServiceTestFactory.CreateService(new LoadingBarOptions { MaxVisibleDuration = "20s" });

        var result = sut.GetSettings();

        result.MaxVisibleDurationMilliseconds.Should().Be(20000);
    }
}
