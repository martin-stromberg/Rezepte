# Testinfrastruktur — Bestand und Audit-Funde

Bestand der Infrastrukturklassen von `Rezepte.Tests.Browser` und der projektübergreifenden
Test-Helfer, inklusive der verifizierten Flake-Quellen. Zeilenangaben beziehen sich auf
Commit `7c959cc`.

## `RezepteAppFixture` — `Rezepte.Tests.Browser/Infrastructure/RezepteAppFixture.cs`

Startet `Rezepte.Web` als eigenen `dotnet`-Kindprozess auf einem freien Loopback-Port
gegen eine SQLite-Temp-Datenbank; `IAsyncLifetime`-Collection-Fixture.

| Element | Stand | Fundstelle |
|---------|-------|------------|
| `StartupTimeoutSeconds` | `60` s; Deadline-Loop in `WaitUntilReadyAsync` pollt per `HttpClient.GetAsync(BaseAddress)` mit `ReadinessPollIntervalMilliseconds = 250`; wirft `TimeoutException` ohne Angabe von Phase/Exitcode-Kontext (Kindprozess-Exit wird separat als `InvalidOperationException` mit Exit-Code gemeldet, Zeilen 229–233) | Zeilen 29, 221–249 |
| `GetFreeTcpPort()` | TOCTOU: `TcpListener(IPAddress.Loopback, 0)` wird gebunden, Port ausgelesen, Listener sofort wieder gestoppt — der Port ist bis zum `ASPNETCORE_URLS`-Bind des Kindprozesses wieder vergebbar | Zeilen 263–275 |
| `DeleteTemporaryDatabase()` | `Directory.Delete(_tempDirectory, recursive: true)` in `try/catch (IOException or UnauthorizedAccessException)` — best-effort **ohne Retry/Backoff**; aufgerufen aus `DisposeAsync` und `RestartWithEmptyDatabaseAsync` | Zeilen 156–172, 106–117, 123–128 |
| `CreateTemporaryDatabase()` | `Directory.CreateTempSubdirectory("rezepte-browser-tests-")` | Zeilen 174–179 |
| Kindprozess-Ausgabe | `RedirectStandardOutput/Error = true`, Handler werfen Daten weg (`(_, _) => { }`), `BeginOutputReadLine` — notwendig gegen Pipe-Blockierung (Kommentar Zeilen 212–214), aber **ohne jede Diagnose-Persistenz**; bei Timeout nur „did not become ready … within the startup window" | Zeilen 190–218, 247–248 |
| Prozess-Stopp | `Kill(entireProcessTree: true)` + `WaitForExit(ShutdownGraceMilliseconds = 5000)` in `try/finally` mit `Dispose` | Zeilen 139–154 |
| Env des Kindprozesses | `ASPNETCORE_URLS`, `ASPNETCORE_ENVIRONMENT=Production`, `ConnectionStrings__Default`, `Jwt__Key`, `RateLimiting__Authentication__PermitLimit=1000` + `GetEnvironmentOverrides()`; **keine** Coverage-/Diagnostics-Variablen (relevant für Coverage-Beitrag = 0) | Zeilen 196–207 |
| `REZEPTE_PUBLISH_DIR` | einzige Verwendung im gesamten Repo (Zeile 25); gesetzt → harter Fehler bei fehlender `Rezepte.Web.dll` (kein Skip); ungesetzt → Fallback auf `Rezepte.Web/bin/<Config>/<Tfm>/publish/Rezepte.Web.dll` relativ zum Repo-Root via `RepositoryPaths`; fehlt → `ApplicationAvailable = false` → `Skip.IfNot` | Zeilen 277–319 |
| `RegisterTestUserAsync` | legt `browsertest`/`BrowserTest!123` per `POST api/auth/register` an | Zeilen 251–261 |

Abgeleitete Fixtures: `ConfiguredRezepteAppFixture` (Env-Overrides),
`PluginSettingsAppFixture` (setzt `PluginUpdates__GitHubApiBaseUrl` auf `GitHubStubServer`,
seedet `PluginSourceReleases`/`PluginSettings` direkt per `Microsoft.Data.Sqlite` in die
App-DB, `DisposeAsync` stoppt App → dann Stub), `RegisterTestsAppFixture`
(`SeedSharedTestUser = false`) sowie in den Testdateien definierte `DisabledFixture` und
`ShortMaxVisibleDurationFixture` (Loading-Bar-Konfigurationen).

## `GitHubStubServer` — `Infrastructure/GitHubStubServer.cs`

`TcpListener`-HTTP-Stub (bewusst nicht `HttpListener` — Windows-URL-ACL, Kommentar
Zeilen 10–14). `Start()` bindet `TcpListener(IPAddress.Loopback, 0)` direkt und behält den
Listener — hier **kein** TOCTOU, der Port bleibt reserviert (Zeilen 48–53).

Audit-Fund: `Dispose()` → `_shutdown.Cancel()` + `_listener.Stop()` +
`_acceptLoop.Wait(TimeSpan.FromSeconds(5))` (Zeilen 96–110). Die 5-s-Warte blockiert den
Teardown; ein `AggregateException` wird geschluckt (Zeilen 104–107). Hängende Handler-Tasks
(`_ = HandleClientAsync(client)`, Zeile 134 — fire-and-forget, nicht in `_acceptLoop`
enthalten) werden **nicht** abgewartet.

## `PlaywrightBrowserFixture` — `Infrastructure/PlaywrightBrowserFixture.cs`

Headless-Chromium-Launch; `PlaywrightException` → `BrowsersAvailable = false` +
`UnavailableReason` → `Skip.IfNot` in `LoadingBarBrowserSession`/Testklassen. Keine
Retry-Logik, `Headless = true` hart kodiert.

## Collections und Skip-Konvention

- `BrowserTestCollection` („Rezepte Browser Tests"), `PluginSettingsTestCollection`
  („Rezepte Plugin Settings Browser Tests"), `RegisterTestCollection` („Rezepte Register
  Browser Tests") — je `ICollectionFixture<PlaywrightBrowserFixture>` +
  App-Fixture-Variante; Tests innerhalb einer Collection laufen seriell, Collections
  untereinander potenziell parallel (xUnit-Default), d. h. bis zu drei App-Kindprozesse +
  ein Chromium gleichzeitig.
- `Xunit.SkippableFact` 1.5.61 nur in `Rezepte.Tests.Browser.csproj` (Zeile 19);
  `Rezepte.Tests.csproj` referenziert es **nicht** (verifiziert — Voraussetzung, falls
  der Übergangsmechanismus Skips dort braucht).
- Alle 34 Browser-Tests sind `[SkippableFact]` und prüfen
  `Skip.IfNot(browserFixture.BrowsersAvailable, …)` +
  `Skip.IfNot(appFixture.ApplicationAvailable, …)`.

## Page Objects — Timeout-Inventar

| Klasse | Timeouts / Sleeps | Fundstelle |
|--------|--------------------|------------|
| `LoadingBarPageObject` | `LoginAsync` `WaitForURLAsync` 10 s (Z. 95); `GotoAsync` **ohne expliziten Timeout** → Playwright-Default 30 s (Z. 105 — Fehlerstelle des dokumentierten Baseline-Flakes); `WaitUntilLoadingBarActiveAsync` Default **1 s** (Z. 245), Aufrufer übergeben teils 5 s (`LoadingBarFormNavigationBrowserTests` Z. 48); `WaitUntilLoadingBarVisibleAsync`/`…ColorChangedAsync` Default 2 s (Z. 258/272); `WaitUntilLoadingBarHiddenAsync` Default 5 s (Z. 285); Shopping-List-Edit-Retry 5× mit 2-s-`WaitForAsync` (Z. 183–197); `DelayRouteAsync` legt `Task.Delay` in die Route (Z. 133–140) | — |
| `BrowserPageObjectBase` | `LoginAsync` `WaitForURLAsync` 10 s; `NavigateToSettingsAsync` `WaitForSelectorAsync` 10 s | Z. 79, 89 |
| `PluginSettingsPageObject` | `NavigateToPluginSettingsAsync` Retry-Loop gegen 30-s-Deadline mit 5-s-Selector-Waits (Z. 80–98); Zeilen-Waits 15 s (Z. 117, 130, 151, 215, 228) | — |
| `SecurityTxtPageObject` | `WaitForSelectorAsync` 10 s | Z. 52, 75, 91 |
| `ShareLinkPageObject` | `WaitForSelectorAsync` 10–20 s | Z. 159, 170–179, 260–274 |
| `LoginPage`/`RegisterPage` (`Auth/`) | `WaitForURLAsync` 10 s; `RegisterPage.GotoAsync` `WaitUntil=Load` 30 s + Selector 30 s | `LoginPage` Z. ~45; `RegisterPage` Z. 61–62 |
| `RegisterTests` | `DemoDataWaitHelper.WaitForDemoDataAsync` pollt UI-Counts mit 500-ms-Intervall bis 120 s (`DemoDataTimeoutMilliseconds`) und liest den `BackgroundJobs`-Status direkt aus der App-SQLite-DB (`Mode=ReadOnly`); `EventuallyReadCountAsync` 30-s-Budget; `CountStablyAsync` max. 8 Reads à 400 ms; Monatsnavigation mit 20-s-Deadline + 5-s-Settle | Z. 19–31, 148–294, 307–382 |

## Feste Sleeps / weitere Flake-Quellen (Browser-Projekt)

- `LoadingBarFormNavigationBrowserTests.cs` Zeile 70: `Page.WaitForTimeoutAsync(500)` —
  Kommentar begründet die feste Warte damit, dass es kein positives Signal zum Pollen
  gibt (Negativ-Assert „bar never activates").
- `RezepteAppFixture` Zeile 243: `Task.Delay(250)` im Readiness-Poll (Zustands-Polling,
  unkritisch).
- `RegisterTests`: zahlreiche `Task.Delay`-Polls innerhalb dokumentierter Deadline-Budgets.

## `Rezepte.Tests` — Hilfsklassen und Env-Abhängigkeiten

- `TestHelpers/`: `RepositoryPaths` (per `<Compile Include>` in `Rezepte.Tests.Browser`
  geteilt — einziges heute projektübergreifend geteiltes Test-Artefakt; **kein**
  gemeinsames NuGet-/Projekt für Test-Helfer, `TempDirectoryCleanup` müsste analog
  geteilt oder dupliziert werden), `EnvironmentVariableScope`,
  `GoogleCredentialsEnvironmentCollection`, `LoadingBarServiceTestFactory`,
  `TestGeminiClient`.
- `TestSupport/`: `FixedTimeProvider`, `SqliteFileWebApplicationFactory` (in-proc;
  DB-Datei-Löschung im `Dispose` ohne Retry).
- `Rezepte.Tests.PluginFixture` — eigenes Solution-Projekt mit `TestImportPlugin.cs`
  (Fixture-Plugin für `PluginManagerTests`), kein Testprojekt.
- `Rezepte.Updater.TestHost` — EXE-Host für `msTools.Updater`-Integrationstests, kein
  Testprojekt.

## Assembly-Referenz statt `ProjectReference` (Build-Reihenfolge-Falle)

`Rezepte.Import.Plugins.AIUrl.csproj` (Zeilen 17–28) und
`Rezepte.Import.Plugins.AIFoto.csproj` referenzieren `Rezepte.Web` über
`<Reference Include="Rezepte.Web"><HintPath>..\Rezepte.Web\bin\$(Configuration)\$(TargetFramework)\Rezepte.Web.dll</HintPath>`
(Override `$(RezepteWebReferencePath)`). Ohne vorgebaute `Rezepte.Web.dll` schlägt der
Build der Plugins — im Baseline-Lauf 1 als `CS0234`/`CS0246` dokumentiert. CI baut
`Rezepte.Web` daher vor den Testprojekten.
