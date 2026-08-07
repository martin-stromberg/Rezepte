using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rezepte.Web.Entities;
using Rezepte.Web.Extensions;
using Rezepte.Web.Services;
using Rezepte.Web.Services.Http;
using Rezepte.Web.Services.Import;
using static Rezepte.Web.Controllers.RecipesController;

namespace Rezepte.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
// allow either Bearer *or* cookie auth for browser uploads (so fetch with cookies or short-lived JWT works)
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme + "," + Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)]
public class CookbooksController(ICookbookService cookbooks, IRecipeService recipes, IRemoteContentFetcher remoteContent) : ApiControllerBase
{
    private readonly ICookbookService _cookbooks = cookbooks;
    private readonly IRecipeService _recipes = recipes;
    private readonly IRemoteContentFetcher _remoteContent = remoteContent;

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
        if (!ok) return BadRequest(new { message = error ?? "Löschen fehlgeschlagen." });
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
            await using var ms = await file.ReadToMemoryStreamAsync(ct);

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
    public async Task<IActionResult> ImportFromUrl(string cookbookId, [FromBody] ImportUrlRequest request, [FromServices] IImportService importService, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        if (request == null || string.IsNullOrWhiteSpace(request.Url)) return BadRequest(new { message = "No URL provided." });

        // Prüfe Besitz / Existenz des Kochbuchs
        var cookbook = await _cookbooks.GetByIdAsync(userId, cookbookId, ct);
        if (cookbook is null) return NotFound(new { message = "Cookbook not found." });

        return await ImportFromUrlAsync(request.Url, cookbookId, importService, userId, ct);
    }

    [HttpPost("import")]
    [RequestSizeLimit(524288000)] // 500 MB limit, anpassen nach Bedarf
    [RequestFormLimits(MultipartBodyLengthLimit = 524288000)]
    public async Task<IActionResult> ImportFromFile(IFormFile file, [FromServices] IImportService importService, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        if (file == null || file.Length == 0) return BadRequest(new { message = "No file uploaded." });

        try
        {
            await using var ms = await file.ReadToMemoryStreamAsync(ct);

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
    public async Task<IActionResult> ImportFromUrl([FromBody] ImportUrlRequest request, [FromServices] IImportService importService, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        if (request == null || string.IsNullOrWhiteSpace(request.Url)) return BadRequest(new { message = "No URL provided." });

        return await ImportFromUrlAsync(request.Url, null, importService, userId, ct);
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

    // --- Ergänzungen innerhalb existing controller (neue Endpoints) ---
    // POST api/cookbooks/{cookbookId}/import-session/start
    [HttpPost("{cookbookId}/import-session/start")]
    public async Task<IActionResult> StartImportSession(string cookbookId, [FromBody] ImportUrlRequest request, [FromServices] ImportOrchestrator orchestrator, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        if (request == null || string.IsNullOrWhiteSpace(request.Url)) return BadRequest(new { message = "No URL provided." });

        return await StartImportSessionFromUrlAsync(request.Url, cookbookId, orchestrator, userId, ct);
    }

    // GET api/cookbooks/{cookbookId}/import-session/{sessionId}/status
    [HttpGet("{cookbookId}/import-session/{sessionId}/status")]
    public IActionResult GetImportSessionStatus(string cookbookId, string sessionId, [FromServices] ImportOrchestrator orchestrator)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var session = orchestrator.GetSessionForUser(sessionId, userId);
        if (session == null) return NotFound();
        return Ok(ToSessionStatus(session));
    }

    // POST api/cookbooks/{cookbookId}/import-session/{sessionId}/confirm
    [HttpPost("{cookbookId}/import-session/{sessionId}/confirm")]
    public IActionResult ConfirmImportSession(string cookbookId, string sessionId, [FromBody] ConfirmRequest req, [FromServices] ImportOrchestrator orchestrator)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var ok = orchestrator.Confirm(sessionId, userId, req.Accepted);
        if (!ok) return NotFound();
        return NoContent();
    }

    // POST api/cookbooks/{cookbookId}/import-session/{sessionId}/selection
    [HttpPost("{cookbookId}/import-session/{sessionId}/selection")]
    public async Task<IActionResult> SubmitImportSessionSelection(string cookbookId, string sessionId, [FromBody] ImportCollectionSelectionRequest req, [FromServices] ImportOrchestrator orchestrator, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        var validation = await ValidateSelectionCookbooksAsync(userId, req, ct);
        if (validation is not null) return validation;

        var result = orchestrator.SubmitSelection(sessionId, userId, ToSelection(req));
        if (result.IsNotFound) return NotFound();
        if (!result.Success) return BadRequest(new { message = result.Error });
        return NoContent();
    }

    // POST api/cookbooks/{cookbookId}/import-session/{sessionId}/selection/cancel
    [HttpPost("{cookbookId}/import-session/{sessionId}/selection/cancel")]
    public IActionResult CancelImportSessionSelection(string cookbookId, string sessionId, [FromServices] ImportOrchestrator orchestrator)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var result = orchestrator.CancelSelection(sessionId, userId);
        if (result.IsNotFound) return NotFound();
        if (!result.Success) return BadRequest(new { message = result.Error });
        return NoContent();
    }

    // --- Neue Endpoints ohne cookbookId, damit der Blazor-Client die session-basierten Aufrufe auch ohne Cookbook nutzt ---
    // POST api/cookbooks/import-session/start
    [HttpPost("import-session/start")]
    public async Task<IActionResult> StartImportSessionNoCookbook([FromBody] ImportUrlRequest request, [FromServices] ImportOrchestrator orchestrator, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        if (request == null || string.IsNullOrWhiteSpace(request.Url)) return BadRequest(new { message = "No URL provided." });

        return await StartImportSessionFromUrlAsync(request.Url, null, orchestrator, userId, ct);
    }

    // GET api/cookbooks/import-session/{sessionId}/status
    [HttpGet("import-session/{sessionId}/status")]
    public IActionResult GetImportSessionStatusNoCookbook(string sessionId, [FromServices] ImportOrchestrator orchestrator)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var session = orchestrator.GetSessionForUser(sessionId, userId);
        if (session == null) return NotFound();
        return Ok(ToSessionStatus(session));
    }

    // POST api/cookbooks/import-session/{sessionId}/confirm
    [HttpPost("import-session/{sessionId}/confirm")]
    public IActionResult ConfirmImportSessionNoCookbook(string sessionId, [FromBody] ConfirmRequest req, [FromServices] ImportOrchestrator orchestrator)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var ok = orchestrator.Confirm(sessionId, userId, req.Accepted);
        if (!ok) return NotFound();
        return NoContent();
    }

    // POST api/cookbooks/import-session/{sessionId}/selection
    [HttpPost("import-session/{sessionId}/selection")]
    public async Task<IActionResult> SubmitImportSessionSelectionNoCookbook(string sessionId, [FromBody] ImportCollectionSelectionRequest req, [FromServices] ImportOrchestrator orchestrator, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        var validation = await ValidateSelectionCookbooksAsync(userId, req, ct);
        if (validation is not null) return validation;

        var result = orchestrator.SubmitSelection(sessionId, userId, ToSelection(req));
        if (result.IsNotFound) return NotFound();
        if (!result.Success) return BadRequest(new { message = result.Error });
        return NoContent();
    }

    // POST api/cookbooks/import-session/{sessionId}/selection/cancel
    [HttpPost("import-session/{sessionId}/selection/cancel")]
    public IActionResult CancelImportSessionSelectionNoCookbook(string sessionId, [FromServices] ImportOrchestrator orchestrator)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var result = orchestrator.CancelSelection(sessionId, userId);
        if (result.IsNotFound) return NotFound();
        if (!result.Success) return BadRequest(new { message = result.Error });
        return NoContent();
    }

    // --- Neue Endpoints: Starten einer Import-Session aus einer hochgeladenen Datei ---
    // POST api/cookbooks/{cookbookId}/import-session/start-file
    [HttpPost("{cookbookId}/import-session/start-file")]
    [RequestSizeLimit(524288000)] // 500 MB
    [RequestFormLimits(MultipartBodyLengthLimit = 524288000)]
    public async Task<IActionResult> StartImportSessionFromFile(string cookbookId, [FromForm(Name = "file")] IFormFile? file, [FromServices] ImportOrchestrator orchestrator, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        if (file == null || file.Length == 0) return BadRequest(new { message = "No file uploaded." });

        // Prüfe Besitz / Existenz des Kochbuchs
        var cookbook = await _cookbooks.GetByIdAsync(userId, cookbookId, ct);
        if (cookbook is null) return NotFound(new { message = "Cookbook not found." });

        try
        {
            await using var ms = await file.ReadToMemoryStreamAsync(ct);

            return await StartImportSessionFromStreamAsync(ms, file.FileName, null, cookbookId, orchestrator, userId, ct);
        }
        catch (OperationCanceledException)
        {
            return StatusCode(499);
        }
        catch (Exception ex)
        {
            return Problem(title: "Start import session failed", detail: ex.Message, statusCode: 500);
        }
    }

    // POST api/cookbooks/import-session/start-file
    [HttpPost("import-session/start-file")]
    [RequestSizeLimit(524288000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 524288000)]
    public async Task<IActionResult> StartImportSessionFromFileNoCookbook([FromForm(Name = "file")] IFormFile? file, [FromServices] ImportOrchestrator orchestrator, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        if (file == null || file.Length == 0) return BadRequest(new { message = "No file uploaded." });

        try
        {
            await using var ms = await file.ReadToMemoryStreamAsync(ct);

            return await StartImportSessionFromStreamAsync(ms, file.FileName, null, null, orchestrator, userId, ct);
        }
        catch (OperationCanceledException)
        {
            return StatusCode(499);
        }
        catch (Exception ex)
        {
            return Problem(title: "Start import session failed", detail: ex.Message, statusCode: 500);
        }
    }

    // private helper that centralizes importing the content behind a remote URL
    private async Task<IActionResult> ImportFromUrlAsync(string url, string? cookbookId, IImportService importService, string userId, CancellationToken ct)
    {
        if (!RemoteContentFetcher.TryCreateHttpUri(url, out var uri))
            return BadRequest(new { message = "Invalid URL. Only http(s) URLs are supported." });

        try
        {
            var fetched = await _remoteContent.FetchAsync(uri, ct);
            if (!fetched.Success)
                return BadRequest(new { message = $"Remote request failed: {fetched.StatusCode}" });

            await using var ms = fetched.Content!;
            var result = await importService.ImportAsync(ms, fetched.FileName, cookbookId, userId, ct).ConfigureAwait(false);
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

    // private helper that centralizes starting an import session from a remote URL
    private async Task<IActionResult> StartImportSessionFromUrlAsync(string url, string? cookbookId, ImportOrchestrator orchestrator, string userId, CancellationToken ct)
    {
        if (!RemoteContentFetcher.TryCreateHttpUri(url, out var uri))
            return BadRequest(new { message = "Invalid URL. Only http(s) URLs are supported." });

        var fetched = await _remoteContent.FetchAsync(uri, ct);
        if (!fetched.Success)
            return BadRequest(new { message = $"Remote request failed: {fetched.StatusCode}", detail = fetched.ErrorBody });

        await using var ms = fetched.Content!;
        return await StartImportSessionFromStreamAsync(ms, fetched.FileName, uri.ToString(), cookbookId, orchestrator, userId, ct);
    }

    // private helper that centralizes starting an import session from an already-read stream
    private async Task<IActionResult> StartImportSessionFromStreamAsync(Stream ms, string fileName, string? uri, string? cookbookId, ImportOrchestrator orchestrator, string userId, CancellationToken ct)
    {
        if (ms == null) return BadRequest(new { message = "No stream provided." });
        if (string.IsNullOrWhiteSpace(fileName)) fileName = "import-from-stream";

        try
        {
            // Ensure stream position is at start for orchestrator
            if (ms.CanSeek) ms.Seek(0, SeekOrigin.Begin);

            var sessionId = await orchestrator.StartImportAsync(ms, fileName, uri, cookbookId, userId, ct).ConfigureAwait(false);
            return Ok(new { sessionId });
        }
        catch (OperationCanceledException)
        {
            return StatusCode(499);
        }
        catch (Exception ex)
        {
            return Problem(title: "Start import session failed", detail: ex.Message, statusCode: 500);
        }
    }

    private object ToSessionStatus(ImportOrchestrator.ImportSession session)
    {
        return new
        {
            status = session.Status,
            waitingForConfirmation = session.WaitingForConfirmation,
            confirmationPrompt = session.ConfirmationPrompt,
            result = session.Result != null ? new { success = session.Result.Success, error = session.Result.Error, created = session.Result.CreatedRecipeIds } : null,
            state = session.State,
            readOnly = session.ReadOnly,
            collection = session.CollectionPreview is null
                ? null
                : new
                {
                    id = session.CollectionPreview.Id,
                    title = session.CollectionPreview.Title,
                    sourceUri = session.CollectionPreview.SourceUri,
                    items = session.CollectionPreview.Items.Select(i => new
                    {
                        id = i.Id,
                        title = i.Title,
                        url = i.Url,
                        thumbnailUrl = i.ThumbnailUrl,
                        description = i.Description
                    })
                },
            items = session.CollectionItems.Select(i => new
            {
                itemId = i.ItemId,
                title = i.Title,
                url = i.Url,
                targetCookbookId = i.TargetCookbookId,
                state = i.State.ToString(),
                error = i.Error,
                recipeId = i.RecipeId
            })
        };
    }

    private async Task<IActionResult?> ValidateSelectionCookbooksAsync(string userId, ImportCollectionSelectionRequest req, CancellationToken ct)
    {
        if (req.Items.Count == 0)
        {
            return BadRequest(new { message = "Es muss mindestens ein Rezept ausgewaehlt werden." });
        }

        foreach (var item in req.Items)
        {
            if (string.IsNullOrWhiteSpace(item.TargetCookbookId))
            {
                return BadRequest(new { message = "Fuer jedes ausgewaehlte Rezept muss ein Zielkochbuch gesetzt sein." });
            }

            if (await _cookbooks.GetByIdAsync(userId, item.TargetCookbookId, ct) is null)
            {
                return BadRequest(new { message = $"Kochbuch {item.TargetCookbookId} wurde nicht gefunden." });
            }
        }

        return null;
    }

    private static ImportCollectionSelection ToSelection(ImportCollectionSelectionRequest req)
    {
        return new ImportCollectionSelection(req.Items
            .Select(i => new ImportCollectionSelectionItem(i.ItemId, i.Url, i.TargetCookbookId))
            .ToList());
    }

    public record CreateCookbookRequest(string Name, string? Description);
    public record UpdateCookbookRequest(string Name, string? Description);
    public record ImportUrlRequest(string Url);
    public record ConfirmRequest(bool Accepted);
    public record ImportCollectionSelectionRequest(List<ImportCollectionSelectionItemRequest> Items);
    public record ImportCollectionSelectionItemRequest(string ItemId, string Url, string TargetCookbookId);
}
