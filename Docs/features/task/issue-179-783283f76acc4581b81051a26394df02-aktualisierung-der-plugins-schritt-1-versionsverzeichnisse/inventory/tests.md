# Tests — Bestandsaufnahme

## Test-Ausgangszustand vor der Umsetzung

- **Zeitpunkt (mit Zeitzone):** 2026-11-14, eigener Plugin-Lauf abgeschlossen ca. 18:47 +0100 (CET)
- **Branch und Commit-ID:** `task/issue-179-783283f76acc4581b81051a26394df02-aktualisierung-der-plugins-schritt-1-versionsverzeichnisse` @ `ee49bad5d613468bcbb884a3f24d5948b8b1483b` („chore: Schritt 1 in Arbeit - versionsbezogene Pluginverzeichnisse"). Der Produktivcode ist gegenüber dem projektweiten Baseline-Commit `3e0d864` unverändert (`git diff 3e0d864..HEAD --stat` zeigt ausschließlich `Docs/`-Artefakte: Anforderung, Plan, Projekt-Inventar, Tracking).
- **Uncommittete Änderungen im getesteten Stand:** untracked `Docs/features/task/...-schritt-1-versionsverzeichnisse/` (dieses Feature-Verzeichnis) sowie modifiziertes `Docs/projects/.../status.html`. Keine Änderungen an Produktivcode, Tests oder Konfiguration.
- **Testumgebung und Runtime-/SDK-Versionen:** Windows (Git Bash), .NET SDK `10.0.403` (TargetFramework `net10.0`, `Directory.Build.props`), Node `v24.15.0` (nur `semantic-release`, nicht testrelevant). Identisch zur dokumentierten Baseline-Umgebung.
- **Ermittelte Testsuiten und Quellen der Testbefehle:**
  - `Rezepte.Tests` (xUnit) und `Rezepte.Tests.Browser` (xUnit + Playwright) — beide in `Rezepte.sln`.
  - `Docs/testing.md`: `dotnet test` als Standardbefehl; CI `.github/workflows/pr-staging-ci.yml` (`dotnet test Rezepte.sln --configuration Release --no-build`), `.github/workflows/release.yml` (getrennte Testaufrufe, Release).
  - **Projektweiter Ausgangslauf:** Es liegt ein vollständiger, dokumentierter `dotnet test Rezepte.sln`-Lauf (Debug) vom selben Codestand vor — siehe [projektweite Baseline](../../../../projects/task/issue-179-783283f76acc4581b81051a26394df02-aktualisierung-der-plugins/inventory/tests.md) mit Nachweisen unter `.../inventory/test-results/`. Dieser wird hier als projektweite Baseline zitiert und nicht wiederholt; der eigene Lauf ergänzt ihn gezielt für den Plugin-Bereich.

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| B1 (`Rezepte.Tests`, Baseline) | `dotnet test Rezepte.sln` (Debug, impliziter Build) | Repo-Root | 1 (Gesamtlauf) | 609 | 0 | 0 | [Baseline-Log](../../../../projects/task/issue-179-783283f76acc4581b81051a26394df02-aktualisierung-der-plugins/inventory/test-results/dotnet-test-sln-debug.log) |
| B2 (`Rezepte.Tests.Browser`, Baseline) | `dotnet test Rezepte.sln` (Debug) | Repo-Root | 1 (Gesamtlauf) | 19 | 1 | 0 | [Baseline-Log](../../../../projects/task/issue-179-783283f76acc4581b81051a26394df02-aktualisierung-der-plugins/inventory/test-results/dotnet-test-sln-debug.log) |
| B3 (`Rezepte.Tests.Browser`, Baseline-Wiederholung) | `dotnet test Rezepte.Tests.Browser/Rezepte.Tests.Browser.csproj --no-build --filter "FullyQualifiedName~Register_WithDemoData_CreatesDemoData"` | Repo-Root | 1 | 0 | 1 | 0 | [Baseline-Log](../../../../projects/task/issue-179-783283f76acc4581b81051a26394df02-aktualisierung-der-plugins/inventory/test-results/dotnet-test-browser-rerun-failed-test.log) |
| 1 (`Rezepte.Tests`, Plugin-Filter, eigener Lauf) | `dotnet test Rezepte.Tests --filter "FullyQualifiedName~Plugin"` (Debug, impliziter Build) | Repo-Root | 0 | 49 | 0 | 0 | [dotnet-test-rezepte-tests-plugin-filter.log](test-results/dotnet-test-rezepte-tests-plugin-filter.log) |

Der Filter `FullyQualifiedName~Plugin` erfasst 49 Tests: alle `[Fact]`-Methoden der Plugin-Testklassen (`PluginManagerTests` 17, `PluginSettingsServiceTests` 9, `PluginUpdateServiceTests` 3, `AIFotoImportPluginTests` 3, `AIUrlImportPluginTests` 3, `PluginPackageInstallerTests` 2, `PluginPackageValidatorTests` 2, `PluginSourceReleaseTests` 2, `ImportServicePluginTests` 2, `ProductiveImportPluginParserTests` 1, `PluginSourceReleaseMigrationTests` 1 = 45) plus Methoden mit „Plugin" im Namen aus anderen Klassen (`ImportOrchestratorTests` 3, `ContractExportScriptTests.ExportFailsFastWhenRequiredPluginSdkPathIsMissing` 1).

### Nachgewiesene bestehende Testfehler

| Test-ID inkl. Testfall | Suite / Dateipfad | Fehlerbild / Fehlermeldung | Lauf und Nachweis |
|-----------------------|------------------|---------------------------|-------------------|
| `Rezepte.Tests.Browser.RegisterTests.Register_WithDemoData_CreatesDemoData` | `Rezepte.Tests.Browser`, `Rezepte.Tests.Browser/Auth/RegisterTests.cs:74` | `System.TimeoutException : Demo data state did not match expected value True within 120000ms.` — Demo-Seed meldet `Succeeded`, UI-Zähler erreichen Erwartungswert nicht | Baseline-Läufe B2 und B3 (identisches Fehlerbild); [Baseline-Log](../../../../projects/task/issue-179-783283f76acc4581b81051a26394df02-aktualisierung-der-plugins/inventory/test-results/dotnet-test-sln-debug.log), [Rerun-Log](../../../../projects/task/issue-179-783283f76acc4581b81051a26394df02-aktualisierung-der-plugins/inventory/test-results/dotnet-test-browser-rerun-failed-test.log) |

Der Fehler liegt im Bereich Registrierung/Demo-Daten, steht in keinem Zusammenhang zur Plugin-Anforderung und gilt als **preexisting** (in der Baseline in zwei Läufen nachgewiesen). Im eigenen Plugin-Lauf (Lauf 1) gab es keine Fehlschläge.

### Testlücken und Ausführungsprobleme

- **Projektweiter Lauf in diesem Verzeichnis nicht wiederholt:** bewusst; der Codestand ist identisch zur dokumentierten Baseline (`3e0d864`-Code, HEAD `ee49bad` = Planungscommits ohne Codeänderung). Die Baseline gilt als projektweiter Ausgangsnachweis.
- **Externe-Plugin-Tests ohne Abdeckung (auch in Lauf 1):** `PluginManagerTests.InitializeAsync_ShouldLoadPublishedExternalChefkochPluginWithoutAdjacentContractAssembly` und `InitializeAsync_ShouldLoadAllPublishedExternalOnlinePluginsWithoutAdjacentContractAssembly` verlassen den Test ohne Assertion, wenn `PluginWorkspace.ExternalPluginRepositoryExists()` fehlschlägt (`external/rezepte-import-plugins-private` bzw. `REZEPTE_EXTERNAL_PLUGINS_PATH` existiert in dieser Arbeitskopie nicht). Beide Tests zählen als „bestanden", ohne den Publish-/Ladepfad auszuführen.
- **`DiscoverFromDirectory_ShouldNotKeepAssembliesLoadedFromTemporaryDirectory`:** Sonderbehandlung bei aktivem Coverlet-Collector (`IsCodeCoverageCollectorAttached`); Lauf 1 ohne Coverage-Collector, bestanden.
- **Keine UI-Tests für `PluginSettings.razor`** (unverändert zur Baseline; für Schritt 1 nicht vorgesehen).
- Keine Build-, Setup- oder Infrastrukturfehler im eigenen Lauf; `Rezepte.Tests.Browser` wurde in Lauf 1 nicht ausgeführt (Filter/Projektwahl) — Aussage dazu nur über die Baseline.

## Testklassen (Plugin-Bereich)

### `PluginPackageInstallerTests`

Datei: `Rezepte.Tests/Services/Import/PluginPackageInstallerTests.cs` (2 `[Fact]`, eigene `TempWorkspace`-/`IHostEnvironment`-/`IPluginManager`-Stubs)

- `InstallAsync_ShouldRestoreBackupAndReinitializeAfterReloadFailure` — Reload-Fehler (`FailingPluginManager.InitializeAsync` wirft beim 1. Aufruf) → `PluginPackageInstallException.Status == ReloadFailed`, Backup-Rücksicherung (`plugins/PluginA/PluginA.dll` = „original"), zweiter Initialisierungsaufruf. **Bildet das In-place-/Backup-Modell ab, das die Anforderung ersetzt.**
- `InstallAsync_ShouldRemovePartiallyCopiedNewTargetAfterCopyFailure` — gelockte Quelldatei → Copy-Fehler → teilkopiertes Ziel `plugins/PluginB` entfernt, `PluginPackageInstallException`.

### `PluginUpdateServiceTests`

Datei: `Rezepte.Tests/Services/Import/PluginUpdateServiceTests.cs` (3 `[Fact]`, In-Memory-EF-Core, Fakes)

- `CheckForUpdatesAsync_ShouldIgnoreDisabledSources` — deaktivierte Quelle löst keinen GitHub-Aufruf aus.
- `CheckForUpdatesAsync_ShouldRetryPreviouslyFailedReleaseVersionOnLaterRun` — `ValidationFailed`-Release wird erneut verarbeitet → `Installed` inkl. `ReloadStatus`/`ReloadedAt`.
- `CheckForUpdatesAsync_ShouldPersistReloadFailureSeparately` — `FailingPackageInstaller(ReloadFailed)` → `Status`/`ReloadStatus`/`ReloadError` = `ReloadFailed`, `InstalledAt` leer.

### `PluginManagerTests`

Datei: `Rezepte.Tests/Services/Import/PluginManagerTests.cs` (17 `[Fact]`, Temp-`PluginWorkspace` + In-Memory-`RezepteDbContext`)

- Discovery: `InitializeAsync_ShouldDiscoverExternalPluginDirectlyUnderPlugins` (DLL direkt unter `plugins/`), `...FromSubfolderWithAdjacentAbstractionsAssembly` (eine Unterverzeichnis-Ebene), `...FromApplicationBaseDirectory` (`{BaseDirectory}/plugins`), `...DiscoverProductiveExternalImportPlugins` (`backup`, `ai-foto`, `ai-url`)
- Fehlerfälle: `...MarkBrokenDllAsLoadFailed`, `...IgnoreAdjacentContractAssemblyWithoutPlugin`, `...MarkPluginWithInvalidHandlerTypeAsIncompatible`
- Settings-Sync: `...UseDefaultPriorityForInitialOrder`, `...KeepExistingOrderAndAppendNewPlugins`, `...MarkPreviouslyConfiguredPluginAsMissing` (`Missing` + `"Plugin was not found during startup discovery."`)
- Externe Publishes (early return ohne externes Repo): `...LoadPublishedExternalChefkochPluginWithoutAdjacentContractAssembly`, `...LoadAllPublishedExternalOnlinePluginsWithoutAdjacentContractAssembly`
- Sonstige: `DiscoverFromDirectory_ShouldNotKeepAssembliesLoadedFromTemporaryDirectory`, `GetActiveHandlersAsync_ShouldNotInstantiateDisabledPlugins`, `DefaultCheckUsabilityAsync_ShouldReturnUsable_ForPluginWithoutOverride`, `GetPluginsUsabilityAsync_ShouldReturnResultsForLoadedPlugins`, `...TreatCheckExceptionAsNotUsable`

### Angrenzende Testklassen (vom Filter erfasst)

- `PluginPackageValidatorTests` (`Rezepte.Tests/Services/Import/PluginPackageValidatorTests.cs`, 2 `[Fact]`): ZIP-Pfad-Traversal-Abwehr, Multi-Plugin-Verzeichnisse
- `PluginSourceReleaseTests` (2 `[Fact]`): `GitHubRepository.Parse`, `FindZipAsset`
- `PluginSourceReleaseMigrationTests` (1 `[Fact]`): Reload-Spalten-Migration
- `PluginSettingsServiceTests` (9 `[Fact]`): Aktivierung, Reihenfolge, Quellen-Verwaltung, Usability-Population
- `ImportServicePluginTests` (2 `[Fact]`), `AIFotoImportPluginTests`/`AIUrlImportPluginTests` (je 3), `ProductiveImportPluginParserTests` (1)

## Hilfsmethoden / Testinfrastruktur

### `PluginManagerTests.PluginWorkspace` (private Klasse)

- `Create()` / `CreateWithoutPluginRoot()` — Temp-Workspace (`%TEMP%/rezepte-plugin-tests/{guid}`) mit `plugins/` unter `ContentRoot`
- `CopyFixturePlugin(targetDirectory)` — kopiert `Rezepte.Tests.PluginFixture.dll` + `Rezepte.Import.Abstractions.dll`
- `CopyOutputAssembly(fileName, targetDirectory)`, `CopyProductivePlugins()` (Backup/AIFoto/AIUrl je in Unterverzeichnis), `PublishExternalPlugin(projectName)` (`dotnet publish` ins Plugin-Root)
- `ExternalPluginRepositoryExists()` / `GetExternalPluginRepositoryRoot()` / `ReadChefkochFixture()` — externes Plugin-Repo via `REZEPTE_EXTERNAL_PLUGINS_PATH` oder `external/rezepte-import-plugins-private`

### Fakes/Stubs

- `PluginPackageInstallerTests`: `FailingPluginManager`/`SuccessfulPluginManager` (`IPluginManager`-Stubs, nutzen die Interface-Default-Implementierung von `CoordinateReloadAsync` → `replacePlugins` + `ReloadAsync`/`InitializeAsync`), `TestHostEnvironment`, `TempWorkspace`
- `PluginUpdateServiceTests`: `FakeGitHubReleaseClient`, `FakeSecretStore`, `FakePackageValidator`, `FakePackageInstaller`, `FailingPackageInstaller` (wirft `PluginPackageInstallException` mit Status)
- `Rezepte.Tests.PluginFixture` (eigenes Projekt): Testplugins `external-test-plugin`, `invalid-handler-plugin`, `throwing-usability-plugin`

**Testlücke für die Anforderung:** Es existieren keine Tests zu Versionsverzeichnissen, Löschmarkierungen, Ausschluss markierter Verzeichnisse aus der Erkennung, Bereinigung beim Start/Austausch oder Rollback im Versionsverzeichnis-Modell — die Mechanismen existieren noch nicht.
