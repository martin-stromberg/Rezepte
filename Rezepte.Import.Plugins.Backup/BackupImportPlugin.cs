using System.IO.Compression;
using System.Text.Json;
using Rezepte.Import.Abstractions;

namespace Rezepte.Import.Plugins.Backup;

/// <summary>
/// Import plugin that reads recipes exported by the application backup format.
/// </summary>
public sealed class BackupImportPlugin : IImportPlugin
{
    private static readonly Type _handlerType = typeof(BackupImportHandler);

    /// <summary>
    /// Unique identifier of the plugin.
    /// </summary>
    public string Id => "backup";

    /// <summary>
    /// Display name shown in the user interface.
    /// </summary>
    public string DisplayName => "Backup";

    /// <summary>
    /// Description of the plugin.
    /// </summary>
    public string? Description => "Importiert Backup-ZIP-Dateien.";

    /// <summary>
    /// Version of the plugin.
    /// </summary>
    public string Version => "1.0.0";

    /// <summary>
    /// Type of the handler that performs the import.
    /// </summary>
    public Type HandlerType => _handlerType;
}

/// <summary>
/// Imports recipes from a backup ZIP archive.
/// </summary>
public sealed class BackupImportHandler : IImportHandler
{
    /// <summary>
    /// Gets or sets the identifier of the user that owns the import.
    /// </summary>
    public string UserId { private get; set; } = string.Empty;

    /// <summary>
    /// Determines whether the provided stream is a backup ZIP archive.
    /// </summary>
    /// <param name="stream">Stream to inspect.</param>
    /// <param name="fileName">Name of the uploaded file.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>true</c> if the stream contains a backup archive; otherwise <c>false</c>.</returns>
    public Task<bool> CanHandleAsync(Stream stream, string fileName, CancellationToken ct = default)
    {
        if (!fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(false);

        try
        {
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
            return Task.FromResult(archive.GetEntry("recipes.json") is not null);
        }
        catch (InvalidDataException)
        {
            // not a readable zip archive, so this handler cannot process it
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Imports recipes from the backup ZIP archive.
    /// </summary>
    /// <param name="stream">Stream containing the ZIP archive.</param>
    /// <param name="fileName">Name of the uploaded file.</param>
    /// <param name="uri">Optional URI the archive was loaded from.</param>
    /// <param name="targetCookbookId">Identifier of the cookbook to import into.</param>
    /// <param name="userId">Identifier of the user performing the import.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the import operation.</returns>
    public async Task<ImportResult> HandleAsync(Stream stream, string fileName, string? uri, string targetCookbookId, string userId, CancellationToken ct = default)
    {
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
        var jsonEntry = archive.GetEntry("recipes.json");
        if (jsonEntry is null)
            return new ImportResult(false, "recipes.json not found in archive.", []);

        ExportRootDto? root;
        await using (var jsonStream = jsonEntry.Open())
        {
            root = await JsonSerializer.DeserializeAsync<ExportRootDto>(jsonStream, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }, ct).ConfigureAwait(false);
        }

        if (root?.Recipes is null || root.Recipes.Count == 0)
            return new ImportResult(true, null, []);

        var importedRecipes = new List<ImportedRecipe>();
        foreach (var recipe in root.Recipes)
        {
            ct.ThrowIfCancellationRequested();
            importedRecipes.Add(await ToImportedRecipeAsync(archive, recipe, uri, ct).ConfigureAwait(false));
        }

        return new ImportResult(true, null, [], importedRecipes);
    }

    private static async Task<ImportedRecipe> ToImportedRecipeAsync(ZipArchive archive, ExportRecipeDto recipe, string? uri, CancellationToken ct)
    {
        var images = new List<ImportedImage>();
        foreach (var imagePath in recipe.ImagePaths ?? [])
        {
            var normalized = imagePath.Replace('\\', '/');
            var entry = archive.GetEntry(normalized);
            if (entry is null)
                continue;

            await using var entryStream = entry.Open();
            await using var imageStream = new MemoryStream();
            await entryStream.CopyToAsync(imageStream, ct).ConfigureAwait(false);
            var imageFileName = Path.GetFileName(normalized);
            images.Add(new ImportedImage
            {
                Data = imageStream.ToArray(),
                FileName = imageFileName,
                ContentType = GetContentTypeFromExtension(Path.GetExtension(imageFileName))
            });
        }

        return new ImportedRecipe
        {
            Title = recipe.Title,
            Description = recipe.Description,
            SourceUri = recipe.Uri ?? uri,
            Portions = recipe.Portions ?? 0,
            Ingredients = (recipe.Steps ?? [])
                .Take(1)
                .SelectMany(s => s.Ingredients ?? [])
                .Select(i => new ImportedIngredient { Quantity = $"{i.Amount} {i.Unit}".Trim(), Name = i.Name })
                .ToList(),
            Steps = (recipe.Steps ?? []).Select(s => new ImportedRecipeStep { Text = s.Description }).ToList(),
            Images = images
        };
    }

    private static string GetContentTypeFromExtension(string? extension)
    {
        return (extension ?? string.Empty).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }

    private sealed record ExportRootDto
    {
        public List<ExportRecipeDto>? Recipes { get; init; }
    }

    private sealed record ExportRecipeDto
    {
        public string? Title { get; init; }
        public string? Description { get; init; }
        public string? Uri { get; init; }
        public int? Portions { get; init; }
        public List<ExportStepDto>? Steps { get; init; }
        public List<string>? ImagePaths { get; init; }
    }

    private sealed record ExportStepDto
    {
        public string? Description { get; init; }
        public List<ExportIngredientDto>? Ingredients { get; init; }
    }

    private sealed record ExportIngredientDto
    {
        public decimal Amount { get; init; }
        public string? Unit { get; init; }
        public string Name { get; init; } = string.Empty;
    }
}
