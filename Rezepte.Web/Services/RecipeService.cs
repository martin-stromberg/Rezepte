using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Rezepte.Web.Components.Pages;
using Rezepte.Web.Data;
using Rezepte.Web.Entities;
using System.IO;

namespace Rezepte.Web.Services;

public interface IRecipeService
{
    Task<Recipe?> GetByIdAsync(string userId, string id, CancellationToken ct);
    Task<List<Recipe>> GetByCookbookAsync(string userId, string cookbookId, CancellationToken ct);
    Task<List<Recipe>> GetAvailableForCookbookAsync(string userId, string cookbookId, CancellationToken ct);
    Task<(bool ok, string? error, Recipe? recipe)> CreateAsync(string userId, string cookbookId, string title, string? description, string? uri, IReadOnlyList<RecipeCreateStep> steps, CancellationToken ct);
    Task<(bool ok, string? error)> UpdateAsync(string userId, string id, string title, string? description, IReadOnlyList<RecipeCreateStep> steps, CancellationToken ct);
    Task<(bool ok, string? error)> DeleteAsync(string userId, string id, CancellationToken ct);
    Task<(bool ok, string? error, List<Recipe> created)> AddExistingToCookbookAsync(string userId, string cookbookId, IEnumerable<string> recipeIds, CancellationToken ct);
    Task<(bool ok, string? error)> RemoveFromCookbookAsync(string userId, string cookbookId, string recipeId, CancellationToken ct);

    Task<(bool ok, string? error, string? imageId)> SetImageAsync(string userId, string recipeId, Stream imageStream, string fileName, CancellationToken ct);
    Task<(bool ok, string? error, RecipeImage? image)> AddImageAsync(string userId, string recipeId, Stream imageStream, string fileName, string contentType, CancellationToken ct);
    Task<RecipeImage?> GetImageAsync(string recipeId, string imageId, CancellationToken ct);
    IQueryable<RecipeImage> GetImages(string recipeId, int offset, int count);
    Task<int> GetImageCountAsync(string recipeId, CancellationToken ct);
    Task<(bool ok, string? error)> DeleteImageAsync(string userId, string recipeId, string imageId, CancellationToken ct);
    Task<List<Recipe>> GetLatestAsync(string userId, int count, CancellationToken ct);
    Task<Recipe> FindByUri(string userId, string v, CancellationToken ct);
}

public record RecipeCreateIngredient(decimal Amount, string? Unit, string Name);
public record RecipeCreateStep(string? Title, string Description, int DurationMinutes, bool RequiresOvernightRest, IReadOnlyList<RecipeCreateIngredient> Ingredients);

public class RecipeService(RezepteDbContext db, IWebHostEnvironment env, IHttpContextAccessor httpContextAccessor) : IRecipeService
{
    private readonly RezepteDbContext _db = db;
    private readonly IWebHostEnvironment _env = env;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    protected string CurrentUserId
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            var currentUserId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return currentUserId;
        }
    }

    public async Task<Recipe?> GetByIdAsync(string userId, string id, CancellationToken ct)
    {
        return await _db.Recipes
            .AsNoTracking()
            .Include(r => r.RecipeCookbooks)
            .Include(r => r.Steps)
                .ThenInclude(s => s.Ingredients)
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId, ct);
    }

    public async Task<List<Recipe>> GetByCookbookAsync(string userId, string cookbookId, CancellationToken ct)
    {
        return await _db.Recipes.AsNoTracking()
            .Include(r => r.Images)
            .Where(r => r.RecipeCookbooks.Any(c => (c.CookbookId == cookbookId) || (cookbookId == "")) && r.UserId == userId)
            .OrderBy(r => r.Title)
            .ToListAsync(ct);
    }

    public async Task<List<Recipe>> GetAvailableForCookbookAsync(string userId, string cookbookId, CancellationToken ct)
    {
        return await _db.Recipes.Include(r => r.RecipeCookbooks).AsNoTracking()
            .Where(r => r.RecipeCookbooks.Any(c => c.CookbookId != cookbookId) && r.UserId == userId)
            .OrderBy(r => r.Title)
            .ToListAsync(ct);
    }

    public async Task<(bool ok, string? error, Recipe? recipe)> CreateAsync(string userId, string cookbookId, string title, string? description, string? uri, IReadOnlyList<RecipeCreateStep> steps, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length < 3) return (false, "Der Titel muss mindestens 3 Zeichen haben.", null);
        var cookbookExists = await _db.Cookbooks.AsNoTracking().AnyAsync(c => c.Id == cookbookId && c.UserId == userId, ct);
        if (!cookbookExists && !string.IsNullOrWhiteSpace(cookbookId)) return (false, "Kochbuch nicht gefunden.", null);

        var entity = new Recipe
        {
            UserId = userId,
            Title = title.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            Uri = string.IsNullOrWhiteSpace(uri) ? null : uri.Trim(),
        };
        if (cookbookExists)
            entity.RecipeCookbooks.Add(new RecipeCookbook
            {
                CookbookId = cookbookId,
                RecipeId = entity.Id
            });
        _db.Recipes.Add(entity);

        // Steps (+ ingredients)
        if (steps is not null)
        {
            for (var i = 0; i < steps.Count; i++)
            {
                var s = steps[i];
                if (string.IsNullOrWhiteSpace(s.Description)) return (false, "Jeder Zubereitungsschritt benötigt eine Beschreibung.", null);
                if (s.DurationMinutes < 0) return (false, "Zubereitungsdauer darf nicht negativ sein.", null);
                var step = new RecipeStep
                {
                    RecipeId = entity.Id,
                    StepIndex = i,
                    Title = string.IsNullOrWhiteSpace(s.Title) ? null : s.Title.Trim(),
                    Description = s.Description.Trim(),
                    DurationMinutes = s.DurationMinutes,
                    RequiresOvernightRest = s.RequiresOvernightRest
                };
                _db.RecipeSteps.Add(step);

                if (s.Ingredients is not null)
                {
                    foreach (var ing in s.Ingredients)
                    {
                        if (string.IsNullOrWhiteSpace(ing.Name)) return (false, "Zutaten benötigen eine Bezeichnung.", null);
                        _db.RecipeIngredients.Add(new RecipeIngredient
                        {
                            StepId = step.Id,
                            Amount = ing.Amount,
                            Unit = string.IsNullOrWhiteSpace(ing.Unit) ? null : ing.Unit.Trim(),
                            Name = ing.Name.Trim()
                        });
                    }
                }
            }
        }

        await _db.SaveChangesAsync(ct);
        return (true, null, entity);
    }

    public async Task<(bool ok, string? error)> UpdateAsync(string userId, string id, string title, string? description, IReadOnlyList<RecipeCreateStep> steps, CancellationToken ct)
    {
        var recipe = await _db.Recipes.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId, ct);
        if (recipe is null) return (false, "Rezept nicht gefunden.");
        if (recipe.UserId != CurrentUserId)
            return (false, "Rezept nicht im Besitz des angemeldeten Benutzers.");
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length < 3) return (false, "Der Titel muss mindestens 3 Zeichen haben.");

        recipe.Title = title.Trim();
        recipe.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

        // Remove old steps + ingredients
        var oldSteps = await _db.RecipeSteps.Where(s => s.RecipeId == recipe.Id).ToListAsync(ct);
        var oldStepIds = oldSteps.Select(s => s.Id).ToList();
        var oldIngredients = await _db.RecipeIngredients.Where(i => oldStepIds.Contains(i.StepId)).ToListAsync(ct);
        _db.RecipeIngredients.RemoveRange(oldIngredients);
        _db.RecipeSteps.RemoveRange(oldSteps);

        // Add new steps
        if (steps is not null)
        {
            for (var i = 0; i < steps.Count; i++)
            {
                var s = steps[i];
                if (string.IsNullOrWhiteSpace(s.Description)) return (false, "Jeder Zubereitungsschritt benötigt eine Beschreibung.");
                if (s.DurationMinutes < 0) return (false, "Zubereitungsdauer darf nicht negativ sein.");
                var step = new RecipeStep
                {
                    RecipeId = recipe.Id,
                    StepIndex = i,
                    Title = string.IsNullOrWhiteSpace(s.Title) ? null : s.Title.Trim(),
                    Description = s.Description.Trim(),
                    DurationMinutes = s.DurationMinutes,
                    RequiresOvernightRest = s.RequiresOvernightRest
                };
                _db.RecipeSteps.Add(step);

                if (s.Ingredients is not null)
                {
                    foreach (var ing in s.Ingredients)
                    {
                        if (string.IsNullOrWhiteSpace(ing.Name)) return (false, "Zutaten benötigen eine Bezeichnung.");
                        _db.RecipeIngredients.Add(new RecipeIngredient
                        {
                            StepId = step.Id,
                            Amount = ing.Amount,
                            Unit = string.IsNullOrWhiteSpace(ing.Unit) ? null : ing.Unit.Trim(),
                            Name = ing.Name.Trim()
                        });
                    }
                }
            }
        }

        await _db.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<(bool ok, string? error)> DeleteAsync(string userId, string id, CancellationToken ct)
    {
        var recipe = await _db.Recipes.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId, ct);
        if (recipe is null) return (false, "Rezept nicht gefunden.");
        if (recipe.UserId != CurrentUserId)
            return (false, "Rezept nicht im Besitz des angemeldeten Benutzers.");
        _db.Recipes.Remove(recipe);
        await _db.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<(bool ok, string? error, List<Recipe> created)> AddExistingToCookbookAsync(string userId, string cookbookId, IEnumerable<string> recipeIds, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cookbookId)) return (false, "CookbookId required.", new List<Recipe>());
        var exists = await _db.Cookbooks.AsNoTracking().AnyAsync(c => c.Id == cookbookId && c.UserId == userId, ct);
        if (!exists) return (false, "Kochbuch nicht gefunden.", new List<Recipe>());
        var ids = recipeIds?.Distinct().ToList() ?? new List<string>();
        if (ids.Count == 0) return (true, null, new List<Recipe>());

        var created = new List<Recipe>();
        foreach (var rid in ids)
        {
            var source = await _db.Recipes
                .Include(r => r.RecipeCookbooks)
                .Include(r => r.Steps)
                    .ThenInclude(s => s.Ingredients)
                .FirstOrDefaultAsync(r => r.Id == rid && r.UserId == userId, ct);
            if (source is null) { continue; }
            if (source.RecipeCookbooks.Any(c => c.CookbookId == cookbookId)) { continue; }
            source.RecipeCookbooks.Add(new RecipeCookbook
            {
                CookbookId = cookbookId,
                RecipeId = source.Id
            });
            created.Add(source);
        }
        await _db.SaveChangesAsync(ct);
        return (true, null, created);
    }
    public async Task<(bool ok, string? error)> RemoveFromCookbookAsync(string userId, string cookbookId, string recipeId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(userId)) return (false, "Unauthorized");
        var recipe = await _db.Recipes.Include(r => r.RecipeCookbooks).FirstOrDefaultAsync(r => r.Id == recipeId, ct).ConfigureAwait(false);
        if (recipe == null) return (false, "Rezept nicht gefunden.");
        if (recipe.UserId != userId) return (false, "Keine Berechtigung.");

        var existingAssignment = recipe.RecipeCookbooks.FirstOrDefault(rc => rc.CookbookId == cookbookId);
        if (existingAssignment is null) return (false, "Rezept ist nicht in diesem Kochbuch.");

        recipe.RecipeCookbooks.Remove(existingAssignment);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return (true, null);
    }

    public async Task<(bool ok, string? error, string? imageId)> SetImageAsync(string userId, string recipeId, Stream imageStream, string fileName, CancellationToken ct)
    {
        var recipe = await _db.Recipes.Include(r => r.Images)
            .FirstOrDefaultAsync(r => r.Id == recipeId && r.UserId == userId, ct);
        if (recipe == null)
        {
            return (false, "Rezept nicht gefunden.", null);
        }

        using var ms = new MemoryStream();
        await imageStream.CopyToAsync(ms, ct);

        var image = new RecipeImage
        {
            RecipeId = recipeId,
            FileName = fileName,
            ContentType = "", // Optional: ContentType setzen, falls bekannt
            Data = ms.ToArray()
        };

        recipe.Images.Add(image);
        await _db.SaveChangesAsync(ct);

        return (true, null, image.Id);
    }

    public async Task<(bool ok, string? error, RecipeImage? image)> AddImageAsync(string userId, string recipeId, Stream imageStream, string fileName, string contentType, CancellationToken ct)
    {
        var recipe = await _db.Recipes.Include(r => r.Images)
            .FirstOrDefaultAsync(r => r.Id == recipeId && r.UserId == userId, ct);
        if (recipe == null)
        {
            return (false, "Rezept nicht gefunden.", null);
        }

        using var ms = new MemoryStream();
        await imageStream.CopyToAsync(ms, ct);

        var image = new RecipeImage
        {
            RecipeId = recipeId,
            FileName = fileName,
            ContentType = contentType, 
            Data = ms.ToArray()
        };

        recipe.Images.Add(image);
        await _db.SaveChangesAsync(ct);

        return (true, null, image);
    }

    public async Task<RecipeImage?> GetImageAsync(string recipeId, string imageId, CancellationToken ct)
    {
        return await _db.RecipeImages
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.RecipeId == recipeId && i.Id == imageId, ct);
    }

    public IQueryable<RecipeImage> GetImages(string recipeId, int offset, int count)
    {
        return _db.RecipeImages.AsNoTracking().Where(i => i.RecipeId == recipeId).Skip(offset).Take(count);
    }
    public async Task<int> GetImageCountAsync(string recipeId, CancellationToken ct)
    {
        return await _db.RecipeImages.AsNoTracking().CountAsync(i => i.RecipeId == recipeId, ct);
    }

    public async Task<(bool ok, string? error)> DeleteImageAsync(string userId, string recipeId, string imageId, CancellationToken ct)
    {
        var recipe = await _db.Recipes
            .Include(r => r.Images)
            .FirstOrDefaultAsync(r => r.Id == recipeId && r.UserId == userId, ct);
        if (recipe == null)
        {
            return (false, "Rezept nicht gefunden.");
        }
        if (recipe.UserId != CurrentUserId)
            return (false, "Rezept nicht im Besitz des angemeldeten Benutzers.");

        var image = recipe.Images.FirstOrDefault(i => i.Id == imageId);
        if (image == null)
        {
            return (false, "Bild nicht gefunden.");
        }

        recipe.Images.Remove(image);
        await _db.SaveChangesAsync(ct);

        return (true, null);
    }

    public async Task<List<Recipe>> GetLatestAsync(string userId, int count, CancellationToken ct)
    {
        return await _db.Recipes.AsNoTracking()
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .Take(count)
            .ToListAsync(ct);
    }

    public async Task<Recipe> FindByUri(string userId, string uri, CancellationToken ct)
    {
        return await _db.Recipes.AsNoTracking()
            .FirstOrDefaultAsync(r => r.UserId == userId && r.Uri == uri, ct);
    }
}
