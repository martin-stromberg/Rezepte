using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rezepte.Web.Entities;
using Rezepte.Web.Security;
using Rezepte.Web.Services;
using Rezepte.Web.Services.Import;
using System.Security.Claims;
using static Rezepte.Web.Controllers.RecipesController;

namespace Rezepte.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
// allow either Bearer *or* cookie auth for browser uploads (so fetch with cookies or short-lived JWT works)
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme + "," + Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)]
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

        var (urlAllowed, urlError, uri) = await RemoteUrlGuard.TryValidateAsync(request.Url, ct);
        if (!urlAllowed || uri is null)
            return BadRequest(new { message = urlError });

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
    [RequestSizeLimit(524288000)] // 500 MB limit, anpassen nach Bedarf
    [RequestFormLimits(MultipartBodyLengthLimit = 524288000)]
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

        var (urlAllowed, urlError, uri) = await RemoteUrlGuard.TryValidateAsync(request.Url, ct);
        if (!urlAllowed || uri is null)
            return BadRequest(new { message = urlError });

        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);

        try
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36 Edg/141.0.0.0");
            client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
            client.DefaultRequestHeaders.Referrer = new Uri("https://www.bing.com/");
            using var resp = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                var errorContent = await resp.Content.ReadAsStringAsync();
                return BadRequest(new { message = $"Remote request failed: {resp.StatusCode}" });
            }

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

    // --- Ergänzungen innerhalb existing controller (neue Endpoints) ---
    // POST api/cookbooks/{cookbookId}/import-session/start
    [HttpPost("{cookbookId}/import-session/start")]
    public async Task<IActionResult> StartImportSession(string cookbookId, [FromBody] ImportUrlRequest request, [FromServices] ImportOrchestrator orchestrator, [FromServices] IHttpClientFactory httpClientFactory, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        if (request == null || string.IsNullOrWhiteSpace(request.Url)) return BadRequest(new { message = "No URL provided." });

        var (urlAllowed, urlError, uri) = await RemoteUrlGuard.TryValidateAsync(request.Url, ct);
        if (!urlAllowed || uri is null)
            return BadRequest(new { message = urlError });

        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);

        // Set common browser-like headers to reduce chance of 403 from remote hosts
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36 Edg/141.0.0.0");
        client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8");
        client.DefaultRequestHeaders.Referrer = new Uri("https://www.bing.com/");

        using var resp = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode)
        {
            var errorBody = await resp.Content.ReadAsStringAsync();
            return BadRequest(new { message = $"Remote request failed: {resp.StatusCode}", detail = errorBody });
        }

        await using var ms = new MemoryStream();
        await using var remoteStream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        await remoteStream.CopyToAsync(ms, ct).ConfigureAwait(false);
        ms.Seek(0, SeekOrigin.Begin);

        var fileName = Path.GetFileName(uri.LocalPath);
        if (string.IsNullOrWhiteSpace(fileName)) fileName = "import-from-url";

        return await StartImportSessionFromStreamAsync(ms, fileName, uri.ToString(), cookbookId, orchestrator, userId, ct);
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
    public async Task<IActionResult> StartImportSessionNoCookbook([FromBody] ImportUrlRequest request, [FromServices] ImportOrchestrator orchestrator, [FromServices] IHttpClientFactory httpClientFactory, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        if (request == null || string.IsNullOrWhiteSpace(request.Url)) return BadRequest(new { message = "No URL provided." });

        var (urlAllowed, urlError, uri) = await RemoteUrlGuard.TryValidateAsync(request.Url, ct);
        if (!urlAllowed || uri is null)
            return BadRequest(new { message = urlError });

        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);

        // Set browser-like headers here as well
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36 Edg/141.0.0.0");
        client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8");
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("de,de-DE;q=0.9,en;q=0.8,en-GB;q=0.7,en-US;q=0.6");
        client.DefaultRequestHeaders.Referrer = new Uri("https://www.bing.com/");

        using var resp = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode)
        {
            var errorBody = await resp.Content.ReadAsStringAsync();
            return BadRequest(new { message = $"Remote request failed: {resp.StatusCode}", detail = errorBody });
        }

        await using var ms = new MemoryStream();

        // Read raw response stream (may be compressed). If server sent Content-Encoding headers,
        // wrap the stream with the appropriate decompressor(s) before copying into memory.
        await using var remoteStream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        var encodings = resp.Content.Headers.ContentEncoding.Select(e => e?.Trim().ToLowerInvariant()).Where(e => !string.IsNullOrEmpty(e)).ToArray();
        Stream source = remoteStream;
        if (encodings.Length > 0)
        {
            // If multiple encodings are present, they are applied in the listed order;
            // to decompress, we must reverse that order.
            for (int i = encodings.Length - 1; i >= 0; i--)
            {
                var enc = encodings[i];
                if (enc == "br" || enc == "brotli")
                {
                    source = new System.IO.Compression.BrotliStream(source, System.IO.Compression.CompressionMode.Decompress, leaveOpen: true);
                }
                else if (enc == "gzip")
                {
                    source = new System.IO.Compression.GZipStream(source, System.IO.Compression.CompressionMode.Decompress, leaveOpen: true);
                }
                else if (enc == "deflate")
                {
                    source = new System.IO.Compression.DeflateStream(source, System.IO.Compression.CompressionMode.Decompress, leaveOpen: true);
                }
                else
                {
                    // Unknown encoding: fallback to raw stream (cannot decompress)
                    source = remoteStream;
                    break;
                }
            }
        }

        // Copy (decompressed) bytes into memory stream
        await source.CopyToAsync(ms, ct).ConfigureAwait(false);
        ms.Seek(0, SeekOrigin.Begin);

        var fileName = Path.GetFileName(uri.LocalPath);
        if (string.IsNullOrWhiteSpace(fileName)) fileName = "import-from-url";

        return await StartImportSessionFromStreamAsync(ms, fileName, uri.ToString(), null, orchestrator, userId, ct);
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
            await using var ms = new MemoryStream();
            await file.CopyToAsync(ms, ct).ConfigureAwait(false);
            ms.Seek(0, SeekOrigin.Begin);

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
            await using var ms = new MemoryStream();
            await file.CopyToAsync(ms, ct).ConfigureAwait(false);
            ms.Seek(0, SeekOrigin.Begin);

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
            return BadRequest(new { message = "Es muss mindestens ein Rezept ausgewählt werden." });
        }

        foreach (var item in req.Items)
        {
            if (string.IsNullOrWhiteSpace(item.TargetCookbookId))
            {
                return BadRequest(new { message = "Für jedes ausgewählte Rezept muss ein Zielkochbuch gesetzt sein." });
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
