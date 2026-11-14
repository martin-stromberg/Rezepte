# Tests — Bestandsaufnahme (Schritt 2: Einzelquellen-Check)

## Test-Ausgangszustand vor der Umsetzung

- **Zeitpunkt (mit Zeitzone):** 2026-11-14, Lauf gestartet ca. 19:47 +0100 (CET), abgeschlossen 19:47:44 +0100
- **Branch und Commit-ID:** `task/issue-179-783283f76acc4581b81051a26394df02-aktualisierung-der-plugins-schritt-2-einzelquellen-check` @ `56fa4196be3b80964d05d0f06df3636a26efe1e5` („chore: Schritt 2 in Arbeit - manuelle Einzelquellen-Update-Pruefung")
- **Uncommittete Änderungen im getesteten Stand:** nur untracked `Docs/features/task/...-einzelquellen-check/` (Feature-Artefakte: `requirement.md`, `todo.md`, `inventory/`). Keine Änderungen an Produktivcode, Tests oder Konfiguration (`git diff` gegen `Rezepte.Web`/`Rezepte.Tests` leer).
- **Testumgebung und Runtime-/SDK-Versionen:** Windows (Git Bash), .NET SDK `10.0.403` (TargetFramework `net10.0`), Debug-Konfiguration, EF-Core-InMemory-Provider für Service-Tests.
- **Ermittelte Testsuiten und Quellen der Testbefehle:** Vollständige Suites/Befehle siehe [Projekt-Baseline](../../../../projects/task/issue-179-783283f76acc4581b81051a26394df02-aktualisierung-der-plugins/inventory/tests.md) (`Rezepte.Tests` 609 Tests, `Rezepte.Tests.Browser` 20 Tests; `Docs/testing.md`: `dotnet test` Repo-Root). Für diesen Schritt wurde gemäß Vorgabe nur der Plugin-gefilterte Lauf auf `Rezepte.Tests` ausgeführt.

### Projekt-Baseline (zitiert)

Aus [Projekt-inventory/tests.md](../../../../projects/task/issue-179-783283f76acc4581b81051a26394df02-aktualisierung-der-plugins/inventory/tests.md) (Ausgangslauf 2026-11-14 ~18:26–18:33 +0100 auf dem Basisbranch): `dotnet test Rezepte.sln` (Debug) — **`Rezepte.Tests`: 609 bestanden / 0 fehlgeschlagen / 0 übersprungen**; **`Rezepte.Tests.Browser`: 19 bestanden / 1 fehlgeschlagen** mit dem in zwei Läufen nachgewiesenen preexisting Fehler `Rezepte.Tests.Browser.RegisterTests.Register_WithDemoData_CreatesDemoData` (`System.TimeoutException`, Demo-Daten-Zähler). Der aktuelle Schritt-Branch steht auf diesem Basisbranch (plus nur Docs-Commits), die Baseline bleibt damit gültig.

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| 1 (`Rezepte.Tests`, Plugin-Filter) | `dotnet test Rezepte.Tests --filter "FullyQualifiedName~Plugin"` | Repo-Root | 0 | 100 | 0 | 0 | [dotnet-test-rezepte-tests-plugin-filter.log](test-results/dotnet-test-rezepte-tests-plugin-filter.log) |

### Nachgewiesene bestehende Testfehler

Im Plugin-gefilterten Lauf wurde **kein** Testfehler nachgewiesen (100/100 bestanden). Der projektweit bekannte preexisting Fehlschlag `RegisterTests.Register_WithDemoData_CreatesDemoData` (`Rezepte.Tests.Browser`) wurde in diesem Lauf nicht ausgeführt — Nachweis weiterhin in der [Projekt-Baseline](../../../../projects/task/issue-179-783283f76acc4581b81051a26394df02-aktualisierung-der-plugins/inventory/tests.md).

### Testlücken und Ausführungsprobleme

- **Gefilterter Umfang:** `FullyQualifiedName~Plugin` umfasst 100 Tests in `Rezepte.Tests` — alle Plugin-bezogenen Testklassen (u. a. `PluginUpdateServiceTests`, `PluginSettingsServiceTests`, `PluginManagerTests`, `PluginPackageInstallerTests`, `PluginPackageValidatorTests`, `PluginSourceRelease*`, `PluginDirectoryRegistry*`, `ImportServicePluginTests`, Plugin-Parser-Tests). Nicht-Plugin-Tests der Suite (509) wurden nicht ausgeführt; deren Stand bleibt die Projekt-Baseline (609/609 grün).
- **Browser-Suite nicht ausgeführt** (Scope dieser Bestandsaufnahme); bekannte preexisting-Fehlschlag-Referenz: Projekt-Baseline.
- **Keine Tests für Einzelquellen-Check, Ergebnis-DTO oder Serialisierung zwischen Start-Lauf und manuellem Check** — Funktionen existieren noch nicht.
- Bekannte Coverage-Lücke des Bestands (aus Projekt-Baseline übernommen): Zwei `PluginManagerTests` verlassen den Test ohne Assertion, wenn das externe Plugin-Repo fehlt (`ExternalPluginRepositoryExists()` → early return ohne externes Repo in dieser Arbeitskopie).
- Keine Build-, Setup- oder Infrastrukturfehler.

## Testklassen

### `PluginUpdateServiceTests`

Datei: `Rezepte.Tests/Services/Import/PluginUpdateServiceTests.cs` — EF-Core-InMemory (`CreateDb`, Z. 139–145), SUT-Factory `CreateSut` (Z. 147–156).

- `CheckForUpdatesAsync_ShouldIgnoreDisabledSources` (Z. 20) — `Enabled=false`-Quelle löst keinen `GetLatestReleaseAsync`-Aufruf aus (`LatestReleaseCalls == 0`). Hinweis: `TrustConfirmed`-Filter ist hier nicht separat abgedeckt.
- `CheckForUpdatesAsync_ShouldRetryPreviouslyFailedReleaseVersionOnLaterRun` (Z. 44) — `ValidationFailed`-Datensatz wird erneut verarbeitet (`DownloadCalls == 1`) und endet `Installed` inkl. `ReloadStatus`/`ReloadedAt`, `Error` zurückgesetzt.
- `CheckForUpdatesAsync_ShouldPersistReloadFailureSeparately` (Z. 85) — `PluginPackageInstallException(ReloadFailed)` → `Status`/`ReloadStatus` = `ReloadFailed`, `ReloadError = "reload failed"`, `InstalledAt` bleibt null.
- `CheckForUpdatesAsync_ShouldPassReleaseTagAndSourceKey` (Z. 115) — `PluginPackageInstallOptions` erhält `ReleaseTag = "v9.9"`, `SourceKey = "source-1"` (= `PluginSource.Id`).

**Für Schritt 2 fehlende Abdeckung (Anforderungsliste):** Einzelquellen-Check (Erfolg, Skip installierter Releases, Retry fehlgeschlagener, Fehlerfälle kein Release/kein ZIP-Asset/fehlendes PAT, Ablehnung unbekannter/deaktivierter/nicht bestätigter Quelle), Fortschreibung der Statusfelder, Serialisierung gegenüber `CheckForUpdatesAsync`.

### `PluginSettingsServiceTests`

Datei: `Rezepte.Tests/Services/Import/PluginSettingsServiceTests.cs` — SUT wird direkt per Konstruktor erzeugt (`new PluginSettingsService(db, FakePluginManager, CreateServiceProvider(), httpContextAccessor?, secretStore?)`).

- `SetEnabledAsync_ShouldPersistActivation`, `MoveAsync_ShouldSwapOrderWithNeighbor` — Basisfunktionen.
- `SaveSourceAsync_ShouldCanonicalizeGlobalSourceAndStorePatOnlyInSecretStore` — PAT nur im `FakeSecretStore`, `SecretName`-Indirektion, `HasSecret` im Item.
- `SaveSourceAsync_ShouldRequireTrustConfirmationForNewSource` — `InvalidOperationException`.
- `SaveSourceAsync_ShouldRejectNonAdminUsers` / `...RejectMissingHttpContext` — `UnauthorizedAccessException` ohne Admin-Rolle bzw. ohne `HttpContext`; **direktes Muster für die Admin-Tests der neuen Check-Methode**.
- `GetPluginsAsync_Should*` (3 Tests) — Usability-Befüllung.

**Für Schritt 2 fehlende Abdeckung:** Admin-Prüfung der neuen Methode (Ablehnung ohne Admin, Delegation an `IPluginUpdateService`). Neue Abhängigkeit `IPluginUpdateService` muss in den SUT-Konstruktoraufrufen ergänzt werden.

## Hilfsmethoden / Fakes

### `PluginUpdateServiceTests` (private Klassen)

- `CreateDb()` — `RezepteDbContext` mit `UseInMemoryDatabase(Guid.NewGuid())`.
- `CreateSut(db, github, installer = null)` — `PluginUpdateService` mit Fakes + `NullLogger`.
- `FakeGitHubReleaseClient(GitHubReleaseInfo? release)` — Zähler `LatestReleaseCalls`/`DownloadCalls`; `DownloadAssetAsync` schreibt Dummy-ZIP-Datei.
- `FakeSecretStore` — `GetAsync` liefert fest `"token"` (kein konfigurierbarer Miss-Fall; für PAT-Fehlertests anzupassen).
- `FakePackageValidator` — liefert `PluginPackageValidationResult(true, null, tempRoot, [tempRoot], [])` mit Temp-Extract-Root.
- `FakePackageInstaller` — `LastOptions`-Capture; nur `InstallAsync` implementiert (`InstallWithReloadTrackingAsync` via Default-Interface-Impl → ruft `beforeReload` dann `InstallAsync`).
- `FailingPackageInstaller(string status)` — wirft `PluginPackageInstallException(status, "reload failed", …)`.

### `PluginSettingsServiceTests` (private Klassen/Helfer)

- `CreateDb()` — InMemory-DbContext.
- `CreateSetting(pluginId, orderIndex, enabled)` — `PluginSetting`-Fixture (`Status = PluginStatus.Loaded`).
- `CreateHttpContextAccessor(isAdmin)` — `HttpContextAccessor` mit `DefaultHttpContext`, Admin via `Claim(ClaimTypes.Role, "Admin")`.
- `CreateServiceProvider()` — leerer `ServiceProvider` (`new ServiceCollection().BuildServiceProvider()`).
- `FakeSecretStore` — `Dictionary<string,string> Values`; `GetAsync` liefert `null` bei fehlendem Namen (PAT-Fehlfall abbildbar).
- `FakePluginManager(usability?)` — `IPluginManager`-Stub; `InitializeAsync`/`GetActiveHandlersAsync`/`GetPluginsUsabilityAsync` implementiert, Rest via Default-Interface-Impl (u. a. `CoordinateReloadAsync`).

### Verwandte Testinfrastruktur (Projekt-Baseline)

`PluginPackageInstallerTests` nutzt `FailingPluginManager`/`SuccessfulPluginManager`, `TestHostEnvironment`, `TempWorkspace` — bei Bedarf wiederverwendbar für Serialisierungs-Aspekte des `InstallLock`. Details: [Projekt-inventory/tests.md](../../../../projects/task/issue-179-783283f76acc4581b81051a26394df02-aktualisierung-der-plugins/inventory/tests.md).
