# Interfaces — Bestandsaufnahme

## `IRecipeService`
Datei: `Rezepte.Web/Services/RecipeService.cs` (Interface in Zeilen 16–248; die Datei `Rezepte.Web/Services/IRecipeService.cs` enthält nur `using`-Direktiven und **keine** Interface-Definition)

Konstante: `MaxSearchPageSize = 100` (Z. 22).

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetByIdAsync` | `userId, id, ct` | `Task<Recipe?>` | Eigenes Rezept inkl. Schritte/Zutaten/Beilagen laden |
| `GetSideDishesAsync` | `userId, recipeId, ct` | `Task<List<RecipeSideDishInfo>>` | Beilagen des eigenen Rezepts |
| `GetByCookbookAsync` | `userId, cookbookId, ct` | `Task<List<Recipe>>` | Rezepte eines Kochbuchs |
| `GetAvailableForCookbookAsync` | `userId, cookbookId, ct` | `Task<List<Recipe>>` | Zuweisbare Rezepte |
| `CreateAsync` (Überladung ohne Beilagen) | `userId, cookbookId, title, description, uri, portions, steps, ct` | `Task<(bool ok, string? error, Recipe? recipe)>` | Rezept anlegen |
| `CreateAsync` (mit Beilagen) | zusätzlich `sideDishRecipeIds` | `Task<(bool ok, string? error, Recipe? recipe)>` | Rezept mit Beilagen anlegen |
| `UpdateAsync` (2 Überladungen) | `userId, id, title, description, uri, portions, steps[, sideDishRecipeIds], ct` | `Task<(bool ok, string? error)>` | Rezept aktualisieren |
| `DeleteAsync` | `userId, id, ct` | `Task<(bool ok, string? error)>` | Rezept löschen |
| `AddExistingToCookbookAsync` | `userId, cookbookId, recipeIds, ct` | `Task<(bool ok, string? error, List<Recipe> created)>` | Rezepte zu Kochbuch |
| `RemoveFromCookbookAsync` | `userId, cookbookId, recipeId, ct` | `Task<(bool ok, string? error)>` | Zuordnung lösen |
| `SetImageAsync` | `userId, recipeId, imageStream, fileName, ct` | `Task<(bool ok, string? error, string? imageId)>` | Bild ersetzen |
| `AddImageAsync` | `userId, recipeId, imageStream, fileName, contentType, ct` | `Task<(bool ok, string? error, RecipeImage? image)>` | Bild hinzufügen |
| `GetImageAsync` | `recipeId, imageId, ct` | `Task<RecipeImage?>` | Bild laden (ohne `userId`) |
| `GetImages` | `recipeId, offset, count` | `IQueryable<RecipeImage>` | Bildliste (ohne `userId`) |
| `GetImageCountAsync` | `recipeId, ct` | `Task<int>` | Bildanzahl |
| `DeleteImageAsync` | `userId, recipeId, imageId, ct` | `Task<(bool ok, string? error)>` | Bild löschen |
| `GetLatestAsync` | `userId, count, ct` | `Task<List<Recipe>>` | Neueste eigene Rezepte |
| `FindByUri` | `userId, v, ct` | `Task<Recipe?>` | Rezept per Quell-URI finden |
| `SearchAsync` | `userId, q, tags, cookbookId, page, pageSize, sort, ct` | `Task<SearchResult>` | Rezeptsuche |

Hinweis: Sämtliche rezeptlesenden Methoden sind `userId`-gebunden; es gibt keinen anonymen/token-basierten Lese-Contract.

## `ISettingsService`
Datei: `Rezepte.Web/Services/ISettingsService.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetUserAiEnabledAsync` | `userId, ct` | `Task<bool>` | Benutzer-KI-Toggle (Z. 16) |
| `SetUserAiEnabledAsync` | `userId, enabled, ct` | `Task` | (Z. 23) |
| `GetGlobalAiEnabledAsync` | `ct` | `Task<bool>` | Globaler KI-Toggle (Z. 30) |
| `SetGlobalAiEnabledAsync` | `enabled, ct` | `Task` | (Z. 36) |
| `GetUserGoogleVisionEnabledAsync` / `Set…` | `userId[, enabled], ct` | `Task<bool>` / `Task` | (Z. 45, 52) |
| `GetUserGeminiEnabledAsync` / `Set…` | `userId[, enabled], ct` | `Task<bool>` / `Task` | (Z. 60, 67) |
| `GetGlobalGoogleVisionEnabledAsync` / `Set…` | `[enabled,] ct` | `Task<bool>` / `Task` | (Z. 75, 81) |
| `GetGlobalGeminiEnabledAsync` / `Set…` | `[enabled,] ct` | `Task<bool>` / `Task` | (Z. 88, 94) |
| `GetUserRequireAiConfirmationAsync` / `Set…` | `userId[, required], ct` | `Task<bool>` / `Task` | (Z. 103, 110) |
| `GetUserShoppingListEditModeAsync` / `Set…` | `userId[, editMode], ct` | `Task<bool>` / `Task` | (Z. 119, 126) |
| `GetGlobalMaxRequestsPerHourAsync` / `Set…` | `[value,] ct` | `Task<int?>` / `Task` | (Z. 134, 140) |
| `GetGlobalMaxRequestsPerDayAsync` / `Set…` | `[value,] ct` | `Task<int?>` / `Task` | (Z. 147, 153) |
| `GetGlobalDisableOnLimitReachedAsync` / `Set…` | `[disable,] ct` | `Task<bool>` / `Task` | (Z. 160, 166) |
| `GetSecurityTxtSettingsAsync` | `ct` | `Task<SecurityTxtSettings>` | (Z. 173) |
| `SetSecurityTxtSettingsAsync` | `settings, ct` | `Task` | (Z. 179) |

Hinweis: Kein Member für eine Share-Link-Gültigkeitsdauer.

## Nicht vorhanden

- `IRecipeShareService` o. ä. existiert nicht.
- Es gibt keine relevanten Enums für die Anforderung (bestehende Enums wie `RecurrenceType`/`WeekDays` in `Entities/CalendarEvent.cs` sind kalenderspezifisch und nicht betroffen).
