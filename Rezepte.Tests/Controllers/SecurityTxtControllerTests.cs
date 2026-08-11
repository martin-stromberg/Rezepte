using FluentAssertions;
using Microsoft.AspNetCore.Http;
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
        return new SecurityTxtController(settingsMock.Object, renderer)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    private static SecurityTxtController CreateControllerForRequest(SecurityTxtSettings settings, string path)
    {
        var controller = CreateController(settings);
        controller.HttpContext.Request.Scheme = "https";
        controller.HttpContext.Request.Host = new HostString("rezepte.example");
        controller.HttpContext.Request.Path = path;
        return controller;
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
    public async Task GetSecurityTxt_UsesCanonicalForPlainTextPath()
    {
        var sut = CreateControllerForRequest(EnabledSettings, "/security.txt");

        var result = await sut.GetSecurityTxt(CancellationToken.None);
        var content = (ContentResult)result;

        content.Content.Should().Contain("Canonical: https://rezepte.example/security.txt");
    }

    [Fact]
    public async Task GetSecurityTxt_UsesCanonicalForWellKnownAliasPath()
    {
        var sut = CreateControllerForRequest(EnabledSettings, "/.well-known/security.txt");

        var result = await sut.GetSecurityTxt(CancellationToken.None);
        var content = (ContentResult)result;

        content.Content.Should().Contain("Canonical: https://rezepte.example/security.txt");
    }

    [Fact]
    public async Task GetSecurityMd_UsesCanonicalForMarkdownPath()
    {
        var sut = CreateControllerForRequest(EnabledSettings, "/.well-known/security.md");

        var result = await sut.GetSecurityMd(CancellationToken.None);
        var content = (ContentResult)result;

        content.Content.Should().Contain("https://rezepte.example/.well-known/security.md");
    }

    [Fact]
    public async Task GetSecurityHtml_UsesCanonicalForHtmlPath()
    {
        var sut = CreateControllerForRequest(EnabledSettings, "/.well-known/security.html");

        var result = await sut.GetSecurityHtml(CancellationToken.None);
        var content = (ContentResult)result;

        content.Content.Should().Contain("https://rezepte.example/.well-known/security.html");
    }
}
