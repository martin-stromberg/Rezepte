using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rezepte.Web.Data;
using Rezepte.Web.Entities;

namespace Rezepte.Web.Services;

public interface IExportService
{
    /// <summary>
    /// Exportiert die Rezepte des angegebenen Benutzers als ZIP-Stream.
    /// </summary>
    Task<Stream> ExportUserAsync(string userId, bool includeImages, bool includePdf, CancellationToken ct = default);

    /// <summary>
    /// Exportiert alle Daten (Admin-Export) als ZIP-Stream.
    /// </summary>
    Task<Stream> ExportAllAsync(string adminUserId, bool includeImages, bool includePdf, CancellationToken ct = default);

    /// <summary>
    /// Stellt Daten aus einem Export-ZIP wieder her.
    /// Achtung: Implementierung ist vorsichtig — legt fehlende Entitäten an, überschreibt
    /// bestehende nicht. Prüfe und erweitere nach Bedarf (Transaktionen, Validierung, BackgroundJob).
    /// </summary>
    Task RestoreFromZipAsync(Stream zipStream, string adminUserId, CancellationToken ct = default);
}

/// <summary>
/// Optionaler PDF-Generator: wenn registriert, wird pro Rezept ein PDF erzeugt.
/// Implementierung ist optional; wenn nicht vorhanden, werden keine PDFs erzeugt.
/// </summary>
public interface IPdfGenerator
{
    /// <summary>
    /// Erzeugt ein PDF für das gegebene Rezept (z.B. HTML-to-PDF) und liefert die Binärdaten zurück.
    /// </summary>
    Task<byte[]?> GenerateRecipePdfAsync(ExportRecipeDto recipe, CancellationToken ct = default);
}

/// <summary>
/// Export-Service: erzeugt ZIP mit
/// /recipes.json
/// /images/{recipeId}/imageNN.ext
/// /pdf/{author} - {title}.pdf   (optional)
/// </summary>
public class ExportService : IExportService
{
    private readonly RezepteDbContext _db;
    private readonly ILogger<ExportService> _logger;
    private readonly IPdfGenerator? _pdfGenerator;
    private readonly JsonSerializerOptions _jsonOptions;

    public ExportService(RezepteDbContext db, ILogger<ExportService> logger, IPdfGenerator? pdfGenerator = null)
    {
        _db = db;
        _logger = logger;
        _pdfGenerator = pdfGenerator;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task<Stream> ExportUserAsync(string userId, bool includeImages, bool includePdf, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(userId);
        _logger.LogInformation("Starting user export for {UserId}", userId);

        // Load cookbooks + recipes for the user
        var cookbooks = await _db.Cookbooks
            .AsNoTracking()
            .Where(cb => cb.UserId == userId)
            .ToListAsync(ct);

        // Also include recipes that may not belong to a cookbook? adjust if needed.
        var recipes = await _db.Recipes
            .AsNoTracking()
            .Include(r => r.Images!)
            .Include(r => r.Steps!)
                .ThenInclude(s => s.Ingredients!)
            .Where(r => r.UserId == userId)
            .ToListAsync(ct);

        return await CreateZipAsync(userId, false, cookbooks, recipes, includeImages, includePdf, ct);
    }

    public async Task<Stream> ExportAllAsync(string adminUserId, bool includeImages, bool includePdf, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(adminUserId);
        _logger.LogInformation("Starting admin export by {AdminUserId}", adminUserId);

        // load all users, cookbooks, recipes
        var users = await _db.Users.AsNoTracking().Select(u => new Rezepte.Web.Services.User(u.Id, u.Username, u.Email, u.PasswordHash, u.IsAdmin)).ToListAsync(ct);

        var cookbooks = await _db.Cookbooks
                .AsNoTracking()
                .ToListAsync(ct);
        var recipes = await _db.Recipes
            .AsNoTracking()
            .Include(r => r.RecipeCookbooks)
            .Include(r => r.Images!)
            .Include(r => r.Steps!)
                .ThenInclude(s => s.Ingredients!)
            .ToListAsync(ct);

        return await CreateZipAsync(adminUserId, true, cookbooks, recipes, includeImages, includePdf, ct, users);
    }

    private async Task<Stream> CreateZipAsync(
        string initiatorUserId,
        bool includeUsers,
        List<Cookbook> cookbooks,
        List<Recipe> recipes,
        bool includeImages,
        bool includePdf,
        CancellationToken ct,
        List<User>? users = null)
    {
        ct.ThrowIfCancellationRequested();
        // Prepare DTOs for JSON export
        var cookbookDtos = cookbooks.Select(cb => new ExportCookbookDto
        {
            Id = cb.Id,
            UserId = cb.UserId,
            Title = cb.Name,
            Description = cb.Description
        }).ToList();

        var recipeDtos = new List<ExportRecipeDto>();
        foreach (var r in recipes)
        {
            ct.ThrowIfCancellationRequested();
            var dto = new ExportRecipeDto
            {
                Id = r.Id,
                OwnerId = r.UserId,
                Title = r.Title,
                Description = r.Description,
                Steps = (r.Steps ?? Enumerable.Empty<RecipeStep>())
                    .OrderBy(s => s.StepIndex)
                    .Select(s => new ExportStepDto
                    {
                        Id = s.Id,
                        StepIndex = s.StepIndex,
                        Title = s.Title,
                        Description = s.Description,
                        DurationMinutes = s.DurationMinutes,
                        RequiresOvernightRest = s.RequiresOvernightRest,
                        Ingredients = (s.Ingredients ?? Enumerable.Empty<RecipeIngredient>())
                            .Select(i => new ExportIngredientDto
                            {
                                Id = i.Id,
                                Amount = i.Amount,
                                Unit = i.Unit,
                                Name = i.Name
                            }).ToList()
                    }).ToList(),
                ImagePaths = new List<string>(),
                Cookbooks = (r.RecipeCookbooks ?? Enumerable.Empty<RecipeCookbook>()).Select(rc => new ExportRecipeCookbookDto
                    {
                        RecipeId = r.Id,
                        CookbookId = rc.CookbookId
                    }).ToList()
            };

            // Images: map to relative paths that will be present in the archive
            if (includeImages)
            {
                var imgs = (r.Images ?? Enumerable.Empty<RecipeImage>()).OrderBy(img => img.CreatedAt).ToList();
                var imagePaths = new List<string>();
                for (var i = 0; i < imgs.Count; i++)
                {
                    var img = imgs[i];
                    var ext = Path.GetExtension(img.FileName) ?? ".bin";
                    var imageFileName = $"image{(i + 1):D2}{ext}";
                    var relativePath = Path.Combine("images", r.Id, imageFileName).Replace('\\', '/');
                    imagePaths.Add(relativePath);
                }
                dto.ImagePaths.AddRange(imagePaths);                
            }
            recipeDtos.Add(dto);
        }

        // Build root export object
        var exportRoot = new ExportRootDto
        {
            FormatVersion = "1.0",
            ExportedAt = DateTime.UtcNow,
            Cookbooks = cookbookDtos,
            Recipes = recipeDtos,
            Users = includeUsers ? users?.Select(u => new ExportUserDto { Id = u.Id, UserName = u.Username, Email = u.Email, IsAdmin = u.IsAdmin }).ToList() : null
        };

        // Serialize recipes.json
        var recipesJson = JsonSerializer.Serialize(exportRoot, _jsonOptions);

        // Create ZIP in memory (caller should dispose)
        var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            // recipes.json
            var jsonEntry = archive.CreateEntry("recipes.json", CompressionLevel.Optimal);
            await using (var entryStream = jsonEntry.Open())
            using (var sw = new StreamWriter(entryStream, Encoding.UTF8))
            {
                await sw.WriteAsync(recipesJson).ConfigureAwait(false);
            }

            // images
            foreach (var r in recipes)
            {
                ct.ThrowIfCancellationRequested();
                if (includeImages)
                {
                    var imgs = (r.Images ?? Enumerable.Empty<RecipeImage>()).OrderBy(img => img.CreatedAt).ToList();
                    for (var i = 0; i < imgs.Count; i++)
                    {
                        var img = imgs[i];
                        var ext = Path.GetExtension(img.FileName) ?? ".bin";
                        var imageFileName = $"image{(i + 1):D2}{ext}";
                        var entryPath = $"images/{r.Id}/{imageFileName}";

                        var imageEntry = archive.CreateEntry(entryPath, CompressionLevel.Optimal);
                        await using (var entryStream = imageEntry.Open())
                        {
                            // img.Data may be large; stream it
                            await entryStream.WriteAsync(img.Data, 0, img.Data?.Length ?? 0, ct).ConfigureAwait(false);
                        }
                    }
                }

                // Optional PDF
                if (includePdf && _pdfGenerator != null)
                {
                    ct.ThrowIfCancellationRequested();
                    var exportDto = recipeDtos.FirstOrDefault(x => x.Id == r.Id);
                    if (exportDto != null)
                    {
                        try
                        {
                            var pdfBytes = await _pdfGenerator.GenerateRecipePdfAsync(exportDto, ct).ConfigureAwait(false);
                            if (pdfBytes != null && pdfBytes.Length > 0)
                            {
                                // sanitize file name
                                var safeAuthor = SanitizeFileName(r.UserId ?? "Unknown");
                                var safeTitle = SanitizeFileName(r.Title ?? "Recipe");
                                var pdfName = $"{safeAuthor} - {safeTitle}.pdf";
                                var pdfEntry = archive.CreateEntry($"pdf/{pdfName}", CompressionLevel.Optimal);
                                await using (var entryStream = pdfEntry.Open())
                                {
                                    await entryStream.WriteAsync(pdfBytes, 0, pdfBytes.Length, ct).ConfigureAwait(false);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "PDF generation failed for recipe {RecipeId}", r.Id);
                            // Continue without failing entire export
                        }
                    }
                }
            }

            // metadata for admin exports (optional)
            if (includeUsers && users != null)
            {
                var meta = new
                {
                    exportedAt = DateTime.UtcNow,
                    recipeCount = recipes.Count,
                    cookbookCount = cookbooks.Count,
                    userCount = users.Count
                };
                var metaEntry = archive.CreateEntry("metadata.json", CompressionLevel.Optimal);
                await using (var entryStream = metaEntry.Open())
                using (var sw = new StreamWriter(entryStream, Encoding.UTF8))
                {
                    await sw.WriteAsync(JsonSerializer.Serialize(meta, _jsonOptions)).ConfigureAwait(false);
                }
            }
        }

        ms.Seek(0, SeekOrigin.Begin);
        _logger.LogInformation("Export ZIP prepared (initiator={Initiator})", initiatorUserId);
        return ms;
    }

    public async Task RestoreFromZipAsync(Stream zipStream, string adminUserId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(zipStream);
        _logger.LogInformation("Starting restore by {AdminUserId}", adminUserId);

        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true);

        // recipes.json lesen
        var jsonEntry = archive.GetEntry("recipes.json");
        if (jsonEntry == null)
            throw new InvalidOperationException("Invalid export archive: recipes.json missing.");

        ExportRootDto? exportRoot;
        await using (var jsonStream = jsonEntry.Open())
        {
            exportRoot = await JsonSerializer.DeserializeAsync<ExportRootDto>(jsonStream, _jsonOptions, ct).ConfigureAwait(false);
        }

        if (exportRoot == null)
            throw new InvalidOperationException("Invalid export archive: recipes.json could not be parsed.");

        // Beginne DB-Transaktion für atomare Wiederherstellung
        await using var tx = await _db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
        try
        {
            // --- NEU: Entferne vorhandene Daten, behalte nur das Konto des ausführenden Benutzers ---
            if (string.IsNullOrEmpty(adminUserId))
                throw new InvalidOperationException("adminUserId must be provided to perform destructive restore.");

            _logger.LogInformation("Destructive restore: deleting existing data except user {AdminUserId}", adminUserId);

            // Lösche Dependents zuerst, dann übergeordnete Entitäten.
            // Verwende ExecuteDeleteAsync für performante Batch-Löschungen (EF Core 7+).
            // Falls ExecuteDeleteAsync in eurer Umgebung nicht verfügbar ist, ersetzt durch RemoveRange()-Pattern.
            await _db.RecipeImages.ExecuteDeleteAsync(ct).ConfigureAwait(false);
            await _db.RecipeIngredients.ExecuteDeleteAsync(ct).ConfigureAwait(false);
            await _db.RecipeSteps.ExecuteDeleteAsync(ct).ConfigureAwait(false);
            await _db.RecipeCookbooks.ExecuteDeleteAsync(ct).ConfigureAwait(false);
            await _db.Recipes.ExecuteDeleteAsync(ct).ConfigureAwait(false);
            await _db.Cookbooks.ExecuteDeleteAsync(ct).ConfigureAwait(false);            

            // Benutzer: alle löschen außer adminUserId (das Konto bleibt erhalten)
            await _db.Users.Where(u => u.Id != adminUserId).ExecuteDeleteAsync(ct).ConfigureAwait(false);

            // Stelle sicher, dass DB in konsistentem Zustand ist bevor wir neue Daten anlegen
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);

            // 1) Benutzer anlegen (nur wenn nicht existierend). Passwort-Hash wird nicht wiederhergestellt.
            if (exportRoot.Users != null)
            {
                foreach (var u in exportRoot.Users)
                {
                    ct.ThrowIfCancellationRequested();
                    var exists = await _db.Users.AnyAsync(x => x.Id == u.Id, ct).ConfigureAwait(false);
                    if (exists) continue;

                    var newUser = new Rezepte.Web.Entities.User
                    {
                        Id = u.Id,
                        Username = u.UserName,
                        Email = u.Email ?? string.Empty,
                        PasswordHash = string.Empty, // sichere Wiederherstellung: Admin muss Passwort neu setzen
                        IsAdmin = u.IsAdmin
                    };
                    _db.Users.Add(newUser);
                }
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
            var allUsers = await _db.Users.AsNoTracking().ToListAsync(ct).ConfigureAwait(false);

            // 2) Cookbooks anlegen falls fehlen
            if (exportRoot.Cookbooks != null)
            {
                foreach (var cb in exportRoot.Cookbooks)
                {
                    ct.ThrowIfCancellationRequested();
                    var exists = await _db.Cookbooks.AnyAsync(x => x.Id == cb.Id, ct).ConfigureAwait(false);
                    if (exists) continue;

                    var newCb = new Rezepte.Web.Entities.Cookbook
                    {
                        Id = cb.Id,
                        UserId = cb.UserId,
                        Name = cb.Title,
                        Description = cb.Description,
                        CreatedAt = DateTime.UtcNow
                    };
                    if (!allUsers.Any(u => u.Id == newCb.UserId))
                        newCb.UserId = adminUserId;
                    _db.Cookbooks.Add(newCb);
                }
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            }

            // 3) Rezepte, Schritte, Zutaten, Bilder
            if (exportRoot.Recipes != null)
            {
                foreach (var r in exportRoot.Recipes)
                {
                    ct.ThrowIfCancellationRequested();

                    var recipeExists = await _db.Recipes.AnyAsync(x => x.Id == r.Id, ct).ConfigureAwait(false);
                    if (!recipeExists)
                    {
                        var newRecipe = new Rezepte.Web.Entities.Recipe
                        {
                            Id = r.Id,
                            UserId = r.OwnerId ?? string.Empty,
                            Title = r.Title ?? string.Empty,
                            Description = r.Description,
                            CreatedAt = DateTime.UtcNow
                        };
                        if (!allUsers.Any(u => u.Id == newRecipe.UserId))
                            newRecipe.UserId = adminUserId;

                        foreach (var cb in r.Cookbooks ?? Enumerable.Empty<ExportRecipeCookbookDto>())
                        {
                            // Prüfe, ob Cookbook existiert
                            var cbExists = await _db.Cookbooks.AnyAsync(x => x.Id == cb.CookbookId, ct).ConfigureAwait(false);
                            if (!cbExists) continue;
                            var rc = new Rezepte.Web.Entities.RecipeCookbook
                            {
                                RecipeId = r.Id,
                                CookbookId = cb.CookbookId
                            };
                            newRecipe.RecipeCookbooks.Add(rc);
                        }

                        _db.Recipes.Add(newRecipe);
                        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
                    }

                    // Schritte & Zutaten (lege nur an, falls die Step-Id nicht vorhanden ist)
                    if (r.Steps != null)
                    {
                        foreach (var s in r.Steps.OrderBy(x => x.StepIndex))
                        {
                            var stepExists = await _db.RecipeSteps.AnyAsync(x => x.Id == s.Id, ct).ConfigureAwait(false);
                            if (!stepExists)
                            {
                                var newStep = new Rezepte.Web.Entities.RecipeStep
                                {
                                    Id = s.Id,
                                    RecipeId = r.Id,
                                    StepIndex = s.StepIndex,
                                    Title = s.Title,
                                    Description = s.Description,
                                    DurationMinutes = s.DurationMinutes,
                                    RequiresOvernightRest = s.RequiresOvernightRest
                                };
                                _db.RecipeSteps.Add(newStep);
                            }

                            if (s.Ingredients != null)
                            {
                                foreach (var ing in s.Ingredients)
                                {
                                    var ingExists = await _db.RecipeIngredients.AnyAsync(x => x.Id == ing.Id, ct).ConfigureAwait(false);
                                    if (!ingExists)
                                    {
                                        var newIng = new Rezepte.Web.Entities.RecipeIngredient
                                        {
                                            Id = ing.Id,
                                            StepId = s.Id,
                                            Amount = ing.Amount,
                                            Unit = ing.Unit,
                                            Name = ing.Name
                                        };
                                        _db.RecipeIngredients.Add(newIng);
                                    }
                                }
                            }
                        }
                        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
                    }

                    // Bilder: werden im ExportRoot als relative Pfade (images/{recipeId}/...) gelistet.
                    if (r.ImagePaths != null)
                    {
                        foreach (var imgPath in r.ImagePaths)
                        {
                            var normalized = imgPath.Replace('\\', '/');
                            var entry = archive.GetEntry(normalized);
                            if (entry == null) continue;

                            // Prüfe, ob Bild bereits existiert (vergleich auf FileName + RecipeId)
                            var fileName = Path.GetFileName(normalized);
                            var imgExists = await _db.RecipeImages.AnyAsync(x => x.RecipeId == r.Id && x.FileName == fileName, ct).ConfigureAwait(false);
                            if (imgExists) continue;

                            await using var entryStream = entry.Open();
                            await using var msImg = new MemoryStream();
                            await entryStream.CopyToAsync(msImg, ct).ConfigureAwait(false);
                            var imgBytes = msImg.ToArray();

                            var newImg = new Rezepte.Web.Entities.RecipeImage
                            {
                                Id = Guid.NewGuid().ToString(),
                                RecipeId = r.Id,
                                FileName = fileName,
                                ContentType = GetContentTypeFromExtension(Path.GetExtension(fileName)),
                                Data = imgBytes,
                                CreatedAt = DateTime.UtcNow
                            };
                            _db.RecipeImages.Add(newImg);
                        }
                        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
                    }
                }
            }

            await tx.CommitAsync(ct).ConfigureAwait(false);
            _logger.LogInformation("Restore finished successfully by {AdminUserId}", adminUserId);
        }
        catch (OperationCanceledException)
        {
            await tx.RollbackAsync(ct).ConfigureAwait(false);
            throw;
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct).ConfigureAwait(false);
            _logger.LogError(ex, "Restore failed, transaction rolled back (admin={AdminUserId})", adminUserId);
            throw;
        }
    }

    private static string GetContentTypeFromExtension(string? ext)
    {
        if (string.IsNullOrEmpty(ext)) return "application/octet-stream";
        ext = ext.ToLowerInvariant();
        return ext switch
        {
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }

    private static string SanitizeFileName(string input)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            input = input.Replace(c, '_');
        }
        return input;
    }

}

#region DTOs for export JSON
public record ExportRootDto
{
    public string FormatVersion { get; init; } = "1.0";
    public DateTime ExportedAt { get; init; }
    public List<ExportCookbookDto>? Cookbooks { get; init; }
    public List<ExportRecipeDto>? Recipes { get; init; }
    public List<ExportUserDto>? Users { get; init; }
}

public record ExportCookbookDto
{
    public string Id { get; init; } = default!;
    public string UserId { get; init; } = default!;
    public string Title { get; init; } = default!;
    public string? Description { get; init; }
}

public record ExportRecipeDto
{
    public string Id { get; init; } = default!;
    public string? OwnerId { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? Uri { get; init; }
    public List<ExportStepDto>? Steps { get; init; }
    public List<string>? ImagePaths { get; init; }
    public List<ExportRecipeCookbookDto>? Cookbooks { get; init; }
}

public record ExportRecipeCookbookDto
{
    public string RecipeId { get; init; } = default!;
    public string CookbookId { get; init; } = default!;
}

public record ExportStepDto
{
    public string Id { get; init; } = default!;
    public int StepIndex { get; init; }
    public string? Title { get; init; }
    public string Description { get; init; } = default!;
    public int DurationMinutes { get; init; }
    public bool RequiresOvernightRest { get; init; }
    public List<ExportIngredientDto>? Ingredients { get; init; }
}

public record ExportIngredientDto
{
    public string Id { get; init; } = default!;
    public decimal Amount { get; init; }
    public string? Unit { get; init; }
    public string Name { get; init; } = default!;
}

public record ExportUserDto
{
    public string Id { get; init; } = default!;
    public string UserName { get; init; } = default!;
    public string? Email { get; init; }
    public bool IsAdmin { get; init; }
}
#endregion