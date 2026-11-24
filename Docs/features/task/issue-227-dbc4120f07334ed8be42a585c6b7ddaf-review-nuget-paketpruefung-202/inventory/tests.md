# Tests und Test-Ausgangszustand

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-11-24, ca. 02:41–03:10 UTC+01:00 (MEZ)
- Branch und Commit-ID: `task/issue-227-dbc4120f07334ed8be42a585c6b7ddaf-review-nuget-paketpruefung-202`, `6f7982882b5ef3515491356c135394fa275718eb`
- Uncommittete Änderungen im getesteten Stand: nur die untracked Anforderungs-/Inventar-Dateien unter `docs/features/task/issue-227-…/` (diese Dokumentation selbst); kein Produktivcode, keine `*.csproj`, keine Tests verändert
- Testumgebung und Runtime-/SDK-Versionen: Windows (Git Bash), `dotnet --version` `11.0.100` (installierte SDKs: `10.0.404`, `11.0.100`; kein `global.json` → lokal SDK 11, CI pinnt `10.0.x`), `TargetFramework` `net10.0`; Playwright-Browser-Cache vorhanden (`%USERPROFILE%\AppData\Local\ms-playwright`, u. a. `chromium-1255`), Browser-Tests dadurch lokal ausführbar
- Ermittelte Testsuiten und Quellen der Testbefehle: `docs/testing.md`, `.github/workflows/pr-staging-ci.yml` + `staging-ci.yml` (Lanes `Category!=OsInterface` blockierend, `Category=OsInterface` + Browser best-effort), `docs/help/os-interface-tests.md`. Suites: `Rezepte.Tests` (xUnit, 774 Tests), `Rezepte.Tests.Browser` (xUnit + Playwright, 35 Tests)

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| 1 | `dotnet test Rezepte.Tests/Rezepte.Tests.csproj --logger "console;verbosity=normal"` | Repo-Root | 1 (Build-Fehler, siehe unten) | — (kein Testlauf) | — | — | [run1-dotnet-test-Rezepte.Tests-build-order-fail.log](test-results/run1-dotnet-test-Rezepte.Tests-build-order-fail.log) |
| 2 | `dotnet test Rezepte.Tests/Rezepte.Tests.csproj --logger "console;verbosity=normal"` | Repo-Root | 0 | 774 | 0 | 0 | [run2-dotnet-test-Rezepte.Tests.log](test-results/run2-dotnet-test-Rezepte.Tests.log) |
| 3 | `dotnet test Rezepte.Tests.Browser/Rezepte.Tests.Browser.csproj --logger "console;verbosity=normal"` | Repo-Root | 0 | 35 | 0 | 0 | [dotnet-test-Rezepte.Tests.Browser.log](test-results/dotnet-test-Rezepte.Tests.Browser.log) |

Ergänzende Paket-Befundläufe (read-only, gehören fachlich zur Anforderung; Details in
[packages.md](packages.md)):

| Lauf | Befehl | Exit-Code | Ergebnis | Nachweis |
|------|--------|-----------|----------|----------|
| R | `dotnet restore Rezepte.sln` | 0 | Restore erfolgreich, alle Quellen erreichbar | [dotnet-restore-sln.log](test-results/dotnet-restore-sln.log) |
| V | `dotnet list Rezepte.sln package --vulnerable --include-transitive` | 0 | Kein Befund in allen 10 Projekten | [dotnet-list-package-vulnerable.log](test-results/dotnet-list-package-vulnerable.log) |
| D | `dotnet list Rezepte.sln package --deprecated` | 0 | `xunit` 2.9.3 `Legacy` in beiden Testprojekten | [dotnet-list-package-deprecated.log](test-results/dotnet-list-package-deprecated.log) |
| O | `dotnet list Rezepte.sln package --outdated` | 0 | Update-Liste je Projekt (in [packages.md](packages.md) ausgewertet) | [dotnet-list-package-outdated.log](test-results/dotnet-list-package-outdated.log) |

### Nachgewiesene bestehende Testfehler

Keine. In den erfolgreich ausgeführten Läufen 2 und 3 sind alle 809 Tests
(774 + 35) bestanden; es gibt keinen nachgewiesenen fachlichen Testfehlschlag.

### Testlücken und Ausführungsprobleme

- **Lauf 1 (Build-Fehler, kein Testlauf):** Der erste Aufruf von
  `dotnet test Rezepte.Tests/Rezepte.Tests.csproj` auf sauberem `bin/`-Stand scheiterte
  in der Build-Phase: Die Projekte `Rezepte.Import.Plugins.AIUrl` und
  `Rezepte.Import.Plugins.AIFoto` referenzieren `Rezepte.Web.dll` per
  `<Reference>`/`HintPath` (kein `ProjectReference`) und wurden vor `Rezepte.Web`
  kompiliert → `MSB3245` (Verweis nicht aufgelöst) plus `CS0234`/`CS0246`/`CS0535`
  (Namespace/Typ `Rezepte.Web.*` unbekannt). Es lief kein einziger Test. Das ist die
  in `pr-staging-ci.yml` kommentierte Build-Reihenfolgen-Falle (CI baut
  `Rezepte.Web` zuerst). Nach dem ersten (Teil-)Build lag `Rezepte.Web.dll` vor;
  Lauf 2 baute fehlerfrei und führte alle Tests aus. Für Reproduktion auf sauberem
  Checkout ist die CI-Reihenfolge zu nutzen: erst `dotnet build Rezepte.Web/Rezepte.Web.csproj`,
  dann `dotnet test`.
- **Ungefilterte lokale Läufe:** Beide Läufe wurden ohne `Category`-Filter
  ausgeführt und decken damit auch die `OsInterface`-Teilmenge ab (in der CI die
  best-effort-Lane); alle `OsInterface`-Tests bestanden lokal.
- **`Rezepte.Updater.TestHost` / `Rezepte.Tests.PluginFixture`:** Keine Testprojekte
  (TestHost ist eine Exe zum manuellen Updater-Test; PluginFixture ist eine
  Hilfsbibliothek ohne Test-Framework-Referenzen) — nicht ausführbar, kein Mangel.
- **Nicht ausgeführt:** `dotnet format --verify-no-changes` und das
  Coverage-Gate ≥ 70 % (CI-Gates, für den Test-Ausgangszustand nicht erforderlich).

## Testklassen

### `Rezepte.Tests` (108 `.cs`-Dateien ohne `bin`/`obj`, 774 Tests, davon 78 Testdateien `*Tests.cs`)

Bereiche und markante Klassen (Auswahl, alle Dateien unter `Rezepte.Tests/`):

- `Components/`: `LoadingBarRenderingTests`, `MainLayoutSearchTests`,
  `PluginSettingsComponentTests`, `RecipeSearchComponentTests`,
  `ShareLinkSettingsTests`, `ShareRecipeDialogTests`,
  `Shared/CollectionImportSelectionStateTests`, `SharedRecipePageTests` —
  bUnit-Komponententests.
- `ContractExport/`: `ContractExportScriptTests` — ApiCompat-Vertragsexport
  (`OsInterface`, startet Kindprozesse/`dotnet`).
- `Controllers/`: `AdminExportCleanupControllerTests`, `AuthControllerTests`,
  `RecipesControllerSearchTests`, `RecipesControllerShareTests`,
  `SecurityTxtControllerTests`, `SettingsControllerSecurityTxtValidationTests`,
  `SettingsControllerShareLinkValidityTests`, `SettingsCredentialAvailabilityTests`,
  `ShareEndpointAuthenticationTests`.
- `Deployment/`: `CsprojCredentialCopyTests`, `DeploymentDocumentationTests`
  (`OsInterface`), `LoadingBarWiringTests`.
- `Extensions/`: `FormFileExtensionsTests`.
- `Security/`: `JwtSigningKeyProviderTests`, `PasswordHasherTests`,
  `RemoteUrlGuardTests`.
- `Services/` (u. a.): `RecipeServiceTests`, `RecipeServiceSearchSqliteTests`,
  `UserServiceTests`, `SettingsServiceTests`, `CookbookServiceTests`,
  `CalendarServiceTests`, `TokenServiceTests`, `AiUsageServiceTests`,
  `ApplicationUpdateSettingsServiceTests`, `ApplicationUpdatePreInstallHandlerTests`,
  `UpdateBackupServiceTests`, `ExportService*Tests`,
  `ExportCleanupHostedServiceTests`, `ExportCleanupServiceTests`,
  `GoogleCredentialsProviderTests`, `RemoteContentFetcherTests` (`Http/`),
  `ShoppingList*Tests`, `SecurityTxtRendererTests`, `LoadingBar*Tests`,
  `UsernameValidatorTests` (`Validation/`).
- `Services/Import/`: `ImportOrchestratorTests`, `ImportParserBaseTests`,
  `ProductiveImportPluginParserTests`, `PluginManagerTests` (`OsInterface`),
  `PluginPackageInstallerTests`, `PluginPackageValidatorTests`,
  `PluginSettingsServiceTests`, `PluginSourceRelease*Tests`,
  `PluginUpdateServiceTests`, `AIFotoImportPluginTests`, `AIUrlImportPluginTests`,
  `AIImportHandlerActivationTests`, `GeminiClientTests`,
  `GeminiUsabilityChecksTests`, `GitHubReleaseClientTests`, `UrlHelpersTests`.
- `Services/BackgroundJobs/`: `BackgroundJobPersistenceTests`,
  `DemoDataSeedingJobHandlerTests`, `ExportJobPayloadTests`.
- `ViewModels/`: `UserAdminViewModelTests`, `UserProfileViewModelTests`.
- `TestHelpers/`: `TempDirectoryCleanupTests`, `FlakyConventionTests`
  (Konventionstest für `Flaky`-Markierung).

### `Rezepte.Tests.Browser` (30 `.cs`-Dateien, 35 Tests)

- `SecurityTxtBrowserTests`, `ShareLinkBrowserTests`, `RecipeSearchBrowserTests`,
  `PluginSettingsBrowserTests`, `RegisterTests` (`Auth/`),
  `LoadingBarVisibilityBrowserTests`, `LoadingBarColorBrowserTests`,
  `LoadingBarDisabledBrowserTests`, `LoadingBarFormNavigationBrowserTests`,
  `LoadingBarSafetyTimeoutBrowserTests` — E2E gegen real publishte `Rezepte.Web`-App
  (Kestrel-Kindprozess + Playwright-Chromium).

## Hilfsmethoden / Testinfrastruktur

### `Rezepte.Tests/TestHelpers`

- `TestCategories` — Konstanten `Category`, `OsInterface`, `Flaky` für die
  xUnit-Traits der CI-Lanes.
- `TempDirectoryCleanup` — Best-Effort-Aufräumen von Temp-Verzeichnissen inkl.
  Prozess-Kill bei Locks.
- `RepositoryPaths` — Auflösung von Repo-Pfaden für Tests.
- `EnvironmentVariableScope` — Scoped Setzen/Rücksetzen von Umgebungsvariablen.
- `GoogleCredentialsEnvironmentCollection` — xUnit-Collection zur Serialisierung
  von Tests, die Google-Credential-Umgebungsvariablen manipulieren.
- `LoadingBarServiceTestFactory`, `TestGeminiClient` — Factories/Fakes für
  Loading-Bar-Service bzw. Gemini-KI-Client.
- `FlakyConventionTests` — erzwingt, dass `Flaky` nur zusammen mit `OsInterface`
  markiert wird.

### `Rezepte.Tests/TestSupport`

- `FixedTimeProvider` — deterministische Zeit für Tests.
- `SqliteFileWebApplicationFactory` — `WebApplicationFactory`-Variante mit
  dateibasierter SQLite für Integrationstests.

### `Rezepte.Tests.Browser/Infrastructure`

- `RezepteAppFixture` — startet die publishte App als Kindprozess (Kestrel),
  schreibt `browser-app-logs/`.
- `PlaywrightBrowserFixture`, `BrowserTestCollection`,
  `PluginSettingsTestCollection`, `RegisterTestCollection` — Browser-Setup und
  xUnit-Collections.
- `PlaywrightTraces` — Trace-/Screenshot-Artefakte nach `playwright-traces/`.
- `GitHubStubServer` — TcpListener-Stub für GitHub-API-Aufrufe im Update-Flow.
- Page Objects: `BrowserPageObjectBase`, `LoginPage`, `RegisterPage`,
  `SecurityTxtPageObject`, `ShareLinkPageObject`, `PluginSettingsPageObject`,
  `LoadingBarPageObject`, `LoadingBarBrowserSession`, `RecipeSeedHelper`,
  `BrowserTimeouts`, `ConfiguredRezepteAppFixture`, `PluginSettingsAppFixture`,
  `RegisterTestsAppFixture`.
