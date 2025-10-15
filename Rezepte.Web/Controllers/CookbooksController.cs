using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rezepte.Web.Entities;
using Rezepte.Web.Services;
using Rezepte.Web.Services.Import;
using System.Security.Claims;
using static Rezepte.Web.Controllers.RecipesController;

namespace Rezepte.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class CookbooksController(ICookbookService cookbooks, IRecipeService recipes) : ControllerBase
{
    private readonly ICookbookService _cookbooks = cookbooks;
    private readonly IRecipeService _recipes = recipes;

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

    [HttpPost("{cookbookId}/recipes")]
    public async Task<IActionResult> AddRecipesToCookbook(string cookbookId, [FromBody] List<string> recipeIds, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var (ok, error, created) = await _recipes.AddExistingToCookbookAsync(userId, cookbookId, recipeIds, ct);
        if (!ok) return BadRequest(new { message = error ?? "Hinzufügen fehlgeschlagen." });

        var dtos = created.Select(r => new RecipeListItemDto(r.Id, r.Title, null, r.Description)).ToList();
        return Ok(dtos);
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

    [HttpDelete("{cookbookId}/recipes/{recipeId}")]
    public async Task<IActionResult> RemoveRecipeFromCookbook(string cookbookId, string recipeId, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        // Verwende den Service statt direkten DbContext‑Zugriff
        var (ok, error) = await _recipes.RemoveFromCookbookAsync(userId, cookbookId, recipeId, ct);
        if (!ok) return BadRequest(new { message = error ?? "Entfernen fehlgeschlagen." });

        return NoContent();
    }

    [HttpPost("{cookbookId}/import")]
    public async Task<IActionResult> ImportFromFile(string cookbookId, IFormFile file, [FromServices] IImportService importService, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        if (file == null || file.Length == 0) return BadRequest(new { message = "No file uploaded." });

        // Prüfe Besitz / Existenz des Kochbuchs
        var cookbook = await _cookbooks.GetByIdAsync(userId, cookbookId, ct);
        if (cookbook is null) return NotFound(new { message = "Cookbook not found." });

        try
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms, ct).ConfigureAwait(false);
            ms.Seek(0, SeekOrigin.Begin);

            var result = await importService.ImportAsync(ms, file.FileName, cookbookId, userId, ct).ConfigureAwait(false);
            if (!result.Success)
                return BadRequest(new { message = result.Error });

            return Ok(new { created = result.CreatedRecipeIds });
        }
        catch (OperationCanceledException)
        {
            return StatusCode(499);
        }
        catch (Exception ex)
        {
            // Import errors mapped to ProblemDetails by controller pipeline, but log something if desired
            return Problem(title: "Import failed", detail: ex.Message, statusCode: 500);
        }
    }

    [HttpPost("{cookbookId}/import-url")]
    public async Task<IActionResult> ImportFromUrl(string cookbookId, [FromBody] ImportUrlRequest request, [FromServices] IImportService importService, [FromServices] IHttpClientFactory httpClientFactory, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        if (request == null || string.IsNullOrWhiteSpace(request.Url)) return BadRequest(new { message = "No URL provided." });

        // Prüfe Besitz / Existenz des Kochbuchs
        var cookbook = await _cookbooks.GetByIdAsync(userId, cookbookId, ct);
        if (cookbook is null) return NotFound(new { message = "Cookbook not found." });

        if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            return BadRequest(new { message = "Invalid URL. Only http(s) URLs are supported." });

        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);

        try
        {
            using var resp = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
                return BadRequest(new { message = $"Remote request failed: {resp.StatusCode}" });

            // Copy to MemoryStream because handlers may need seekable stream
            await using var ms = new MemoryStream();
            await using var remoteStream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            await remoteStream.CopyToAsync(ms, ct).ConfigureAwait(false);
            ms.Seek(0, SeekOrigin.Begin);

            // Try to infer filename from URL or content-disposition
            var fileName = Path.GetFileName(uri.LocalPath);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                if (resp.Content?.Headers?.ContentDisposition?.FileNameStar is string fnStar && !string.IsNullOrWhiteSpace(fnStar))
                    fileName = fnStar.Trim('"');
                else if (resp.Content?.Headers?.ContentDisposition?.FileName is string fn && !string.IsNullOrWhiteSpace(fn))
                    fileName = fn.Trim('"');
                else
                    fileName = "import-from-url";
            }

            var result = await importService.ImportAsync(ms, fileName, cookbookId, userId, ct).ConfigureAwait(false);
            if (!result.Success)
                return BadRequest(new { message = result.Error });

            return Ok(new { created = result.CreatedRecipeIds });
        }
        catch (OperationCanceledException)
        {
            return StatusCode(499);
        }
        catch (Exception ex)
        {
            return Problem(title: "Import failed", detail: ex.Message, statusCode: 500);
        }
    }

    [HttpPost("import")]
    public async Task<IActionResult> ImportFromFile(IFormFile file, [FromServices] IImportService importService, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        if (file == null || file.Length == 0) return BadRequest(new { message = "No file uploaded." });

        try
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms, ct).ConfigureAwait(false);
            ms.Seek(0, SeekOrigin.Begin);

            var result = await importService.ImportAsync(ms, file.FileName, null, userId, ct).ConfigureAwait(false);
            if (!result.Success)
                return BadRequest(new { message = result.Error });

            return Ok(new { created = result.CreatedRecipeIds });
        }
        catch (OperationCanceledException)
        {
            return StatusCode(499);
        }
        catch (Exception ex)
        {
            // Import errors mapped to ProblemDetails by controller pipeline, but log something if desired
            return Problem(title: "Import failed", detail: ex.Message, statusCode: 500);
        }
    }
    [HttpPost("import-url")]
    public async Task<IActionResult> ImportFromUrl([FromBody] ImportUrlRequest request, [FromServices] IImportService importService, [FromServices] IHttpClientFactory httpClientFactory, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        if (request == null || string.IsNullOrWhiteSpace(request.Url)) return BadRequest(new { message = "No URL provided." });

        
        if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            return BadRequest(new { message = "Invalid URL. Only http(s) URLs are supported." });

        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);

        try
        {
            using var resp = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
                return BadRequest(new { message = $"Remote request failed: {resp.StatusCode}" });

            // Copy to MemoryStream because handlers may need seekable stream
            await using var ms = new MemoryStream();
            await using var remoteStream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            await remoteStream.CopyToAsync(ms, ct).ConfigureAwait(false);
            ms.Seek(0, SeekOrigin.Begin);

            // Try to infer filename from URL or content-disposition
            var fileName = Path.GetFileName(uri.LocalPath);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                if (resp.Content?.Headers?.ContentDisposition?.FileNameStar is string fnStar && !string.IsNullOrWhiteSpace(fnStar))
                    fileName = fnStar.Trim('"');
                else if (resp.Content?.Headers?.ContentDisposition?.FileName is string fn && !string.IsNullOrWhiteSpace(fn))
                    fileName = fn.Trim('"');
                else
                    fileName = "import-from-url";
            }

            var result = await importService.ImportAsync(ms, fileName, null, userId, ct).ConfigureAwait(false);
            if (!result.Success)
                return BadRequest(new { message = result.Error });

            return Ok(new { created = result.CreatedRecipeIds });
        }
        catch (OperationCanceledException)
        {
            return StatusCode(499);
        }
        catch (Exception ex)
        {
            return Problem(title: "Import failed", detail: ex.Message, statusCode: 500);
        }
    }

    [HttpPost("reorder")]
    public async Task<IActionResult> Reorder([FromBody] List<string>? orderedIds, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        if (orderedIds == null || orderedIds.Count == 0)
            return BadRequest(new { message = "Keine Reihenfolge übergeben." });

        var (ok, error) = await _cookbooks.ReorderAsync(userId, orderedIds, ct);
        if (!ok) return BadRequest(new { message = error ?? "Reihenfolge konnte nicht gespeichert werden." });

        return Ok();
    }

    public record CreateCookbookRequest(string Name, string? Description);
    public record UpdateCookbookRequest(string Name, string? Description);
    public record ImportUrlRequest(string Url);
}
