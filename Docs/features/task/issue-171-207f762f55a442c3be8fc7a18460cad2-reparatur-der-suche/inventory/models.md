# Datenmodell — Rezeptsuche

Alle Tabellen werden über `RezepteDbContext` (`Rezepte.Web/Data/RezepteDbContext.cs`) auf einem SQLite-Provider bedient (`UseSqlite`, registriert in `Rezepte.Web/Extensions/ServiceCollectionExtensions.cs:67`).

## `Recipe`

Datei: `Rezepte.Web/Entities/Recipe.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `string` | Primärschlüssel, `Guid.NewGuid().ToString("n")` |
| `UserId` | `string` | Besitzer; `SearchAsync` filtert mit `r.UserId == userId` (Pflichtfilter, MaxLength 64, Index `UserId+Title`) |
| `Title` | `string` | Suchfeld (`EF.Functions.Like`), Pflicht, MaxLength 200 |
| `Description` | `string?` | Suchfeld (`EF.Functions.Like`), MaxLength 4000, Snippet-Fallback |
| `CreatedAt` | `DateTime` | Standardsortierung „newest"/„relevance" (`OrderByDescending`), DTO `PublishedLabel` |
| `Steps` | `ICollection<RecipeStep>` | Navigation; per `Include`/`ThenInclude` in `SearchAsync` geladen |
| `Images` | `ICollection<RecipeImage>` | Navigation; liefert `PrimaryImageUrl` |
| `RecipeCookbooks` | `ICollection<RecipeCookbook>` | Navigation; Grundlage des `cookbookId`-Filters |
| `SideDishes` | `ICollection<RecipeSideDish>` | Navigation; für die Suche nicht verwendet |
| `UsedAsSideDishFor` | `ICollection<RecipeSideDish>` | Inverse Navigation; für die Suche nicht verwendet |
| `Uri` | `string?` | Quell-URI (`internal set`); nicht suchrelevant |
| `Portions` | `int` | Portionen; nicht suchrelevant |

## `RecipeStep`

Datei: `Rezepte.Web/Entities/RecipeStep.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `string` | Primärschlüssel (Guid-String) |
| `RecipeId` | `string` | Fremdschlüssel zu `Recipe`, Cascade-Delete |
| `StepIndex` | `int` | Reihenfolge; Snippet wählt `OrderBy(StepIndex).First()` |
| `Title` | `string?` | Schritttitel; **nicht** durchsucht |
| `Description` | `string` | Suchfeld (`EF.Functions.Like`), Pflicht, MaxLength 4000; primäre Snippet-Quelle |
| `DurationMinutes` | `int` | Dauer; nicht suchrelevant |
| `RequiresOvernightRest` | `bool` | Ruhezeit-Flag; nicht suchrelevant |
| `Ingredients` | `ICollection<RecipeIngredient>` | Navigation; durchsucht über `Any(i => Like(i.Name))` |

## `RecipeIngredient`

Datei: `Rezepte.Web/Entities/RecipeIngredient.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `string` | Primärschlüssel (Guid-String) |
| `StepId` | `string` | Fremdschlüssel zu `RecipeStep`, Cascade-Delete |
| `Amount` | `decimal` | Menge; nicht suchrelevant |
| `Unit` | `string?` | Einheit, MaxLength 64; nicht suchrelevant |
| `Name` | `string` | Suchfeld (`EF.Functions.Like`), Pflicht, MaxLength 200; Snippet-Fallback (Join der ersten 10 Namen) |

## `RecipeCookbook`

Datei: `Rezepte.Web/Entities/RecipeCookbook.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `string` | Primärschlüssel (Guid-String) |
| `CookbookId` | `string` | Fremdschlüssel zu `Cookbook`, Cascade-Delete; Filterkriterium des `cookbookId`-Parameters |
| `Cookbook` | `Cookbook` | Navigation |
| `RecipeId` | `string` | Fremdschlüssel zu `Recipe`, Cascade-Delete |
| `Recipe` | `Recipe` | Navigation |

## `RecipeImage`

Datei: `Rezepte.Web/Entities/RecipeImage.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `string` | Primärschlüssel (Guid-String) |
| `RecipeId` | `string` | Fremdschlüssel zu `Recipe`, Cascade-Delete |
| `Recipe` | `Recipe` | Navigation |
| `FileName` | `string` | Dateiname; nicht suchrelevant |
| `ContentType` | `string` | MIME-Typ; nicht suchrelevant |
| `Data` | `byte[]` | Binärdaten; nicht suchrelevant |
| `CreatedAt` | `DateTime` | `PrimaryImageUrl` = neuestes Bild (`OrderByDescending(CreatedAt).First()`) |
| `Url` | `string` | Berechnet (`=> $"/api/recipes/{RecipeId}/image/{Id}"`), nicht persistiert; liefert `PrimaryImageUrl` |

## Ergebnistypen und DTOs

### `RecipeService.SearchResult`

Datei: `Rezepte.Web/Services/RecipeService.cs` (Zeilen 956–970), geschachtelte Klasse von `RecipeService`.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `TotalCount` | `int` | Trefferanzahl vor Paging (`CountAsync`) |
| `Items` | `IEnumerable<SearchResultItem>` | Seiteninhalt nach `Skip/Take` |

### `RecipeService.SearchResultItem`

Datei: `Rezepte.Web/Services/RecipeService.cs` (Zeilen 1018–1045), geschachtelte Klasse von `RecipeService`.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `string` | Rezept-Id |
| `Title` | `string` | Rezepttitel |
| `CreatedAt` | `DateTime` | Erstellungsdatum → `PublishedLabel` |
| `PrimaryImageUrl` | `string?` | URL des neuesten Bildes |
| `Snippet` | `string` | Erste Schrittbeschreibung, sonst Zutaten-Join (max. 10), sonst `Description`, sonst leer; auf 200 Zeichen + „…" gekürzt |
| `Tags` | `string[]` | Immer `Array.Empty<string>()` — es existiert kein Tag-Modell |

### `RecipesController.RecipeSearchItemDto` / `RecipeSearchResponseDto`

Datei: `Rezepte.Web/Controllers/RecipesController.cs` (Zeilen 600–601), private geschachtelte Records.

- `RecipeSearchItemDto(string Id, string Title, string Snippet, string? PrimaryImageUrl, string[] Tags, string? PublishedLabel)` — `PublishedLabel` = `CreatedAt.ToString("yyyy-MM-dd")`; `Snippet` erneut auf 200 Zeichen gekappt.
- `RecipeSearchResponseDto(RecipeSearchItemDto[] Items, int Total)` — JSON-Antwort von `GET /api/recipes/search`.

### Lokale View-Records der Aufrufer (nicht persistiert)

- `RecipeSearch.razor:162-163`: `RecipeResult(string Id, string Title, string Snippet, string? PrimaryImageUrl, string[]? Cookbooks, string[]? Tags, string? PublishedLabel)` und `SearchResponseDto(RecipeResult[]? Items, int Total)` — deserialisiert die API-Antwort; Feld `Cookbooks` wird von der API nicht geliefert (immer `null`).
- `CalendarEventDialog.razor:282-283`: `RecipeSearchResultDto(Id, Title, Snippet, PrimaryImageUrl, DefaultPortions)` / `RecipeSearchResponseDto(Items, Total)` — `DefaultPortions` wird von der API nicht geliefert.
- `RecipeEdit.razor:513-514`: `RecipeSearchResponseDto(List<RecipeSearchResultDto> Items, int Total)` / `RecipeSearchResultDto(Id, Title, Snippet, PrimaryImageUrl, DefaultPortions)` — ebenfalls mit nicht geliefertem `DefaultPortions`.
