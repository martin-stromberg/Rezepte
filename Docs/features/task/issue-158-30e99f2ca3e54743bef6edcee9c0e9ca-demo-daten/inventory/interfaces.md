# Interfaces – Bestandsaufnahme „Demo-Daten bei Registrierung"

## `IUserService`
Datei: `Rezepte.Web/Services/UserService.cs` (Zeilen 25–115)

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `RegisterAsync` | `string username, string password, CancellationToken ct` | `Task<(bool ok, string? error, User? user)>` | Registriert Benutzer; **kein `createDemoData`-Parameter vorhanden** |
| `LoginAsync` | `username, password, ct` | `Task<User?>` | Authentifizierung |
| `FindByUsernameAsync` | `username, ct` | `Task<User?>` | Suche per Name |
| `HasAnyUsersAsync` | `ct` | `Task<bool>` | Existieren Benutzer? |
| `GetByIdAsync` | `id, ct` | `Task<User?>` | Benutzer per Id |
| `UpdateProfileAsync` | `id, username, email, ct` | `Task<(bool, string?, User?)>` | Profil-Update |
| `ChangePasswordAsync` | `id, currentPassword, newPassword, ct` | `Task<(bool, string?)>` | Passwort ändern |
| `GetAllAsync` | `ct` | `Task<List<User>>` | Admin: alle Benutzer |
| `UpdateUserAsync` | `id, username, email, isAdmin, ct` | `Task<(bool, string?)>` | Admin-Update |
| `DeleteAsync` | `id, ct` | `Task<(bool, string?)>` | Benutzer löschen |

Zugehöriges Record: `User(string Id, string Username, string Email, string PasswordHash, bool IsAdmin, DateTime RegistrationTime)` (Zeile 20).

## `IBackgroundJobQueue`
Datei: `Rezepte.Web/Services/BackgroundJobs/IBackgroundJobQueue.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `EnqueueAsync` | `string jobType, string? payloadJson = null, string? initiatorUserId = null, CancellationToken ct = default` | `Task<Guid>` | Job persistieren + in Channel einreihen |
| `GetJobAsync` | `Guid jobId, CancellationToken ct` | `Task<BackgroundJob?>` | Job-Status abfragen |

## `IBackgroundJobHandler`
Datei: `Rezepte.Web/Services/BackgroundJobs/IBackgroundJobHandler.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `JobType` (Property) | — | `string` | Eindeutiger Job-Typ, z. B. `export:user` |
| `HandleAsync` | `BackgroundJob job, IServiceProvider scopeServices, CancellationToken ct` | `Task` | Führt Job innerhalb des vom HostedService erzeugten Scopes aus |

Registrierung erfolgt einzeln per `AddScoped<IBackgroundJobHandler, …>` (siehe `ServiceCollectionExtensions.cs` Zeilen 197–198). Es existiert **kein** Handler für Demo-Daten.

## `ICookbookService`
Datei: `Rezepte.Web/Services/CookbookService.cs` (Zeilen 10–76)

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetAllAsync` | `userId, ct` | `Task<List<Cookbook>>` | Alle Kochbücher des Users |
| `GetByIdAsync` | `userId, id, ct` | `Task<Cookbook?>` | Einzelnes Kochbuch |
| `CreateAsync` | `userId, name, description, ct` | `Task<(bool ok, string? error, Cookbook? cookbook)>` | Anlegen (Name ≥ 3 Zeichen) |
| `UpdateAsync` | `userId, id, name, description, ct` | `Task<(bool, string?)>` | Umbenennen |
| `DeleteAsync` | `userId, id, ct` | `Task<(bool, string?)>` | Löschen |
| `ReorderAsync` | `userId, orderedIds, ct` | `Task<(bool, string?)>` | Reihenfolge persistieren |

## `IRecipeService`
Datei: `Rezepte.Web/Services/RecipeService.cs` (Zeilen 16–242)

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetByIdAsync` | `userId, id, ct` | `Task<Recipe?>` | Rezept laden |
| `GetSideDishesAsync` | `userId, recipeId, ct` | `Task<List<RecipeSideDishInfo>>` | Beilagen |
| `GetByCookbookAsync` | `userId, cookbookId, ct` | `Task<List<Recipe>>` | Rezepte eines Kochbuchs |
| `GetAvailableForCookbookAsync` | `userId, cookbookId, ct` | `Task<List<Recipe>>` | Noch nicht zugeordnete Rezepte |
| `CreateAsync` (2 Überladungen) | `userId, cookbookId, title, description, uri, portions, steps[, sideDishRecipeIds], ct` | `Task<(bool, string?, Recipe?)>` | Rezept mit Schritten/Zutaten anlegen |
| `UpdateAsync` (2 Überladungen) | `userId, id, title, description, uri, portions, steps[, sideDishRecipeIds], ct` | `Task<(bool, string?)>` | Rezept ersetzen |
| `DeleteAsync` | `userId, id, ct` | `Task<(bool, string?)>` | Löschen |
| `AddExistingToCookbookAsync` | `userId, cookbookId, recipeIds, ct` | `Task<(bool, string?, List<Recipe>)>` | Rezepte einem Kochbuch zuordnen |
| `RemoveFromCookbookAsync` | `userId, cookbookId, recipeId, ct` | `Task<(bool, string?)>` | Zuordnung lösen |
| `SetImageAsync` / `AddImageAsync` / `GetImageAsync` / `GetImages` / `GetImageCountAsync` / `DeleteImageAsync` | div. | div. | Bildverwaltung |
| `GetLatestAsync` | `userId, count, ct` | `Task<List<Recipe>>` | Neueste Rezepte |
| `FindByUri` | `userId, v, ct` | `Task<Recipe?>` | Suche per URI |
| `SearchAsync` | `userId, q, tags, cookbookId, page, pageSize, sort, ct` | `Task<SearchResult>` | Suche |

## `ICalendarService`
Datei: `Rezepte.Web/Services/ICalendarService.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetEventAsync` | `userId, eventId, ct` | `Task<CalendarEvent?>` | Event laden |
| `GetEventsForUserAsync` | `userId, from, to, ct` | `Task<IEnumerable<CalendarEvent>>` | Events im Zeitraum |
| `CreateEventAsync` | `userId, recipeId, startDate, timeOfDay, portions, recurrence, recurrenceDays, ct` | `Task<(bool, string?, CalendarEvent?)>` | Event anlegen |
| `UpdateEventAsync` | `userId, eventId, startDate, timeOfDay, portions, recurrence, recurrenceDays, ct` | `Task<(bool, string?)>` | Event ändern |
| `DeleteEventAsync` | `userId, eventId, ct` | `Task<(bool, string?)>` | Event löschen |
| `GetOccurrencesAsync` | `userId, from, to, ct` | `Task<IEnumerable<(CalendarEvent Ev, DateTime Occurrence)>>` | Wiederholungen expandieren |

## `IShoppingListService`
Datei: `Rezepte.Web/Services/ShoppingListService.cs` (Zeilen 10–150)

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetGroupsAsync` | `userId, ct` | `Task<List<ShoppingListGroup>>` | Gruppen laden |
| `EnsureDefaultGroupAsync` | `userId, ct` | `Task<ShoppingListGroup>` | Standardgruppe „Einkaufsliste" sicherstellen |
| `AddGroupAsync` / `RenameGroupAsync` / `DeleteGroupAsync` | div. | `Task<(bool, …)>` | Gruppenverwaltung |
| `AddItemAsync` | `userId, groupId, amount, unit, name, ct` | `Task<(bool, string?, ShoppingListItem?)>` | Einzelnen Artikel hinzufügen |
| `UpdateItemAsync` / `SetItemCheckedAsync` / `DeleteItemAsync` | div. | `Task<(bool, string?)>` | Artikelverwaltung |
| `GetRecipeIngredientsAsync` | `userId, recipeId, ct` | `Task<List<ShoppingListRecipeIngredient>>` | Zutaten eines Rezepts |
| `GetRecipeIngredientGroupsAsync` | `userId, recipeId, ct` | `Task<List<ShoppingListRecipeIngredientGroup>>` | Zutaten gruppiert (inkl. Beilagen) |
| `AddRecipeIngredientsAsync` | `userId, recipeId, ingredientIds, ct` | `Task<(bool, string?, ShoppingListGroup?)>` | Ausgewählte Rezept-Zutaten als Gruppe anlegen |
| `AddRecipeIngredientGroupsAsync` | `userId, recipeId, selections, ct` | `Task<(bool, string?, List<ShoppingListGroup>)>` | Mehrere Rezept-Zutatenauswahlen anlegen |

Hilfs-Records: `ShoppingListRecipeIngredient`, `ShoppingListRecipeIngredientGroup`, `ShoppingListRecipeIngredientSelection` (Zeilen 161–177).
