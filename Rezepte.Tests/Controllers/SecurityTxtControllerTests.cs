using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Rezepte.Web.Controllers;
using Rezepte.Web.Dtos;
using Rezepte.Web.Services;
using Xunit;

namespace Rezepte.Tests.Controllers;

public class SecurityTxtControllerTests
{
    private static readonly SecurityTxtSettings EnabledSettings = new(
        Enabled: true,
        Contact: "mailto:security@example.com",
        Expires: new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero),
        Encryption: null,
        Acknowledgments: null,
        PreferredLanguages: null,
        Canonical: null,
        Policy: null,
        Hiring: null);

    private static readonly SecurityTxtSettings DisabledSettings = EnabledSettings with { Enabled = false };

    private static SecurityTxtController CreateController(SecurityTxtSettings settings)
    {
        var settingsMock = new Mock<ISettingsService>();
        settingsMock.Setup(s => s.GetSecurityTxtSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);
        var renderer = new SecurityTxtRenderer();
        return new SecurityTxtController(settingsMock.Object, renderer);
    }

    [Fact]
    public async Task GetSecurityTxt_ReturnsOk_WhenEnabled()
    {
        var sut = CreateController(EnabledSettings);

        var result = await sut.GetSecurityTxt(CancellationToken.None);

        result.Should().BeOfType<ContentResult>()
            .Which.StatusCode.Should().BeNull();
        var content = (ContentResult)result;
        content.ContentType.Should().StartWith("text/plain");
    }

    [Fact]
    public async Task GetSecurityTxt_ReturnsNotFound_WhenDisabled()
    {
        var sut = CreateController(DisabledSettings);

        var result = await sut.GetSecurityTxt(CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetWellKnownSecurityTxt_ReturnsOk_WhenEnabled()
    {
        var sut = CreateController(EnabledSettings);

        var result = await sut.GetSecurityTxt(CancellationToken.None);

        result.Should().BeOfType<ContentResult>();
    }

    [Fact]
    public async Task GetSecurityMd_ReturnsOk_WithMarkdownContentType()
    {
        var sut = CreateController(EnabledSettings);

        var result = await sut.GetSecurityMd(CancellationToken.None);

        result.Should().BeOfType<ContentResult>()
            .Which.ContentType.Should().StartWith("text/markdown");
    }

    [Fact]
    public async Task GetSecurityHtml_ReturnsOk_WithHtmlContentType()
    {
        var sut = CreateController(EnabledSettings);

        var result = await sut.GetSecurityHtml(CancellationToken.None);

        result.Should().BeOfType<ContentResult>()
            .Which.ContentType.Should().StartWith("text/html");
    }

    [Fact]
    public async Task GetSecurityTxt_RequiresNoAuthentication()
    {
        var sut = CreateController(EnabledSettings);

        var result = await sut.GetSecurityTxt(CancellationToken.None);

        result.Should().NotBeOfType<UnauthorizedResult>();
        result.Should().NotBeOfType<ForbidResult>();
    }
}
