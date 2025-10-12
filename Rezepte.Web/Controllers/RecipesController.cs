using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rezepte.Web.Entities;
using Rezepte.Web.Services;
using System.Security.Claims;

namespace Rezepte.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class RecipesController(IRecipeService recipes) : ControllerBase
{
    private readonly IRecipeService _recipes = recipes;

    private string? GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    [HttpGet("by-cookbook/{cookbookId}")]
    public async Task<IActionResult> GetByCookbook(string cookbookId, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        var list = await _recipes.GetByCookbookAsync(userId, cookbookId, ct);
        var dtos = list.Select(r => new RecipeListItemDto(r.Id, r.Title, r.Description)).ToList();
        return Ok(dtos);
    }

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
        var dtos = paged.Select(r => new RecipeListItemDto(r.Id, r.Title, r.Description)).ToList();
        return Ok(dtos);
    }

    [HttpPost("add-existing/{cookbookId}")]
    public async Task<IActionResult> AddExisting(string cookbookId, [FromBody] AddExistingRequest dto, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        var (ok, error, created) = await _recipes.AddExistingToCookbookAsync(userId, cookbookId, dto.RecipeIds, ct);
        if (!ok)
            return BadRequest(new { message = error ?? "Hinzufügen fehlgeschlagen." });
        var dtos = created.Select(r => new RecipeListItemDto(r.Id, r.Title, r.Description)).ToList();
        return Ok(dtos);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        var r = await _recipes.GetByIdAsync(userId, id, ct);
        if (r is null) return NotFound();
        var lastImage = await _recipes.GetImages(r.Id, 0, 1).OrderByDescending(img => img.CreatedAt).FirstOrDefaultAsync(ct);
        var imageCount = await _recipes.GetImageCountAsync(r.Id, ct);
        var dto = new RecipeDto(
            r.Id,
            r.CookbookId,
            r.UserId,
            r.Title,
            r.Description,
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
            ImageCount: imageCount
        );
        return Ok(dto);
    }

    public record AddExistingRequest(List<string> RecipeIds);
    public record CreateRecipeRequest(string CookbookId, string Title, string? Description, List<CreateRecipeStep> Steps);
    public record CreateRecipeStep(string? Title, string Description, int DurationMinutes, bool RequiresOvernightRest, List<CreateRecipeIngredient> Ingredients);
    public record CreateRecipeIngredient(decimal Amount, string? Unit, string Name);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRecipeRequest dto, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        var steps = dto.Steps?.Select(s => new RecipeCreateStep(
            s.Title,
            s.Description,
            s.DurationMinutes,
            s.RequiresOvernightRest,
            (s.Ingredients ?? new()).Select(i => new RecipeCreateIngredient(i.Amount, i.Unit, i.Name)).ToList()
        )).ToList() ?? new List<RecipeCreateStep>();

        var (ok, error, recipe) = await _recipes.CreateAsync(userId, dto.CookbookId, dto.Title, dto.Description, steps, ct);
        if (!ok || recipe is null)
            return BadRequest(new { message = error ?? "Anlegen fehlgeschlagen." });

        return CreatedAtAction(nameof(GetById), new { id = recipe.Id }, new { id = recipe.Id });
    }

    public record UpdateRecipeRequest(string Title, string? Description, List<CreateRecipeStep> Steps);

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateRecipeRequest dto, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        var steps = dto.Steps?.Select(s => new RecipeCreateStep(
            s.Title,
            s.Description,
            s.DurationMinutes,
            s.RequiresOvernightRest,
            (s.Ingredients ?? new()).Select(i => new RecipeCreateIngredient(i.Amount, i.Unit, i.Name)).ToList()
        )).ToList() ?? new List<RecipeCreateStep>();

        var (ok, error) = await _recipes.UpdateAsync(userId, id, dto.Title, dto.Description, steps, ct);
        if (!ok)
            return BadRequest(new { message = error ?? "Speichern fehlgeschlagen." });
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        var (ok, error) = await _recipes.DeleteAsync(userId, id, ct);
        if (!ok)
            return BadRequest(new { message = error ?? "Löschen fehlgeschlagen." });
        return NoContent();
    }

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

        using var stream = file.OpenReadStream();
        var (ok, error, image) = await _recipes.AddImageAsync(userId, id, stream, file.FileName, file.ContentType, ct);

        if (!ok)
        {
            return BadRequest(new { message = error ?? "Bild konnte nicht gespeichert werden." });
        }

        return Ok(new { imageId = image!.Id });
    }

    [HttpGet("{recipeId}/image/{imageId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetImage(string recipeId, string imageId, CancellationToken ct)
    {
        var image = await _recipes.GetImageAsync(recipeId, imageId, ct);
        if (image == null) return NotFound();
        var contentType = string.IsNullOrWhiteSpace(image.ContentType) ? "image/jpeg" : image.ContentType;
        return File(image.Data, contentType, image.FileName);
    }

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

    [HttpDelete("{recipeId}/image/{imageId}")]
    public async Task<IActionResult> DeleteImage(string recipeId, string imageId, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        var (ok, error) = await _recipes.DeleteImageAsync(userId, recipeId, imageId, ct);
        if (!ok) return BadRequest(new { message = error ?? "Bild konnte nicht gelöscht werden." });
        return NoContent();
    }

    [HttpGet("latest")]
    [AllowAnonymous]
    public async Task<IActionResult> GetLatest([FromQuery] int count = 20, CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        var recipes = await _recipes.GetLatestAsync(userId, count, ct);

        var dtos = recipes.Select(r =>
        {
            var lastImage = _recipes.GetImages(r.Id, 0, 1).OrderByDescending(i => i.CreatedAt).FirstOrDefault();
            return new RecipeListItemDto(
                r.Id,
                r.Title,
                lastImage?.Url
            );
        }).ToList();

        return Ok(dtos);
    }

    // DTO für die Startseite
    public record RecipeListItemDto(string Id, string Title, string? ImageUrl);
    public record RecipeDto(string Id, string CookbookId, string OwnerId, string Title, string? Description, List<RecipeStepDto> Steps, string? ImageUrl, int ImageCount);
    public record RecipeStepDto(string Id, int StepIndex, string? Title, string Description, int DurationMinutes, bool RequiresOvernightRest, List<RecipeIngredientDto> Ingredients);
    public record RecipeIngredientDto(string Id, decimal Amount, string? Unit, string Name);
}
