using FluentAssertions;
using Rezepte.Tests.TestHelpers;
using Rezepte.Web.Configuration;
using Xunit;

namespace Rezepte.Tests.Services;

public class LoadingBarServiceDurationParsingTests
{
    [Fact]
    public void GetSettings_WithHideDelayInMilliseconds_ConvertsToMilliseconds()
    {
        var sut = LoadingBarServiceTestFactory.CreateService(new LoadingBarOptions { HideDelay = "250ms" });

        var result = sut.GetSettings();

        result.HideDelayMilliseconds.Should().Be(250);
    }

    [Fact]
    public void GetSettings_WithHideDelayInSeconds_ConvertsToMilliseconds()
    {
        var sut = LoadingBarServiceTestFactory.CreateService(new LoadingBarOptions { HideDelay = "0.5s" });

        var result = sut.GetSettings();

        result.HideDelayMilliseconds.Should().Be(500);
    }

    [Fact]
    public void GetSettings_WithMaxVisibleDurationInSeconds_ConvertsToMilliseconds()
    {
        var sut = LoadingBarServiceTestFactory.CreateService(new LoadingBarOptions { MaxVisibleDuration = "20s" });

        var result = sut.GetSettings();

        result.MaxVisibleDurationMilliseconds.Should().Be(20000);
    }
}
