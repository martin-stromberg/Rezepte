# Tests: Ausgangszustand und Bestand

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-11-24, ca. 12:27–12:40 UTC (13:27–13:40 UTC+0100, lokale Maschinenzeit)
- Branch und Commit-ID: `task/issue-233-a6a76b9511a848bcb7504e6cb9647419-net11`, `aa9263eb39459f6bd3c515847d7eb73c1fd9caba` („Backmerge from main to staging (#232)", committet 2026-11-24T04:12:05+01:00) — identisch mit `staging`-Head nach Merge von PR #230/#232
- Uncommittete Änderungen im getesteten Stand: nur das neue, ungetrackte Verzeichnis `Docs/features/task/issue-233-a6a76b9511a848bcb7504e6cb9647419-net11/` (requirement.md + Inventory-Artefakte); keine Änderungen an getrackten Dateien
- Testumgebung und Runtime-/SDK-Versionen:
  - Lokale Windows-Maschine, `win-x64`, OS 10.0.26300
  - Aktives .NET-SDK: `11.0.100` (kein `global.json` → höchstes installiertes SDK greift); zusätzlich installiert `10.0.404`
  - Installierte Runtimes: `Microsoft.NETCore.App`/`Microsoft.AspNetCore.App` je `8.0.34`, `9.0.23`, `10.0.15`, `11.0.0`
  - `pwsh` 7.6.10, Node `v24.15.0`
  - Playwright-Browser-Cache vorhanden (`%LOCALAPPDATA%\ms-playwright`, u. a. `chromium-1255`) — Browser-Tests ohne Neuinstallation lauffähig
- Ermittelte Testsuiten und Quellen der Testbefehle:
  - `Rezepte.Tests` (xUnit, 774 Tests gesamt) — zwei CI-Lanes: `--filter "Category!=OsInterface"` (blockierend, mit Coverage) und `--filter "Category=OsInterface"` (best-effort)
  - `Rezepte.Tests.Browser` (xUnit + Playwright, 35 Tests) — best-effort-Lane, benötigt Chromium + publizierte `Rezepte.Web`
  - Befehlsquellen: `Docs/testing.md`, `.github/workflows/pr-staging-ci.yml`, `.github/workflows/staging-ci.yml`
  - `Rezepte.Tests.PluginFixture` ist eine Hilfs-Assembly ohne eigene Testmethoden; `Rezepte.Updater.TestHost` ist eine Executable (kein Testprojekt)

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| 1 | `dotnet restore Rezepte.sln` | Repo-Root | 0 | — (10 Projekte wiederhergestellt) | — | — | [01-restore.log](test-results/01-restore.log) |
| 2 | `dotnet build Rezepte.sln --configuration Release --no-restore` | Repo-Root | 0 | — (0 Warnungen, 0 Fehler; alle 10 Projekte + `net10.0`-Publish) | — | — | [02-build-sln.log](test-results/02-build-sln.log) |
| 3 | `dotnet test Rezepte.Tests/Rezepte.Tests.csproj --configuration Release --no-build --filter "Category!=OsInterface"` | Repo-Root | 0 | 759 | 0 | 0 | [03-test-unit-nonOsInterface.log](test-results/03-test-unit-nonOsInterface.log), [baseline-unit-nonOsInterface.trx](test-results/trx/baseline-unit-nonOsInterface.trx) |
| 4 | `dotnet test Rezepte.Tests/Rezepte.Tests.csproj --configuration Release --no-build --filter "Category=OsInterface"` | Repo-Root | 0 | 15 | 0 | 0 | [04-test-unit-OsInterface.log](test-results/04-test-unit-OsInterface.log), [baseline-unit-OsInterface.trx](test-results/trx/baseline-unit-OsInterface.trx) |
| 5 | `dotnet test Rezepte.Tests.Browser/Rezepte.Tests.Browser.csproj --configuration Release --no-build` | Repo-Root | 0 | 35 | 0 | 0 | [05-test-browser.log](test-results/05-test-browser.log), [baseline-browser.trx](test-results/trx/baseline-browser.trx) |
| 6 | `dotnet list Rezepte.sln package --vulnerable --include-transitive` | Repo-Root | 0 | — (keine verwundbaren Pakete in allen 10 Projekten) | — | — | [06-vulnerable-packages.log](test-results/06-vulnerable-packages.log) |
| 7 | `dotnet test Rezepte.sln --configuration Release --no-build` | Repo-Root | 0 | 809 (774 `Rezepte.Tests` ungefiltert + 35 `Rezepte.Tests.Browser`) | 0 | 0 | [07-test-sln.log](test-results/07-test-sln.log) |

Anmerkungen zu den Läufen:

- Alle Läufe mit dem aktiven SDK `11.0.100` gegen TFM `net10.0` (Runtime `10.0.15` installiert). Der Build aus Lauf 2 erzeugte neben den `bin/Release/net10.0`-Artefakten auch `Rezepte.Web/bin/Release/net10.0/publish/` (Target `PublishRezepteWebForBrowserTests`), wovon Lauf 5 profitierte.
- `Rezepte.Tests` ungefiltert = 759 + 15 = 774 (Deckung mit Lane-Splitting geprüft).
- Lauf 7 dokumentiert, dass `dotnet test Rezepte.sln` auf dem Baseline-Stand funktioniert — relevant für Akzeptanzkriterium 1 (dort mit `net11.0` gefordert).

### Nachgewiesene bestehende Testfehler

Es wurden keine Testfehler nachgewiesen: alle 809 Tests bestanden, 0 fehlgeschlagen, 0 übersprungen.

### Testlücken und Ausführungsprobleme

- Keine fehlgeschlagenen, abgebrochenen oder übersprungenen Tests im Baseline-Lauf.
- Der CI-Lauf unterscheidet sich vom lokalen Lauf: CI läuft auf `ubuntu-latest` mit Coverage-Sammlung (`XPlat Code Coverage`) und 70-%-Coverage-Gate — lokal ohne Coverage erhoben. Playwright wurde aus dem vorhandenen lokalen Browser-Cache bedient; CI installiert Chromium über `playwright.ps1 install --with-deps chromium`.
- `dotnet format --verify-no-changes` (CI-Gate 1) und `dotnet build -p:TreatWarningsAsErrors=true` (CI-Gate 3) wurden lokal nicht separat ausgeführt — der Build in Lauf 2 lief bereits ohne Warnungen.

## Testklassen

Bestand nach Verzeichnis (Dateien `*Tests.cs`/`*Test.cs`, Stand Baseline-Commit):

### `Rezepte.Tests` (insgesamt 78 Testklassen-Dateien)

- `Services/` — 49 Testklassen (zentrale Service-Tests: Benutzer, Kochbücher, Rezepte, Einstellungen, KI-Nutzung, Import-Plugins u. a.)
- `Controllers/` — 9 Testklassen
- `Components/` — 8 Testklassen (bUnit-Komponententests)
- `Security/` — 3 Testklassen
- `Deployment/` — 3 Testklassen, darunter `DeploymentDocumentationTests.cs` (assertiert u. a. die Zeichenfolge `-f net10.0` in `Docs/install.md`, Zeile 68)
- `ContractExport/` — 1 Testklasse `ContractExportScriptTests.cs` (assertiert das Aufräumverhalten von `Export-ImportContract.ps1`; legt Fake-Pfad `bin/Release/net10.0` an, Zeilen 269/271)
- `ViewModels/` — 2 Testklassen
- `Extensions/` — 1 Testklasse
- `TestHelpers/` — 2 Testklassen (`TempDirectoryCleanupTests`, `FlakyConventionTests` — Konventionstest für Flaky-Markierung)

### `Rezepte.Tests.Browser` (10 Testklassen-Dateien)

- `LoadingBarColorBrowserTests.cs`, `LoadingBarDisabledBrowserTests.cs`, `LoadingBarFormNavigationBrowserTests.cs`, `LoadingBarSafetyTimeoutBrowserTests.cs`, `LoadingBarVisibilityBrowserTests.cs`, `PluginSettingsBrowserTests.cs`, `RecipeSearchBrowserTests.cs`, `SecurityTxtBrowserTests.cs`, `ShareLinkBrowserTests.cs`, `Auth/RegisterTests.cs`

### `Rezepte.Tests.PluginFixture`

- Hilfs-Assembly für Plugin-Tests, keine eigenen Testmethoden (nur Projekt-/Fixtur-Code, referenziert `Rezepte.Import.Abstractions`).

### `Rezepte.Updater.TestHost`

- Executable (`OutputType=Exe`), kein Testprojekt; referenziert `msTools.Updater` und `Microsoft.Extensions.Hosting`.

## Hilfsmethoden

### `Rezepte.Tests/TestHelpers`

- `TestCategories.cs` — Konstante `Category` (Trait-Name für `Category`-Filter: `OsInterface` u. a.)
- `RepositoryPaths.cs` — Repo-Pfadauflösung (auch per Link in `Rezepte.Tests.Browser` eingebunden)
- `TempDirectoryCleanup.cs` — Aufräumen temporärer Verzeichnisse (eingebunden in Browser-Tests)
- `FlakyConventionTests.cs` — Konventionstest für Flaky-Kennzeichnung (eingebunden in Browser-Tests)
- `EnvironmentVariableScope.cs` — scoped Env-Variablen
- `GoogleCredentialsEnvironmentCollection.cs` — xUnit-Collection für Google-Credential-Env
- `LoadingBarServiceTestFactory.cs`, `TestGeminiClient.cs` — fachliche Testfabriken/-stubs

### `Rezepte.Tests/TestSupport`

- `SqliteFileWebApplicationFactory.cs` — `WebApplicationFactory`-Ableitung für SQLite-Datei-DBs (Integrationstests)
- `FixedTimeProvider.cs` — deterministischer `TimeProvider`

### `Rezepte.Tests.Browser/Infrastructure` (Playwright-App-Fixtures und Page Objects)

- `RezepteAppFixture.cs` / `ConfiguredRezepteAppFixture.cs` / `PluginSettingsAppFixture.cs` / `RegisterTestsAppFixture.cs` — starten die App als Kindprozess (Kestrel) aus dem Publish-Output (`Rezepte.Web/bin/Release/$(Configuration)/$(TargetFramework)/publish`, dokumentierter Standard-Auflösungspfad, `REZEPTE_PUBLISH_DIR`-Override möglich)
- `PlaywrightBrowserFixture.cs`, `BrowserTimeouts.cs`, `BrowserTestCollection.cs`, `PluginSettingsTestCollection.cs`, `RegisterTestCollection.cs` — Browser-/Collection-Fixtures
- Page Objects: `BrowserPageObjectBase.cs`, `LoadingBarPageObject.cs`, `LoadingBarBrowserSession.cs`, `PluginSettingsPageObject.cs`, `SecurityTxtPageObject.cs`, `ShareLinkPageObject.cs`, `Auth/LoginPage.cs`, `Auth/RegisterPage.cs`
- `GitHubStubServer.cs` — TcpListener-Stub für GitHub-API
- `RecipeSeedHelper.cs`, `PlaywrightTraces.cs` — Seed-Daten bzw. Trace-Artefakte (`playwright-traces/`)
