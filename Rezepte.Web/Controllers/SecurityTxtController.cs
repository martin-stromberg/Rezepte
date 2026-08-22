using Microsoft.AspNetCore.Mvc;
using Rezepte.Web.Dtos;
using Rezepte.Web.Services;

namespace Rezepte.Web.Controllers;

[ApiController]
public class SecurityTxtController : ControllerBase
{
    private readonly ISettingsService _settings;
    private readonly ISecurityTxtRenderer _renderer;

    public SecurityTxtController(ISettingsService settings, ISecurityTxtRenderer renderer)
    {
        _settings = settings;
        _renderer = renderer;
    }

    [HttpGet("/security.txt")]
    [HttpGet("/.well-known/security.txt")]
    public Task<IActionResult> GetSecurityTxt(CancellationToken ct) =>
        RenderIfEnabledAsync(ct, _renderer.RenderPlainText, "text/plain; charset=utf-8", "/security.txt");

    [HttpGet("/.well-known/security.md")]
    public Task<IActionResult> GetSecurityMd(CancellationToken ct) =>
        RenderIfEnabledAsync(ct, _renderer.RenderMarkdown, "text/markdown; charset=utf-8", "/.well-known/security.md");

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
