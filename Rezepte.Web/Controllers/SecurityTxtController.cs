using Microsoft.AspNetCore.Mvc;
using Rezepte.Web.Dtos;
using Rezepte.Web.Services;

namespace Rezepte.Web.Controllers;

/// <summary>
/// Represents the security txt controller class.
/// </summary>
[ApiController]
public class SecurityTxtController : ControllerBase
{
    private readonly ISettingsService _settings;
    private readonly ISecurityTxtRenderer _renderer;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecurityTxtController"/> class.
    /// </summary>
    /// <param name="settings">The settings parameter.</param>
    /// <param name="renderer">The renderer parameter.</param>
    public SecurityTxtController(ISettingsService settings, ISecurityTxtRenderer renderer)
    {
        _settings = settings;
        _renderer = renderer;
    }

    /// <summary>
    /// Gets the security txt.
    /// </summary>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    [HttpGet("/security.txt")]
    [HttpGet("/.well-known/security.txt")]
    public Task<IActionResult> GetSecurityTxt(CancellationToken ct) =>
        RenderIfEnabledAsync(ct, _renderer.RenderPlainText, "text/plain; charset=utf-8", "/security.txt");

    /// <summary>
    /// Gets the security md.
    /// </summary>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    [HttpGet("/.well-known/security.md")]
    public Task<IActionResult> GetSecurityMd(CancellationToken ct) =>
        RenderIfEnabledAsync(ct, _renderer.RenderMarkdown, "text/markdown; charset=utf-8", "/.well-known/security.md");

    /// <summary>
    /// Gets the security html.
    /// </summary>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    [HttpGet("/.well-known/security.html")]
    public Task<IActionResult> GetSecurityHtml(CancellationToken ct) =>
        RenderIfEnabledAsync(ct, _renderer.RenderHtml, "text/html; charset=utf-8", "/.well-known/security.html");

    private async Task<IActionResult> RenderIfEnabledAsync(
        CancellationToken ct,
        Func<SecurityTxtSettings, string> render,
        string contentType,
        string canonicalPath)
    {
        var settings = await _settings.GetSecurityTxtSettingsAsync(ct);
        if (!settings.Enabled) return NotFound();

        var canonicalUrl = !string.IsNullOrWhiteSpace(settings.Canonical)
            ? settings.Canonical
            : $"{Request.Scheme}://{Request.Host}{Request.PathBase}{canonicalPath}";
        var effectiveSettings = settings with { Canonical = canonicalUrl };

        return Content(render(effectiveSettings), contentType);
    }
}
