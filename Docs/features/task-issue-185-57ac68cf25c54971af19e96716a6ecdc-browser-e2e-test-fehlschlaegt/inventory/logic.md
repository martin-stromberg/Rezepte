# Logik — Bestandsaufnahme

Betroffene Logik- und Infrastrukturklassen rund um den Browser-E2E-Test
`InPageSearch_Submit_UpdatesResultList` und die Seite `/recipes/search`.

## `RecipeSearch` (Razor-Komponente)

Datei: `Rezepte.Web/Components/Pages/RecipeSearch.razor`

- Route `@page "/recipes/search"`, `@rendermode InteractiveServer` (Zeile 2) — serverseitiges
  Prerendering ist damit standardmäßig aktiv; beim Circuit-Start läuft die Komponente ein
  zweites Mal durch ihren Lebenszyklus.
- `[Authorize]`-Attribut, injiziert `IRecipeService`, `AuthenticationStateProvider`,
  `NavigationManager`, `ILogger<RecipeSearch>`, `IStringLocalizer<UiStrings>`.
- GET-Suchformular (Zeilen 19–30, dokumentierter Kommentar: „plain GET form" bewusst ohne
  `@onsubmit`/`@bind`, kein `action`-Attribut): `<input id="search-q" name="q" ... value="@(q ?? string.Empty)" />`
  plus hidden `pageSize`-Feld und Submit-Button `button[type='submit']`.
- Zustandsblöcke (Zeilen 32–87): `isLoading` → Spinner; `searchFailed` → `.alert-danger`;
  leere `results` → `.alert-secondary` („Keine Treffer."); sonst `.list-group` mit
  `.list-group-item`-Links und Paginierung (`button[aria-label='Nächste'/'Vorherige']`).

| Member | Sichtbarkeit | Kurzbeschreibung |
|--------|-------------|------------------|
| `q` | `public` `[Parameter] [SupplyParameterFromQuery(Name = "q")]` | Query-Parameter; speist `value` von `#search-q` und die Suche |
| `pageNumber` | `public` `[Parameter] [SupplyParameterFromQuery(Name = "page")]` | Seitennummer, Default 1 |
| `pageSize` | `public` `[Parameter] [SupplyParameterFromQuery(Name = "pageSize")]` | Seitengröße, Default 10 |
| `isLoading`, `searchFailed`, `cts`, `results`, `totalItems` | `private` Felder | Ladezustand, Fehlerflag, `CancellationTokenSource`, Trefferarray, Gesamttrefferzahl |
| `CurrentPage` | `private` Property | `pageNumber < 1 ? 1 : pageNumber` |
| `CurrentPageSize` | `private` Property | `pageSize < 1 ? 20 : Math.Min(pageSize, IRecipeService.MaxSearchPageSize)` |
| `TotalPages` | `private` Property | `Math.Max(1, Ceiling(totalItems / CurrentPageSize))` |
| `OnParametersSetAsync` | `protected override` | Ruft bei jeder Parameter-Setzung `RunSearchAsync()` auf (Zeilen 112–115) — läuft im Prerender UND beim Circuit-Start erneut |
| `RunSearchAsync` | `private` | Bricht Vorgänger per `cts` ab, setzt `isLoading = true`, löscht `results`; liest `userId` aus `AuthStateProvider` (ohne User → `searchFailed`); ruft `RecipeService.SearchAsync(userId, q, null, null, CurrentPage, CurrentPageSize, "newest", token)`; im `finally` `isLoading = false` + `StateHasChanged()` (nur wenn noch aktive Suche). Löst beim Circuit-Start mindestens zwei Re-Renders aus |
| `ChangePage` | `private` | `Nav.NavigateTo("/recipes/search?q=…&page=…&pageSize=…")` |
| `Dispose` | `public` | Bricht `cts` ab, nullt das Feld (In-flight-Abbruch → „superseded", kein Fehler) |
| `RecipeResult` | `private sealed record` | View-Model der Trefferliste, siehe [models.md](models.md) |

Abonnierte Events: keine (Parameter-Änderungen über `OnParametersSetAsync`).
Publizierte Events: `StateHasChanged()` im `finally` von `RunSearchAsync` triggert Re-Render.

## `MainLayout` (Navbar-Suche, Kontext)

Datei: `Rezepte.Web/Components/Layout/MainLayout.razor`

- Zeile 39–40: `<form ... @onsubmit="OnSubmitSearch" action="/recipes/search" method="get">`
  mit `InputText id="nav-search" name="q" ... @bind-Value="searchQuery" @bind-Value:event="oninput"`.
- `OnSubmitSearch` (Zeile ~116): `Nav.NavigateTo($"/recipes/search?q={q}")` — gleiche Ziel-URL
  wie der native GET-Fallback. Vom Browser-Test `NavbarSearch_ShowsSeededRecipe` über
  `SubmitNavigationSearchAsync` benutzt; nicht direkt betroffen.

## `RecipeService` / `IRecipeService` (Scope-Ausschluss, nur Signatur)

Datei: `Rezepte.Web/Services/RecipeService.cs` (Interface und Klasse liegen in derselben Datei;
`Rezepte.Web/Services/IRecipeService.cs` enthält nur ein `using`-Statement, keine Deklaration)

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `SearchAsync(userId, q, tags, cookbookId, page, pageSize, sort, ct)` | `public` (Zeile 882) | EF-Core-LIKE-Suche über `Title`, `Description`, `Step.Description`, `Ingredient.Name`; `page`/`pageSize` geclamped (`MaxSearchPageSize = 100`); Sortierung `newest`/`title`; projiziert auf `SearchResult`/`SearchResultItem` |

Wird aufgerufen von `RecipeSearch.RunSearchAsync`, `RecipesController.SearchAsync`,
`CalendarEventDialog.TriggerRecipeSearchAsync`, `RecipeEdit.razor`. Keine Änderung vorgesehen.

## Browser-Test-Infrastruktur

### `LoadingBarPageObject`
Datei: `Rezepte.Tests.Browser/Infrastructure/LoadingBarPageObject.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `CreateAsync(browser, baseAddress)` | `public static` | Neuer `IBrowserContext` + `IPage`, startet Playwright-Tracing (Screenshots/Snapshots/Sources) |
| `LoginAsync(username, password)` | `public` | `/login`, `#username`/`#password` füllen, Submit, wartet auf URL ≠ `/login` (10 s) |
| `GotoAsync(path)` | `public` | `Page.GotoAsync(baseAddress + path)` — wartet nur auf `load`, **kein** Warten auf Circuit-Stabilität |
| `ClickNavigationLinkAsync(href)` | `public` | Klick mit `NoWaitAfter` (obsoleter Schalter, bewusst genutzt) |
| `DelayRouteAsync(glob, ms)` / `BlockRouteAsync(glob)` | `public` | Route-Delay/Abort für Timing-Tests |
| `SubmitNavigationSearchAsync(term)` | `public` | Füllt `#nav-search`, klickt Submit-Button (Navbar-Formular) |
| `SubmitInteractiveShoppingListItemAsync(item)` | `public` | Shopping-Liste: enthält bereits ein Deadline-Retry-Muster (bis zu 5 Klick-Versuche) |
| `IsLoadingBarActiveAsync`, `GetLoadingBarColorAsync`, `GetLoadingBarOpacityAsync` | `public` | Loading-Bar-Zustandsabfragen |
| `WaitUntilLoadingBarActiveAsync` / `...VisibleAsync` / `...ColorChangedAsync` / `...HiddenAsync` | `public` | `WaitForFunctionAsync`-Helfer (Defaults 1 s/2 s/2 s/5 s) — **kein** Helfer für „interaktiver Circuit aktiv / Seite stabil" vorhanden |
| `DisposeAsync` | `public` | Trace-ZIP nach `playwright-traces/` sichern, Context schließen |

### `LoadingBarBrowserSession`
Datei: `Rezepte.Tests.Browser/Infrastructure/LoadingBarBrowserSession.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `StartLoggedInSessionAsync(browserFixture, appFixture)` | `internal static` | `Skip.IfNot` bei fehlendem Browser/App; `LoadingBarPageObject.CreateAsync` + `LoginAsync` mit `RezepteAppFixture.TestUsername`/`TestPassword` |

### `RecipeSeedHelper`
Datei: `Rezepte.Tests.Browser/Infrastructure/RecipeSeedHelper.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `SeedRecipesAsync(page, baseAddress, params titles)` | `public static` | `GET /api/session/token` → Bearer-JWT; je Titel `POST /api/recipes` (Titel, Beschreibung, 4 Portionen). Unverändert wiederverwendet |

### `RezepteAppFixture`
Datei: `Rezepte.Tests.Browser/Infrastructure/RezepteAppFixture.cs`

| Methode/Property | Sichtbarkeit | Kurzbeschreibung |
|------------------|-------------|------------------|
| `InitializeAsync` | `public` | Publizierte `Rezepte.Web.dll` suchen (`REZEPTE_PUBLISH_DIR` oder `Rezepte.Web/bin/<Cfg>/<tfm>/publish/`), freien TCP-Port wählen, App-Prozess mit SQLite-Temp-DB + `ASPNETCORE_ENVIRONMENT=Production` + Test-JWT-Key + `RateLimiting__Authentication__PermitLimit=1000` starten, Readiness-Poll (60 s), Testuser `browsertest` registrieren |
| `RestartWithEmptyDatabaseAsync` | `public` | App-Prozess gegen neue leere DB neu starten (für Register-Tests) |
| `BaseAddress`, `DatabasePath`, `ApplicationAvailable`, `ApplicationUnavailableSkipReason` | `public` Properties | Zustand der gestarteten App |
| `SeedSharedTestUser` | `protected virtual` | Überschreibbar (Register-Fixture deaktiviert) |

### `PlaywrightBrowserFixture`
Datei: `Rezepte.Tests.Browser/Infrastructure/PlaywrightBrowserFixture.cs`

| Methode/Property | Sichtbarkeit | Kurzbeschreibung |
|------------------|-------------|------------------|
| `InitializeAsync` | `public` | `Playwright.CreateAsync()` + headless Chromium; `PlaywrightException` → `BrowsersAvailable = false` + `UnavailableReason` (Tests skippen statt fehlschlagen) |
| `Browser`, `BrowsersAvailable`, `UnavailableReason` | `public` Properties | Geteilter Browser über `BrowserTestCollection` |

### `BrowserTestCollection` / `RegisterTestCollection` / `PluginSettingsTestCollection`
Dateien: `Rezepte.Tests.Browser/Infrastructure/*Collection.cs`

- `BrowserTestCollection` (Name `"Rezepte Browser Tests"`): shared `PlaywrightBrowserFixture` +
  `RezepteAppFixture`; u. a. `RecipeSearchBrowserTests` und alle `LoadingBar*BrowserTests`.
- `RegisterTestCollection`: `RegisterTestsAppFixture` (abgeleitet von `RezepteAppFixture`,
  `SeedSharedTestUser = false`) für `Auth/RegisterTests`.
- `PluginSettingsTestCollection`: eigene `PluginSettingsAppFixture` für `PluginSettingsBrowserTests`.

## CI-Ausführungskontext

Datei: `.github/workflows/staging-ci.yml` (Workflow „Pre-Release", nur Kontext — keine Änderung)

- Job `build & test` (Zeilen 94–211): Restore → `dotnet build` (Rezepte.Web, Rezepte.Tests,
  Rezepte.Tests.Browser, jeweils Release) → `playwright.ps1 install --with-deps chromium` →
  `dotnet publish Rezepte.Web -c Release` → `dotnet test Rezepte.sln -c Release --no-build`
  mit TRX-Logger → Coverage-Gate (70 %) → Artefakt-Uploads (`test-results-staging`,
  `playwright-traces-staging`).
- `Rezepte.Tests.Browser.csproj`: Target `PublishRezepteWebForBrowserTests` publiziert
  `Rezepte.Web` automatisch beim Build des Testprojekts (MSBuild-`Publish`-Target).
