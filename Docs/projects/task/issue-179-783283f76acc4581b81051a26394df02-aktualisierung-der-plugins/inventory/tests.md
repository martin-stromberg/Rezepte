# Tests — Bestandsaufnahme

## Test-Ausgangszustand vor der Umsetzung

- **Zeitpunkt (mit Zeitzone):** 2026-11-14, Lauf 1 gestartet ca. 18:26 +0100 (CET), Lauf 2 abgeschlossen ca. 18:33 +0100
- **Branch und Commit-ID:** `task/issue-179-783283f76acc4581b81051a26394df02-aktualisierung-der-plugins` @ `3e0d86497efe0f0b20cdb79c74b59d3b61d30ee2` („Backmerge from main to staging (#176)")
- **Uncommittete Änderungen im getesteten Stand:** nur untracked `Docs/projects/` (Projektartefakte dieses Laufs: `requirement.md`, `todo.md`, `inventory/`). Keine Änderungen an Produktivcode, Tests oder Konfiguration.
- **Testumgebung und Runtime-/SDK-Versionen:** Windows (Git Bash), .NET SDK `10.0.403` (TargetFramework `net10.0`, `Directory.Build.props`), Node `v24.15.0` (nur für `semantic-release` relevant, nicht für Tests), Playwright-Chromium installiert unter `%USERPROFILE%\AppData\Local\ms-playwright` (mehrere `chromium-*`-/`chromium_headless_shell-*`-Versionen). Browser-Tests hosten `Rezepte.Web` selbst (Publish über `PublishRezepteWebForBrowserTests`-Target im csproj) und nutzen `Xunit.SkippableFact` für fehlende Browser/App.
- **Ermittelte Testsuiten und Quellen der Testbefehle:**
  - `Rezepte.Tests` (xUnit, 609 Tests) und `Rezepte.Tests.Browser` (xUnit + Playwright, 20 Tests) — beide in `Rezepte.sln`.
  - `Docs/testing.md`: `dotnet test` (Repo-Root) als Standardbefehl; Browser-Tests dokumentiert mit Release-Build + `playwright.ps1 install --with-deps chromium` + `dotnet test Rezepte.Tests.Browser/... -c Release --no-build`.
  - CI: `.github/workflows/pr-staging-ci.yml` (Zeile 148: `dotnet test Rezepte.sln --configuration Release --no-build` nach Release-Builds und Playwright-Install), `.github/workflows/release.yml` (getrennte `dotnet test`-Aufrufe für beide Testprojekte, Release).
  - Für den Ausgangslauf wurde der dokumentierte Standardbefehl `dotnet test Rezepte.sln` in der Default-Konfiguration (Debug) gewählt; die Solution umfasst beide Suiten, sodass der Lauf projektweit ist. Keine weiteren Testprojekte in der Solution (`Rezepte.Updater.TestHost` enthält kein Testprojekt).

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| 1a (`Rezepte.Tests`) | `dotnet test Rezepte.sln` (Debug, impliziter Build) | Repo-Root | 1 (Gesamtlauf) | 609 | 0 | 0 | [dotnet-test-sln-debug.log](test-results/dotnet-test-sln-debug.log) |
| 1b (`Rezepte.Tests.Browser`) | `dotnet test Rezepte.sln` (Debug, impliziter Build) | Repo-Root | 1 (Gesamtlauf) | 19 | 1 | 0 | [dotnet-test-sln-debug.log](test-results/dotnet-test-sln-debug.log) |
| 2 (`Rezepte.Tests.Browser`, Wiederholung) | `dotnet test Rezepte.Tests.Browser/Rezepte.Tests.Browser.csproj --no-build --filter "FullyQualifiedName~Register_WithDemoData_CreatesDemoData"` | Repo-Root | 1 | 0 | 1 | 0 | [dotnet-test-browser-rerun-failed-test.log](test-results/dotnet-test-browser-rerun-failed-test.log) |

### Nachgewiesene bestehende Testfehler

| Test-ID inkl. Testfall | Suite / Dateipfad | Fehlerbild / Fehlermeldung | Lauf und Nachweis |
|-----------------------|------------------|---------------------------|-------------------|
| `Rezepte.Tests.Browser.RegisterTests.Register_WithDemoData_CreatesDemoData` | `Rezepte.Tests.Browser`, `Rezepte.Tests.Browser/Auth/RegisterTests.cs:74` (Hilfsmethode `DemoDataWaitHelper.WaitForDemoDataAsync`, Zeile 305) | `System.TimeoutException : Demo data state did not match expected value True within 120000ms. Last observed: cookbooks=5, recipes=43, calendar=0, shopping=6; seed job status=Succeeded, error=(none)` — der Demo-Daten-Seed meldet `Succeeded`, die Zähler in der UI erreichen den Erwartungswert aber nicht innerhalb von 120 s | Lauf 1b und Lauf 2 (identisches Fehlerbild, deterministisch reproduzierbar); [Log Lauf 1](test-results/dotnet-test-sln-debug.log), [Log Lauf 2](test-results/dotnet-test-browser-rerun-failed-test.log) |

Der Fehler liegt im Bereich Registrierung/Demo-Daten (`RegisterTests`) und steht in keinem fachlichen Zusammenhang zur Plugin-Anforderung; er gilt als **preexisting** (in zwei Läufen mit identischem Fehlerbild nachgewiesen).

### Testlücken und Ausführungsprobleme

- **Externe-Plugin-Tests ohne Abdeckung:** `PluginManagerTests.InitializeAsync_ShouldLoadPublishedExternalChefkochPluginWithoutAdjacentContractAssembly` und `InitializeAsync_ShouldLoadAllPublishedExternalOnlinePluginsWithoutAdjacentContractAssembly` verlassen den Test ohne Assertion, wenn das private externe Plugin-Repository nicht vorliegt (`PluginWorkspace.ExternalPluginRepositoryExists()` → `external/rezepte-import-plugins-private` bzw. `REZEPTE_EXTERNAL_PLUGINS_PATH`). Das Verzeichnis existiert in dieser Arbeitskopie nicht; beide Tests zählen daher als „bestanden", haben den Publish-/Lade-Pfad aber nicht ausgeführt.
- **Keine UI-Tests für `PluginSettings.razor`:** Weder bUnit-Komponententests (`Rezepte.Tests/Components/`) noch Browser-Tests (`Rezepte.Tests.Browser/`) decken die Plugin-Administration ab.
- **`DiscoverFromDirectory_ShouldNotKeepAssembliesLoadedFromTemporaryDirectory`:** Enthält eine Sonderbehandlung bei aktivem Coverlet-Collector (`IsCodeCoverageCollectorAttached`) — in diesem Lauf ohne Coverage-Collector ausgeführt und bestanden.
- Keine Build-, Setup- oder Infrastrukturfehler; beide Suiten liefen vollständig durch.

## Testklassen (Plugin-Bereich)

### `PluginUpdateServiceTests`

Datei: `Rezepte.Tests/Services/Import/PluginUpdateServiceTests.cs` (In-Memory-EF-Core, Fakes)

- `CheckForUpdatesAsync_ShouldIgnoreDisabledSources` — deaktivierte Quellen lösen keinen GitHub-Aufruf aus
- `CheckForUpdatesAsync_ShouldRetryPreviouslyFailedReleaseVersionOnLaterRun` — `ValidationFailed`-Release wird erneut verarbeitet und endet `Installed` inkl. `ReloadStatus`/`ReloadedAt`
- `CheckForUpdatesAsync_ShouldPersistReloadFailureSeparately` — `ReloadFailed` landet in `Status`/`ReloadStatus`/`ReloadError`, `InstalledAt` bleibt leer

### `PluginPackageInstallerTests`

Datei: `Rezepte.Tests/Services/Import/PluginPackageInstallerTests.cs`

- `InstallAsync_ShouldRestoreBackupAndReinitializeAfterReloadFailure` — Backup-Rücksicherung + zweiter Initialisierungsaufruf nach Reload-Fehler; `PluginPackageInstallException.Status == ReloadFailed`
- `InstallAsync_ShouldRemovePartiallyCopiedNewTargetAfterCopyFailure` — teilkopiertes Zielverzeichnis wird nach Copy-Fehler (gelockte Quelldatei) entfernt

### `PluginManagerTests`

Datei: `Rezepte.Tests/Services/Import/PluginManagerTests.cs` (17 `[Fact]`)

- `InitializeAsync_ShouldDiscoverExternalPluginDirectlyUnderPlugins` — DLL direkt unter `plugins/` wird `Loaded`
- `InitializeAsync_ShouldDiscoverExternalPluginFromSubfolderWithAdjacentAbstractionsAssembly` — Unterverzeichnis mit beiliegender `Rezepte.Import.Abstractions.dll`
- `InitializeAsync_ShouldDiscoverExternalPluginFromApplicationBaseDirectory` — Discovery aus `{BaseDirectory}/plugins`
- `InitializeAsync_ShouldMarkBrokenDllAsLoadFailed` — ungültige DLL → `LoadFailed`, `Enabled = false`
- `InitializeAsync_ShouldIgnoreAdjacentContractAssemblyWithoutPlugin` — Vertrags-Assembly allein erzeugt keinen Eintrag
- `InitializeAsync_ShouldDiscoverProductiveExternalImportPlugins` — `ai-foto`, `ai-url`, `backup` aus den Produktiv-Assemblies
- `InitializeAsync_ShouldLoadPublishedExternalChefkochPluginWithoutAdjacentContractAssembly` — externer Publish inkl. Handler-Import (siehe Testlücken: skipped-like early return ohne externes Repo)
- `InitializeAsync_ShouldLoadAllPublishedExternalOnlinePluginsWithoutAdjacentContractAssembly` — dto. für sechs Online-Plugins (siehe Testlücken)
- `InitializeAsync_ShouldUseDefaultPriorityForInitialOrder` — Erst-Reihenfolge nach `DefaultPriority` (`ai-*` ans Ende)
- `InitializeAsync_ShouldMarkPluginWithInvalidHandlerTypeAsIncompatible` — `Incompatible` mit Fehlertext „Configured handler type does not implement IImportHandler."
- `InitializeAsync_ShouldKeepExistingOrderAndAppendNewPlugins` — neue Plugins werden hinter bestehende `OrderIndex`-Werte angehängt (Wiedererfassungs-Pfad)
- `InitializeAsync_ShouldMarkPreviouslyConfiguredPluginAsMissing` — nicht mehr entdecktes Plugin → `Missing` + `"Plugin was not found during startup discovery."`
- `DiscoverFromDirectory_ShouldNotKeepAssembliesLoadedFromTemporaryDirectory` — collectible Context gibt Assembly frei
- `GetActiveHandlersAsync_ShouldNotInstantiateDisabledPlugins` — deaktivierte Plugins liefern keine Handler
- `DefaultCheckUsabilityAsync_ShouldReturnUsable_ForPluginWithoutOverride` — Default-Usability
- `GetPluginsUsabilityAsync_ShouldReturnResultsForLoadedPlugins` / `...TreatCheckExceptionAsNotUsable` — Usability-Ergebnisse und Fehlerbehandlung

### `PluginSettingsServiceTests`

Datei: `Rezepte.Tests/Services/Import/PluginSettingsServiceTests.cs` (9 `[Fact]`)

- `SetEnabledAsync_ShouldPersistActivation`, `MoveAsync_ShouldSwapOrderWithNeighbor`
- `SaveSourceAsync_ShouldCanonicalizeGlobalSourceAndStorePatOnlyInSecretStore` — URL-Kanonisierung, PAT nur im `ISystemSecretStore`, `HasSecret`
- `SaveSourceAsync_ShouldRequireTrustConfirmationForNewSource`, `...RejectNonAdminUsers`, `...RejectMissingHttpContext` — `EnsureAdmin`- und Trust-Vorbedingungen
- `GetPluginsAsync_ShouldPopulateUsabilityForLoadedPlugins`, `...ExposeUsabilityIssuesForMisconfiguredAiPlugin`, `...ReportUsableForFullyConfiguredPlugin`

Keine Tests zum Entfernen von `Missing`-Plugins oder zu manuellen Quellen-Checks (Funktionen existieren noch nicht).

### Weitere angrenzende Testklassen

- `PluginPackageValidatorTests` (`Rezepte.Tests/Services/Import/PluginPackageValidatorTests.cs`): ZIP-Pfad-Traversal-Abwehr, Multi-Plugin-Verzeichnisse
- `GitHubReleaseClientTests` (`Rezepte.Tests/Services/Import/GitHubReleaseClientTests.cs`): Asset-API-URLs, Octet-Stream-Download, `Retry-After`-Retry, `GitHubRateLimitException`
- `PluginSourceReleaseTests` (`Rezepte.Tests/Services/Import/PluginSourceReleaseTests.cs`): `GitHubRepository.Parse`-Kanonisierung, `FindZipAsset`-Assetnamen-Toleranz
- `PluginSourceReleaseMigrationTests` (`Rezepte.Tests/Services/Import/PluginSourceReleaseMigrationTests.cs`): Migration legt Reload-Spalten an

## Hilfsmethoden / Testinfrastruktur

### `PluginManagerTests.PluginWorkspace` (private Klasse)

- `Create()` / `CreateWithoutPluginRoot()` — Temp-Workspace (`%TEMP%/rezepte-plugin-tests/{guid}`) mit `plugins/` unter `ContentRoot`
- `CopyFixturePlugin(targetDirectory)` — kopiert `Rezepte.Tests.PluginFixture.dll` + `Rezepte.Import.Abstractions.dll`
- `CopyOutputAssembly(fileName, targetDirectory)` — kopiert Test-Output-Assemblies
- `CopyProductivePlugins()` — kopiert `Backup`, `AIFoto`, `AIUrl` je in Unterverzeichnis
- `PublishExternalPlugin(projectName)` — `dotnet publish` externer Pluginprojekte ins Plugin-Root
- `ExternalPluginRepositoryExists()` / `GetExternalPluginRepositoryRoot()` — externes Repo via `REZEPTE_EXTERNAL_PLUGINS_PATH` oder `external/rezepte-import-plugins-private`
- `ReadChefkochFixture()` — HTML-Fixture aus dem externen Repo

### Fakes in den Testklassen

- `PluginUpdateServiceTests`: `FakeGitHubReleaseClient` (Zähler `LatestReleaseCalls`/`DownloadCalls`), `FakeSecretStore`, `FakePackageValidator` (Temp-Extract-Root), `FakePackageInstaller`, `FailingPackageInstaller` (wirft `PluginPackageInstallException` mit Status)
- `PluginPackageInstallerTests`: `FailingPluginManager`/`SuccessfulPluginManager` (`IPluginManager`-Stubs), `TestHostEnvironment`, `TempWorkspace`
- `PluginSettingsServiceTests`: `FakePluginManager` (konfigurierbare Usability-Ergebnisse), `FakeSecretStore`, `CreateHttpContextAccessor(isAdmin)`, `CreateSetting(pluginId, orderIndex, enabled)`
- `Rezepte.Tests.PluginFixture` (eigenes Projekt): Testplugin `external-test-plugin`, `invalid-handler-plugin`, `throwing-usability-plugin` u. a.
- `Rezepte.Tests.Browser`: `PlaywrightBrowserFixture` (Browser-Launch mit Skip-Fallback), `RezepteAppFixture`/`ConfiguredRezepteAppFixture`/`RegisterTestsAppFixture` (selbst-gehostete App), `LoadingBarBrowserSession` (Skip bei fehlendem Browser/App)
