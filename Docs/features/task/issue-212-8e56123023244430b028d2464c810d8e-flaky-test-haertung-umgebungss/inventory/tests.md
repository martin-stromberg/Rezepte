# Test-Ausgangszustand und bestehende Testklassen

Dieses Dokument hält den projektweiten Test-Ausgangszustand vor der Umsetzung der
Anforderung fest (Nachweisregel: nur hier dokumentierte Fehlschläge gelten später als
„preexisting") und listet die bestehenden Testklassen und Hilfsmethoden des betroffenen
Bereichs.

## Test-Ausgangszustand vor der Umsetzung

- **Zeitpunkt (mit Zeitzone):** 2026-11-22, ca. 17:21–17:26 UTC (lokale Zeit UTC+1)
- **Branch und Commit-ID:** `task/issue-212-8e56123023244430b028d2464c810d8e-flaky-test-haertung-umgebungss`, Commit `7c959cc2b523dc610abae0cc3706447d770ac184` („Backmerge from main to staging (#220)")
- **Uncommittete Änderungen im getesteten Stand:** Keine geänderten/getrackten Dateien; einziges Untracked-Verzeichnis `Docs/features/task/issue-212-…/` (Anforderungs-/Inventardokumente, kein Code).
- **Testumgebung und Runtime-/SDK-Versionen:** Lokale Windows-Entwicklermaschine (Devin-Umgebung); `dotnet --version` = **11.0.100** (weiteres SDK 10.0.404 installiert, kein `global.json` vorhanden → SDK 11 aktiv, TFM `net10.0` aus `Directory.Build.props`); `pwsh` 7.6.10; Node v24.15.0; Playwright-Browser-Cache vorhanden (`%LOCALAPPDATA%\ms-playwright`, u. a. chromium-1255); `git status --porcelain` zeigte vor dem Lauf keine Codeänderungen.
- **Ermittelte Testsuiten und Quellen der Testbefehle:**
  - `Rezepte.Tests` — deterministische Suite (xUnit; 587 Testmethoden = 550 `[Fact]` + 37 `[Theory]`, expandiert 764 Testfälle). Befehl laut `Docs/testing.md` (`dotnet test`) und CI (`dotnet test Rezepte.sln --configuration Release --no-build`). Lokal wurde das Projekt einzeln getestet, weil die AI-Plugin-Projekte eine Assembly-Referenz (kein `ProjectReference`) auf die vorgebaute `Rezepte.Web.dll` besitzen und die Solution-Projektmappe sonst eine Build-Reihenfolge erzwingt, die CI über den vorgeschalteten `dotnet build Rezepte.Web`-Schritt herstellt.
  - `Rezepte.Tests.Browser` — umgebungssensitive Suite (xUnit + `Xunit.SkippableFact`; 34 `[SkippableFact]`-Methoden). Befehl laut `Docs/testing.md` Abschnitt „Browser-Tests": Build des Projekts (publisht `Rezepte.Web` automatisch via `PublishRezepteWebForBrowserTests`-Target), `playwright.ps1 install --with-deps chromium`, dann `dotnet test … --no-build`.
  - Weitere Solution-Projekte ohne eigene Tests: `Rezepte.Tests.PluginFixture` (Plugin-DLL-Fixture), `Rezepte.Updater.TestHost` (EXE-Testhost für `msTools.Updater`, kein Testprojekt).

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| 1 — `Rezepte.Tests` Build-Versuch | `dotnet test Rezepte.Tests/Rezepte.Tests.csproj --configuration Release --logger "trx;LogFileName=rezepte-tests-baseline.trx" --logger "console;verbosity=normal"` | Repo-Root | 1 | – (kein Testlauf) | – | – | [Console-Log](test-results/rezepte-tests-console.log) |
| 2 — `Rezepte.Tests` Ausgangslauf | derselbe Befehl wie Lauf 1 (Wiederholung, `Rezepte.Web.dll` aus Lauf 1 lag inzwischen vor) | Repo-Root | 0 | 764 | 0 | 0 | [TRX](test-results/rezepte-tests-baseline.trx), [Console-Log](test-results/rezepte-tests-console-run2.log) |
| 3 — `Rezepte.Tests.Browser` Build | `dotnet build Rezepte.Tests.Browser/Rezepte.Tests.Browser.csproj --configuration Release` | Repo-Root | 0 | – | – | – | [Build-Log](test-results/browser-build.log) |
| 4 — `Rezepte.Tests.Browser` Ausgangslauf | `dotnet test Rezepte.Tests.Browser/Rezepte.Tests.Browser.csproj --configuration Release --no-build --logger "trx;LogFileName=rezepte-tests-browser-baseline.trx" --logger "console;verbosity=normal"` | Repo-Root | 1 | 33 | 1 | 0 | [TRX](test-results/rezepte-tests-browser-baseline.trx), [Console-Log](test-results/rezepte-tests-browser-console.log) |
| 5 — Einzel-Wiederholung des Fehlschlags | `dotnet test Rezepte.Tests.Browser/Rezepte.Tests.Browser.csproj --configuration Release --no-build --filter "FullyQualifiedName~RecipeSearchBrowserTests.DirectUrl_WithQuery_ShowsResults" --logger "trx;LogFileName=rezepte-tests-browser-rerun.trx"` | Repo-Root | 0 | 1 | 0 | 0 | [TRX](test-results/rezepte-tests-browser-rerun.trx), [Console-Log](test-results/rezepte-tests-browser-rerun-console.log) |

Lauf 1 dokumentiert einen **Build-/Reihenfolgefehler, keinen Testfehler**: Die Projekte
`Rezepte.Import.Plugins.AIUrl` und `Rezepte.Import.Plugins.AIFoto` referenzieren
`Rezepte.Web` über `<Reference Include="Rezepte.Web">` + `<HintPath>` auf die vorgebaute
`bin/<Configuration>/net10.0/Rezepte.Web.dll` (alternativ `$(RezepteWebReferencePath)`),
nicht über `ProjectReference`. Auf dem unvorbereiteten Arbeitsstand baute MSBuild die
Plugins vor `Rezepte.Web` → `CS0234`/`CS0246`-Fehler, `dotnet test` endete mit Exit 1 ohne
Testausführung. Die CI umgeht das durch den expliziten Schritt `dotnet build
Rezepte.Web …` **vor** den Testprojekt-Builds (vgl. `pr-staging-ci.yml` Zeilen 121–128,
`staging-ci.yml` Zeilen 110–117). Lauf 2 lief auf dem nun vorgebauten Stand.

### Nachgewiesene bestehende Testfehler

| Test-ID inkl. Testfall | Suite / Dateipfad | Fehlerbild / Fehlermeldung | Lauf und Nachweis |
|-----------------------|------------------|---------------------------|-------------------|
| `Rezepte.Tests.Browser.RecipeSearchBrowserTests.DirectUrl_WithQuery_ShowsResults` | `Rezepte.Tests.Browser/RecipeSearchBrowserTests.cs` (Methode an Zeile ~60; Fehlerstelle `LoadingBarPageObject.GotoAsync`, `Rezepte.Tests.Browser/Infrastructure/LoadingBarPageObject.cs` Zeile 105) | `System.TimeoutException : Timeout 30000ms exceeded.` — Playwright-`GotoAsync("…/login")` erreichte den `load`-Zustand nicht innerhalb des 30-s-Defaults; Stacktrace über `LoadingBarPageObject.LoginAsync` (Zeile 89) → `LoadingBarBrowserSession.StartLoggedInSessionAsync` (Zeile 21). Der Kindprozess lief (`…:65186`); die Navigation lief dennoch in den Timeout — klassisches Flake-Bild. | Lauf 4: [TRX](test-results/rezepte-tests-browser-baseline.trx), [Console-Log](test-results/rezepte-tests-browser-console.log) |

Der Fehlschlag ist **flaky, nicht deterministisch**: Dieselbe Testmethode bestand in
Lauf 5 (isolierte Wiederholung) in 3 s. Für die Nachweisregel gilt Lauf 4 als Ausgangslauf;
der grüne Lauf 5 verdeckt ihn nicht.

### Testlücken und Ausführungsprobleme

- **Lauf 1 (Build-Abbruch):** Kein Test von `Rezepte.Tests` ausgeführt; Ursache oben
  dokumentiert (Assembly-Referenz `Rezepte.Web` statt `ProjectReference` in den
  AI-Plugin-Projekten; betrifft `Rezepte.Import.Plugins.AIUrl.csproj` Zeilen 17–28 und
  `Rezepte.Import.Plugins.AIFoto.csproj` analoge Blöcke). Infrastrukturfehler, kein
  Testfehler.
- **Solution-weiter Lauf nicht ausgeführt:** `dotnet test Rezepte.sln` wurde lokal nicht
  ausgeführt, weil die Browser-Suite denselben Publish-/Playwright-Bestand nutzt und die
  projektweisen Läufe die CI-Teilmenge bereits vollständig abdecken (`Rezepte.sln` enthält
  außer den beiden Testprojekten nur Produktiv-/Fixture-Projekte ohne Tests).
- **Lokal nicht reproduzierbarer Coverage-Schritt:** Die Coverage-Erhebung
  (`--collect:"XPlat Code Coverage"`) wurde im Baseline-Lauf nicht mitgefahren; die
  Coverage-Mechanik ist in [workflows.md](workflows.md) dokumentiert. Kein Aussage über
  die 70-%-Quote auf dieser Maschine.
- **Bedingte No-op-Tests in `Rezepte.Tests`:** `PluginManagerTests.InitializeAsync_ShouldLoadPublishedExternalChefkochPluginWithoutAdjacentContractAssembly` (Zeile ~173) und `…_ShouldLoadAllPublishedExternalOnlinePluginsWithoutAdjacentContractAssembly` (Zeile ~207) verlassen die Methode per `return`, wenn `PluginWorkspace.ExternalPluginRepositoryExists()` `false` liefert — also kein externer Plugin-Checkout unter `external/rezepte-import-plugins-private` existiert und `REZEPTE_EXTERNAL_PLUGINS_PATH` nicht gesetzt ist. Sie gelten im Lauf als „Bestanden", prüfen dann aber nichts (stiller Skip, im TRX nicht als Skipped markiert).
- **Übersprungene Tests:** In beiden Ausgangsläufen 0; die Browser-Tests hatten alle
  Voraussetzungen (Publish-Output + Chromium) erfüllt.

## Testklassen

### `Rezepte.Tests` (deterministische Lane — 587 Testmethoden)

Die vollständige Liste aller Testklassen mit Methoden und Einstufung steht in
[testliste.md](testliste.md). Zusammenfassung der relevanten Infrastruktur- und
Audit-klassen:

- `ContractExport.ContractExportScriptTests` (`ContractExport/ContractExportScriptTests.cs`, 11 Tests) — führt `scripts/Export-ImportContract.ps1` per `pwsh`/`powershell`-Kindprozess aus (`PowerShellPath`, Zeile 485: PATH-/PATHEXT-Suche), baut exportierte Workspaces mit `dotnet build` (Zeilen 103–111, inkl. NuGet-Restore) → **umgebungssensitiv**.
- `Deployment.DeploymentDocumentationTests` (`Deployment/DeploymentDocumentationTests.cs`, 3 Tests) — `FrameworkDependentLinuxPublish_…` führt `dotnet publish … -r linux-x64` als Kindprozess aus (Zeilen 66–128) → **umgebungssensitiv**; die beiden anderen Tests lesen nur Repo-Dateien.
- `PluginManagerTests` (`Services/Import/PluginManagerTests.cs`, 22 Tests) — Temp-Arbeitsverzeichnisse, `Directory.Delete` ohne Retry (Zeilen 91, 682, 699), `dotnet publish`-Kindprozess (`PluginWorkspace.PublishExternalPlugin`, Zeilen 732–769) nur hinter `ExternalPluginRepositoryExists()`-Guard, `REZEPTE_EXTERNAL_PLUGINS_PATH`-Auswertung (Zeile 788).
- `GoogleCredentialsProviderTests`, `SettingsCredentialAvailabilityTests`, `AIImportHandlerActivationTests`, `GeminiClientTests` — mutieren `GOOGLE_APPLICATION_CREDENTIALS`/`GOOGLE_GEMINI_API_KEY` via `EnvironmentVariableScope`, serialisiert über `GoogleCredentialsEnvironmentCollection` (`TestHelpers/GoogleCredentialsEnvironmentCollection.cs`).
- `ShareEndpointAuthenticationTests`, `SettingsControllerShareLinkValidityTests`, `SecurityTxtControllerTests_Authentication` — `SqliteFileWebApplicationFactory` (in-Prozess `WebApplicationFactory<Program>` gegen SQLite-**Tempdatei**).
- `RecipeSearchComponentTests` (Zeilen 263, 278), `ImportOrchestratorTests` (Zeilen 243, 491), `PluginUpdateServiceTests_Serialization` (Zeilen 36, 77), `ApplicationUpdatePreInstallHandlerTests` (Zeile 108), `ExportCleanupHostedServiceTests` (Zeile 79) — `Task.Delay`-Warten; Details in [testliste.md](testliste.md).

### `Rezepte.Tests.Browser` (umgebungssensitive Lane — 34 Testmethoden)

| Testklasse | Datei | Tests | Inhalt |
|------------|-------|-------|--------|
| `LoadingBarColorBrowserTests` | `LoadingBarColorBrowserTests.cs` | 2 | Farbkonfiguration der Ladebalken-Animation |
| `LoadingBarDisabledBrowserTests` | `LoadingBarDisabledBrowserTests.cs` | 1 | Deaktiviertes Feature rendert kein Host-Element |
| `LoadingBarFormNavigationBrowserTests` | `LoadingBarFormNavigationBrowserTests.cs` | 2 | Ladebalken bei Formular-Submit; enthält `WaitForTimeoutAsync(500)` (Zeile 70, kommentiert als „deterministic wait", da kein positives Signal pollbar) |
| `LoadingBarSafetyTimeoutBrowserTests` | `LoadingBarSafetyTimeoutBrowserTests.cs` | 1 | Max-Visible-Duration-Sicherheits-Timeout |
| `LoadingBarVisibilityBrowserTests` | `LoadingBarVisibilityBrowserTests.cs` | 3 | Aktivierung/Sichtbarkeit/Verstecken der Ladeleiste |
| `PluginSettingsBrowserTests` | `PluginSettingsBrowserTests.cs` | 5 | Plugin-Quellen-UI gegen `GitHubStubServer` |
| `RecipeSearchBrowserTests` | `RecipeSearchBrowserTests.cs` | 7 | Navbar-/In-Page-Suche, Pagination; Retry-Schleifen mit Deadline-Budget (`InteractionRetryTimeoutMilliseconds = 15000`, `PageContentTimeoutMilliseconds = 20000`) |
| `SecurityTxtBrowserTests` | `SecurityTxtBrowserTests.cs` | 4 | security.txt Endpunkte + Admin-UI |
| `ShareLinkBrowserTests` | `ShareLinkBrowserTests.cs` | 7 | Share-Links, anonyme Sicht, Side-Dishes |
| `RegisterTests` | `Auth/RegisterTests.cs` | 2 | Registrierung mit/ohne Demo-Daten; `DemoDataWaitHelper.WaitForDemoDataAsync` pollt UI-Counts + Seed-Job-Status aus SQLite (Timeouts bis 120 s) |

## Hilfsmethoden und Testinfrastruktur

### `Rezepte.Tests/TestHelpers` und `Rezepte.Tests/TestSupport`

- `RepositoryPaths` (`TestHelpers/RepositoryPaths.cs`) — `FindRepositoryRoot()` (sucht `Rezepte.sln` aufwärts von `AppContext.BaseDirectory`), `ReadRepositoryFile(...)`. **Wird per `<Compile Include>` auch in `Rezepte.Tests.Browser` geteilt** (`Rezepte.Tests.Browser.csproj` Zeile 29).
- `EnvironmentVariableScope` (`TestHelpers/EnvironmentVariableScope.cs`) — sichert/restauriert `GOOGLE_APPLICATION_CREDENTIALS` + `GOOGLE_GEMINI_API_KEY`.
- `GoogleCredentialsEnvironmentCollection` (`TestHelpers/GoogleCredentialsEnvironmentCollection.cs`) — xUnit-`CollectionDefinition` „GoogleCredentialsEnvironment" zur Serialisierung der Env-Mutation.
- `LoadingBarServiceTestFactory`, `TestGeminiClient` — Service-/Client-Fakes.
- `FixedTimeProvider` (`TestSupport/FixedTimeProvider.cs`) — steuerbarer `TimeProvider`.
- `SqliteFileWebApplicationFactory` (`TestSupport/SqliteFileWebApplicationFactory.cs`) — `WebApplicationFactory<Program>` mit Production-Environment, Test-JWT-Key, SQLite-Datei-DB; `Dispose` löscht die DB-Datei nach `SqliteConnection.ClearAllPools()` (`File.Delete`, kein Retry).

### `Rezepte.Tests.Browser/Infrastructure` und `Auth`

- `RezepteAppFixture` (`Infrastructure/RezepteAppFixture.cs`) — startet `dotnet Rezepte.Web.dll` als Kindprozess auf freiem Loopback-Port gegen `Directory.CreateTempSubdirectory("rezepte-browser-tests-")`-SQLite-DB; `REZEPTE_PUBLISH_DIR`-Override (Zeile 25/279–294); Audit-Funde siehe [testinfrastruktur.md](testinfrastruktur.md).
- `PlaywrightBrowserFixture` — headless Chromium; `BrowsersAvailable`/`UnavailableReason` treiben `Skip.IfNot`.
- `ConfiguredRezepteAppFixture` — Basisklasse für Env-Override-Fixtures; Ableitungen: `PluginSettingsAppFixture` (GitHub-API-Stub + DB-Seeding via `Microsoft.Data.Sqlite`), `RegisterTestsAppFixture` (`SeedSharedTestUser = false`), `DisabledFixture`/`ShortMaxVisibleDurationFixture` (in den Testdateien definiert).
- `GitHubStubServer` (`Infrastructure/GitHubStubServer.cs`) — `TcpListener`-HTTP-Stub für `releases/latest`; Accept-Loop mit `_acceptLoop.Wait(5s)` im `Dispose` (Zeilen 96–110).
- `BrowserTestCollection`, `PluginSettingsTestCollection`, `RegisterTestCollection` — xUnit-Collections (serielle Ausführung je Collection, geteilte Fixtures).
- `LoadingBarBrowserSession` — `Skip.IfNot`-Vorprüfung + eingeloggte `LoadingBarPageObject`-Session.
- `BrowserPageObjectBase` — Basisklasse: Tracing, Login, Settings-Navigation, `RegisterUserAsync`; Trace-ZIP nach `playwright-traces/` pro Page-Object.
- `LoadingBarPageObject`, `PluginSettingsPageObject`, `SecurityTxtPageObject`, `ShareLinkPageObject`, `LoginPage`, `RegisterPage` — Page Objects mit expliziten Timeouts (1–30 s; Details in [testinfrastruktur.md](testinfrastruktur.md)).
- `RecipeSeedHelper` — seedet Rezepte über die echte App-API (`api/session/token` + `api/recipes`).
- `DemoDataWaitHelper` (innen in `Auth/RegisterTests.cs`) — pollt UI-Counts und liest den `BackgroundJobs`-Status direkt aus der App-SQLite-DB.
