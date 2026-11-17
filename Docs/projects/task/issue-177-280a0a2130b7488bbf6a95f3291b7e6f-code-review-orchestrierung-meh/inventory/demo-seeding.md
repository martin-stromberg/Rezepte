# Demo-Seeding — Bestandsaufnahme

Geschäftsvorfall „Demo-Daten für einen neuen Benutzer anlegen": aktuell vollständig im Job-Handler komponiert.

## `DemoDataSeedingJobHandler`

`Rezepte.Web/Services/BackgroundJobs/Handlers/DemoDataSeedingJobHandler.cs` (318 Zeilen), `IBackgroundJobHandler`-Implementierung.

### Konstanten/Felder

- `JobTypeName = "seed-demo-data"` — Zeile 17; `JobType` Property Zeile 41.
- `CalendarEventCount = 5` — Zeile 22.
- `DefaultPortions = 2` — Zeile 27 (Fallback bei `Portions <= 0`).
- `DinnerHour = 18` — Zeile 32.
- `DemoImagesPath = Path.Combine(AppContext.BaseDirectory, "DemoData", "demo-images.zip")` — Zeile 38.

### `HandleAsync(job, scopeServices, ct)` — Zeilen 44-78

Sequenz:

1. `ct.ThrowIfCancellationRequested()` (47); `DemoDataSeedPayload.FromJson(job.PayloadJson)` (49); `UserId` leer → `InvalidOperationException` (50-53).
2. `IUserService` via `scopeServices` (55), `GetByIdAsync` — User fehlt → `InvalidOperationException` (56-60).
3. Services aus `scopeServices`: `ICookbookService` (62), `IRecipeService` (63), `ICalendarService` (64), `IShoppingListService` (65), `ILogger<DemoDataSeedingJobHandler>` (66).
4. `OpenDemoImages(logger)` (70) → `ZipArchive?`.
5. Fachsequenz (72-75): `SeedCookbooksAsync` → `SeedRecipesAsync` → `SeedCalendarAsync` → `SeedShoppingListAsync`.

### Private Sequenz-Methoden

- `SeedCookbooksAsync` (87-105): iteriert `DemoDataSource.Cookbooks`; `cookbookService.CreateAsync(userId, cookbook.Name, null, ct)` (95); Fehler/`created==null` → `InvalidOperationException` (98); liefert `Dictionary<Name, Id>` (Ordinal).
- `SeedRecipesAsync` (119-161): je Demo-Kochbuch × `demoRecipe` → `recipeService.CreateAsync(userId, cookbookId, Title, Description, null, Portions, Steps, ct)` (137-145) — `uri` = `null`; Fehler → `InvalidOperationException` (148); `AttachImageAsync` (151); Rezepte des Kochbuchs `DemoDataSource.CalendarCookbookName` als `SeededRecipe`-Paare gesammelt (153-156).
- `OpenDemoImages` (168-177): Datei fehlt → `LogWarning` + `null` (kein Fehler); sonst `ZipArchive` read.
- `AttachImageAsync` (190-218): ZIP-Eintrag per `Path.GetFileNameWithoutExtension(e.Name) == demoRecipe.Title` (OrdinalIgnoreCase, 199-200); kein Eintrag → stiller Rücksprung; `recipeService.AddImageAsync(userId, recipe.Id, stream, entry.Name, GetImageContentType(entry.Name), ct)` (207-213); Misserfolg nur `LogWarning` (216) — **kein Throw**.
- `GetImageContentType` (225-231): `.png→image/png`, `.jpg/.jpeg→image/jpeg`, sonst `application/octet-stream`.
- `SeedCalendarAsync` (242-269): `Take(CalendarEventCount)` der Kalender-Kochbuch-Rezepte; je Tag `DateTime.Today.AddDays(i+1)`, `TimeSpan.FromHours(18)`, `Portions>0 ? Portions : 2`, `RecurrenceType.None`, `WeekDays.None` → `calendarService.CreateEventAsync` (253-261); Fehler → `InvalidOperationException` (264); liefert geplante `SeededRecipe`-Liste.
- `SeedShoppingListAsync` (279-309): erstes geplantes Rezept; `shoppingListService.EnsureDefaultGroupAsync(userId, ct)` (291); je Step×Ingredient `AddItemAsync(userId, group.Id, Amount, Unit, Name, ct)` (296-302); Fehler → `InvalidOperationException` (305).
- `SeededRecipe(Demo, Recipe)` — private record, Zeile 317.

### Fehlerverhalten/Nebenbedingungen

- Jeder Domänenaufruf außer `AddImageAsync` wirft bei Misserfolg `InvalidOperationException` → Job landet auf `Failed` (`BackgroundJobHostedService.cs:109-115`), kein Retry, keine Transaktion (Teildaten bleiben bestehen).
- Fehlende `demo-images.zip` oder fehlende Einzelbilder sind kein Fehler.
- `demo-images.zip` wird per `Rezepte.Web.csproj:36-40` (`Content Include="DemoData\demo-images.zip"`, CopyToOutput/PublishDirectory `PreserveNewest`) ins Output kopiert.

## Job-Infrastruktur

- `Rezepte.Web/Services/BackgroundJobs/DemoDataSeedPayload.cs:10-52` — `record DemoDataSeedPayload(string UserId)`, `ToJson()` (18), `FromJson(string?)` (26; leerer JSON → leere UserId; ungültiges JSON → `InvalidOperationException`).
- `Rezepte.Web/Services/BackgroundJobs/IBackgroundJobQueue.cs:20` — `Task<Guid> EnqueueAsync(string jobType, string? payloadJson = null, string? initiatorUserId = null, CancellationToken ct = default)`; `GetJobAsync` (31).
- `Rezepte.Web/Services/BackgroundJobs/BackgroundJobQueue.cs:45-68` — persistiert `BackgroundJob`-Row (eigener Scope, 50-62) + Channel-Write (65); `GetJobAsync` 76-81. Registrierung via `services.AddBackgroundJobQueue()` (`ServiceCollectionExtensions.cs:237`; `BackgroundJobHostedService` wird dort ebenfalls verdrahtet — Erweiterungsmethode in `Rezepte.Web/Services/BackgroundJobs/`).
- `Rezepte.Web/Services/BackgroundJobs/IBackgroundJobHandler.cs:7-26` — `JobType` + `HandleAsync(BackgroundJob job, IServiceProvider scopeServices, CancellationToken ct)`.
- `Rezepte.Web/Services/BackgroundJobs/BackgroundJobHostedService.cs:56-117` — Scope je Job (60), Handlerauflösung via `GetServices<IBackgroundJobHandler>()` + `JobType`-Match (84-89), Status-Transitions Pending→Running→Succeeded/Failed/Cancelled.

### Weitere Handler (legitime Orchestrierung, unverändert)

- `ExportUserJobHandler`, `ExportAllJobHandler` — `Rezepte.Web/Services/BackgroundJobs/Handlers/`; DI `ServiceCollectionExtensions.cs:197-198`.
- DI `ServiceCollectionExtensions.cs:199` — `services.AddScoped<IBackgroundJobHandler, DemoDataSeedingJobHandler>();`

## Auslöser — `UserService.RegisterAsync`

`Rezepte.Web/Services/UserService.cs`:

- Interface `IUserService.RegisterAsync(username, password, createDemoData, ct) → (bool ok, string? error, User? user)` — Zeile 36.
- Implementierung Zeilen 133-162: Username-Validierung (136-138), Duplikat-Check (140-141), `isFirst` → `IsAdmin` (143-148), `SaveChangesAsync` (151), **Job-Anlage Zeilen 154-159**: `new DemoDataSeedPayload(user.Id)` + `_backgroundJobQueue.EnqueueAsync(DemoDataSeedingJobHandler.JobTypeName, payload.ToJson(), user.Id, ct)`.
- `UserService` scoped (`ServiceCollectionExtensions.cs:187`); Abhängigkeiten: `RezepteDbContext`, `IUsernameValidator`, `IBackgroundJobQueue` (Zeile 125).
- Aufrufer: `AuthController.cs:79` (`createDemoData` aus Form/JSON), `AdminUsersController.cs:53` (immer `false`).

## `DemoDataSource`

`Rezepte.Web/Services/DemoDataSource.cs` (630 Zeilen), statische Klasse:

- `CalendarCookbookName = "Abendessen"` — Zeile 12.
- `Cookbooks → IReadOnlyList<DemoCookbook>` — Zeile 18, via `BuildCookbooks()` (44-629): 5 Kochbücher — „Frühstück" (8 Rezepte), „Abendessen" (10), „Snacks" (9), „Weihnachtszeit" (8), „Grillsaison" (8) = **43 Rezepte**.
- `DemoCookbook` record — `Rezepte.Web/Services/DemoCookbook.cs:9` — `(string Name, IReadOnlyList<DemoRecipe> Recipes)`.
- `DemoRecipe` record — `Rezepte.Web/Services/DemoRecipe.cs:11` — `(string Title, string? Description, int Portions, IReadOnlyList<RecipeCreateStep> Steps)`.
- Hilfs-Factorys `Ing`/`Step` (DemoDataSource.cs:27-38) bauen `RecipeCreateIngredient`/`RecipeCreateStep` aus `RecipeService.cs:257,267`.

## Domänen-Service-Signaturen (wie vom Handler verwendet)

| Aufruf | Fundstelle | Signatur / Rückgabe |
|---|---|---|
| `ICookbookService.CreateAsync` | `Rezepte.Web/Services/CookbookService.cs:126` | `Task<(bool ok, string? error, Cookbook? cookbook)> CreateAsync(string userId, string name, string? description, CancellationToken ct)` |
| `IRecipeService.CreateAsync` | `Rezepte.Web/Services/RecipeService.cs:410` | `Task<(bool ok, string? error, Recipe? recipe)> CreateAsync(string userId, string? cookbookId, string title, string? description, string? uri, int? portions, IReadOnlyList<RecipeCreateStep> steps, CancellationToken ct)` (delegiert an Überladung mit `sideDishRecipeIds`, :431) |
| `IRecipeService.AddImageAsync` | `Rezepte.Web/Services/RecipeService.cs:744` | `Task<(bool ok, string? error, RecipeImage? image)> AddImageAsync(string userId, string recipeId, Stream imageStream, string fileName, string contentType, CancellationToken ct)` |
| `ICalendarService.CreateEventAsync` | `Rezepte.Web/Services/CalendarService.cs:75` | `Task<(bool ok, string? error, CalendarEvent? ev)> CreateEventAsync(string userId, string? recipeId, DateTime startDate, TimeSpan timeOfDay, int portions, RecurrenceType recurrence, WeekDays recurrenceDays, CancellationToken ct)` |
| `IShoppingListService.EnsureDefaultGroupAsync` | `Rezepte.Web/Services/ShoppingListService.cs:219` | `Task<ShoppingListGroup> EnsureDefaultGroupAsync(string userId, CancellationToken ct)` |
| `IShoppingListService.AddItemAsync` | `Rezepte.Web/Services/ShoppingListService.cs:330` | `Task<(bool ok, string? error, ShoppingListItem? item)> AddItemAsync(string userId, string groupId, decimal amount, string? unit, string name, CancellationToken ct)` |
| `IUserService.GetByIdAsync` | `Rezepte.Web/Services/UserService.cs:68` (Interface) / `:202` (Impl) | `Task<User?> GetByIdAsync(string id, CancellationToken ct)` |

Alle genannten Services sind scoped registriert (`ServiceCollectionExtensions.cs:187-189, 224-225`) — ein `IDemoDataSeeder`-Service kann sie direkt scoped injizieren; `scopeServices`-Auflösung im Handler ist nur wegen der Handler-Infrastruktur nötig.

## Dokumentation

`Docs/help/user-accounts.md:40-44` („Technischer Hintergrund") beschreibt den aktuellen Ablauf inkl. der Komposition im `DemoDataSeedingJobHandler` — bei Kapselung in `IDemoDataSeeder` ist dieser Abschnitt anzupassen.
