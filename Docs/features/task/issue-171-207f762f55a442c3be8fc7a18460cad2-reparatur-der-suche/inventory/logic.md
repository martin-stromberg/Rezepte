# Logik — Rezeptsuche

## `RecipeService`

Datei: `Rezepte.Web/Services/RecipeService.cs` (Klasse ab Zeile 280; `SearchAsync` Zeilen 876–951)

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `SearchAsync(string userId, string? q, string? tags, string? cookbookId, int page, int pageSize, string sort, CancellationToken ct)` | `public` | Kernmethode der Suche; aufgerufen von `RecipesController.SearchAsync` |
| `GetByIdAsync`, `GetSideDishesAsync`, `GetByCookbookAsync`, `GetAvailableForCookbookAsync`, `CreateAsync` (2×), `UpdateAsync` (2×), `DeleteAsync`, `AddExistingToCookbookAsync`, `RemoveFromCookbookAsync`, `SetImageAsync`, `AddImageAsync`, `GetImageAsync`, `GetImages`, `GetImageCountAsync`, `DeleteImageAsync`, `GetLatestAsync`, `FindByUri` | `public` | Übrige `IRecipeService`-Implementierung; `GetAvailableForCookbookAsync` speist `GET api/recipes/available-for/{cookbookId}` (`RecipeSelectDialog`) |
| `NormalizeRecipeIds`, `ValidateSideDishIdsAsync`, `ReplaceSideDishes` | `private` | Beilagen-Hilfsmethoden; nicht suchrelevant |

Abonnierte Events: keine. Publizierte Events: keine.

### `SearchAsync` — Ablauf im Detail (Zeilen 876–951)

1. Basis-Query: `_db.Recipes.AsNoTracking().Where(r => r.UserId == userId)` mit `Include(r => r.Images)`, `Include(r => r.Steps!).ThenInclude(s => s.Ingredients!)`, `Include(r => r.RecipeCookbooks)` (Zeilen 879–886).
2. Suchprädikat nur bei nicht-leerem `q` (Zeilen 889–898): `EF.Functions.Like` auf `Title`, `Description`, `Steps[].Description`, `Steps[].Ingredients[].Name` mit Pattern `%q.Trim()%`. Auf dem SQLite-Provider ist `Like` nur für ASCII case-insensitiv.
3. `cookbookId`-Filter über `RecipeCookbooks.Any(cb => cb.CookbookId == cookbookId)` (Zeilen 900–903).
4. `tags`-Parameter wird ignoriert — Code-Kommentar Zeilen 905–906: kein Tag-Modell vorhanden.
5. `total = await query.CountAsync(ct)` (Zeile 909).
6. Sortierung per `sort?.ToLowerInvariant()`-Switch: `"title"` → `OrderBy(Title)`, `"newest"` und Fallback → `OrderByDescending(CreatedAt)` (Zeilen 912–917). `"relevance"` besitzt keine eigene Logik.
7. Paging `Skip((page - 1) * pageSize).Take(pageSize)` und Projektion auf `SearchResultItem` (Zeilen 920–939): `PrimaryImageUrl` = neuestes `Images.Url`, `Snippet` = erste `Steps.Description` (nach `StepIndex`), sonst `string.Join(", ", …)` über bis zu 10 Zutatennamen, sonst `Description`, sonst `string.Empty`; `Tags = Array.Empty<string>()`.
8. Nachgelagerte Kürzung von `Snippet` auf 200 Zeichen + „…" im Speicher (Zeilen 942–948).

## `RecipesController`

Datei: `Rezepte.Web/Controllers/RecipesController.cs` — `[ApiController]`, `[Route("api/[controller]")]`, klassenweit `[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]` (nur Bearer-JWT, keine Cookies; Ausnahme: `GetImage` akzeptiert zusätzlich Cookies).

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `SearchAsync(q, tags, cookbookId, page = 1, pageSize = 10, sort = "relevance", ct)` — Zeilen 567–597 | `public` | `GET /api/recipes/search`; löst `userId` über `GetUserId()` (sonst `401`), clamped `page` ≥ 1 und `pageSize` auf 1–`MaxPageSize` (100), ruft `_recipes.SearchAsync`, mappt auf `RecipeSearchResponseDto` |
| `GetAvailableFor(cookbookId, page = 1, pageSize = 20, search = null, ct)` — Zeilen 66–79 | `public` | `GET /api/recipes/available-for/{cookbookId}`; filtert in-memory mit `r.Title.Contains(search)` (case-sensitiv), liefert `List<RecipeListItemDto>`; Aufrufer: `RecipeSelectDialog.razor` |
| `GetByCookbook`, `GetById`, `GetSideDishes`, `GetCookbooksForRecipe`, `AddExisting`, `Create`, `Update`, `Delete`, `UploadImage`, `GetImage`, `GetImages`, `DeleteImage`, `GetLatestForCookbook` | `public` | Übrige Endpunkte; `GetById`/`GetSideDishes` werden von `RecipeEdit.razor` bzw. `CalendarEventDialog.razor` im Such-/Pickerkontext mitverwendet |
| `MapSteps` — Zeilen 254–261 | `private` | DTO→`RecipeCreateStep`-Mapping; nicht suchrelevant |

Abonnierte/publizierte Events: keine.

## `ApiControllerBase`

Datei: `Rezepte.Web/Controllers/ApiControllerBase.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `GetUserId()` | `protected` | `User.FindFirst(ClaimTypes.NameIdentifier)?.Value` — liefert `null` → Aufrufer antwortet `Unauthorized()` |
| `CurrentUserId` | `protected` | `GetUserId() ?? string.Empty` |
| `TryGetUserId(out string)` | `protected` | bool-Variante der Userauflösung |

## `ApiClient`

Datei: `Rezepte.Web/Services/ApiClient.cs` (Namespace `Rezepte.Web`, `IDisposable`)

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `Http` | `public` Property | exponierter `HttpClient` mit `BaseAddress = nav.BaseUri` (Zeilen 27–30); alle UI-Suchaufrufe gehen über `Client.Http.GetFromJsonAsync` |
| `ApiClient(HttpClient, NavigationManager)` | `public` ctor | setzt `BaseAddress`, falls nicht bereits gesetzt |
| `GetAsync`, `GetAsync<T>`, `PostAsJsonAsync<T>`, `PutAsJsonAsync<T>` | `public` | generische Wrapper; von der Suche nicht genutzt |
| `Dispose` | `public` | disposed `Http` |

DI-Verdrahtung (`ServiceCollectionExtensions.cs:154-170`): Named Client `"ApiClient"` mit `AntiForgeryHandler`; scoped `ApiClient` mit `CircuitAuthHandler` als äußerstem `DelegatingHandler` und `BaseAddress = nav.BaseUri`.

## `CircuitAuthHandler`

Datei: `Rezepte.Web/Services/CircuitAuthHandler.cs` — `DelegatingHandler`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `SendAsync(request, ct)` | `protected override` | ermittelt User aus `IHttpContextAccessor.HttpContext?.User`, bei nicht authentifiziertem Kontext Fallback auf `AuthenticationStateProvider.GetAuthenticationStateAsync()` (Circuit); hängt bei vorhandener `NameIdentifier`-Claim ein Bearer-JWT an (`TokenService.GetToken` bzw. `CreateToken`, Zeilen 45–60) |

Für SSR (kein Circuit) ist der `HttpContext`-Pfad maßgeblich; im interaktiven Circuit ist `HttpContext` `null` und der `AuthenticationStateProvider`-Pfad greift.

## `TokenService`

Datei: `Rezepte.Web/Services/TokenService.cs` (implementiert `ITokenService`)

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `CreateToken(userId, username, isAdmin)` | `public` | erzeugt JWT (HMAC-SHA256, Issuer/Audience/Key aus `IJwtSigningKeyProvider`, Claims `NameIdentifier`, `Name`, optional `Role=Admin`, 8 h Laufzeit) und cached ihn unter `token:{userId}` |
| `GetToken(userId)` | `public` | liest gecachten Token aus `IMemoryCache` |

## `JwtSigningKeyProvider`

Datei: `Rezepte.Web/Security/JwtSigningKeyProvider.cs` (implementiert `IJwtSigningKeyProvider`)

| Methode / Member | Sichtbarkeit | Kurzbeschreibung |
|------------------|-------------|------------------|
| ctor(`IConfiguration`, `IHostEnvironment`) | `public` | liest `Jwt:Issuer`/`Jwt:Audience` (Defaults `rezepte`/`rezepte.api`); `Jwt:Key` < 32 Zeichen oder `JWT_KEY`-Platzhalter → außerhalb Development `InvalidOperationException` beim Start, in Development ephemeraler Zufallsschlüssel; `Key = SHA256(secret)` |
| `Key`, `Issuer`, `Audience` | `public` Properties | Signaturmaterial und Token-Metadaten |

## `AntiForgeryHandler`

Datei: `Rezepte.Web/Services/AntiForgeryHandler.cs` — `DelegatingHandler`, innerer Handler des Named Clients `"ApiClient"`.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `SendAsync(request, ct)` | `protected override` | nur bei nicht-GET/HEAD: übernimmt `RequestVerificationToken`-Cookie bzw. `.AspNetCore.Antiforgery.*`-Cookie als Header; für den GET-Suchaufruf wirkungslos |

## `ServiceCollectionExtensions.AddRezepteServices`

Datei: `Rezepte.Web/Extensions/ServiceCollectionExtensions.cs` (Zeilen 39–239)

| Bereich | Zeilen | Kurzbeschreibung |
|---------|--------|------------------|
| `AddRazorComponents().AddInteractiveServerComponents()` | 50–57 | registriert interaktives Server-Rendering; ob eine Seite es nutzt, entscheidet `@rendermode` je Komponente |
| `AddDbContext<RezepteDbContext>(UseSqlite)` | 66–67 | SQLite-Provider, ConnectionString `Default` |
| `JwtSigningKeyProvider`-Instanz + JWT-Bearer-Validierung | 70–71, 129–140 | `TokenValidationParameters` mit Issuer/Audience/Key des Providers |
| `AddHttpClient("ApiClient")` + `AntiForgeryHandler` | 150–155 | gepoolte Basis-Handler-Pipeline |
| `AddScoped<ApiClient>` mit `CircuitAuthHandler` + `BaseAddress` | 157–170 | Self-Call-Pipeline der Blazor-Komponenten |
| `AddScoped<IRecipeService, RecipeService>` | 189 | DI-Registrierung des Such-Services |

## `Program.cs` — Request-Pipeline

Datei: `Rezepte.Web/Program.cs`

- `app.MapRazorComponents<App>().AddInteractiveServerRenderMode()` (Zeilen 103–104): Komponenten ohne `@rendermode` werden statisch (SSR) gerendert — betrifft `RecipeSearch.razor`.
- `app.MapControllers()` (Zeile 107); `UseAntiforgery` nur für nicht-`/api`-Routen (Zeilen 94–97); kein `UsePathBase`/Forwarded-Headers-Eintrag — die App terminiert direkt auf `nav.BaseUri`.
