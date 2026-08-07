using Microsoft.AspNetCore.Mvc;
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
    public async Task<IActionResult> GetSecurityTxt(CancellationToken ct)
    {
        var settings = await _settings.GetSecurityTxtSettingsAsync(ct);
        if (!settings.Enabled) return NotFound();
        return Content(_renderer.RenderPlainText(settings), "text/plain; charset=utf-8");
    }

    [HttpGet("/.well-known/security.md")]
    public async Task<IActionResult> GetSecurityMd(CancellationToken ct)
    {
        var settings = await _settings.GetSecurityTxtSettingsAsync(ct);
        if (!settings.Enabled) return NotFound();
        return Content(_renderer.RenderMarkdown(settings), "text/markdown; charset=utf-8");
    }

    [HttpGet("/.well-known/security.html")]
    public async Task<IActionResult> GetSecurityHtml(CancellationToken ct)
    {
        var settings = await _settings.GetSecurityTxtSettingsAsync(ct);
        if (!settings.Enabled) return NotFound();
        return Content(_renderer.RenderHtml(settings), "text/html; charset=utf-8");
    }
}
