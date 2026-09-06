using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Rezepte.Web.Entities;
using Rezepte.Web.Services;
using Rezepte.Web.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace Rezepte.Web.Controllers;

/// <summary>
/// recipess the controller.
/// </summary>
/// <param name="recipes">The recipes parameter.</param>
/// <param name="imageOptions">The image options parameter.</param>
/// <returns>The result.</returns>
[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class RecipesController(IRecipeService recipes, IOptions<ImageOptions> imageOptions) : ApiControllerBase
{
    private readonly IRecipeService _recipes = recipes;
    private readonly ImageOptions _imageOptions = imageOptions.Value;
    private const int MaxPageSize = 100;

    /// <summary>
    /// Gets the by cookbook.
    /// </summary>
    /// <param name="cookbookId">The cookbook id parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    [HttpGet("by-cookbook/{cookbookId}")]
    public async Task<IActionResult> GetByCookbook(string cookbookId, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        var list = await _recipes.GetByCookbookAsync(userId, cookbookId, ct);
        var dtos = list.Select(r =>
        {
            var lastImage = _recipes.GetImages(r.Id, 0, 1).OrderByDescending(i => i.CreatedAt).FirstOrDefault();
            return new RecipeListItemDto(
                r.Id,
                r.Title,
                lastImage?.Url,
                r.Description
            );
        }).ToList();

        return Ok(dtos);
    }

    /// <summary>
    /// Gets the available for.
    /// </summary>
    /// <param name="cookbookId">The cookbook id parameter.</param>
    /// <param name="page">The page parameter.</param>
    /// <param name="pageSize">The page size parameter.</param>
    /// <param name="search">The search parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    [HttpGet("available-for/{cookbookId}")]
    public async Task<IActionResult> GetAvailableFor(string cookbookId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null, CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        var all = await _recipes.GetAvailableForCookbookAsync(userId, cookbookId, ct);
        var query = all.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(r => r.Title.Contains(search));
        }
        var paged = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        var dtos = paged.Select(r => new RecipeListItemDto(r.Id, r.Title, null, r.Description)).ToList();
        return Ok(dtos);
    }

    /// <summary>
    /// Adds the existing.
    /// </summary>
    /// <param name="cookbookId">The cookbook id parameter.</param>
    /// <param name="dto">The dto parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    [HttpPost("add-existing/{cookbookId}")]
    public async Task<IActionResult> AddExisting(string cookbookId, [FromBody] AddExistingRequest dto, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        var (ok, error, created) = await _recipes.AddExistingToCookbookAsync(userId, cookbookId, dto.RecipeIds, ct);
        if (!ok)
            return BadRequest(new { message = error ?? "Hinzufügen fehlgeschlagen." });
        var dtos = created.Select(r => new RecipeListItemDto(r.Id, r.Title, null, r.Description)).ToList();
        return Ok(dtos);
    }

    /// <summary>
    /// Gets the by id.
    /// </summary>
    /// <param name="id">The id parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        var r = await _recipes.GetByIdAsync(userId, id, ct);
        if (r is null) return NotFound();
        var lastImage = await _recipes.GetImages(r.Id, 0, 1).FirstOrDefaultAsync(ct);
        var imageCount = await _recipes.GetImageCountAsync(r.Id, ct);
        var dto = new RecipeDto(
            r.Id,
            r.UserId,
            r.Title,
            r.Description,
            r.Portions,
            r.Steps
                .OrderBy(s => s.StepIndex)
                .Select(s => new RecipeStepDto(
                    s.Id,
                    s.StepIndex,
                    s.Title,
                    s.Description,
                    s.DurationMinutes,
                    s.RequiresOvernightRest,
                    s.Ingredients.Select(i => new RecipeIngredientDto(i.Id, i.Amount, i.Unit, i.Name)).ToList()
                )).ToList(),
            ImageUrl: lastImage?.Url,
            sourceUri: r.Uri,
            ImageCount: imageCount,
            RecipeCookbooks: r.RecipeCookbooks.Select(rc => new RecipeCookbookDtp(rc.Id, rc.RecipeId, rc.CookbookId)).ToList(),
            SideDishes: r.SideDishes
                .Where(sd => sd.SideDishRecipe is not null)
                .OrderBy(sd => sd.OrderIndex)
                .ThenBy(sd => sd.SideDishRecipe!.Title)
                .Select(sd => new RecipeSideDishDto(
                    sd.SideDishRecipeId,
                    sd.SideDishRecipe!.Title,
                    sd.SideDishRecipe.Description,
                    _recipes.GetImages(sd.SideDishRecipeId, 0, 1).OrderByDescending(i => i.CreatedAt).Select(i => i.Url).FirstOrDefault(),
                    sd.SideDishRecipe.Portions))
                .ToList()
        );
        return Ok(dto);
    }

    /// <summary>
    /// Gets the side dishes.
    /// </summary>
    /// <param name="id">The id parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    [HttpGet("{id}/side-dishes")]
    public async Task<IActionResult> GetSideDishes(string id, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        var sideDishes = await _recipes.GetSideDishesAsync(userId, id, ct);
        return Ok(sideDishes.Select(sd => new RecipeSideDishDto(sd.Id, sd.Title, sd.Description, sd.ImageUrl, sd.Portions)).ToList());
    }

    /// <summary>
    /// Gets the cookbooks for recipe.
    /// </summary>
    /// <param name="id">The id parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    [HttpGet("{id}/cookbooks")]
    public async Task<IActionResult> GetCookbooksForRecipe(string id, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var r = await _recipes.GetByIdAsync(userId, id, ct);
        if (r is null) return NotFound();

        var list = r.RecipeCookbooks.Select(rc => rc.CookbookId).ToList();
        return Ok(list);
    }

    /// <summary>
    /// Adds the existing request.
    /// </summary>
    /// <param name="RecipeIds">The recipe ids parameter.</param>
    /// <returns>The result.</returns>
    public record AddExistingRequest(List<string> RecipeIds);
    /// <summary>
    /// Creates the recipe request.
    /// </summary>
    /// <param name="CookbookId">The cookbook id parameter.</param>
    /// <param name="Title">The title parameter.</param>
    /// <param name="Description">The description parameter.</param>
    /// <param name="Uri">The uri parameter.</param>
    /// <param name="Portions">The portions parameter.</param>
    /// <param name="Steps">The steps parameter.</param>
    /// <param name="SideDishRecipeIds">The side dish recipe ids parameter.</param>
    /// <returns>The result.</returns>
    public record CreateRecipeRequest(string? CookbookId, string Title, string? Description, string? Uri, int? Portions, List<CreateRecipeStep> Steps, List<string>? SideDishRecipeIds);
    /// <summary>
    /// Creates the recipe step.
    /// </summary>
    /// <param name="Title">The title parameter.</param>
    /// <param name="Description">The description parameter.</param>
    /// <param name="DurationMinutes">The duration minutes parameter.</param>
    /// <param name="RequiresOvernightRest">The requires overnight rest parameter.</param>
    /// <param name="Ingredients">The ingredients parameter.</param>
    /// <returns>The result.</returns>
    public record CreateRecipeStep(string? Title, string Description, int DurationMinutes, bool RequiresOvernightRest, List<CreateRecipeIngredient> Ingredients);
    /// <summary>
    /// Creates the recipe ingredient.
    /// </summary>
    /// <param name="Amount">The amount parameter.</param>
    /// <param name="Unit">The unit parameter.</param>
    /// <param name="Name">The name parameter.</param>
    /// <returns>The result.</returns>
    public record CreateRecipeIngredient(decimal Amount, string? Unit, string Name);

    /// <summary>
    /// Creates the value.
    /// </summary>
    /// <param name="dto">The dto parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRecipeRequest dto, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        var steps = MapSteps(dto.Steps);

        var (ok, error, recipe) = await _recipes.CreateAsync(userId, dto.CookbookId, dto.Title, dto.Description, dto.Uri, dto.Portions, steps, dto.SideDishRecipeIds, ct);
        if (!ok || recipe is null)
            return BadRequest(new { message = error ?? "Anlegen fehlgeschlagen." });

        return CreatedAtAction(nameof(GetById), new { id = recipe.Id }, new { id = recipe.Id });
    }

    /// <summary>
    /// Updates the recipe request.
    /// </summary>
    /// <param name="Title">The title parameter.</param>
    /// <param name="Description">The description parameter.</param>
    /// <param name="Uri">The uri parameter.</param>
    /// <param name="Portions">The portions parameter.</param>
    /// <param name="Steps">The steps parameter.</param>
    /// <param name="SideDishRecipeIds">The side dish recipe ids parameter.</param>
    /// <returns>The result.</returns>
    public record UpdateRecipeRequest(string Title, string? Description, string? Uri, int? Portions, List<CreateRecipeStep> Steps, List<string>? SideDishRecipeIds);

    private static List<RecipeCreateStep> MapSteps(List<CreateRecipeStep>? steps) =>
        steps?.Select(s => new RecipeCreateStep(
            s.Title,
            s.Description,
            s.DurationMinutes,
            s.RequiresOvernightRest,
            (s.Ingredients ?? new()).Select(i => new RecipeCreateIngredient(i.Amount, i.Unit, i.Name)).ToList()
        )).ToList() ?? new List<RecipeCreateStep>();

    /// <summary>
    /// Updates the value.
    /// </summary>
    /// <param name="id">The id parameter.</param>
    /// <param name="dto">The dto parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateRecipeRequest dto, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        var steps = MapSteps(dto.Steps);

        var (ok, error) = await _recipes.UpdateAsync(userId, id, dto.Title, dto.Description, dto.Uri, dto.Portions, steps, dto.SideDishRecipeIds, ct);
        if (!ok)
            return BadRequest(new { message = error ?? "Speichern fehlgeschlagen." });
        return NoContent();
    }

    /// <summary>
    /// Deletes the value.
    /// </summary>
    /// <param name="id">The id parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        var (ok, error) = await _recipes.DeleteAsync(userId, id, ct);
        if (!ok) return BadRequest(new { message = error ?? "Löschen fehlgeschlagen." });
        return NoContent();
    }

    /// <summary>
    /// uploads the image.
    /// </summary>
    /// <param name="id">The id parameter.</param>
    /// <param name="file">The file parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    [HttpPost("{id}/image")]
    public async Task<IActionResult> UploadImage(string id, IFormFile file, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "Keine Datei hochgeladen." });
        }

        // Größe prüfen
        if (file.Length > _imageOptions.MaxSizeBytes)
        {
            return BadRequest(new { message = $"Datei zu groß. Maximal {_imageOptions.MaxSizeBytes} Bytes erlaubt." });
        }

        // ContentType prüfen (einfache Whitelist)
        var contentType = file.ContentType ?? string.Empty;
        if (!_imageOptions.AllowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Nicht unterstützter Dateityp." });
        }

        // Optional: Datei‑Header / Magic‑Bytes Check hier ergänzen (empfohlen für Production).
        await using var stream = file.OpenReadStream();
        var (ok, error, image) = await _recipes.AddImageAsync(userId, id, stream, file.FileName, contentType, ct);

        if (!ok)
        {
            return BadRequest(new { message = error ?? "Bild konnte nicht gespeichert werden." });
        }

        return Ok(new { imageId = image!.Id });
    }

    // Images are rendered by the browser, so cookie authentication is accepted in addition to bearer tokens.
    /// <summary>
    /// Gets the image.
    /// </summary>
    /// <param name="recipeId">The recipe id parameter.</param>
    /// <param name="imageId">The image id parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    [HttpGet("{recipeId}/image/{imageId}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme + "," + Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)]
    public async Task<IActionResult> GetImage(string recipeId, string imageId, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var recipe = await _recipes.GetByIdAsync(userId, recipeId, ct);
        if (recipe is null) return NotFound();

        var image = await _recipes.GetImageAsync(recipeId, imageId, ct);
        if (image == null) return NotFound();

        var contentType = string.IsNullOrWhiteSpace(image.ContentType) ? "image/jpeg" : image.ContentType;

        // ETag via SHA256 über die Daten
        string etag;
        using (var sha = SHA256.Create())
        {
            var hash = sha.ComputeHash(image.Data);
            etag = "\"" + Convert.ToBase64String(hash) + "\""; // quoted ETag
        }

        // If-None-Match prüfen
        if (Request.Headers.TryGetValue("If-None-Match", out var inm) && inm.Any(h => h == etag))
        {
            Response.Headers["ETag"] = etag;
            Response.Headers["Cache-Control"] = $"private, max-age={_imageOptions.CacheMaxAgeSeconds}";
            return StatusCode(StatusCodes.Status304NotModified);
        }

        // Cache Header setzen
        Response.Headers["Cache-Control"] = $"private, max-age={_imageOptions.CacheMaxAgeSeconds}";
        Response.Headers["ETag"] = etag;

        return File(image.Data, contentType, image.FileName);
    }

    /// <summary>
    /// Gets the images.
    /// </summary>
    /// <param name="recipeId">The recipe id parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    [HttpGet("{recipeId}/images")]
    public async Task<IActionResult> GetImages(string recipeId, CancellationToken ct)
    {
        var recipe = await _recipes.GetByIdAsync(GetUserId() ?? string.Empty, recipeId, ct);
        if (recipe is null) return NotFound();

        var images = _recipes.GetImages(recipe.Id, 0, int.MaxValue)
            .OrderByDescending(img => img.CreatedAt)
            .Select(img => new
            {
                img.Id,
                img.Url
            })
            .ToList();

        return Ok(images);
    }

    /// <summary>
    /// Deletes the image.
    /// </summary>
    /// <param name="recipeId">The recipe id parameter.</param>
    /// <param name="imageId">The image id parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    [HttpDelete("{recipeId}/image/{imageId}")]
    public async Task<IActionResult> DeleteImage(string recipeId, string imageId, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        var (ok, error) = await _recipes.DeleteImageAsync(userId, recipeId, imageId, ct);
        if (!ok) return BadRequest(new { message = error ?? "Bild konnte nicht gelöscht werden." });
        return NoContent();
    }

    //[HttpGet("latest")]
    //[AllowAnonymous]
    //public async Task<IActionResult> GetLatest([FromQuery] int count = 20, CancellationToken ct = default)
    //{
    //    var userId = GetUserId();
    //    if (userId is null) return Unauthorized();
    //    var recipes = await _recipes.GetLatestAsync(userId, count, ct);

    //    var dtos = recipes.Select(r =>
    //    {
    //        var lastImage = _recipes.GetImages(r.Id, 0, 1).OrderByDescending(i => i.CreatedAt).FirstOrDefault();
    //        return new RecipeListItemDto(
    //            r.Id,
    //            r.Title,
    //            lastImage?.Url,
    //            null
    //        );
    //    }).ToList();

    //    return Ok(dtos);
    //}

    /// <summary>
    /// Gets the latest for cookbook.
    /// </summary>
    /// <param name="cookbookId">The cookbook id parameter.</param>
    /// <param name="count">The count parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    [HttpGet("latest")]
    public async Task<IActionResult> GetLatestForCookbook([FromQuery] string? cookbookId, [FromQuery] int count = 20, CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        // Hole alle Rezepte im Kochbuch, sortiere nach CreatedAt und nehme die neuesten
        var list = string.IsNullOrWhiteSpace(cookbookId) ? await _recipes.GetLatestAsync(userId, count, ct) : await _recipes.GetByCookbookAsync(userId, cookbookId, ct);
        var latest = list.OrderByDescending(r => r.CreatedAt).Take(Math.Max(1, count)).ToList();

        var dtos = latest.Select(r =>
        {
            var lastImage = _recipes.GetImages(r.Id, 0, 1).OrderByDescending(i => i.CreatedAt).FirstOrDefault();
            return new RecipeListItemDto(
                r.Id,
                r.Title,
                lastImage?.Url,
                r.Description
            );
        }).ToList();

        return Ok(dtos);
    }

    // DTO für die Startseite
    /// <summary>
    /// recipes the list item dto.
    /// </summary>
    /// <param name="Id">The id parameter.</param>
    /// <param name="Title">The title parameter.</param>
    /// <param name="ImageUrl">The image url parameter.</param>
    /// <param name="Description">The description parameter.</param>
    /// <returns>The result.</returns>
    public record RecipeListItemDto(string Id, string Title, string? ImageUrl, string? Description);
    /// <summary>
    /// recipes the dto.
    /// </summary>
    /// <param name="Id">The id parameter.</param>
    /// <param name="OwnerId">The owner id parameter.</param>
    /// <param name="Title">The title parameter.</param>
    /// <param name="Description">The description parameter.</param>
    /// <param name="NumberOfPortions">The number of portions parameter.</param>
    /// <param name="Steps">The steps parameter.</param>
    /// <param name="ImageUrl">The image url parameter.</param>
    /// <param name="sourceUri">The source uri parameter.</param>
    /// <param name="ImageCount">The image count parameter.</param>
    /// <param name="RecipeCookbooks">The recipe cookbooks parameter.</param>
    /// <param name="SideDishes">The side dishes parameter.</param>
    /// <returns>The result.</returns>
    public record RecipeDto(string Id, string OwnerId, string Title, string? Description, int NumberOfPortions, List<RecipeStepDto> Steps, string? ImageUrl, string? sourceUri, int ImageCount, List<RecipeCookbookDtp> RecipeCookbooks, List<RecipeSideDishDto> SideDishes);
    /// <summary>
    /// recipes the cookbook dtp.
    /// </summary>
    /// <param name="Id">The id parameter.</param>
    /// <param name="RecipeId">The recipe id parameter.</param>
    /// <param name="CookbookId">The cookbook id parameter.</param>
    /// <returns>The result.</returns>
    public record RecipeCookbookDtp(string Id, string RecipeId, string CookbookId);
    /// <summary>
    /// recipes the side dish dto.
    /// </summary>
    /// <param name="Id">The id parameter.</param>
    /// <param name="Title">The title parameter.</param>
    /// <param name="Description">The description parameter.</param>
    /// <param name="ImageUrl">The image url parameter.</param>
    /// <param name="Portions">The portions parameter.</param>
    /// <returns>The result.</returns>
    public record RecipeSideDishDto(string Id, string Title, string? Description, string? ImageUrl, int Portions);
    /// <summary>
    /// recipes the step dto.
    /// </summary>
    /// <param name="Id">The id parameter.</param>
    /// <param name="StepIndex">The step index parameter.</param>
    /// <param name="Title">The title parameter.</param>
    /// <param name="Description">The description parameter.</param>
    /// <param name="DurationMinutes">The duration minutes parameter.</param>
    /// <param name="RequiresOvernightRest">The requires overnight rest parameter.</param>
    /// <param name="Ingredients">The ingredients parameter.</param>
    /// <returns>The result.</returns>
    public record RecipeStepDto(string Id, int StepIndex, string? Title, string Description, int DurationMinutes, bool RequiresOvernightRest, List<RecipeIngredientDto> Ingredients);
    /// <summary>
    /// recipes the ingredient dto.
    /// </summary>
    /// <param name="Id">The id parameter.</param>
    /// <param name="Amount">The amount parameter.</param>
    /// <param name="Unit">The unit parameter.</param>
    /// <param name="Name">The name parameter.</param>
    /// <returns>The result.</returns>
    public record RecipeIngredientDto(string Id, decimal Amount, string? Unit, string Name);


    /// <summary>
    /// Suche nach Rezepten (Titel, Zutaten, Schritte, Tags). 
    /// Query-Parameter: q, tags (comma separated), cookbookId, page, pageSize, sort (relevance|newest|title).
    /// </summary>
    /// <param name="q">The q parameter.</param>
    /// <param name="tags">The tags parameter.</param>
    /// <param name="cookbookId">The cookbook id parameter.</param>
    /// <param name="page">The page parameter.</param>
    /// <param name="pageSize">The page size parameter.</param>
    /// <param name="sort">The sort parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <param>...</param>
    /// <param>...</param>
    /// <param>...</param>
    /// <returns>The result.</returns>
    [HttpGet("search")]
    public async Task<IActionResult> SearchAsync(
        [FromQuery] string? q,
        [FromQuery] string? tags,
        [FromQuery] string? cookbookId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string sort = "relevance",
        CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        if (page < 1) page = 1;
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var recipes = await _recipes.SearchAsync(userId, q, tags, cookbookId, page, pageSize, sort, ct);

        // map to DTOs and create safe snippets
        var items = recipes.Items.Select(i => new RecipeSearchItemDto
        (
            Id: i.Id,
            Title: i.Title ?? string.Empty,
            Snippet: (i.Snippet ?? string.Empty).Length <= 200 ? (i.Snippet ?? string.Empty) : (i.Snippet ?? string.Empty).Substring(0, 200) + "...",
            PrimaryImageUrl: i.PrimaryImageUrl,
            Tags: i.Tags ?? Array.Empty<string>(),
            PublishedLabel: i.CreatedAt.ToString("yyyy-MM-dd")
        )).ToArray();

        var result = new RecipeSearchResponseDto(items, recipes.TotalCount);
        return Ok(result);
    }

    // DTOs returned by this controller
    private sealed record RecipeSearchItemDto(string Id, string Title, string Snippet, string? PrimaryImageUrl, string[] Tags, string? PublishedLabel);
    private sealed record RecipeSearchResponseDto(RecipeSearchItemDto[] Items, int Total);
}
