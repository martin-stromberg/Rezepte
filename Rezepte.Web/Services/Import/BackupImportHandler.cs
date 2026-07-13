using Microsoft.Extensions.Logging;
using Rezepte.Import.Abstractions;
using Rezepte.Web.Entities;
using System;
using System.IO.Compression;

namespace Rezepte.Web.Services.Import;

/// <summary>
/// Handler für Backup‑ZIPs (erwartet recipes.json im ZIP).
/// Legt Rezepte im Zielkochbuch an (nutzt IRecipeService.CreateAsync).
/// </summary>
public class BackupImportHandler : BaseImportHandler, IImportHandler
{
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

    public async Task<ImportResult> HandleAsync(Stream stream, string fileName, string? uri, string targetCookbookId, string userId, CancellationToken ct = default)
    {
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
        var jsonEntry = archive.GetEntry("recipes.json");
        if (jsonEntry == null) return new ImportResult(false, "recipes.json not found in archive.", new List<string>());

        ExportRootDto? root;
        await using (var js = jsonEntry.Open())
        {
            root = await System.Text.Json.JsonSerializer.DeserializeAsync<ExportRootDto>(js, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }, ct).ConfigureAwait(false);
        }

        if (root == null || root.Recipes == null || root.Recipes.Count == 0)
            return new ImportResult(true, null, new List<string>()); // nothing to do

        var importedRecipes = new List<ImportedRecipe>();
        foreach (var r in root.Recipes)
        {
            ct.ThrowIfCancellationRequested();
            importedRecipes.Add(await ToImportedRecipeAsync(archive, r, uri, ct).ConfigureAwait(false));
        }

        return new ImportResult(true, null, new List<string>(), importedRecipes);
    }

    private static async Task<ImportedRecipe> ToImportedRecipeAsync(ZipArchive archive, ExportRecipeDto r, string? uri, CancellationToken ct)
    {
        var images = new List<ImportedImage>();
        foreach (var imgPath in r.ImagePaths ?? [])
        {
            var normalized = imgPath.Replace('\\', '/');
            var entry = archive.GetEntry(normalized);
            if (entry == null) continue;

            await using var entryStream = entry.Open();
            await using var msImg = new MemoryStream();
            await entryStream.CopyToAsync(msImg, ct).ConfigureAwait(false);
            var fileNameImg = Path.GetFileName(normalized);
            images.Add(new ImportedImage
            {
                Data = msImg.ToArray(),
                FileName = fileNameImg,
                ContentType = GetContentTypeFromExtension(Path.GetExtension(fileNameImg))
            });
        }

        return new ImportedRecipe
        {
            Title = r.Title,
            Description = r.Description,
            SourceUri = r.Uri ?? uri,
            Portions = r.Portions ?? 0,
            Ingredients = (r.Steps ?? [])
                .Take(1)
                .SelectMany(s => s.Ingredients ?? [])
                .Select(i => new ImportedIngredient { Quantity = $"{i.Amount} {i.Unit}".Trim(), Name = i.Name })
                .ToList(),
            Steps = (r.Steps ?? []).Select(s => new ImportedRecipeStep { Text = s.Description }).ToList(),
            Images = images
        };
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
