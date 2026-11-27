# Tests

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-11-27, ca. 07:04–07:35 +01:00 (W. Europe Standard Time)
- Branch und Commit-ID: `task/issue-239-f321cfdb0c9b4ec18fc30789610d8ffd-fehler-beim-aktualisieren-von` @ `92e2c1b46482269338f47f3935378992dfec6ba1` (von `origin/staging` abgezweigt)
- Uncommittete Änderungen im getesteten Stand: nur untracked `Docs/features/task/issue-239-f321cfdb0c9b4ec18fc30789610d8ffd-fehler-beim-aktualisieren-von/` (requirement.md und die hier abgelegten Testnachweise); keine Änderungen an Produktiv- oder Testcode
- Testumgebung und Runtime-/SDK-Versionen: Windows (Git Bash), .NET SDK 11.0.100 (`global.json`: `11.0.100`, `rollForward: latestFeature`; zusätzlich SDK 10.0.404 installiert), Laufzeit .NET 11.0.0, xUnit.net VSTest Adapter 4.0.1 (xunit.v3 4.0.3), bUnit 2.15.2, Playwright-Browsercache vorhanden (`ms-playwright`, u. a. chromium-1255)
- Ermittelte Testsuiten und Quellen der Testbefehle: `.github/workflows/staging-ci.yml` (und analog `pr-staging-ci.yml`): Job `build-and-test` führt `dotnet test Rezepte.Tests/Rezepte.Tests.csproj -c Release --no-build --filter "Category!=OsInterface"` aus; Job `browser-tests` (best-effort, `continue-on-error`) führt `dotnet test Rezepte.Tests.Browser/...` sowie das `Category=OsInterface`-Subset von `Rezepte.Tests` aus. `Rezepte.Updater.TestHost` ist ein Testhost-Projekt ohne eigene Test-Suite.
- Build vor den Läufen: `dotnet restore Rezepte.sln`, `dotnet build Rezepte.Web/Rezepte.Web.csproj -c Release --no-restore` (erzeugt u. a. `Rezepte.Web/bin/Release/net11.0/publish/`), `dotnet build Rezepte.Tests/Rezepte.Tests.csproj -c Release --no-restore`, `dotnet build Rezepte.Tests.Browser/Rezepte.Tests.Browser.csproj -c Release --no-restore` — alle erfolgreich, 0 Warnungen, 0 Fehler.

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| 1 deterministische Lane | `dotnet test Rezepte.Tests/Rezepte.Tests.csproj --configuration Release --no-build --filter "Category!=OsInterface" --results-directory <docs>/inventory/test-results/deterministic --logger "trx;LogFileName=test-results-deterministic.trx" --logger "console;verbosity=normal"` | Repo-Root | 0 | 763 | 0 | 0 | [TRX](test-results/deterministic/test-results-deterministic.trx) |
| 2 OsInterface-Subset | `dotnet test Rezepte.Tests/Rezepte.Tests.csproj --configuration Release --no-build --filter "Category=OsInterface" --results-directory <docs>/inventory/test-results/os-interface --logger "trx;LogFileName=test-results-os-interface.trx" --logger "console;verbosity=normal"` | Repo-Root | 0 (Testlauf „erfolgreich"; das umgebende Shell-Kommando endete mit 1, weil ein zusätzliches `tee` in ein noch nicht existierendes Verzeichnis scheiterte — TRX und Zähler belegen den erfolgreichen Lauf) | 15 | 0 | 0 | [TRX](test-results/os-interface/test-results-os-interface.trx) |
| 3 Browser-Subset (Plugin-Settings) | `dotnet test Rezepte.Tests.Browser/Rezepte.Tests.Browser.csproj --configuration Release --no-build --filter "FullyQualifiedName~PluginSettingsBrowserTests" --results-directory <docs>/inventory/test-results/browser-pluginsettings --logger "trx;LogFileName=test-results-browser-pluginsettings.trx" --logger "console;verbosity=normal"` | Repo-Root | 0 | 5 | 0 | 0 | [TRX](test-results/browser-pluginsettings/test-results-browser-pluginsettings.trx) |

### Nachgewiesene bestehende Testfehler

Keine. Alle ausgeführten Tests (763 + 15 + 5) sind bestanden; kein einziger Fehlschlag oder übersprungener Test nachgewiesen.

Beobachtete Diagnoseausgaben (keine Testfehlschläge, best-effort-Cleanup-Meldungen von `TempDirectoryCleanup`/`RezepteAppFixture`): wiederholt „could not be deleted after 5 attempts: Access to the path 'Rezepte.Import.Abstractions.dll' / 'Rezepte.Import.Plugins.AIFoto.dll' is denied" im Umfeld von `PluginManagerTests` sowie einmal „rezepte-browser-test.db ... being used by another process" — Windows-Dateilocking auf noch geladene Assemblies bzw. die SQLite-Datei; die Tests selbst liefen durch.

### Testlücken und Ausführungsprobleme

- Übrige `Rezepte.Tests.Browser`-Suite **nicht ausgeführt** (`Auth/RegisterTests`, `LoadingBar*BrowserTests`, `RecipeSearchBrowserTests`, `SecurityTxtBrowserTests`, `ShareLinkBrowserTests`): umgebungsabhängige Best-effort-Lane; für den Scope wurde nur die anforderungsrelevante Klasse `PluginSettingsBrowserTests` ausgeführt. Ausführbarkeit wäre gegeben (Publish-Output + Playwright-Cache vorhanden).
- Kein Test simuliert ein fehlendes/nicht beschreibbares Temp-Basisverzeichnis oder prüft die Zwischenebenen `rezepte-plugin-download`/`-package`/`-backup`: Alle beteiligten Produktiv-Fundstellen rufen `Path.GetTempPath()` direkt auf und sind in Tests nicht ersetzbar; `PluginUpdateService`-Tests nutzen Fake-Collaborators, `PluginPackageInstallerTests` einen `TestHostEnvironment`-Content-Root, aber das Temp-Basisverzeichnis ist nirgends injizierbar.
- `Rezepte.Updater.TestHost` enthält keine Test-Suite (Host für Updater-Selbsttests).

## Testklassen

### `PluginUpdateServiceTests` (`Rezepte.Tests/Services/Import/PluginUpdateServiceTests.cs`)
- `CheckForUpdatesAsync_ShouldIgnoreDisabledSources` — deaktivierte Quellen werden übersprungen
- `CheckForUpdatesAsync_ShouldRetryPreviouslyFailedReleaseVersionOnLaterRun` — fehlgeschlagener Release wird erneut verarbeitet
- `CheckForUpdatesAsync_ShouldPersistReloadFailureSeparately` — Reload-Fehler landet in `ReloadStatus`/`ReloadError`
- `CheckForUpdatesAsync_ShouldPassReleaseTagAndSourceKey` — `PluginPackageInstallOptions` erhält Tag + `Source.Id`

### `PluginUpdateServiceTests_CheckSource` (`Rezepte.Tests/Services/Import/PluginUpdateServiceTests_CheckSource.cs`)
- `..._ShouldInstallNewReleaseAndReturnInstalled` — Happy Path inkl. Status `Installed` und `LastSuccessfulReleaseTag`
- `..._ShouldReturnAlreadyInstalledForInstalledRelease` — `NoUpdate` mit `AlreadyInstalled`, kein Download
- `..._ShouldRetryPreviouslyFailedRelease` — erneute Verarbeitung eines `ValidationFailed`-Datensatzes
- `..._ShouldReturnFailedWhenNoReleaseFound` — `NoPublishedRelease`-Mapping in `LastError`
- `..._ShouldReturnFailedWhenReleaseHasNoZipAsset` — `NoZipAsset` + Status `ValidationFailed`
- `..._ShouldReturnFailedWhenPrivateSourceHasNoPat` — `PrivateSourceNoPat`, kein API-Call
- `..._ShouldReturnFailedOnInstallFailure` — `PluginPackageInstallException` → `ReloadFailed`, `Error` = `ex.Message` („reload failed")
- `..._ShouldReturnFailedOnUnexpectedException` — beliebige Exception → `Failed(ex.Message)` (demonstriert die rohe-Exception-Weitergabe)

### `PluginUpdateServiceTests_Preconditions` (`Rezepte.Tests/Services/Import/PluginUpdateServiceTests_Preconditions.cs`)
- `..._ShouldReturnFailedForUnknownSource` / `..._ShouldReturnFailedForDisabledSource` / `..._ShouldReturnFailedForUnconfirmedSource` — die drei Vorbedingungs-Konstanten

### `PluginUpdateServiceTests_Serialization` (`Rezepte.Tests/Services/Import/PluginUpdateServiceTests_Serialization.cs`)
- `CheckSourceForUpdatesAsync_ShouldWaitForRunningStartupCheck`, `CheckForUpdatesAsync_ShouldWaitForRunningSourceCheck`, `CheckSourceForUpdatesAsync_ShouldReleaseGateAfterFailedCheck` — `CheckGate`-Serialisierung über Instanzgrenzen hinweg

### `PluginSettingsServiceTests_CheckSource` (`Rezepte.Tests/Services/Import/PluginSettingsServiceTests_CheckSource.cs`)
- `..._ShouldRejectNonAdminUsers`, `..._ShouldRejectMissingHttpContext`, `..._ShouldDelegateToUpdateService` — Admin-Guard + Delegation

### `PluginPackageValidatorTests` (`Rezepte.Tests/Services/Import/PluginPackageValidatorTests.cs`)
- `ValidateAsync_ShouldRejectZipPathTraversal` — `../`-Eintrag → `Failed("unsafe path")`
- `ValidateAsync_ShouldAcceptMultiplePluginSubdirectoriesWhenDiscoverySucceeds` — zwei Plugin-Verzeichnisse → `PluginDirectories`

### `PluginPackageInstallerTests` (`Rezepte.Tests/Services/Import/PluginPackageInstallerTests.cs`)
- `InstallAsync_ShouldInstallIntoVersionedDirectory` — `PluginA@v1.0` + State-Eintrag
- `InstallAsync_ShouldMarkPredecessorPendingDeletion` — Vorgänger → `PendingDeletes`
- `InstallAsync_ShouldMarkDroppedPluginOfSameSourcePending` — weggefallene Plugins derselben Quelle
- `InstallAsync_ShouldKeepPreviousVersionEffective_WhenReloadFails` — Rollback hält alte Version wirksam
- `InstallAsync_ShouldKeepRegisteredDirectory_WhenSameTagReinstallFailsOnReload` — Same-Tag-Reinstall: Backup/Rollback
- `InstallAsync_ShouldRestoreRegisteredDirectory_WhenSameTagReinstallFailsDuringCopy` — Restore aus dem Temp-Backup
- `InstallAsync_ShouldRestoreState_WhenInstallFailsBeforeReplacement` — kein State-Eintrag bei vorzeitigem Fehler
- `InstallAsync_ShouldKeepFlatDirectory_WhenVersionedInstallShadowsIt` — flaches Verzeichnis bleibt bestehen
- `InstallAsync_ShouldNotPersistPendingDeletion_WhenRegisteredDirectoryDeleteFails` — registriertes Verzeichnis nie als Pending-Delete persistieren (mit `RecordingDirectoryRegistry`; POSIX-Zweig via `SetUnixFileMode`)
- `InstallAsync_ShouldRemovePartialBackup_WhenBackupCopyFails` — prüft konkret `{Temp}/rezepte-plugin-backup`-Verzeichnisse auf Orphans

### `PluginSettingsComponentTests` (`Rezepte.Tests/Components/PluginSettingsComponentTests.cs`)
bUnit-Tests der Razor-Komponente (lokalisierte Meldungen/Tooltips; relevanter Ausschnitt):
- `CheckSourceAsync_CallsService_AndShowsInstalled` / `..._ShowsNoUpdate` — Erfolgs-Alerts
- `CheckSourceAsync_ShowsLocalizedFailed` — bekannte Konstante → lokalisierte Meldung
- `CheckSourceAsync_WhenServiceThrows_ShowsFailedAlert` — Exception → „Prüfung fehlgeschlagen. (boom-check)"
- `CheckSourceAsync_ShowsFailedWithDetail_ForUnknownError` — unbekannter Fehlertext → generische Meldung mit Detail (genau das Fehlerbild des Issues)
- `CheckSourceAsync_SetsBusy_AndDisablesRow`, `..._ReloadsSources`, `CheckButton_Disabled_WhenSourceNotEnabledOrUntrusted`, `InfoText_IsRendered`, `ActionButtons_HaveLocalizedTooltipAndAriaLabel`, `RemoveButton_*`, `RemovePluginAsync_*`, `LastErrorAt_IsRendered`

### `PluginSettingsBrowserTests` (`Rezepte.Tests.Browser/PluginSettingsBrowserTests.cs`, Trait `Category=OsInterface`)
- `PluginSettings_ShowsInfoText_AndCheckButtonWithTooltip`, `CheckButton_WithoutPat_ShowsLocalizedError_AndUpdatesStatus`, `CheckButton_IsDisabled_WithReason_WhenSourceDeactivated`, `CheckButton_WhenReleaseAlreadyInstalled_ShowsNoUpdate`, `MissingPlugin_CanBeRemoved` — E2E über realen App-Prozess + `GitHubStubServer`

### `GitHubReleaseClientTests` (`Rezepte.Tests/Services/Import/GitHubReleaseClientTests.cs`)
- `GetLatestReleaseAsync_ShouldUseAssetApiUrlForDownload`, `DownloadAssetAsync_ShouldRequestAssetApiUrlAsOctetStream` (schreibt nach `Path.GetTempPath()`), Rate-Limit-Retry/-Exception

## Hilfsmethoden

### `PluginUpdateServiceTestsBase` (`Rezepte.Tests/Services/Import/PluginUpdateServiceTestsBase.cs`)
- `CreateDb()` — InMemory-`RezepteDbContext` (EF Core InMemory)
- `AddSource(db, ...)` — `PluginSource`-Fixture
- `CreateSut(db, gitHubReleaseClient, installer, secretStore)` — baut `PluginUpdateService` mit Fakes + `NullLogger`
- `CreateRelease(tagName, releaseId, assetId)` — `GitHubReleaseInfo`-Fixture
- `FakeGitHubReleaseClient` — feste Release-Antwort; `DownloadAssetAsync` schreibt via `File.WriteAllText(targetPath, ...)` (würde bei fehlendem Work-Root ebenfalls scheitern)
- `ThrowingGitHubReleaseClient`, `BlockingGitHubReleaseClient` — Fehler-/Serialisierungsszenarien
- `FakeSecretStore`, `FakePackageValidator` (legt eigenes Temp-Verzeichnis `rezepte-update-tests` an), `FakePackageInstaller`, `FailingPackageInstaller` (wirft `PluginPackageInstallException` mit wählbarem Status)

### `PluginDirectoryRegistryTestsBase` (`Rezepte.Tests/Services/Import/PluginDirectoryRegistryTestsBase.cs`)
- `CreateRegistry(workspace)` — `PluginDirectoryRegistry` gegen `TestHostEnvironment`
- `TestHostEnvironment` — `IHostEnvironment`-Implementierung mit setzbarem `ContentRootPath`
- `TempWorkspace` — Temp-Root + `PluginRoot`-Property (`{root}/plugins`), `Dispose` via `TempDirectoryCleanup`

### `TempDirectoryCleanup` (`Rezepte.Tests/TestHelpers/TempDirectoryCleanup.cs`)
- `DeleteBestEffort(root, processesToKill, normalizeAttributes)` — never-throws-Verzeichnisaufräumung mit Retry (5 Versuche, 100·n ms Backoff) und optionalem Prozess-Kill
- `DeleteFileBestEffort(path)` — Datei-Variante

### `TestCategories` (`Rezepte.Tests/TestHelpers/TestCategories.cs`)
- `Category`/`OsInterface`/`Flaky` — zentrale Trait-Namen für die CI-Lanes

### Browser-Testinfrastruktur (`Rezepte.Tests.Browser/Infrastructure/`)
- `RezepteAppFixture` — startet publizierte `Rezepte.Web.dll` als Kindprozess (Temp-SQLite, Readiness-Poll, Prozesslog unter `browser-app-logs/`); benötigt `Rezepte.Web/bin/<cfg>/<tfm>/publish/` oder `REZEPTE_PUBLISH_DIR`
- `PluginSettingsAppFixture`/`PluginSettingsTestCollection` — setzt `PluginUpdates__GitHubApiBaseUrl` auf `GitHubStubServer`; `SeedInstalledReleaseAsync`, `SeedMissingPluginAsync`
- `PluginSettingsPageObject`, `PlaywrightBrowserFixture`, `GitHubStubServer`
