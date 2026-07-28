using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Rezepte.Tests.TestHelpers;
using Rezepte.Web.Configuration;
using Rezepte.Web.Services;
using Xunit;

namespace Rezepte.Tests.Services;

public class LoadingBarServiceValidationTests
{
    [Fact]
    public void GetSettings_WithInvalidHeight_FallsBackToDefaultHeight()
    {
        var sut = LoadingBarServiceTestFactory.CreateService(new LoadingBarOptions { Height = "not-a-length" });

        var result = sut.GetSettings();

        result.Height.Should().Be("3px");
    }

    [Fact]
    public void GetSettings_WithNullHeight_FallsBackToDefaultHeight()
    {
        var sut = LoadingBarServiceTestFactory.CreateService(new LoadingBarOptions { Height = null! });

        var result = sut.GetSettings();

        result.Height.Should().Be("3px");
    }

    [Fact]
    public void GetSettings_WithInvalidAnimationDuration_FallsBackToDefaultDuration()
    {
        var sut = LoadingBarServiceTestFactory.CreateService(new LoadingBarOptions { AnimationDuration = "fast" });

        var result = sut.GetSettings();

        result.AnimationDuration.Should().Be("2s");
    }

    [Fact]
    public void GetSettings_WithInvalidHideDelay_FallsBackToDefaultHideDelay()
    {
        var sut = LoadingBarServiceTestFactory.CreateService(new LoadingBarOptions { HideDelay = "soon" });

        var result = sut.GetSettings();

        result.HideDelayMilliseconds.Should().Be(300);
    }

    [Fact]
    public void GetSettings_WithMaxVisibleDurationBelowHideDelay_FallsBackToDefault()
    {
        var sut = LoadingBarServiceTestFactory.CreateService(new LoadingBarOptions { HideDelay = "300ms", MaxVisibleDuration = "200ms" });

        var result = sut.GetSettings();

        result.MaxVisibleDurationMilliseconds.Should().Be(15000);
    }

    [Fact]
    public void GetSettings_WithMaxVisibleDurationEqualToHideDelay_FallsBackToDefault()
    {
        var sut = LoadingBarServiceTestFactory.CreateService(new LoadingBarOptions { HideDelay = "300ms", MaxVisibleDuration = "300ms" });

        var result = sut.GetSettings();

        result.MaxVisibleDurationMilliseconds.Should().Be(15000);
    }

    [Fact]
    public void GetSettings_WithHideDelayAboveMaximum_FallsBackToDefaultHideDelay()
    {
        var sut = LoadingBarServiceTestFactory.CreateService(new LoadingBarOptions { HideDelay = "999999999s" });

        var result = sut.GetSettings();

        result.HideDelayMilliseconds.Should().Be(300);
    }

    [Fact]
    public void GetSettings_WithMaxVisibleDurationAboveMaximum_FallsBackToDefaultMaxVisibleDuration()
    {
        var sut = LoadingBarServiceTestFactory.CreateService(new LoadingBarOptions { MaxVisibleDuration = "999999999s" });

        var result = sut.GetSettings();

        result.MaxVisibleDurationMilliseconds.Should().Be(15000);
    }

    [Fact]
    public void GetSettings_WithMaxVisibleDurationBelowMinimum_FallsBackToDefaultMaxVisibleDuration()
    {
        var sut = LoadingBarServiceTestFactory.CreateService(new LoadingBarOptions { MaxVisibleDuration = "50ms" });

        var result = sut.GetSettings();

        result.MaxVisibleDurationMilliseconds.Should().Be(15000);
    }

    [Fact]
    public void GetSettings_WithHideDelayAboveDefaultMaxVisibleDuration_KeepsInvariant()
    {
        var sut = LoadingBarServiceTestFactory.CreateService(new LoadingBarOptions { HideDelay = "30s", MaxVisibleDuration = "20s" });

        var result = sut.GetSettings();

        result.MaxVisibleDurationMilliseconds.Should().BeGreaterThan(result.HideDelayMilliseconds);
        result.HideDelayMilliseconds.Should().Be(300);
        result.MaxVisibleDurationMilliseconds.Should().Be(15000);
    }

    [Fact]
    public void GetSettings_WithZeroHeight_FallsBackToDefaultHeight()
    {
        var sut = LoadingBarServiceTestFactory.CreateService(new LoadingBarOptions { Height = "0px" });

        var result = sut.GetSettings();

        result.Height.Should().Be("3px");
    }

    [Fact]
    public void GetSettings_WithZeroAnimationDuration_FallsBackToDefaultDuration()
    {
        var sut = LoadingBarServiceTestFactory.CreateService(new LoadingBarOptions { AnimationDuration = "0s" });

        var result = sut.GetSettings();

        result.AnimationDuration.Should().Be("2s");
    }

    [Fact]
    public void GetSettings_WithInvalidColorEntries_RemovesInvalidEntries()
    {
        var sut = LoadingBarServiceTestFactory.CreateService(new LoadingBarOptions { Colors = new[] { "#FF6B6B", "not-a-color", "#123" } });

        var result = sut.GetSettings();

        result.Colors.Should().BeEquivalentTo(new[] { "#FF6B6B", "#123" });
    }

    [Fact]
    public void GetSettings_WithOnlyInvalidColors_FallsBackToDefaultColors()
    {
        var sut = LoadingBarServiceTestFactory.CreateService(new LoadingBarOptions { Colors = new[] { "red", "blue" } });

        var result = sut.GetSettings();

        result.Colors.Should().BeEquivalentTo(LoadingBarOptions.DefaultColors);
    }

    [Fact]
    public void GetSettings_WithNullColors_FallsBackToDefaultColors()
    {
        var sut = LoadingBarServiceTestFactory.CreateService(new LoadingBarOptions { Colors = null! });

        var result = sut.GetSettings();

        result.Colors.Should().BeEquivalentTo(LoadingBarOptions.DefaultColors);
    }

    [Fact]
    public void GetSettings_WithEmptyColors_FallsBackToDefaultColors()
    {
        var sut = LoadingBarServiceTestFactory.CreateService(new LoadingBarOptions { Colors = Array.Empty<string>() });

        var result = sut.GetSettings();

        result.Colors.Should().BeEquivalentTo(LoadingBarOptions.DefaultColors);
    }

    [Fact]
    public void GetSettings_WithInvalidHeight_LogsWarning()
    {
        var loggerMock = new Mock<ILogger<LoadingBarService>>();
        var sut = LoadingBarServiceTestFactory.CreateService(new LoadingBarOptions { Height = "not-a-length" }, loggerMock.Object);

        sut.GetSettings();

        loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }
}
