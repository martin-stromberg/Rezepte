using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Rezepte.Web.Controllers;
using Rezepte.Web.Dtos;
using Rezepte.Web.Services;
using Xunit;

namespace Rezepte.Tests.Controllers;

public class SettingsControllerSecurityTxtValidationTests
{
    private static SettingsController CreateController(Mock<ISettingsService>? settingsMock = null)
    {
        settingsMock ??= new Mock<ISettingsService>();
        var googleMock = new Mock<IGoogleCredentialsProvider>();
        return new SettingsController(settingsMock.Object, googleMock.Object);
    }

    private static SecurityTxtSettings ValidEnabledSettings() => new(
        Enabled: true,
        Contact: "mailto:security@example.com",
        Expires: DateTimeOffset.UtcNow.AddYears(1),
        Encryption: null,
        Acknowledgments: null,
        PreferredLanguages: null,
        Canonical: null,
        Policy: null,
        Hiring: null);

    [Fact]
    public async Task SetGlobalSecurityTxt_ReturnsBadRequest_WhenEnabledAndContactMissing()
    {
        var controller = CreateController();
        var settings = ValidEnabledSettings() with { Contact = null };

        var result = await controller.SetGlobalSecurityTxt(settings, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task SetGlobalSecurityTxt_ReturnsBadRequest_WhenEnabledAndContactEmpty()
    {
        var controller = CreateController();
        var settings = ValidEnabledSettings() with { Contact = "   " };

        var result = await controller.SetGlobalSecurityTxt(settings, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task SetGlobalSecurityTxt_ReturnsBadRequest_WhenEnabledAndExpiresMissing()
    {
        var controller = CreateController();
        var settings = ValidEnabledSettings() with { Expires = null };

        var result = await controller.SetGlobalSecurityTxt(settings, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task SetGlobalSecurityTxt_ReturnsBadRequest_WhenEnabledAndExpiresInPast()
    {
        var controller = CreateController();
        var settings = ValidEnabledSettings() with { Expires = DateTimeOffset.UtcNow.AddDays(-1) };

        var result = await controller.SetGlobalSecurityTxt(settings, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task SetGlobalSecurityTxt_ReturnsBadRequest_WhenEnabledAndExpiresIsNow()
    {
        var controller = CreateController();
        var settings = ValidEnabledSettings() with { Expires = DateTimeOffset.UtcNow };

        var result = await controller.SetGlobalSecurityTxt(settings, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task SetGlobalSecurityTxt_ReturnsNoContent_WhenEnabledAndAllRequiredFieldsValid()
    {
        var settingsMock = new Mock<ISettingsService>();
        settingsMock.Setup(s => s.SetSecurityTxtSettingsAsync(It.IsAny<SecurityTxtSettings>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var controller = CreateController(settingsMock);
        var settings = ValidEnabledSettings();

        var result = await controller.SetGlobalSecurityTxt(settings, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task SetGlobalSecurityTxt_ReturnsNoContent_WhenDisabledWithNoRequiredFields()
    {
        var settingsMock = new Mock<ISettingsService>();
        settingsMock.Setup(s => s.SetSecurityTxtSettingsAsync(It.IsAny<SecurityTxtSettings>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var controller = CreateController(settingsMock);
        var settings = new SecurityTxtSettings(
            Enabled: false,
            Contact: null,
            Expires: null,
            Encryption: null,
            Acknowledgments: null,
            PreferredLanguages: null,
            Canonical: null,
            Policy: null,
            Hiring: null);

        var result = await controller.SetGlobalSecurityTxt(settings, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task SetGlobalSecurityTxt_SkipsValidation_WhenDisabledEvenIfContactMissing()
    {
        var settingsMock = new Mock<ISettingsService>();
        settingsMock.Setup(s => s.SetSecurityTxtSettingsAsync(It.IsAny<SecurityTxtSettings>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var controller = CreateController(settingsMock);
        var settings = new SecurityTxtSettings(
            Enabled: false,
            Contact: null,
            Expires: null,
            Encryption: null,
            Acknowledgments: null,
            PreferredLanguages: null,
            Canonical: null,
            Policy: null,
            Hiring: null);

        var result = await controller.SetGlobalSecurityTxt(settings, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        settingsMock.Verify(s => s.SetSecurityTxtSettingsAsync(It.IsAny<SecurityTxtSettings>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
