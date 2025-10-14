using Microsoft.Extensions.Logging;
using Rezepte.Web.Entities;
using Rezepte.Web.Services;
using System;
using System.IO.Compression;

namespace Rezepte.Web.Services.Import;

/// <summary>
/// Handler für Backup‑ZIPs (erwartet recipes.json im ZIP).
/// Legt Rezepte im Zielkochbuch an (nutzt IRecipeService.CreateAsync).
/// </summary>
public class BackupImportHandler(IRecipeService recipes, ILogger<BackupImportHandler> logger) : IImportHandler
{
    private readonly IRecipeService _recipes = recipes;
    private readonly ILogger<BackupImportHandler> _logger = logger;

    public async Task<bool> CanHandleAsync(Stream stream, string fileName, CancellationToken ct = default)
    {
        // simple detection: .zip and contains recipes.json
        if (!fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)) return false;
        try
        {
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
            var entry = archive.GetEntry("recipes.json");
            return entry != null;
        }
        catch
        {
            return false;
        }
    }

    public async Task<ImportResult> HandleAsync(Stream stream, string fileName, string targetCookbookId, string userId, CancellationToken ct = default)
    {
        var created = new List<string>();
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
        var jsonEntry = archive.GetEntry("recipes.json");
        if (jsonEntry == null) return new ImportResult(false, "recipes.json not found in archive.", created);

        ExportRootDto? root;
        await using (var js = jsonEntry.Open())
        {
            root = await System.Text.Json.JsonSerializer.DeserializeAsync<ExportRootDto>(js, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }, ct).ConfigureAwait(false);
        }

        if (root == null || root.Recipes == null || root.Recipes.Count == 0)
            return new ImportResult(true, null, created); // nothing to do

        foreach (var r in root.Recipes)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var steps = r.Steps?.Select(s => new RecipeCreateStep(
                    s.Title,
                    s.Description,
                    s.DurationMinutes,
                    s.RequiresOvernightRest,
                    (s.Ingredients ?? new()).Select(i => new RecipeCreateIngredient(i.Amount, i.Unit, i.Name)).ToList()
                )).ToList() ?? new List<RecipeCreateStep>();

                var (ok, error, recipe) = await _recipes.CreateAsync(userId, targetCookbookId, r.Title ?? string.Empty, r.Description, steps, ct).ConfigureAwait(false);
                if (!ok || recipe == null)
                {
                    _logger.LogWarning("Could not create recipe from import: {Title} -> {Error}", r.Title, error);
                    continue;
                }

                created.Add(recipe.Id);

                // Images: if present in archive, attach using AddImageAsync (optional)
                if (r.ImagePaths != null && r.ImagePaths.Count > 0)
                {
                    foreach (var imgPath in r.ImagePaths)
                    {
                        var normalized = imgPath.Replace('\\', '/');
                        var entry = archive.GetEntry(normalized);
                        if (entry == null) continue;
                        await using var entryStream = entry.Open();
                        await using var msImg = new MemoryStream();
                        await entryStream.CopyToAsync(msImg, ct).ConfigureAwait(false);
                        var fileNameImg = Path.GetFileName(normalized);
                        // AddImageAsync expects userId, recipeId, stream, fileName, contentType
                        var (imgOk, imgErr, img) = await _recipes.AddImageAsync(userId, recipe.Id, new MemoryStream(msImg.ToArray()), fileNameImg, GetContentTypeFromExtension(Path.GetExtension(fileNameImg)), ct).ConfigureAwait(false);
                        if (!imgOk)
                        {
                            _logger.LogWarning("Failed to import image {Image} for recipe {RecipeId}: {Error}", fileNameImg, recipe.Id, imgErr);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing recipe {RecipeTitle}", r.Title);
            }
        }

        return new ImportResult(true, null, created);
    }

    private static string GetContentTypeFromExtension(string? ext)
    {
        if (string.IsNullOrEmpty(ext)) return "application/octet-stream";
        ext = ext.ToLowerInvariant();
        return ext switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }
}
