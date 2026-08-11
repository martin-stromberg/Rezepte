using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rezepte.Web.Configuration;
using Rezepte.Web.Data;
using Rezepte.Web.Entities;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using static Grpc.Core.Metadata;

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
    /// Achtung: Implementierung ist vorsichtig - legt fehlende Entitaeten an, ueberschreibt
    /// bestehende nicht. Pruefe und erweitere nach Bedarf (Transaktionen, Validierung, BackgroundJob).
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
    /// Erzeugt ein PDF fuer das gegebene Rezept (z.B. HTML-to-PDF) und liefert die Binaerdaten zurueck.
    /// </summary>
    Task<byte[]?> GenerateRecipePdfAsync(ExportRecipeDto recipe, CancellationToken ct = default);
}

/// <summary>
/// Export-Service: erzeugt ZIP mit
/// /recipes.json
/// /images/{recipeId}/imageNN.ext
/// /pdf/{author} - {title}.pdf   (optional)
/// </summary>
public class ExportService : BaseService, IExportService
{
    private static readonly SemaphoreSlim _restoreLock = new(1, 1);
    private readonly RezepteDbContext _db;
    private readonly ILogger<ExportService> _logger;
    private readonly IPdfGenerator? _pdfGenerator;
    private readonly RestoreValidationOptions _validationOptions;
    private readonly JsonSerializerOptions _jsonOptions;

    public ExportService(RezepteDbContext db, ILogger<ExportService> logger, IPdfGenerator? pdfGenerator = null, RestoreValidationOptions? validationOptions = null)
    {
        _db = db;
        _logger = logger;
        _pdfGenerator = pdfGenerator;
        _validationOptions = validationOptions ?? new RestoreValidationOptions();
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
        var users = await _db.Users.AsNoTracking().Select(u => MatchUser(u)).ToListAsync(ct);

        var cookbooks = await _db.Cookbooks
                .AsNoTracking()
                .ToListAsync(ct);
        var recipes = await _db.Recipes
            .AsNoTracking()
            .Include(r => r.RecipeCookbooks)
            .Include(r => r.SideDishes)
            .Include(r => r.Images!)
            .Include(r => r.Steps!)
                .ThenInclude(s => s.Ingredients!)
            .ToListAsync(ct);
        var systemData = await CreateSystemBackupDataAsync(ct).ConfigureAwait(false);

        return await CreateZipAsync(adminUserId, true, cookbooks, recipes, includeImages, includePdf, ct, users, systemData);
    }

    private async Task<Stream> CreateZipAsync(
        string initiatorUserId,
        bool includeUsers,
        List<Cookbook> cookbooks,
        List<Recipe> recipes,
        bool includeImages,
        bool includePdf,
        CancellationToken ct,
        List<User>? users = null,
        ExportSystemDataDto? systemData = null)
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
                Uri = r.Uri,
                Portions = r.Portions,
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
                }).ToList(),
                SideDishes = (r.SideDishes ?? Enumerable.Empty<RecipeSideDish>())
                    .OrderBy(sd => sd.OrderIndex)
                    .Select(sd => new ExportRecipeSideDishDto
                    {
                        Id = sd.Id,
                        RecipeId = sd.RecipeId,
                        SideDishRecipeId = sd.SideDishRecipeId,
                        OrderIndex = sd.OrderIndex
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
            Users = includeUsers ? users?.Select(u => new ExportUserDto { Id = u.Id, UserName = u.Username, Email = u.Email, IsAdmin = u.IsAdmin }).ToList() : null,
            SystemData = includeUsers ? systemData : null
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

    private async Task<ExportSystemDataDto> CreateSystemBackupDataAsync(CancellationToken ct)
    {
        return new ExportSystemDataDto
        {
            CalendarEvents = await _db.CalendarEvents.AsNoTracking()
                .Select(e => new ExportCalendarEventDto
                {
                    Id = e.Id,
                    UserId = e.UserId,
                    StartDate = e.StartDate,
                    TimeOfDay = e.TimeOfDay,
                    RecipeId = e.RecipeId,
                    Portions = e.Portions,
                    Recurrence = e.Recurrence,
                    RecurrenceDays = e.RecurrenceDays,
                    CreatedAt = e.CreatedAt,
                    ModifiedAt = e.ModifiedAt
                })
                .ToListAsync(ct)
                .ConfigureAwait(false),
            ShoppingListGroups = await _db.ShoppingListGroups.AsNoTracking()
                .Select(g => new ExportShoppingListGroupDto
                {
                    Id = g.Id,
                    UserId = g.UserId,
                    Name = g.Name,
                    RecipeId = g.RecipeId,
                    OrderIndex = g.OrderIndex,
                    CreatedAt = g.CreatedAt,
                    ModifiedAt = g.ModifiedAt
                })
                .ToListAsync(ct)
                .ConfigureAwait(false),
            ShoppingListItems = await _db.ShoppingListItems.AsNoTracking()
                .Select(i => new ExportShoppingListItemDto
                {
                    Id = i.Id,
                    GroupId = i.GroupId,
                    Amount = i.Amount,
                    Unit = i.Unit,
                    Name = i.Name,
                    IsChecked = i.IsChecked,
                    OrderIndex = i.OrderIndex,
                    CreatedAt = i.CreatedAt,
                    ModifiedAt = i.ModifiedAt
                })
                .ToListAsync(ct)
                .ConfigureAwait(false),
            UserSettings = await _db.UserSettings.AsNoTracking()
                .Select(s => new ExportUserSettingDto
                {
                    UserId = s.UserId,
                    AiEnabled = s.AiEnabled,
                    GoogleVisionEnabled = s.GoogleVisionEnabled,
                    GeminiEnabled = s.GeminiEnabled,
                    RequireAiConfirmation = s.RequireAiConfirmation
                })
                .ToListAsync(ct)
                .ConfigureAwait(false),
            AppSettings = await _db.AppSettings.AsNoTracking()
                .Select(s => new ExportAppSettingDto
                {
                    Key = s.Key,
                    Value = s.Value
                })
                .ToListAsync(ct)
                .ConfigureAwait(false),
            PluginSettings = await _db.PluginSettings.AsNoTracking()
                .Select(p => new ExportPluginSettingDto
                {
                    PluginId = p.PluginId,
                    DisplayName = p.DisplayName,
                    Description = p.Description,
                    AssemblyName = p.AssemblyName,
                    TypeName = p.TypeName,
                    Enabled = p.Enabled,
                    OrderIndex = p.OrderIndex,
                    Status = p.Status,
                    Error = p.Error,
                    DiscoveredAt = p.DiscoveredAt,
                    LastSeenAt = p.LastSeenAt
                })
                .ToListAsync(ct)
                .ConfigureAwait(false),
            PluginSources = await _db.PluginSources.AsNoTracking()
                .Select(p => new ExportPluginSourceDto
                {
                    Id = p.Id,
                    RepositoryUrl = p.RepositoryUrl,
                    Owner = p.Owner,
                    Repository = p.Repository,
                    IsPrivate = p.IsPrivate,
                    Enabled = p.Enabled,
                    TrustConfirmed = p.TrustConfirmed,
                    SecretName = p.SecretName,
                    LastSuccessfulReleaseTag = p.LastSuccessfulReleaseTag,
                    LastError = p.LastError,
                    LastCheckedAt = p.LastCheckedAt,
                    LastErrorAt = p.LastErrorAt,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt
                })
                .ToListAsync(ct)
                .ConfigureAwait(false),
            PluginSourceReleases = await _db.PluginSourceReleases.AsNoTracking()
                .Select(r => new ExportPluginSourceReleaseDto
                {
                    Id = r.Id,
                    PluginSourceId = r.PluginSourceId,
                    ReleaseTag = r.ReleaseTag,
                    GitHubReleaseId = r.GitHubReleaseId,
                    AssetId = r.AssetId,
                    AssetName = r.AssetName,
                    Status = r.Status,
                    Error = r.Error,
                    CreatedAt = r.CreatedAt,
                    DownloadedAt = r.DownloadedAt,
                    ValidatedAt = r.ValidatedAt,
                    InstalledAt = r.InstalledAt,
                    ReloadStatus = r.ReloadStatus,
                    ReloadedAt = r.ReloadedAt,
                    ReloadError = r.ReloadError
                })
                .ToListAsync(ct)
                .ConfigureAwait(false),
            AiRequestLogs = await _db.AiRequestLogs.AsNoTracking()
                .Select(l => new ExportAiRequestLogDto
                {
                    Id = l.Id,
                    UserId = l.UserId,
                    Service = l.Service,
                    Timestamp = l.Timestamp,
                    Type = l.Type
                })
                .ToListAsync(ct)
                .ConfigureAwait(false),
            BackgroundJobs = await _db.BackgroundJobs.AsNoTracking()
                .Select(j => new ExportBackgroundJobDto
                {
                    Id = j.Id,
                    JobType = j.JobType,
                    InitiatorUserId = j.InitiatorUserId,
                    CreatedAt = j.CreatedAt,
                    StartedAt = j.StartedAt,
                    CompletedAt = j.CompletedAt,
                    Status = j.Status,
                    PayloadJson = j.PayloadJson,
                    Progress = j.Progress,
                    ResultMessage = j.ResultMessage,
                    Error = j.Error
                })
                .ToListAsync(ct)
                .ConfigureAwait(false)
        };
    }

    public async Task RestoreFromZipAsync(Stream zipStream, string adminUserId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(zipStream);
        _logger.LogInformation("Starting restore by {AdminUserId}", adminUserId);

        await _restoreLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (zipStream.CanSeek && zipStream.Length > _validationOptions.MaxUploadFileSizeBytes)
                throw new InvalidDataException("Restore archive exceeds the maximum upload size.");

            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true);

            ValidateArchive(archive, _validationOptions);

            // recipes.json lesen
            var jsonEntry = archive.GetEntry("recipes.json");
            if (jsonEntry == null)
                throw new InvalidDataException("Invalid export archive: recipes.json missing.");

            var recipeJsonBytes = await ReadEntryBytesAsync(jsonEntry, _validationOptions.MaxRecipesJsonUncompressedBytes, ct).ConfigureAwait(false);
            await using var recipeJsonStream = new MemoryStream(recipeJsonBytes);
            var exportRoot = await JsonSerializer.DeserializeAsync<ExportRootDto>(recipeJsonStream, _jsonOptions, ct).ConfigureAwait(false);

            if (exportRoot == null)
                throw new InvalidDataException("Invalid export archive: recipes.json could not be parsed.");

            // Beginne DB-Transaktion fuer atomare Wiederherstellung
            await using var tx = await _db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
            try
            {
                // --- NEU: Entferne vorhandene Daten, behalte nur das Konto des ausfuehrenden Benutzers ---
                if (string.IsNullOrEmpty(adminUserId))
                    throw new InvalidOperationException("adminUserId must be provided to perform destructive restore.");

                _logger.LogInformation("Destructive restore: deleting existing data except user {AdminUserId}", adminUserId);

                // Loesche Dependents zuerst, dann uebergeordnete Entitaeten.
                // Verwende ExecuteDeleteAsync fuer performante Batch-Loeschungen (EF Core 7+).
                // Falls ExecuteDeleteAsync in eurer Umgebung nicht verfuegbar ist, ersetzt durch RemoveRange()-Pattern.
                await _db.ShoppingListItems.ExecuteDeleteAsync(ct).ConfigureAwait(false);
                await _db.ShoppingListGroups.ExecuteDeleteAsync(ct).ConfigureAwait(false);
                await _db.CalendarEvents.ExecuteDeleteAsync(ct).ConfigureAwait(false);
                await _db.BackgroundJobs.ExecuteDeleteAsync(ct).ConfigureAwait(false);
                await _db.AiRequestLogs.ExecuteDeleteAsync(ct).ConfigureAwait(false);
                await _db.PluginSourceReleases.ExecuteDeleteAsync(ct).ConfigureAwait(false);
                await _db.PluginSources.ExecuteDeleteAsync(ct).ConfigureAwait(false);
                await _db.PluginSettings.ExecuteDeleteAsync(ct).ConfigureAwait(false);
                await _db.AppSettings.ExecuteDeleteAsync(ct).ConfigureAwait(false);
                await _db.UserSettings.ExecuteDeleteAsync(ct).ConfigureAwait(false);
                await _db.RecipeImages.ExecuteDeleteAsync(ct).ConfigureAwait(false);
                await _db.RecipeIngredients.ExecuteDeleteAsync(ct).ConfigureAwait(false);
                await _db.RecipeSteps.ExecuteDeleteAsync(ct).ConfigureAwait(false);
                await _db.RecipeCookbooks.ExecuteDeleteAsync(ct).ConfigureAwait(false);
                await _db.RecipeSideDishes.ExecuteDeleteAsync(ct).ConfigureAwait(false);
                await _db.Recipes.ExecuteDeleteAsync(ct).ConfigureAwait(false);
                await _db.Cookbooks.ExecuteDeleteAsync(ct).ConfigureAwait(false);

                // Benutzer: alle loeschen ausser adminUserId (das Konto bleibt erhalten)
                await _db.Users.Where(u => u.Id != adminUserId).ExecuteDeleteAsync(ct).ConfigureAwait(false);

                // Stelle sicher, dass DB in konsistentem Zustand ist bevor wir neue Daten anlegen
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
                _db.ChangeTracker.Clear();

                // 1) Benutzer anlegen (nur wenn nicht existierend). Passwort-Hash wird nicht wiederhergestellt.
                if (exportRoot.Users != null)
                {
                    var existingUserNames = await _db.Users
                        .AsNoTracking()
                        .Select(u => u.Username)
                        .ToListAsync(ct)
                        .ConfigureAwait(false);
                    var knownUserNames = existingUserNames.ToHashSet(StringComparer.OrdinalIgnoreCase);

                    foreach (var u in exportRoot.Users)
                    {
                        ct.ThrowIfCancellationRequested();
                        var exists = await _db.Users.AnyAsync(x => x.Id == u.Id, ct).ConfigureAwait(false);
                        if (exists) continue;
                        if (!knownUserNames.Add(u.UserName))
                        {
                            _logger.LogInformation(
                                "Skipping restored user {RestoredUserId} because username {Username} already exists. Owned data will be assigned to restore admin if needed.",
                                u.Id,
                                u.UserName);
                            continue;
                        }

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
                var allUsers = (await _db.Users
                        .AsNoTracking()
                        .Select(u => u.Id)
                        .ToListAsync(ct)
                        .ConfigureAwait(false))
                    .ToHashSet(StringComparer.Ordinal);

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
                        if (!allUsers.Contains(newCb.UserId))
                            newCb.UserId = adminUserId;
                        _db.Cookbooks.Add(newCb);
                    }
                    await _db.SaveChangesAsync(ct).ConfigureAwait(false);
                }

                // 3) Rezepte, Schritte, Zutaten, Bilder
                if (exportRoot.Recipes != null)
                {
                    long totalImageBytes = 0;
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
                                Uri = r.Uri,
                                Portions = r.Portions ?? 0,
                                CreatedAt = DateTime.UtcNow
                            };
                            if (!allUsers.Contains(newRecipe.UserId))
                                newRecipe.UserId = adminUserId;

                            foreach (var cb in r.Cookbooks ?? Enumerable.Empty<ExportRecipeCookbookDto>())
                            {
                                // Pruefe, ob Cookbook existiert
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

                                // Pruefe, ob Bild bereits existiert (Vergleich auf FileName + RecipeId)
                                var fileName = Path.GetFileName(normalized);
                                var imgExists = await _db.RecipeImages.AnyAsync(x => x.RecipeId == r.Id && x.FileName == fileName, ct).ConfigureAwait(false);
                                if (imgExists) continue;

                                var imgBytes = await ReadEntryBytesAsync(entry, _validationOptions.MaxImageUncompressedBytes, ct).ConfigureAwait(false);

                                totalImageBytes += imgBytes.Length;
                                if (totalImageBytes > _validationOptions.MaxTotalImageBytes)
                                    throw new InvalidDataException("Total image size in restore archive exceeds the allowed limit.");

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

                    var restoredRecipeIds = (await _db.Recipes
                            .AsNoTracking()
                            .Select(r => r.Id)
                            .ToListAsync(ct)
                            .ConfigureAwait(false))
                        .ToHashSet(StringComparer.Ordinal);

                    foreach (var r in exportRoot.Recipes)
                    {
                        foreach (var sd in r.SideDishes ?? Enumerable.Empty<ExportRecipeSideDishDto>())
                        {
                            ct.ThrowIfCancellationRequested();
                            var recipeId = string.IsNullOrWhiteSpace(sd.RecipeId) ? r.Id : sd.RecipeId;
                            if (!restoredRecipeIds.Contains(recipeId) || !restoredRecipeIds.Contains(sd.SideDishRecipeId))
                                continue;

                            var sideDishExists = await _db.RecipeSideDishes
                                .AnyAsync(x => x.Id == sd.Id || (x.RecipeId == recipeId && x.SideDishRecipeId == sd.SideDishRecipeId), ct)
                                .ConfigureAwait(false);
                            if (sideDishExists) continue;

                            _db.RecipeSideDishes.Add(new RecipeSideDish
                            {
                                Id = sd.Id,
                                RecipeId = recipeId,
                                SideDishRecipeId = sd.SideDishRecipeId,
                                OrderIndex = sd.OrderIndex
                            });
                        }
                    }
                    await _db.SaveChangesAsync(ct).ConfigureAwait(false);
                }

                await RestoreSystemDataAsync(exportRoot.SystemData, adminUserId, ct).ConfigureAwait(false);

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
        finally
        {
            _restoreLock.Release();
        }
    }

    private static void ValidateArchive(ZipArchive archive, RestoreValidationOptions options)
    {
        if (archive.Entries.Count > options.MaxArchiveEntries)
            throw new InvalidDataException($"Archive contains {archive.Entries.Count} entries, exceeding the limit of {options.MaxArchiveEntries}.");

        long totalUncompressed = 0;
        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrWhiteSpace(entry.FullName) || entry.FullName.EndsWith("/", StringComparison.Ordinal))
                continue;

            var normalized = entry.FullName.Replace('\\', '/');
            if (normalized.StartsWith("/", StringComparison.Ordinal) ||
                normalized.Contains("../", StringComparison.Ordinal) ||
                normalized.Contains("/..", StringComparison.Ordinal) ||
                normalized == "..")
                throw new InvalidDataException($"Archive entry {entry.FullName} uses an invalid path.");

            var uncompressed = entry.Length;
            var compressed = entry.CompressedLength;
            if (uncompressed > 0 && compressed > 0)
            {
                var ratio = (double)uncompressed / compressed;
                if (ratio > options.MaxCompressionRatio)
                    throw new InvalidDataException($"Archive entry {entry.FullName} has a compression ratio of {ratio:F1}, exceeding the limit of {options.MaxCompressionRatio}.");
            }

            if (entry.FullName.StartsWith("images/", StringComparison.OrdinalIgnoreCase) &&
                uncompressed > options.MaxImageUncompressedBytes)
                throw new InvalidDataException($"Image {entry.FullName} size {uncompressed} exceeds the limit {options.MaxImageUncompressedBytes}.");

            totalUncompressed += uncompressed;
        }

        if (totalUncompressed > options.MaxTotalUncompressedBytes)
            throw new InvalidDataException($"Archive total uncompressed size {totalUncompressed} exceeds the limit {options.MaxTotalUncompressedBytes}.");

        var recipesJson = archive.Entries.FirstOrDefault(e => string.Equals(e.FullName, "recipes.json", StringComparison.OrdinalIgnoreCase));
        if (recipesJson == null)
            throw new InvalidDataException("Invalid export archive: recipes.json missing.");
        if (recipesJson.Length > options.MaxRecipesJsonUncompressedBytes)
            throw new InvalidDataException($"recipes.json size {recipesJson.Length} exceeds the limit {options.MaxRecipesJsonUncompressedBytes}.");
    }

    private static async Task<byte[]> ReadEntryBytesAsync(ZipArchiveEntry entry, long maxBytes, CancellationToken ct)
    {
        await using var entryStream = entry.Open();
        var buffer = new byte[8192];
        var ms = new MemoryStream();
        long total = 0;
        int read;

        while ((read = await entryStream.ReadAsync(buffer.AsMemory(0, buffer.Length), ct).ConfigureAwait(false)) > 0)
        {
            total += read;
            if (total > maxBytes)
                throw new InvalidDataException($"Archive entry {entry.FullName} exceeds the limit {maxBytes}.");

            await ms.WriteAsync(buffer.AsMemory(0, read), ct).ConfigureAwait(false);
        }

        return ms.ToArray();
    }

    private async Task RestoreSystemDataAsync(ExportSystemDataDto? systemData, string adminUserId, CancellationToken ct)
    {
        if (systemData is null)
            return;

        var userIds = (await _db.Users
                .AsNoTracking()
                .Select(u => u.Id)
                .ToListAsync(ct)
                .ConfigureAwait(false))
            .ToHashSet(StringComparer.Ordinal);
        var recipeIds = (await _db.Recipes
                .AsNoTracking()
                .Select(r => r.Id)
                .ToListAsync(ct)
                .ConfigureAwait(false))
            .ToHashSet(StringComparer.Ordinal);

        string ResolveRequiredUserId(string? userId) =>
            !string.IsNullOrWhiteSpace(userId) && userIds.Contains(userId) ? userId : adminUserId;

        string? ResolveRecipeId(string? recipeId) =>
            !string.IsNullOrWhiteSpace(recipeId) && recipeIds.Contains(recipeId) ? recipeId : null;

        foreach (var setting in systemData.UserSettings)
        {
            var userId = ResolveRequiredUserId(setting.UserId);
            if (await _db.UserSettings.AnyAsync(s => s.UserId == userId, ct).ConfigureAwait(false))
                continue;

            _db.UserSettings.Add(new UserSetting
            {
                UserId = userId,
                AiEnabled = setting.AiEnabled,
                GoogleVisionEnabled = setting.GoogleVisionEnabled,
                GeminiEnabled = setting.GeminiEnabled,
                RequireAiConfirmation = setting.RequireAiConfirmation
            });
        }

        foreach (var setting in systemData.AppSettings)
        {
            if (await _db.AppSettings.AnyAsync(s => s.Key == setting.Key, ct).ConfigureAwait(false))
                continue;

            _db.AppSettings.Add(new AppSetting
            {
                Key = setting.Key,
                Value = setting.Value
            });
        }

        foreach (var setting in systemData.PluginSettings)
        {
            if (await _db.PluginSettings.AnyAsync(p => p.PluginId == setting.PluginId, ct).ConfigureAwait(false))
                continue;

            _db.PluginSettings.Add(new PluginSetting
            {
                PluginId = setting.PluginId,
                DisplayName = setting.DisplayName,
                Description = setting.Description,
                AssemblyName = setting.AssemblyName,
                TypeName = setting.TypeName,
                Enabled = setting.Enabled,
                OrderIndex = setting.OrderIndex,
                Status = setting.Status,
                Error = setting.Error,
                DiscoveredAt = setting.DiscoveredAt,
                LastSeenAt = setting.LastSeenAt
            });
        }

        foreach (var source in systemData.PluginSources)
        {
            if (await _db.PluginSources.AnyAsync(p => p.Id == source.Id, ct).ConfigureAwait(false))
                continue;

            _db.PluginSources.Add(new PluginSource
            {
                Id = source.Id,
                RepositoryUrl = source.RepositoryUrl,
                Owner = source.Owner,
                Repository = source.Repository,
                IsPrivate = source.IsPrivate,
                Enabled = source.Enabled,
                TrustConfirmed = source.TrustConfirmed,
                SecretName = source.SecretName,
                LastSuccessfulReleaseTag = source.LastSuccessfulReleaseTag,
                LastError = source.LastError,
                LastCheckedAt = source.LastCheckedAt,
                LastErrorAt = source.LastErrorAt,
                CreatedAt = source.CreatedAt,
                UpdatedAt = source.UpdatedAt
            });
        }
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        var pluginSourceIds = (await _db.PluginSources
                .AsNoTracking()
                .Select(p => p.Id)
                .ToListAsync(ct)
                .ConfigureAwait(false))
            .ToHashSet(StringComparer.Ordinal);
        foreach (var release in systemData.PluginSourceReleases)
        {
            if (!pluginSourceIds.Contains(release.PluginSourceId))
                continue;
            if (await _db.PluginSourceReleases.AnyAsync(r => r.Id == release.Id, ct).ConfigureAwait(false))
                continue;

            _db.PluginSourceReleases.Add(new PluginSourceRelease
            {
                Id = release.Id,
                PluginSourceId = release.PluginSourceId,
                ReleaseTag = release.ReleaseTag,
                GitHubReleaseId = release.GitHubReleaseId,
                AssetId = release.AssetId,
                AssetName = release.AssetName,
                Status = release.Status,
                Error = release.Error,
                CreatedAt = release.CreatedAt,
                DownloadedAt = release.DownloadedAt,
                ValidatedAt = release.ValidatedAt,
                InstalledAt = release.InstalledAt,
                ReloadStatus = release.ReloadStatus,
                ReloadedAt = release.ReloadedAt,
                ReloadError = release.ReloadError
            });
        }

        foreach (var calendarEvent in systemData.CalendarEvents)
        {
            if (await _db.CalendarEvents.AnyAsync(e => e.Id == calendarEvent.Id, ct).ConfigureAwait(false))
                continue;

            _db.CalendarEvents.Add(new CalendarEvent
            {
                Id = calendarEvent.Id,
                UserId = ResolveRequiredUserId(calendarEvent.UserId),
                StartDate = calendarEvent.StartDate,
                TimeOfDay = calendarEvent.TimeOfDay,
                RecipeId = ResolveRecipeId(calendarEvent.RecipeId),
                Portions = calendarEvent.Portions,
                Recurrence = calendarEvent.Recurrence,
                RecurrenceDays = calendarEvent.RecurrenceDays,
                CreatedAt = calendarEvent.CreatedAt,
                ModifiedAt = calendarEvent.ModifiedAt
            });
        }

        foreach (var group in systemData.ShoppingListGroups)
        {
            if (await _db.ShoppingListGroups.AnyAsync(g => g.Id == group.Id, ct).ConfigureAwait(false))
                continue;

            _db.ShoppingListGroups.Add(new ShoppingListGroup
            {
                Id = group.Id,
                UserId = ResolveRequiredUserId(group.UserId),
                Name = group.Name,
                RecipeId = ResolveRecipeId(group.RecipeId),
                OrderIndex = group.OrderIndex,
                CreatedAt = group.CreatedAt,
                ModifiedAt = group.ModifiedAt
            });
        }
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        var shoppingListGroupIds = (await _db.ShoppingListGroups
                .AsNoTracking()
                .Select(g => g.Id)
                .ToListAsync(ct)
                .ConfigureAwait(false))
            .ToHashSet(StringComparer.Ordinal);
        foreach (var item in systemData.ShoppingListItems)
        {
            if (!shoppingListGroupIds.Contains(item.GroupId))
                continue;
            if (await _db.ShoppingListItems.AnyAsync(i => i.Id == item.Id, ct).ConfigureAwait(false))
                continue;

            _db.ShoppingListItems.Add(new ShoppingListItem
            {
                Id = item.Id,
                GroupId = item.GroupId,
                Amount = item.Amount,
                Unit = item.Unit,
                Name = item.Name,
                IsChecked = item.IsChecked,
                OrderIndex = item.OrderIndex,
                CreatedAt = item.CreatedAt,
                ModifiedAt = item.ModifiedAt
            });
        }

        foreach (var log in systemData.AiRequestLogs)
        {
            if (await _db.AiRequestLogs.AnyAsync(l => l.Id == log.Id, ct).ConfigureAwait(false))
                continue;

            _db.AiRequestLogs.Add(new AiRequestLog
            {
                Id = log.Id,
                UserId = ResolveRequiredUserId(log.UserId),
                Service = log.Service,
                Timestamp = log.Timestamp,
                Type = log.Type
            });
        }

        foreach (var job in systemData.BackgroundJobs)
        {
            if (await _db.BackgroundJobs.AnyAsync(j => j.Id == job.Id, ct).ConfigureAwait(false))
                continue;

            _db.BackgroundJobs.Add(new BackgroundJobs.BackgroundJob
            {
                Id = job.Id,
                JobType = job.JobType,
                InitiatorUserId = string.IsNullOrWhiteSpace(job.InitiatorUserId)
                    ? null
                    : ResolveRequiredUserId(job.InitiatorUserId),
                CreatedAt = job.CreatedAt,
                StartedAt = job.StartedAt,
                CompletedAt = job.CompletedAt,
                Status = job.Status,
                PayloadJson = job.PayloadJson,
                Progress = job.Progress,
                ResultMessage = job.ResultMessage,
                Error = job.Error
            });
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
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
    public ExportSystemDataDto? SystemData { get; init; }
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
    public int? Portions { get; init; }
    public List<ExportStepDto>? Steps { get; init; }
    public List<string>? ImagePaths { get; init; }
    public List<ExportRecipeCookbookDto>? Cookbooks { get; init; }
    public List<ExportRecipeSideDishDto>? SideDishes { get; init; }
}

public record ExportRecipeCookbookDto
{
    public string RecipeId { get; init; } = default!;
    public string CookbookId { get; init; } = default!;
}

public record ExportRecipeSideDishDto
{
    public string Id { get; init; } = default!;
    public string RecipeId { get; init; } = default!;
    public string SideDishRecipeId { get; init; } = default!;
    public int OrderIndex { get; init; }
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

public record ExportSystemDataDto
{
    public List<ExportCalendarEventDto> CalendarEvents { get; init; } = [];
    public List<ExportShoppingListGroupDto> ShoppingListGroups { get; init; } = [];
    public List<ExportShoppingListItemDto> ShoppingListItems { get; init; } = [];
    public List<ExportUserSettingDto> UserSettings { get; init; } = [];
    public List<ExportAppSettingDto> AppSettings { get; init; } = [];
    public List<ExportPluginSettingDto> PluginSettings { get; init; } = [];
    public List<ExportPluginSourceDto> PluginSources { get; init; } = [];
    public List<ExportPluginSourceReleaseDto> PluginSourceReleases { get; init; } = [];
    public List<ExportAiRequestLogDto> AiRequestLogs { get; init; } = [];
    public List<ExportBackgroundJobDto> BackgroundJobs { get; init; } = [];
}

public record ExportCalendarEventDto
{
    public string Id { get; init; } = default!;
    public string UserId { get; init; } = default!;
    public DateTime StartDate { get; init; }
    public TimeSpan TimeOfDay { get; init; }
    public string? RecipeId { get; init; }
    public int Portions { get; init; }
    public RecurrenceType Recurrence { get; init; }
    public WeekDays RecurrenceDays { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ModifiedAt { get; init; }
}

public record ExportShoppingListGroupDto
{
    public string Id { get; init; } = default!;
    public string UserId { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string? RecipeId { get; init; }
    public int OrderIndex { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ModifiedAt { get; init; }
}

public record ExportShoppingListItemDto
{
    public string Id { get; init; } = default!;
    public string GroupId { get; init; } = default!;
    public decimal Amount { get; init; }
    public string? Unit { get; init; }
    public string Name { get; init; } = default!;
    public bool IsChecked { get; init; }
    public int OrderIndex { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ModifiedAt { get; init; }
}

public record ExportUserSettingDto
{
    public string UserId { get; init; } = default!;
    public bool AiEnabled { get; init; }
    public bool GoogleVisionEnabled { get; init; }
    public bool GeminiEnabled { get; init; }
    public bool RequireAiConfirmation { get; init; }
}

public record ExportAppSettingDto
{
    public string Key { get; init; } = default!;
    public string Value { get; init; } = default!;
}

public record ExportPluginSettingDto
{
    public string PluginId { get; init; } = default!;
    public string DisplayName { get; init; } = default!;
    public string? Description { get; init; }
    public string AssemblyName { get; init; } = default!;
    public string TypeName { get; init; } = default!;
    public bool Enabled { get; init; }
    public int OrderIndex { get; init; }
    public string Status { get; init; } = default!;
    public string? Error { get; init; }
    public DateTime DiscoveredAt { get; init; }
    public DateTime LastSeenAt { get; init; }
}

public record ExportPluginSourceDto
{
    public string Id { get; init; } = default!;
    public string RepositoryUrl { get; init; } = default!;
    public string Owner { get; init; } = default!;
    public string Repository { get; init; } = default!;
    public bool IsPrivate { get; init; }
    public bool Enabled { get; init; }
    public bool TrustConfirmed { get; init; }
    public string? SecretName { get; init; }
    public string? LastSuccessfulReleaseTag { get; init; }
    public string? LastError { get; init; }
    public DateTime? LastCheckedAt { get; init; }
    public DateTime? LastErrorAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record ExportPluginSourceReleaseDto
{
    public string Id { get; init; } = default!;
    public string PluginSourceId { get; init; } = default!;
    public string ReleaseTag { get; init; } = default!;
    public long GitHubReleaseId { get; init; }
    public long AssetId { get; init; }
    public string AssetName { get; init; } = default!;
    public string Status { get; init; } = default!;
    public string? Error { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? DownloadedAt { get; init; }
    public DateTime? ValidatedAt { get; init; }
    public DateTime? InstalledAt { get; init; }
    public string? ReloadStatus { get; init; }
    public DateTime? ReloadedAt { get; init; }
    public string? ReloadError { get; init; }
}

public record ExportAiRequestLogDto
{
    public string Id { get; init; } = default!;
    public string UserId { get; init; } = default!;
    public string Service { get; init; } = default!;
    public DateTime Timestamp { get; init; }
    public AiRequestLogType Type { get; init; }
}

public record ExportBackgroundJobDto
{
    public Guid Id { get; init; }
    public string JobType { get; init; } = default!;
    public string? InitiatorUserId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public BackgroundJobs.BackgroundJobStatus Status { get; init; }
    public string? PayloadJson { get; init; }
    public int Progress { get; init; }
    public string? ResultMessage { get; init; }
    public string? Error { get; init; }
}
#endregion
