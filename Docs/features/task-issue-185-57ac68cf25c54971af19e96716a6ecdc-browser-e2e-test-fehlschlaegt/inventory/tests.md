# Tests — Bestandsaufnahme

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-11-15, Basislauf 18:35:55–18:38:53 UTC+01:00 (TRX-Zeitstempel);
  Wiederholungsläufe ca. 18:38–18:41 UTC+01:00
- Branch und Commit-ID: `task/issue-185-57ac68cf25c54971af19e96716a6ecdc-browser-e2e-test-fehlschlaegt`,
  `227ea9a0296cf8ffe55479b7ff1e55ba05c69dab` („Orchestrierung mehrstufiger Datenpflege in
  Services kapseln (#183)", Stand entspricht `staging`-Mergebasis)
- Uncommittete Änderungen im getesteten Stand: nur untracked
  `Docs/features/task-issue-185-.../` (requirement.md, todo.md, dieses inventory-Verzeichnis) —
  keinerlei Änderungen an Produktivcode oder Tests
- Testumgebung und Runtime-/SDK-Versionen: Windows-Laptop (lokal), .NET SDK 10.0.403,
  Laufzeit laut Testhost `.NET 10.0.15`, Microsoft.Playwright 1.61.0 mit lokal installiertem
  Chromium (headless); `Rezepte.Web` Release-Publish wurde beim Build des Browser-Testprojekts
  automatisch erzeugt (`PublishRezepteWebForBrowserTests`-Target)
- Ermittelte Testsuiten und Quellen der Testbefehle:
  - `Rezepte.Tests` (xUnit/bUnit, Unit- und Komponententests) — `dotnet test`
  - `Rezepte.Tests.Browser` (Playwright-E2E) — benötigt Release-Publish von `Rezepte.Web`
    und Playwright-Chromium, siehe `Docs/testing.md` und Job `build & test` in
    `.github/workflows/staging-ci.yml` (dort: `dotnet test Rezepte.sln -c Release --no-build`)
  - Vorbereitung wie CI: `dotnet restore Rezepte.sln`,
    `dotnet build Rezepte.sln -c Release --no-restore` (0 Warnungen, 0 Fehler),
    `pwsh Rezepte.Tests.Browser/bin/Release/net10.0/playwright.ps1 install chromium`

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| 1 (Basis) | `dotnet test Rezepte.sln --configuration Release --no-build --logger "trx;LogFileName=inventory-baseline.trx"` | Repo-Root | `dotnet test`: nicht numerisch erfasst (Pipeline-Ausgang durch `tail` maskiert); Konsole meldet „Der Testlauf war erfolgreich" für `Rezepte.Tests` und „Fehler beim Testlauf" für `Rezepte.Tests.Browser` | 714 + 24 = 738 | 1 | 0 | [Konsole](test-results/dotnet-test-baseline-console.log), [TRX Rezepte.Tests](test-results/Rezepte.Tests-baseline.trx), [TRX Rezepte.Tests.Browser](test-results/Rezepte.Tests.Browser-baseline.trx) |
| 2–6 (5× gefiltert) | `dotnet test Rezepte.Tests.Browser/Rezepte.Tests.Browser.csproj --configuration Release --no-build --filter "FullyQualifiedName~RecipeSearchBrowserTests"` | Repo-Root | 0 (je Iteration) | je 5 (insgesamt 25 Ausführungen) | 0 | 0 | Konsolenausgabe dieser Bestandsaufnahme (kein Report gespeichert) |

### Nachgewiesene bestehende Testfehler

| Test-ID inkl. Testfall | Suite / Dateipfad | Fehlerbild / Fehlermeldung | Lauf und Nachweis |
|-----------------------|------------------|---------------------------|-------------------|
| `Rezepte.Tests.Browser.RegisterTests.Register_WithDemoData_CreatesDemoData` | `Rezepte.Tests.Browser/Auth/RegisterTests.cs` (Zeilen 50–87), Collection `RegisterTestCollection` | `System.TimeoutException : Demo data state did not match expected value True within 120000ms. Last observed: cookbooks=5, recipes=43, calendar=0, shopping=6; seed job status=Succeeded, error=(none)`. Der Seeding-Job meldet `Succeeded`, die Kalender-Demodaten bleiben aber 0 → `DemoDataWaitHelper.WaitForDemoDataAsync` (`RegisterTests.cs:305`) läuft in den 120-s-Timeout. Kein Bezug zum Suchpfad der Anforderung | Lauf 1, [Konsole](test-results/dotnet-test-baseline-console.log), [TRX](test-results/Rezepte.Tests.Browser-baseline.trx) |

### Zum Fehlerbild der Anforderung

Der Zieltest `Rezepte.Tests.Browser.RecipeSearchBrowserTests.InPageSearch_Submit_UpdatesResultList`
war im Basislauf (Lauf 1, 813 ms) **und** in allen fünf gefilterten Wiederholungsläufen
(Läufe 2–6) **grün**. Das in Issue #185 beschriebene Fehlerbild (`#search-q` enthält nach
`FillAsync` noch den Query-Parameter-Wert `E2E-AndererBegriff-…`) ließ sich lokal **nicht**
reproduzieren — konsistent mit einem sporadischen, last-/timingabhängigen Flake, der im
CI-Runner auftritt. Es gibt damit keinen dokumentierten Fehlschlag dieses Tests im
Ausgangszustand; die Flake-Ursache (Prerender/Circuit-Doppellauf) ist aus dem Code und der
Vorgängerdoku ersichtlich (siehe [logic.md](logic.md) und
`Docs/features/task/issue-171-207f762f55a442c3be8fc7a18460cad2-reparatur-der-suche-tasks.md` Zeile 34).

### Testlücken und Ausführungsprobleme

- Keine übersprungenen oder nicht ausgeführten Tests (TRX-Counters: `Rezepte.Tests`
  714/714 ausgeführt, `Rezepte.Tests.Browser` 25/25 ausgeführt, `notExecuted=0`).
- Die Browser-Suite benötigt den Release-Publish von `Rezepte.Web` und Playwright-Chromium;
  ohne diese Voraussetzung würden die Tests via `Skip.IfNot` übersprungen statt fehlzuschlagen
  (siehe `RezepteAppFixture.ResolveApplicationDllPath`, `PlaywrightBrowserFixture`).
- Beim Basislauf wurde kein Coverage-Collector mitgefahren (CI fährt
  `--collect:"XPlat Code Coverage"`); für den Bestandsnachweis nicht erforderlich.
- Playwright-Traces des Laufs liegen unter `Rezepte.Tests.Browser/playwright-traces/`
  (laufzeitgeneriert, nicht in `test-results/` gespiegelt).

## Testklassen

### `RecipeSearchBrowserTests` — `Rezepte.Tests.Browser/RecipeSearchBrowserTests.cs`

Collection `BrowserTestCollection`; Konstanten `PageContentTimeoutMilliseconds = 20000`,
`InteractionRetryTimeoutMilliseconds = 15000`. Alle Tests `[SkippableFact]` und starten mit
`LoadingBarBrowserSession.StartLoggedInSessionAsync` + `RecipeSeedHelper.SeedRecipesAsync`
(GUID-eindeutige Titel).

- `NavbarSearch_ShowsSeededRecipe` (Z. 36) — Navbar-Suche (`#nav-search`) → `WaitForURLAsync`
  `**/recipes/search*` → `.list-group-item:has-text(title)` sichtbar.
- `DirectUrl_WithQuery_ShowsResults` (Z. 58) — `GotoAsync("/recipes/search?q=…")`;
  `Expect(item).ToHaveCountAsync(1)` mit Kommentar zum Prerender/Circuit-`isLoading`-Fenster.
- `Search_WithoutMatches_ShowsNoSearchResults` (Z. 80) — `.alert-secondary` „Keine Treffer.",
  `.alert-danger` muss fehlen.
- `Pagination_NextPage_ShowsPageTwo` (Z. 98) — 12 Rezepte, `pageSize=5`; **enthält bereits das
  etablierte Retry-Muster** (Z. 118–142): Deadline-Schleife um `nextButton.ClickAsync()` mit
  Vorab-Zustandsprüfung (`page=2` in URL oder „Seite 2 von 3" sichtbar), da Klicks vor dem
  Circuit-Start verschluckt werden können.
- `InPageSearch_Submit_UpdatesResultList` (Z. 154) — **der betroffene Test**: `GotoAsync` mit
  anderem `q`, wartet auf `.alert-secondary`, `FillAsync("#search-q", title)` (Z. 165),
  `Expect(...).ToHaveValueAsync(title)` (Z. 166 — Fehlschlagstelle), `ClickAsync` auf
  `form:has(#search-q) button[type='submit']`, `WaitForURLAsync` auf neue `q`-URL,
  Treffer-Warte. **Kein Circuit-Stabilitäts-Warten oder Retry um den Fill.**

### Weitere Browser-Testklassen (Kontext, nicht betroffen)

- `SecurityTxtBrowserTests` (4 Tests), `PluginSettingsBrowserTests` (5),
  `LoadingBarVisibilityBrowserTests` (3), `LoadingBarColorBrowserTests` (2),
  `LoadingBarDisabledBrowserTests` (1), `LoadingBarSafetyTimeoutBrowserTests` (1),
  `LoadingBarFormNavigationBrowserTests` (2) — alle Collection `BrowserTestCollection`.
  `LoadingBarFormNavigationBrowserTests.SearchSubmit_ShowsLoadingBar` dokumentiert die beiden
  Submit-Mechanismen (Enhanced Navigation vs. nativer GET-Fallback) in Z. 40–47.
- `Auth/RegisterTests` (2 Tests, Collection `RegisterTestCollection`) — enthält den im
  Basislauf fehlgeschlagenen `Register_WithDemoData_CreatesDemoData`.

### Relevante Unit-/Komponententests in `Rezepte.Tests`

- `RecipeSearchComponentTests` (`Rezepte.Tests/Components/RecipeSearchComponentTests.cs`,
  6 bUnit-Tests): `RunSearchAsync_WhenCancelled/WheRequestFails/WhenUnauthenticated_ShowsSearchFailed`,
  `RunSearchAsync_WhenEmpty_ShowsNoSearchResults`, `RunSearchAsync_WhenResults_RendersListItems`
  (verifiziert `SearchAsync`-Parameter inkl. `page=1`, `pageSize=20`, `sort="newest"`),
  `Dispose_WhenSearchInFlight_LogsCancellationAsSuperseded`. Nutzt Moq-`IRecipeService` und
  `PathBaseNavigationManager`. Testet **nicht** das `value`-Attribut von `#search-q` oder den
  Prerender/Circuit-Übergang.
- `RecipeServiceSearchSqliteTests` (`Rezepte.Tests/Services/`): 7 SQLite-Servicetests für
  `SearchAsync` (Funde, Case-Insensitivity, User-Filter, Paging/Clamp).
- `RecipesControllerSearchTests` (`Rezepte.Tests/Controllers/`): 3 Tests für
  `GET api/recipes/search` (Clamp, Unauthorized, DTO-Mapping).
- `MainLayoutSearchTests` (`Rezepte.Tests/Components/`): Navbar-Suchformular.

## Hilfsmethoden

### `RecipeSeedHelper` — `Rezepte.Tests.Browser/Infrastructure/RecipeSeedHelper.cs`

- `SeedRecipesAsync(IPage page, string baseAddress, params string[] titles)` — holt
  `GET /api/session/token` über den Cookie-Kontext der eingeloggten Page und legt pro Titel
  per `POST /api/recipes` (Bearer-JWT) ein Rezept an; wirft `InvalidOperationException` bei
  Fehlschlägen.

### `LoadingBarPageObject` — `Rezepte.Tests.Browser/Infrastructure/LoadingBarPageObject.cs`

- `CreateAsync` / `LoginAsync` / `GotoAsync` — Kontext-, Login- und Navigations-Setup.
- `SubmitNavigationSearchAsync` — füllt `#nav-search` und submittet das Navbar-Formular.
- `WaitUntilLoadingBar{Active,Visible,ColorChanged,Hidden}Async` — JS-basierte Zustands-Wartehelfer.
- `DelayRouteAsync` / `BlockRouteAsync` — Route-Timing-Kontrolle.
- `SubmitInteractiveShoppingListItemAsync` — enthält das zweite Deadline-Retry-Muster der Suite.
- Kein vorhandener Helfer für „interaktiver Circuit aktiv / Seite stabil".

### `LoadingBarBrowserSession` — `Rezepte.Tests.Browser/Infrastructure/LoadingBarBrowserSession.cs`

- `StartLoggedInSessionAsync` — Skip-Prüfung + `CreateAsync` + `LoginAsync` in einem Aufruf;
  Einstiegspunkt aller `BrowserTestCollection`-Tests.

### `RegisterPage` / `LoginPage` — `Rezepte.Tests.Browser/Auth/`

- `RegisterPage`: `GotoAsync`, `FillUsernameAsync`, `FillEmailAsync`, `FillPasswordAsync`,
  `CheckCreateDemoDataAsync`, `SubmitAsync` — Page Object des Registrierungsflows.
- `LoginPage.LoginAsync` — Login ohne PageObject-Erzeugung.

### `DemoDataWaitHelper` (privat in `RegisterTests.cs`, Z. 255ff)

- `WaitForDemoDataAsync(page, expected, timeoutMilliseconds, databasePath)` — wartet auf den
  Demo-Daten-Seeding-Zustand; Fehlschlagstelle des dokumentierten Basislauf-Fehlers.
