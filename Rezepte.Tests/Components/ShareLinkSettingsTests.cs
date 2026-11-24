using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Rezepte.Tests.ViewModels;
using Rezepte.Web.Components.Settings;
using System.Net;
using Xunit;

namespace Rezepte.Tests.Components;

/// <summary>
/// Class representing the share link settings tests.
/// </summary>
public sealed class ShareLinkSettingsTests : IDisposable
{
    private readonly BunitContext _context = new();

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public ShareLinkSettingsTests()
    {
        _context.Services.AddLocalization();
    }

    /// <summary>
    /// Loads the current validity days into the input.
    /// </summary>
    [Fact]
    public void LoadsCurrentValue()
    {
        var factory = new ApiClientTestFactory(_ => ApiClientTestFactory.Json(HttpStatusCode.OK, "5"));
        _context.Services.AddSingleton(factory.Create());

        var cut = _context.Render<ShareLinkSettings>();

        cut.WaitForAssertion(() => cut.Find("#sharelink-validity-days"));
        cut.Find("#sharelink-validity-days").GetAttribute("value").Should().Be("5");
    }

    /// <summary>
    /// Saves the validity days via put request.
    /// </summary>
    [Fact]
    public void SavesValue_ViaPutRequest()
    {
        string? requestBody = null;
        var factory = new ApiClientTestFactory(async request =>
        {
            if (request.Method == HttpMethod.Get)
            {
                return ApiClientTestFactory.Json(HttpStatusCode.OK, "30");
            }

            requestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });
        _context.Services.AddSingleton(factory.Create());

        var cut = _context.Render<ShareLinkSettings>();
        cut.WaitForAssertion(() => cut.Find("#sharelink-validity-days"));

        cut.Find("#sharelink-validity-days").Change("14");
        cut.Find("button.btn-primary").Click();

        cut.WaitForAssertion(() => cut.Find(".alert-success"));
        factory.Requests.Should().HaveCount(2);
        factory.Requests[1].Method.Should().Be(HttpMethod.Put);
        factory.Requests[1].RequestUri!.ToString().Should().Be("http://localhost/api/settings/global/sharelinkvalidity");
        requestBody.Should().Be("14");
    }

    /// <summary>
    /// Shows an error when the save request fails.
    /// </summary>
    [Fact]
    public void ShowsError_WhenSaveFails()
    {
        var factory = new ApiClientTestFactory(request =>
            request.Method == HttpMethod.Get
                ? ApiClientTestFactory.Json(HttpStatusCode.OK, "30")
                : new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("invalid") });
        _context.Services.AddSingleton(factory.Create());

        var cut = _context.Render<ShareLinkSettings>();
        cut.WaitForAssertion(() => cut.Find("#sharelink-validity-days"));

        cut.Find("button.btn-primary").Click();

        cut.WaitForAssertion(() => cut.Find(".alert-danger"));
    }

    /// <summary>
    /// Dispose.
    /// </summary>
    public void Dispose()
    {
        _context.Dispose();
    }
}
