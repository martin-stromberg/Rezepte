# Detail: Demo-Daten-Quelle und Domänen-Service-Signaturen

## `DemoDataSource`

Datei: `Rezepte.Web/Services/DemoDataSource.cs` (630 Zeilen), statische Klasse, Namespace `Rezepte.Web.Services`.

- `CalendarCookbookName = "Abendessen"` — Zeile 12.
- `Cookbooks → IReadOnlyList<DemoCookbook>` — Zeile 18, via `BuildCookbooks()` (44–629): 5 Kochbücher — „Frühstück" (8 Rezepte), „Abendessen" (10), „Snacks" (9), „Weihnachtszeit" (8), „Grillsaison" (8) = **43 Rezepte**.
- `DemoCookbook` record — `Rezepte.Web/Services/DemoCookbook.cs:9` — `(string Name, IReadOnlyList<DemoRecipe> Recipes)`.
- `DemoRecipe` record — `Rezepte.Web/Services/DemoRecipe.cs:11` — `(string Title, string? Description, int Portions, IReadOnlyList<RecipeCreateStep> Steps)`.
- Hilfs-Factorys `Ing`/`Step` (DemoDataSource.cs:27–38) bauen `RecipeCreateIngredient`/`RecipeCreateStep`.

## `demo-images.zip`

- Quelle: `Rezepte.Web/DemoData/demo-images.zip`; Kopie ins Output via `Rezepte.Web.csproj:36–40` (`Content`, `CopyToOutputDirectory`/`CopyToPublishDirectory` = PreserveNewest).
- Pfad zur Laufzeit: `Path.Combine(AppContext.BaseDirectory, "DemoData", "demo-images.zip")`.
- Transitiv auch im Test-Output vorhanden: `Rezepte.Tests/bin/Debug/net10.0/DemoData/demo-images.zip` — relevant, weil `AppContext.BaseDirectory` im Test-Host auf das Test-Output-Verzeichnis zeigt (Seeder-Tests laufen daher mit vorhandenem Archiv).
- Bild-Matching: ZIP-Eintrag per Dateiname ohne Erweiterung = Rezepttitel (OrdinalIgnoreCase).

## Domänen-Service-Signaturen (wie von der Sequenz verwendet)

| Aufruf | Fundstelle | Signatur / Rückgabe |
|---|---|---|
| `ICookbookService.CreateAsync` | `Rezepte.Web/Services/CookbookService.cs:126` | `Task<(bool ok, string? error, Cookbook? cookbook)> CreateAsync(string userId, string name, string? description, CancellationToken ct)` |
| `IRecipeService.CreateAsync` | `Rezepte.Web/Services/RecipeService.cs:410` | `Task<(bool ok, string? error, Recipe? recipe)> CreateAsync(string userId, string? cookbookId, string title, string? description, string? uri, int? portions, IReadOnlyList<RecipeCreateStep> steps, CancellationToken ct)` |
| `IRecipeService.AddImageAsync` | `Rezepte.Web/Services/RecipeService.cs:744` | `Task<(bool ok, string? error, RecipeImage? image)> AddImageAsync(string userId, string recipeId, Stream imageStream, string fileName, string contentType, CancellationToken ct)` |
| `ICalendarService.CreateEventAsync` | `Rezepte.Web/Services/CalendarService.cs:75` | `Task<(bool ok, string? error, CalendarEvent? ev)> CreateEventAsync(string userId, string? recipeId, DateTime startDate, TimeSpan timeOfDay, int portions, RecurrenceType recurrence, WeekDays recurrenceDays, CancellationToken ct)` |
| `IShoppingListService.EnsureDefaultGroupAsync` | `Rezepte.Web/Services/ShoppingListService.cs:219` | `Task<ShoppingListGroup> EnsureDefaultGroupAsync(string userId, CancellationToken ct)` |
| `IShoppingListService.AddItemAsync` | `Rezepte.Web/Services/ShoppingListService.cs:330` | `Task<(bool ok, string? error, ShoppingListItem? item)> AddItemAsync(string userId, string groupId, decimal amount, string? unit, string name, CancellationToken ct)` |
| `IUserService.GetByIdAsync` | `Rezepte.Web/Services/UserService.cs` Interface `:68` / Impl `:202` | `Task<User?> GetByIdAsync(string id, CancellationToken ct)` |

## Interface-Konventionen

Interfaces als eigene Datei `I{Name}.cs` im selben Namespace/Ordner wie die Implementierung (`Rezepte.Web.Services`), XML-Doc-Pflicht (Pre-Commit-Hook prüft XML-Dokumentation). Methoden mit `CancellationToken ct` als letztem Parameter.
