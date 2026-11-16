# Logik / Services — Bestandsaufnahme

Analysierte Logik- und Infrastrukturklassen im Kontext der Anforderung „Öffentliche Links".

## `RecipeService`
Datei: `Rezepte.Web/Services/RecipeService.cs` (Implementiert `IRecipeService`, Klasse ab Zeile 286)

Konstruktor-Injektion (Zeile 286): `RezepteDbContext db`, `IWebHostEnvironment env`, `IHttpContextAccessor httpContextAccessor`. Zusätzlich `protected CurrentUserId` (Zeilen 295–305), das `NameIdentifier` aus dem HttpContext liest und bei fehlendem Kontext wirft.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `GetByIdAsync(userId, id, ct)` | public | Lädt Rezept inkl. `RecipeCookbooks`, `SideDishes`→`SideDishRecipe`, `Steps`→`Ingredients` — **nur wenn `r.UserId == userId`** (Z. 314–333) |
| `GetSideDishesAsync(userId, recipeId, ct)` | public | Beilagen eines eigenen Rezepts; filtert Beilagen zusätzlich auf `SideDishRecipe.UserId == userId` (Z. 342–363); Ergebnis `List<RecipeSideDishInfo>` |
| `GetByCookbookAsync` / `GetAvailableForCookbookAsync` | public | Kochbuch-Listen, `userId`-gebunden (Z. 371–393) |
| `CreateAsync` (2 Überladungen) | public | Rezept anlegen inkl. `sideDishRecipeIds` (Z. 71, 88) |
| `UpdateAsync` (2 Überladungen) | public | Rezept ändern inkl. Beilagen (Z. 104, 121) |
| `DeleteAsync` | public | Rezept löschen, `userId`-gebunden (Z. 132) |
| `AddExistingToCookbookAsync` / `RemoveFromCookbookAsync` | public | Kochbuch-Zuordnung (Z. 143, 155) |
| `SetImageAsync` / `AddImageAsync` | public | Bild-Upload, `userId`-gebunden (Z. 169, 183) |
| `GetImageAsync(recipeId, imageId, ct)` | public | Bild laden — **ohne** `userId`; Besitzprüfung erfolgt aufrufend in `RecipesController.GetImage` (Z. 191) |
| `GetImages(recipeId, offset, count)` | public | `IQueryable<RecipeImage>` ohne `userId`-Filter (Z. 199) |
| `GetImageCountAsync` / `DeleteImageAsync` | public | Bildanzahl / Bild löschen (Z. 206, 218) |
| `GetLatestAsync` / `FindByUri` / `SearchAsync` | public | Listen/Suche, `userId`-gebunden (Z. 226–247) |

Hinweise: Es existiert **keine** Methode zum Laden eines Rezepts ohne `userId`-Kontext (anonyme Auflösung). `GetImageAsync`/`GetImages` sind zwar nicht `userId`-gebunden, werden aber nur hinter authentifizierten Endpunkten aufgerufen.

Abonnierte Events: keine. Publizierte Events: keine.

## `RecipeSideDishInfo`
Datei: `Rezepte.Web/Services/RecipeService.cs`, Zeile 277

`public record RecipeSideDishInfo(string Id, string Title, string? Description, string? ImageUrl, int Portions)` — Beilagen-Ergebnisdatensatz des Services.

## `SettingsService`
Datei: `Rezepte.Web/Services/SettingsService.cs` (implementiert `ISettingsService`)

Konstruktor (Z. 21): `RezepteDbContext db`, `ISecurityTxtSettingsService securityTxtSettingsService`. Private Key-Konstanten (Z. 52–60) und Schreib-Helfer `WriteString`/`WriteBool`/`WriteNullableInt`/`UpdateUserSettingAsync` (Z. 322–377).

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `GetUserAiEnabledAsync` / `SetUserAiEnabledAsync` | public | Benutzer-KI-Schalter via `UserSetting` (Z. 33–50) |
| `GetGlobalAiEnabledAsync` / `SetGlobalAiEnabledAsync` | public | Globaler KI-Schalter, `AppSetting`-Key `AiEnabled`, Default `true` (Z. 67–82) |
| `GetUserGoogleVisionEnabledAsync` / `SetUser…` | public | Benutzer-Toggle Google Vision (Z. 91–108) |
| `GetUserGeminiEnabledAsync` / `SetUser…` | public | Benutzer-Toggle Gemini (Z. 116–133) |
| `GetUserRequireAiConfirmationAsync` / `SetUser…` | public | Benutzer-Bestätigungspflicht (Z. 142–159) |
| `GetUserShoppingListEditModeAsync` / `SetUser…` | public | Pro-Benutzer-Modus via `AppSetting`-Key `ShoppingListEditMode:<userId>` (Z. 167–184) |
| `GetGlobalGoogleVisionEnabledAsync` / `SetGlobal…` | public | Globaler Toggle, Default `true` (Z. 194–209) |
| `GetGlobalGeminiEnabledAsync` / `SetGlobal…` | public | Globaler Toggle, Default `true` (Z. 216–231) |
| `GetGlobalMaxRequestsPerHourAsync` / `SetGlobal…` | public | `int?`-Einstellung via `AppSetting`, `null` = unbegrenzt (Z. 239–255) |
| `GetGlobalMaxRequestsPerDayAsync` / `SetGlobal…` | public | `int?`-Einstellung via `AppSetting`, `null` = unbegrenzt (Z. 262–278) |
| `GetGlobalDisableOnLimitReachedAsync` / `SetGlobal…` | public | Bool-Einstellung, Default `false` (Z. 285–300) |
| `GetSecurityTxtSettingsAsync` / `SetSecurityTxtSettingsAsync` | public | Delegiert an `ISecurityTxtSettingsService` (Z. 307–320) |

Für die Link-Gültigkeitsdauer existiert **keine** Methode; das Muster `Get…Async` (Default bei fehlendem `AppSetting`-Eintrag) + `Set…Async` (`WriteNullableInt`/`WriteString`) ist unmittelbar übertragbar.

Abonnierte Events: keine. Publizierte Events: keine.

## `RedirectToRegisterMiddleware`
Datei: `Rezepte.Web/Middleware/RedirectToRegisterMiddleware.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `InvokeAsync(HttpContext)` | public | Prüft `IsExcluded`; sonst Redirect auf `/register` (keine Benutzer) bzw. `/login` (nicht authentifiziert) — Z. 28–46 |
| `RedirectToRegistration` | private | Redirect auf `/register`, wenn noch kein Benutzer existiert (Z. 48–59) |
| `RedirectToLogin` | private | Redirect auf `/login` für nicht authentifizierte Aufrufer (Z. 61–75) |
| `IsExcluded(path)` | private | Freigaben: `/login` (Z. 82), security.txt-Pfade (Z. 86–90), Präfixe `/api`, `/_blazor`, `/_framework`, `/_content`, `/_vs` (Z. 93–98), statische Dateiendungen (Z. 101) |

Eine Freigabe für eine öffentliche Share-Route (z. B. `/share`) existiert **nicht** — anonyme Aufrufe werden aktuell auf `/login` umgeleitet. Registriert in `Program.cs` Zeile 110 via `UseRedirectToRegisterWhenNoUsers()` (Extension Z. 118–121).

## `ApiClient`
Datei: `Rezepte.Web/Services/ApiClient.cs` (Namespace `Rezepte.Web`)

Scoped-Client mit `CircuitAuthHandler` (authentifizierter API-Zugriff aus Blazor-Komponenten).

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `Http` | public (Property) | Zugrunde liegender `HttpClient`, Z. 16 |
| `GetAsync(uri, ct)` | public | Roh-`HttpResponseMessage`, Z. 39 |
| `GetAsync<T>(uri, ct)` | public | JSON-GET mit `EnsureSuccessStatusCode`, Z. 47–53 |
| `PostAsJsonAsync<T>` | public | JSON-POST, Z. 62 |
| `PutAsJsonAsync<T>` | public | JSON-PUT, Z. 71 |
| `Dispose` | public | Räumt `HttpClient` ab, Z. 76–83 |

## `ServiceCollectionExtensions.AddRezepteServices`
Datei: `Rezepte.Web/Extensions/ServiceCollectionExtensions.cs` (Z. 39–240)

Relevante Registrierungen:

- EF Core SQLite: `AddDbContext<RezepteDbContext>` (Z. 66–67).
- Authentifizierung: Cookie (`rezepte.auth`) als Default + JWT Bearer (Z. 93–140); `AddAuthorization` (Z. 142).
- `ApiClient` scoped mit `CircuitAuthHandler` (Z. 158–170).
- **Default-`HttpClient` scoped ohne Auth-Header** („for static/public calls", Z. 172–182) — Cookie-Container aktiv, kein Bearer.
- Services: `IRecipeService`→`RecipeService` (Z. 189), `ISettingsService`→`SettingsService` (Z. 222), `TimeProvider.System` als Singleton (Z. 194).
- ViewModels: `SettingsViewModel` scoped (Z. 233).
- Keine Registrierung für einen Share-Service.

## `SettingsViewModel`
Datei: `Rezepte.Web/ViewModels/SettingsViewModel.cs`

| Member | Sichtbarkeit | Kurzbeschreibung |
|--------|-------------|------------------|
| `Item` (sealed class) | public | Navigationseintrag: `Title`, `Icon`, `ComponentType`, `Visible` (Z. 14–50) |
| `IsAdmin` | public (Property) | `true`, wenn User in Rolle `Admin` (Z. 57) |
| `InitializeAsync` | public | Lädt `AuthenticationState` und setzt `IsAdmin` via `IsInRole("Admin")` (Z. 72–76) |
