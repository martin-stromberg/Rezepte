using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rezepte.Web.Services;
using System.Security.Claims;

namespace Rezepte.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class CookbooksController(ICookbookService cookbooks) : ControllerBase
{
    private readonly ICookbookService _cookbooks = cookbooks;

    private string? GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        var list = await _cookbooks.GetAllAsync(userId, ct);
        return Ok(list);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCookbookRequest dto, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        if (string.IsNullOrWhiteSpace(dto.Name) || dto.Name.Length < 3)
            return BadRequest(new { message = "Der Name muss mindestens 3 Zeichen haben." });

        var (ok, error, entity) = await _cookbooks.CreateAsync(userId, dto.Name, dto.Description, ct);
        if (!ok || entity is null)
            return BadRequest(new { message = error ?? "Anlegen fehlgeschlagen." });
        return Ok(entity);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        var entity = await _cookbooks.GetByIdAsync(userId, id, ct);
        if (entity is null) return NotFound();
        return Ok(entity);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateCookbookRequest dto, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        if (string.IsNullOrWhiteSpace(dto.Name) || dto.Name.Length < 3)
            return BadRequest(new { message = "Der Name muss mindestens 3 Zeichen haben." });

        var (ok, error) = await _cookbooks.UpdateAsync(userId, id, dto.Name, dto.Description, ct);
        if (!ok)
            return BadRequest(new { message = error ?? "Speichern fehlgeschlagen." });
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        var (ok, error) = await _cookbooks.DeleteAsync(userId, id, ct);
        if (!ok)
            return BadRequest(new { message = error ?? "Löschen fehlgeschlagen." });
        return NoContent();
    }

    public record CreateCookbookRequest(string Name, string? Description);
    public record UpdateCookbookRequest(string Name, string? Description);
}
