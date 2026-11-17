using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Rezepte.Tests.TestSupport;
using Rezepte.Web.Controllers;
using Rezepte.Web.Services;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace Rezepte.Tests.Controllers;

/// <summary>
/// Class representing the settings controller share link validity tests.
/// </summary>
public class SettingsControllerShareLinkValidityTests
{
    private static SettingsController CreateController(Mock<ISettingsService>? settingsMock = null)
    {
        settingsMock ??= new Mock<ISettingsService>();
        var googleMock = new Mock<IGoogleCredentialsProvider>();
        return new SettingsController(settingsMock.Object, googleMock.Object);
    }

    /// <summary>
    /// Get global share link validity should return the configured days.
    /// </summary>
    [Fact]
    public async Task GetGlobalShareLinkValidity_ReturnsConfiguredDays()
    {
        var settingsMock = new Mock<ISettingsService>();
        settingsMock.Setup(s => s.GetShareLinkValidityDaysAsync(It.IsAny<CancellationToken>())).ReturnsAsync(14);
        var controller = CreateController(settingsMock);

        var result = await controller.GetGlobalShareLinkValidity(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(14);
    }

    /// <summary>
    /// Set global share link validity should return bad request when value is zero or negative.
    /// </summary>
    /// <param name="days">The invalid validity days value.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public async Task SetGlobalShareLinkValidity_ReturnsBadRequest_WhenValueIsZeroOrNegative(int days)
    {
        var settingsMock = new Mock<ISettingsService>();
        var controller = CreateController(settingsMock);

        var result = await controller.SetGlobalShareLinkValidity(days, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        settingsMock.Verify(s => s.SetShareLinkValidityDaysAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Set global share link validity should return no content when value is valid.
    /// </summary>
    [Fact]
    public async Task SetGlobalShareLinkValidity_ReturnsNoContent_WhenValueIsValid()
    {
        var settingsMock = new Mock<ISettingsService>();
        settingsMock.Setup(s => s.SetShareLinkValidityDaysAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var controller = CreateController(settingsMock);

        var result = await controller.SetGlobalShareLinkValidity(45, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        settingsMock.Verify(s => s.SetShareLinkValidityDaysAsync(45, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// The share link validity endpoints should return unauthorized without authentication.
    /// </summary>
    [Fact]
    public async Task ShareLinkValidityEndpoints_ReturnUnauthorized_WithoutAuthentication()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateAnonymousClient();

        using var getResponse = await client.GetAsync("/api/settings/global/sharelinkvalidity");
        using var putResponse = await client.PutAsJsonAsync("/api/settings/global/sharelinkvalidity", 30);

        getResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        putResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// The share link validity endpoints should return forbidden for an authenticated non-admin user.
    /// </summary>
    [Fact]
    public async Task ShareLinkValidityEndpoints_ReturnForbidden_ForNonAdminUser()
    {
        await using var factory = CreateFactory();
        using var client = CreateAuthenticatedClient(factory, isAdmin: false);

        using var getResponse = await client.GetAsync("/api/settings/global/sharelinkvalidity");
        using var putResponse = await client.PutAsJsonAsync("/api/settings/global/sharelinkvalidity", 30);

        getResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        putResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// The share link validity endpoints should succeed for an admin user.
    /// </summary>
    [Fact]
    public async Task ShareLinkValidityEndpoints_Succeed_ForAdminUser()
    {
        await using var factory = CreateFactory();
        using var client = CreateAuthenticatedClient(factory, isAdmin: true);

        using var putResponse = await client.PutAsJsonAsync("/api/settings/global/sharelinkvalidity", 45);
        using var getResponse = await client.GetAsync("/api/settings/global/sharelinkvalidity");

        putResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await getResponse.Content.ReadFromJsonAsync<int>()).Should().Be(45);
    }

    private static SqliteFileWebApplicationFactory CreateFactory()
        => new(Path.Combine(Path.GetTempPath(), $"rezepte-settings-sharelink-{Guid.NewGuid():N}.db"));

    private static HttpClient CreateAuthenticatedClient(SqliteFileWebApplicationFactory factory, bool isAdmin)
    {
        var tokenService = factory.Services.GetRequiredService<ITokenService>();
        var token = tokenService.CreateToken($"user-{Guid.NewGuid():N}", "test-user", isAdmin);
        var client = factory.CreateAnonymousClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
