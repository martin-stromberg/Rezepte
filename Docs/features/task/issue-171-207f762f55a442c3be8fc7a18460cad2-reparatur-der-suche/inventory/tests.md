# Tests — Rezeptsuche

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-10-24 21:03–21:06 +0200 (MESZ)
- Branch und Commit-ID: `task/issue-171-207f762f55a442c3be8fc7a18460cad2-reparatur-der-suche`, Commit `76079a883c0ed4b7f4cbd0a50b4a1fad8223e81f`
- Uncommittete Änderungen im getesteten Stand: nur untracked — `Docs/features/task/issue-171-207f762f55a442c3be8fc7a18460cad2-reparatur-der-suche/` (Anforderungs-/Bestandsaufnahme-Dokumente; kein Produktivcode, keine Tests)
- Testumgebung und Runtime-/SDK-Versionen: Windows (Git Bash), .NET SDK `10.0.402`, `net10.0`; Playwright-Chromium installiert (Browser-Tests liefen tatsächlich, nicht übersprungen); xunit 2.9.3, FluentAssertions 8.10.0, Moq 4.20.72, Microsoft.Playwright 1.61.0, EF Core InMemory 10.0.11
- Ermittelte Testsuiten und Quellen der Testbefehle:
  - `Rezepte.Tests` (xunit, Unit-/Komponententests, EF Core **InMemory**-Provider) — Befehl aus `README.md`/`Docs/testing.md`: `dotnet test`
  - `Rezepte.Tests.Browser` (xunit + Playwright, startet publizierte `Rezepte.Web` als Prozess mit echter SQLite-DB) — Befehl aus `Docs/testing.md` und CI `.github/workflows/staging-ci.yml` (Job `build-and-test`: `dotnet test Rezepte.sln --configuration Release --no-build --logger "trx;LogFileName=test-results.trx"`)
  - `Rezepte.Tests.PluginFixture` ist trotz Namens eine normale Bibliothek (kein `Microsoft.NET.Test.Sdk`), `Rezepte.Updater.TestHost` eine Exe — beide enthalten keine Tests

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| 1 | `dotnet test Rezepte.sln --configuration Release --logger "trx" --results-directory <inventory/test-results>` | Repo-Root | 0 | 590 (`Rezepte.Tests`) + 15 (`Rezepte.Tests.Browser`) = 605 | 0 | 0 | [Konsole](test-results/dotnet-test-sln-console.log), [TRX Unit](test-results/Martin_DESKTOP-CM8OBSG_2026-10-24_21_03_43_net10.0.trx), [TRX Browser](test-results/Martin_DESKTOP-CM8OBSG_2026-10-24_21_03_48_net10.0.trx) |

Dauer: `Rezepte.Tests` 33 s, `Rezepte.Tests.Browser` 2 m 51 s. Der Browser-Lauf benötigte keinen Skip — Playwright, App-Publish und Nutzer-Seeding funktionierten.

### Nachgewiesene bestehende Testfehler

Es wurden **keine Testfehler nachgewiesen**: 605/605 Tests bestanden im dokumentierten Ausgangslauf. Es existiert damit kein „preexisting failure" für die Suchfunktion.

### Testlücken und Ausführungsprobleme

- Alle drei `SearchAsync`-Service-Tests laufen auf dem **InMemory**-Provider (`UseInMemoryDatabase`, `RecipeServiceTests.cs:24-30`). Provider-spezifisches Verhalten (`EF.Functions.Like`-Übersetzung, Case-Sensitivität/Umlaute auf SQLite, `string.Join`-Projektion im SQL, Kartesische-Explosion der `Include`-Kette) ist ungetestet.
- Kein Test für `RecipesController.SearchAsync` (Parameter-Clamping, `Unauthorized`, DTO-Mapping, `RecipeSearchResponseDto.Total`); `Rezepte.Tests/Controllers/` enthält keine `RecipesController`-Tests.
- Kein bUnit-/Komponenten-Test für `RecipeSearch.razor` (`Rezepte.Tests/Components/` enthält nur `LoadingBarRenderingTests`, `SecurityTxtSettingsTests`, `CollectionImportSelectionStateTests`); der `OperationCanceledException`-Pfad und die SSR-Eigenschaften (fehlendes `@rendermode`, inerte Paginierung, `0`-Bindung von `pageNumber`/`pageSize`) sind ungetestet.
- Kein dedizierter E2E-Test „Suche liefert Trefferliste" in `Rezepte.Tests.Browser`; die Seite wird nur indirekt genutzt (s. `LoadingBarFormNavigationBrowserTests`, `RegisterTests.CountRecipesAsync`).

## Testklassen

### `RecipeServiceTests`

Datei: `Rezepte.Tests/Services/RecipeServiceTests.cs` — xunit, `RecipeService` jeweils mit `CreateDb()` (InMemory, GUID-Name), `CreateMockEnv()` (Moq `IWebHostEnvironment`) und `CreateMockHttpContextAccessor()` (`NameIdentifier`-Claim).

Suchbezogene Methoden:

- `SearchAsync_ShouldFindHonigRecipe_ForOwningUser` (Z. 215) — `CreateAsync` mit Titel „Honig - Senf - Sojamarinade", `SearchAsync(UserA, "Honig", …)` liefert `TotalCount >= 1` und den Titel in `Items`
- `SearchAsync_ShouldNotReturnForeignUserRecipes` (Z. 235) — `SearchAsync(UserB, "Honig", …)` liefert `TotalCount == 0` (User-Filter)
- `SearchAsync_ShouldFilterByStringCookbookId` (Z. 255) — zwei Kochbücher, `cookbookId`-Filter liefert genau das Marinaden-Rezept

Weitere Methoden der Klasse decken `CreateAsync`/`UpdateAsync`/`DeleteAsync`, `GetByCookbookAsync`, `GetAvailableForCookbookAsync`, `AddExistingToCookbookAsync` und Beilagen ab — indirekt relevant, weil `SearchAsync` dieselben Entitäten/`Include`-Kette benutzt.

### `RecipeServiceStepValidationTests`

Datei: `Rezepte.Tests/Services/RecipeServiceStepValidationTests.cs` — Validierungstests von `CreateAsync`-Schritten; kein direkter Suchbezug (gleiche Service-Klasse).

### `CircuitAuthHandlerTests`

Datei: `Rezepte.Tests/Services/CircuitAuthHandlerTests.cs` — deckt den JWT-Anhänge-Mechanismus des Self-Call-Handlers ab (`HttpContext`- und `AuthenticationStateProvider`-Pfad), der für jeden `/api/recipes/search`-Aufruf aus Blazor-Komponenten durchlaufen wird.

### `TokenServiceTests` / `JwtSigningKeyProviderTests`

Dateien: `Rezepte.Tests/Services/TokenServiceTests.cs`, `Rezepte.Tests/Security/JwtSigningKeyProviderTests.cs` — Token-Erzeugung/-Caching bzw. Schlüsselableitung/Fail-Fast bei fehlendem `Jwt:Key`.

### `LoadingBarFormNavigationBrowserTests`

Datei: `Rezepte.Tests.Browser/LoadingBarFormNavigationBrowserTests.cs` — Playwright (Collection `BrowserTestCollection`, `SkippableFact`).

- `SearchSubmit_ShowsLoadingBar` (Z. 32) — füllt `#nav-search` mit „Suppe", klickt den Navbar-Such-Button, verzögert Route `**/recipes/search*` und prüft die Loading-Bar; nutzt `/recipes/search` nur als Navigationsziel (keine Trefferassertion)

### `RegisterTests`

Datei: `Rezepte.Tests.Browser/Auth/RegisterTests.cs` — Collection `RegisterTestCollection`.

- `CountRecipesAsync` (Hilfsmethode, Z. 189–197) — navigiert zu `/recipes/search?q=&page=1&pageSize=100` und zählt `.list-group-item`; wartet auf `.list-group-item, .alert`. Nutzt die Suchseite als Datenquelle, prüft aber nicht das Suchverhalten selbst.

## Hilfsmethoden / Infrastruktur

### `RezepteAppFixture` (`Rezepte.Tests.Browser/Infrastructure/RezepteAppFixture.cs`)

- `InitializeAsync` — startet publizierte `Rezepte.Web.dll` als Prozess auf freiem Port mit temporärer SQLite-DB und festem `Jwt__Key` (`browser-test-signing-key-…`); pollt Readiness (max. 60 s); registriert Testuser `browsertest`/`BrowserTest!123` über `api/auth/register`
- `BaseAddress`, `DatabasePath`, `ApplicationAvailable`, `ApplicationUnavailableSkipReason` — Zugriff für Page-Objekte; Grund, warum Browser-Tests ggf. überspringen statt fehlschlagen

### `PlaywrightBrowserFixture` (`Rezepte.Tests.Browser/Infrastructure/PlaywrightBrowserFixture.cs`)

- `InitializeAsync` — `Playwright.CreateAsync()` + `Chromium.LaunchAsync(Headless: true)`; `PlaywrightException` → `BrowsersAvailable = false`, `UnavailableReason` (Skip statt Fehler)
- `Browser`, `BrowsersAvailable` — geteilte Browser-Instanz der Collection

### `LoadingBarPageObject` (`Rezepte.Tests.Browser/Infrastructure/LoadingBarPageObject.cs`)

- `SubmitNavigationSearchAsync(term)` (Z. 157) — füllt `#nav-search` und klickt `button[aria-label='Suche starten']`
- `DelayRouteAsync`, `BlockRouteAsync` — Playwright-Routensteuerung; `SearchRouteGlob = "**/recipes/search*"` (Z. 35)
- `GotoAsync`, `WaitForSettledContentAsync`, Login-/Navigations-Helfer

### `BrowserTestCollection` / `RegisterTestCollection` / `ConfiguredRezepteAppFixture` / `RegisterTestsAppFixture`

Dateien: `Rezepte.Tests.Browser/Infrastructure/*.cs` — xunit-Collection-Definitionen, die App- und Browser-Fixture gemeinsam nutzen (serielle Ausführung der Browser-Tests).
