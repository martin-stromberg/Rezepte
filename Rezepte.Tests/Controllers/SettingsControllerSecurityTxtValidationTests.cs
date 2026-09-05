using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Rezepte.Web.Controllers;
using Rezepte.Web.Dtos;
using Rezepte.Web.Services;
using Xunit;

namespace Rezepte.Tests.Controllers;

/// <summary>
/// Class representing the settings controller security txt validation tests.
/// </summary>
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

    /// <summary>
    /// Set global security txt returns bad request when enabled and contact missing.
    /// </summary>
    [Fact]
    public async Task SetGlobalSecurityTxt_ReturnsBadRequest_WhenEnabledAndContactMissing()
    {
        var controller = CreateController();
        var settings = ValidEnabledSettings() with { Contact = null };

        var result = await controller.SetGlobalSecurityTxt(settings, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Set global security txt returns bad request when enabled and contact empty.
    /// </summary>
    [Fact]
    public async Task SetGlobalSecurityTxt_ReturnsBadRequest_WhenEnabledAndContactEmpty()
    {
        var controller = CreateController();
        var settings = ValidEnabledSettings() with { Contact = "   " };

        var result = await controller.SetGlobalSecurityTxt(settings, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Set global security txt returns bad request when enabled and expires missing.
    /// </summary>
    [Fact]
    public async Task SetGlobalSecurityTxt_ReturnsBadRequest_WhenEnabledAndExpiresMissing()
    {
        var controller = CreateController();
        var settings = ValidEnabledSettings() with { Expires = null };

        var result = await controller.SetGlobalSecurityTxt(settings, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Set global security txt returns bad request when enabled and expires in past.
    /// </summary>
    [Fact]
    public async Task SetGlobalSecurityTxt_ReturnsBadRequest_WhenEnabledAndExpiresInPast()
    {
        var controller = CreateController();
        var settings = ValidEnabledSettings() with { Expires = DateTimeOffset.UtcNow.AddDays(-1) };

        var result = await controller.SetGlobalSecurityTxt(settings, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Set global security txt returns bad request when enabled and expires is now.
    /// </summary>
    [Fact]
    public async Task SetGlobalSecurityTxt_ReturnsBadRequest_WhenEnabledAndExpiresIsNow()
    {
        var controller = CreateController();
        var settings = ValidEnabledSettings() with { Expires = DateTimeOffset.UtcNow };

        var result = await controller.SetGlobalSecurityTxt(settings, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Set global security txt returns no content when enabled and all required fields valid.
    /// </summary>
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

    /// <summary>
    /// Set global security txt returns no content when disabled with no required fields.
    /// </summary>
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

    /// <summary>
    /// Set global security txt returns bad request when settings is null.
    /// </summary>
    [Fact]
    public async Task SetGlobalSecurityTxt_ReturnsBadRequest_WhenSettingsIsNull()
    {
        var controller = CreateController();

        var result = await controller.SetGlobalSecurityTxt(null!, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Set global security txt skips validation when disabled even if contact missing.
    /// </summary>
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

    /// <summary>
    /// Set global security txt preserves canonical from request.
    /// </summary>
    [Fact]
    public async Task SetGlobalSecurityTxt_PreservesCanonicalFromRequest()
    {
        var settingsMock = new Mock<ISettingsService>();
        settingsMock.Setup(s => s.SetSecurityTxtSettingsAsync(It.IsAny<SecurityTxtSettings>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var controller = CreateController(settingsMock);
        var settings = ValidEnabledSettings() with { Canonical = "https://example.com/admin-value" };

        var result = await controller.SetGlobalSecurityTxt(settings, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        settingsMock.Verify(s => s.SetSecurityTxtSettingsAsync(
                It.Is<SecurityTxtSettings>(x => x.Canonical == "https://example.com/admin-value"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
