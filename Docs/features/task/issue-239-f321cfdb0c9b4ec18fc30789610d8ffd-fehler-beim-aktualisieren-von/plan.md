# Umsetzungsplan: Fehler beim Aktualisieren von Plugins (Issue #239)

## Übersicht

Der Plugin-Update-Pfad legt seine Arbeitsverzeichnisse (Download, Paket-Extraktion, Install-Backup) direkt unter `Path.GetTempPath()` an und scheitert mit einer rohen Laufzeit-Exception, wenn das Temp-Basisverzeichnis nicht existiert oder nicht beschreibbar ist. Die Korrektur führt eine zentrale, injizierbare Abstraktion `IPluginWorkspaceProvider` ein, die Arbeitsverzeichnisse gegen eine geordnete Kandidatenliste (konfigurierter Pfad → System-Temp → garantiert beschreibbarer Fallback `{ContentRootPath}/plugins/.work`) bereitstellt und bei Totalversagen eine `PluginWorkspaceException` mit einer fachlichen Fehlerkonstante wirft, die die UI lokalisiert darstellt.

## Scope-Klärung

| Feld | Festlegung |
|------|------------|
| Verarbeitungseinheit | Eine Plugin-Update-Prüfung einer einzelnen Plugin-Quelle (`CheckSourceForUpdatesAsync` → `ProcessSourceAsync` → `DownloadValidateInstallAsync`, Phasen Release-Ermittlung, Download, Validierung, Installation, Reload) |
| Auslösende Ereignisse | Admin-Klick auf „Auf Updates prüfen" in `PluginSettings.razor`; automatische Startprüfung via `PluginUpdateHostedService` → `CheckForUpdatesAsync` (gleicher Codepfad, fachlich im Scope) |
| Erwartete Wirkung | Update-Prüfung läuft ohne die rohe `FileNotFoundException`-Meldung durch; ein gefundenes Update wird installiert; kann kein Arbeitsverzeichnis bereitgestellt werden, erscheint eine fachliche, lokalisierte Meldung (UI-Alert) statt der rohen Exception |
| Umgang mit fehlenden Objekten | Temp-Arbeitsverzeichnisse `rezepte-plugin-download`/`-package`/`-backup` (je mit GUID-Unterebene): **erzeugen** — flüchtig, nie voraussetzen; Anlegen erstellt alle Zwischenebenen. Beschreibbares Basisverzeichnis: **wiederverwenden** — Kandidatenreihenfolge `PluginUpdates:WorkDirectory` (optional, neu) → `Path.GetTempPath()` → `{ContentRootPath}/plugins/.work` (wird von `PluginDirectoryRegistry`/`PluginPackageInstaller` bereits angelegt und beschrieben). `.work`-Verzeichnis unter `plugins`: **erzeugen** bei Bedarf. |

### Scope-Ausschlüsse

| # | Einschränkung / Ausschluss | Begründung | Status |
|---|----------------------------|------------|--------|
| 1 | `Path.GetTempPath()`-Nutzer außerhalb des Plugin-Update-Pfads (`ExportService.cs:277`, `AdminExportsController.cs:105`) werden nicht geändert — dasselbe strukturelle Muster bleibt dort bestehen | Vom Issue nicht erfasst; die neue Abstraktion könnte dort später wiederverwendet werden, ist aber kein Bestandteil dieser Korrektur | bestätigt (Issue-Vorgabe/Scope: Issue betrifft ausschließlich die Plugin-Aktualisierung, 2026-11-27) |
| 2 | Keine Änderung an der fachlichen Update-Logik (Release-Ermittlung via `IGitHubReleaseClient`, Paketvalidierungsregeln, Installations-/Reload-Koordination) | Requirement schränkt die Korrektur auf Arbeitsverzeichnis-Bereitstellung und Fehlerübersetzung ein | bestätigt (Issue-Vorgabe/Scope, 2026-11-27) |
| 3 | Keine Änderung am Auslösemechanismus (manueller Button, Startprüfung, Fehlerdarstellungs-Flow in der UI) | Nur die Qualität der resultierenden Fehlermeldung ist Ziel des Issues | bestätigt (Issue-Vorgabe/Scope, 2026-11-27) |
| 4 | Die Statusspalte (`@source.LastError`, `PluginSettings.razor:99`) zeigt die neue Fehlerkonstante — wie alle bestehenden Konstanten — als englischen Rohtext; nur der Ergebnis-Alert ist lokalisiert | Bestehende Konvention aller `PluginUpdateCheckErrors`-Konstanten; eine Lokalisierung der Statusspalte wäre eine separate UI-Änderung außerhalb des Issues | bestätigt (Bestandskonvention/Issue-Vorgabe, 2026-11-27) |

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Arbeitsverzeichnis-Bereitstellung | Neue injizierbare Abstraktion `IPluginWorkspaceProvider` (Gateway) mit Methode `CreateWorkDirectory(purpose)`, Singleton-Implementierung `PluginWorkspaceProvider` | Drei Fundstellen mit identischem Muster; die zentrale Kandidatenlogik ist testbar (Temp-Basis per optionalem `Func<string>`-Parameter als Testnaht injizierbar, da kein `InternalsVisibleTo` existiert); konsistent mit der DI-lastigen Architektur |
| Kandidatenreihenfolge für das Basisverzeichnis | `PluginUpdates:WorkDirectory` → `Path.GetTempPath()` → `{ContentRootPath}/plugins/.work`; pro Kandidat `Directory.CreateDirectory(<kandidat>/<purpose>/<guid>)` als Beschreibbarkeitsprobe, bei Fehler Warnlog + nächster Kandidat | Beantwortet die offene Frage „konfigurierbar vs. Fallback" mit beidem: Der konfigurierbare Schlüssel ist der Escape-Hatch für Umgebungen, in denen weder Temp noch Content-Root beschreibbar sind (genau das Container-Szenario des Issues); der Fallback ist die garantiert beschreibbare Variante ohne Konfiguration. `.work` unter `plugins` ist sicher: Es enthält keine Top-Level-DLLs (Arbeitsdaten liegen ab Tiefe 2), damit greifen weder `GetPluginAssemblyCandidates` noch `GetExcludedDirectoryNames`/`TryParseVersionedDirectoryName` (nur `@`-Namen) darauf |
| Fehlerübersetzung | Neue Exception `PluginWorkspaceException` (direkt von `Exception` abgeleitet), deren `Message` die neue Konstante `PluginUpdateCheckErrors.WorkDirectoryUnavailable` ist | Alle bestehenden `ex.Message`-Kanäle (`PluginSource.LastError`, `PluginSourceRelease.Error`, `PluginUpdateCheckResult.Failed`, Wrapping in `PluginPackageInstallException`) transportieren dadurch automatisch die fachliche Konstante — keine zusätzliche Fehlerverzweigung im Service nötig; `MapCheckError` lokalisiert sie wie die übrigen Konstanten. Direkte Ableitung von `Exception` (nicht `IOException`) verhindert, dass der `IOException`-Filter in `PluginPackageValidator.ValidateAsync` sie in eine generische „Could not extract"-Meldung einpackt |

## Programmabläufe

### Arbeitsverzeichnis bereitstellen (`IPluginWorkspaceProvider.CreateWorkDirectory`)

1. Kandidatenliste in fester Reihenfolge bilden: `options.WorkDirectory` (nur wenn nicht leer), `systemTempPath()` (produktiv `Path.GetTempPath()`), `Path.Combine(environment.ContentRootPath, "plugins", ".work")` — doppelte Kandidaten werden übersprungen.
2. Pro Kandidat: `Directory.CreateDirectory(Path.Combine(kandidat, purpose, Guid.NewGuid().ToString("N")))` — legt alle Zwischenebenen an und dient zugleich als Beschreibbarkeitsprobe. Erfolg → vollständiger Pfad wird zurückgegeben.
3. Scheitert ein Kandidat (`IOException`, `UnauthorizedAccessException`, `ArgumentException`, `NotSupportedException` o. ä.): Warnlog mit Kandidat + Fehler, nächster Kandidat wird versucht.
4. Scheitern alle Kandidaten: `PluginWorkspaceException` mit `Message == PluginUpdateCheckErrors.WorkDirectoryUnavailable` werfen.

Beteiligte Klassen/Komponenten: `PluginWorkspaceProvider`, `IOptions<PluginUpdateOptions>`, `IHostEnvironment`, `ILogger<PluginWorkspaceProvider>`, `PluginWorkspaceException`

### Manuelle Update-Prüfung mit robuster Workspace-Bereitstellung

1. Admin klickt „Auf Updates prüfen" → `PluginSettings.CheckSourceAsync` → `PluginSettingsService.CheckSourceForUpdatesAsync` → `PluginUpdateService.CheckSourceForUpdatesAsync` (Vorbedingungen `SourceNotFound`/`SourceDisabled`/`SourceNotTrusted` unverändert).
2. `ProcessSourceAsync`: Release-Ermittlung, PAT-Auflösung, Asset-Auswahl, `PluginSourceRelease`-Upsert — unverändert.
3. `DownloadValidateInstallAsync`: `workRoot = workspaceProvider.CreateWorkDirectory("rezepte-plugin-download")` (ersetzt `Path.GetTempPath()` + `Directory.CreateDirectory`); anschließend unverändert `DownloadAssetAsync` → `packageValidator.ValidateAsync` → `packageInstaller.InstallWithReloadTrackingAsync`.
4. `PluginPackageValidator.ValidateAsync`: `extractRoot = workspaceProvider.CreateWorkDirectory("rezepte-plugin-package")` (ersetzt beide Zeilen 29–30); Extraktions-/Prüflogik unverändert.
5. `PluginPackageInstaller.BackupDirectory`: `backupPath = workspaceProvider.CreateWorkDirectory("rezepte-plugin-backup")`; `CopyDirectory` kopiert in das bereits angelegte Verzeichnis (`Directory.CreateDirectory` dort ist idempotent) — Rest unverändert.
6. Fehlerfall: `PluginWorkspaceException` propagiert aus dem Service vor dem `try` in `ProcessSourceAsync` (`Download`-Arbeitsverzeichnis), aus `ValidateAsync` (im `Validating`-Status → `ValidationFailed`) oder eingepackt in `PluginPackageInstallException` (`InstallFailed`) — in allen Fällen trägt `ex.Message` die Konstante `WorkDirectoryUnavailable`.
7. `MarkSourceFailureAsync` schreibt die Konstante in `PluginSource.LastError`; `PluginUpdateCheckResult.Failed` liefert sie an die UI; `MapCheckError` mappt sie auf `Localizer["PluginCheckWorkDirectoryUnavailable"]` → lokalisierte Alert-Meldung; die technische Exception bleibt im `ILogger`-Log mit Stacktrace.
8. `finally`-Cleanup (`TryDeleteDirectory` für `workRoot`/`ExtractedRoot`, Backup-Aufräumung im Installer) bleibt unverändert.

Beteiligte Klassen/Komponenten: `PluginUpdateService`, `PluginPackageValidator`, `PluginPackageInstaller`, `IPluginWorkspaceProvider`, `PluginSettingsService`, `PluginSettings.razor`

### Automatische Startprüfung

`PluginUpdateHostedService.StartAsync` → `CheckForUpdatesAsync` → gleicher `ProcessSourceAsync`-Pfad wie oben. Bei Workspace-Fehlschlag landet die Konstante in `PluginSource.LastError` und die Exception im Startlog (`PluginUpdateHostedService` fängt nur zum Loggen) — die Anwendung startet trotzdem.

Beteiligte Klassen/Komponenten: `PluginUpdateHostedService`, `PluginUpdateService`, `IPluginWorkspaceProvider`

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `IPluginWorkspaceProvider` | Interface (`Rezepte.Web/Services/Import/Plugins/`) | Abstraktion für die Bereitstellung temporärer Arbeitsverzeichnisse; `string CreateWorkDirectory(string purpose)` |
| `PluginWorkspaceProvider` | Klasse (Singleton) | Implementierung mit Kandidatenliste (Konfiguration → System-Temp → `{ContentRoot}/plugins/.work`), Beschreibbarkeitsprobe per `Directory.CreateDirectory`, Warnlog pro gescheitertem Kandidaten; optionaler `Func<string>?`-Konstruktorparameter für die System-Temp-Auflösung als Testnaht |
| `PluginWorkspaceException` | Exception (sealed) | Signalisiert, dass kein Arbeitsverzeichnis bereitgestellt werden konnte; `Message` ist `PluginUpdateCheckErrors.WorkDirectoryUnavailable`, sodass bestehende `ex.Message`-Kanäle die fachliche Konstante transportieren |

## Änderungen an bestehenden Klassen

### `PluginUpdateCheckErrors` (Konstantenklasse)

- **Neue Konstante:** `WorkDirectoryUnavailable` = `"A temporary work directory could not be created."` — fachlicher Fehler bei nicht bereitstellbarem Arbeitsverzeichnis; wird von `MapCheckError` lokalisiert.

### `PluginUpdateOptions` (Konfigurationsklasse)

- **Neue Eigenschaft:** `WorkDirectory` (`string?`, Standard `null`) — optionaler Basis-Pfad für Plugin-Arbeitsverzeichnisse; hat Vorrang vor System-Temp und Fallback.

### `PluginUpdateService` (Service)

- **Konstruktor:** neuer Parameter `IPluginWorkspaceProvider workspaceProvider` (DI, scoped).
- **Geänderte Methoden:** `DownloadValidateInstallAsync` — Zeilen 171–172 werden durch `var workRoot = workspaceProvider.CreateWorkDirectory("rezepte-plugin-download");` ersetzt; der restliche Ablauf (Status-Fortschreibung, `finally`-Cleanup, `ex.Message`-Fehlerkanäle) bleibt unverändert.

### `PluginPackageValidator` (Service)

- **Konstruktor:** neuer Parameter `IPluginWorkspaceProvider workspaceProvider`.
- **Geänderte Methoden:** `ValidateAsync` — Zeilen 29–30 werden durch `var extractRoot = workspaceProvider.CreateWorkDirectory("rezepte-plugin-package");` ersetzt; `PluginWorkspaceException` propagiert bewusst (nicht vom `IOException`/`UnauthorizedAccessException`-Filter erfasst), damit der Aufrufer die fachliche Konstante erhält.

### `PluginPackageInstaller` (Service)

- **Konstruktor:** neuer Parameter `IPluginWorkspaceProvider workspaceProvider`.
- **Geänderte Methoden:** `BackupDirectory` — Zeile 245 wird durch `var backupPath = workspaceProvider.CreateWorkDirectory("rezepte-plugin-backup");` ersetzt; Fehler laufen wie gehabt durch den `catch` in `InstallWithReloadTrackingAsync` in eine `PluginPackageInstallException` (`InstallFailed`), deren `Message` die Konstante trägt.

### `PluginSettings` (Blazor-Komponente)

- **Geänderte Methoden:** `MapCheckError` — neuer `case PluginUpdateCheckErrors.WorkDirectoryUnavailable => Localizer["PluginCheckWorkDirectoryUnavailable"]` vor dem `not null`-Fallback.

### `UiStrings.resx` (Ressourcen)

- **Neuer Eintrag:** `PluginCheckWorkDirectoryUnavailable` — deutsch, z. B. „Das Arbeitsverzeichnis für die Plugin-Aktualisierung konnte nicht angelegt werden." (finaler Wortlaut bei Umsetzung).

### `ServiceCollectionExtensions` (DI-Registrierung)

- `services.AddSingleton<IPluginWorkspaceProvider, PluginWorkspaceProvider>();` im Plugin-Block (nahe den Zeilen 205–220).

### `GitHubStubServer` (Browser-Testinfrastruktur)

- **Neu:** Bedienung des Asset-Download-Pfads `/assets/{id}/download` mit Binär-Body (ZIP-Bytes), damit der Browser-E2E den vollständigen Download-→Install-Pfad fahren kann (heute nur `releases/latest`; Download endet in 404 — siehe Kommentar Zeile 62–64).

## Datenbankmigrationen

Keine.

## Validierungsregeln

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| `PluginUpdates:WorkDirectory` (Konfiguration) | Keine syntaktische Vorabvalidierung; leer/`null` = nicht gesetzt | Ungültiger oder nicht beschreibbarer Pfad gilt als gescheiterter Kandidat (Warnlog) und führt zum nächsten Kandidaten — kein separater Konfigurationsfehler |

## Konfigurationsänderungen

| Eintrag | Typ | Standardwert | Zweck |
|---------|-----|--------------|-------|
| `PluginUpdates:WorkDirectory` (`PluginUpdateOptions.WorkDirectory`) | `string?` | `null` | Optionales Basisverzeichnis für die temporären Plugin-Arbeitsverzeichnisse; hat Vorrang vor `Path.GetTempPath()` und dem `{ContentRoot}/plugins/.work`-Fallback. `appsettings.json` bleibt unverändert — der Schlüssel ist ein opt-in Escape-Hatch (auch per `PluginUpdates__WorkDirectory`-Umgebungsvariable setzbar). |

## Seiteneffekte und Risiken

- **Plugin-Discovery:** Der Fallback `{ContentRootPath}/plugins/.work` liegt innerhalb des von `PluginManager.GetPluginRoots()` durchsuchten Managed-Roots. Unbedenklich: `GetPluginAssemblyCandidates` prüft nur Top-Level-DLLs je direktem Unterverzeichnis, `.work` enthält nur Unterverzeichnisse (Arbeitsdaten ab Tiefe 2); `GetExcludedDirectoryNames`/`TryParseVersionedDirectoryName`/`UpdateInstallState` werten nur `@`-benannte direkte Kinder aus. Verifiziert per `PluginManagerTests.InitializeAsync_ShouldIgnoreWorkDirectory` (siehe `Tests` → „Neue Tests" und `Umsetzungsreihenfolge` Schritt 6), dass `.work` — auch mit Purpose/GUID-Resten und Plugin-DLLs ab Tiefe 2 — keinen Discovery-Eintrag erzeugt.
- **Speicherort der Backups:** Schlägt das System-Temp fehl, liegen Install-Backups unter dem Content-Root statt unter Temp — gleiches Laufwerk wie das Installationsziel (vorteilhaft fürs Restore) und bereits beschreibbarkeitsbewährt.
- **Verwaiste Arbeitsverzeichnisse:** Verhalten unverändert — `finally`-Cleanup bleibt; bei Prozessabbruch können Reste unter `.work` (statt nur Temp) liegen bleiben. Kein neuer Aufräum-Mechanismus geplant.
- **Konstruktor-Signaturänderungen** an `PluginUpdateService`, `PluginPackageValidator`, `PluginPackageInstaller` erfordern Anpassungen in den Test-Helpsites (`PluginUpdateServiceTestsBase.CreateSut`, `PluginPackageValidatorTests`, `PluginPackageInstallerTests.CreateInstaller`).
- **Startprüfung:** `PluginUpdateHostedService` nutzt denselben Pfad — mit der Korrektur schlägt sie nicht mehr mit roher Exception fehl; verbleibt nur der fachliche `LastError` + Logeintrag.
- **E2E-Fixture:** Redirect von `TMP`/`TEMP`/`TMPDIR` auf einen blockierten Pfad für den App-Prozess (Issue-Reproduktion) — Risiko, dass andere App-Teile Temp benötigen; vor Test-Finalisierung zu verifizieren, dass die App unter dieser Umgebung noch startet und die Settings-Seite bedienbar bleibt.
- **Statusspalte `LastError`:** zeigt die Konstante unlokalisiert — bewusster Ausschluss (siehe Scope-Ausschluss 4).

## Umsetzungsreihenfolge

1. **`PluginUpdateCheckErrors.WorkDirectoryUnavailable` + `PluginWorkspaceException` + `IPluginWorkspaceProvider` + `PluginWorkspaceProvider` + `PluginUpdateOptions.WorkDirectory` anlegen und DI-Registrierung**
   - Voraussetzungen: Keine — baut auf vorhandenen Typen (`IHostEnvironment`, `IOptions<PluginUpdateOptions>`, `ILogger`) auf.
   - Beschreibung: Konstante mit XML-Doc im bestehenden Muster; Exception-Klasse; Interface + Implementierung mit Kandidatenliste, optionaler `Func<string>`-Testnaht für `Path.GetTempPath()`; `WorkDirectory`-Eigenschaft in `PluginUpdateOptions`; Singleton-Registrierung in `ServiceCollectionExtensions`.

2. **`PluginUpdateService` umstellen**
   - Voraussetzungen: Schritt 1.
   - Beschreibung: Konstruktorparameter `IPluginWorkspaceProvider` ergänzen; `DownloadValidateInstallAsync` nutzt `CreateWorkDirectory("rezepte-plugin-download")`.

3. **`PluginPackageValidator` umstellen**
   - Voraussetzungen: Schritt 1.
   - Beschreibung: Konstruktorparameter ergänzen; `ValidateAsync` nutzt `CreateWorkDirectory("rezepte-plugin-package")`.

4. **`PluginPackageInstaller` umstellen**
   - Voraussetzungen: Schritt 1.
   - Beschreibung: Konstruktorparameter ergänzen; `BackupDirectory` nutzt `CreateWorkDirectory("rezepte-plugin-backup")`.

5. **UI-Lokalisierung: `MapCheckError`-Fall + `UiStrings.resx`-Eintrag**
   - Voraussetzungen: Schritt 1 (Konstante).
   - Beschreibung: Neuer `case` in `PluginSettings.razor`; deutscher `PluginCheckWorkDirectoryUnavailable`-Eintrag in `UiStrings.resx`.

6. **Unit-Tests: neue `PluginWorkspaceProviderTests` + Anpassung der Testbasen**
   - Voraussetzungen: Schritte 1–4; vorhandene Hilfsobjekte `TempWorkspace`/`TestHostEnvironment` (`PluginDirectoryRegistryTestsBase`), `TempDirectoryCleanup`.
   - Beschreibung: Provider-Tests (Konfig-Pfad, Temp-Default, Temp-Ausfall → Fallback, Totalausfall → `PluginWorkspaceException` mit Konstante, Zwischenebenen); `PluginUpdateServiceTestsBase.CreateSut` um Workspace-Provider-Fake erweitern; `PluginPackageValidatorTests`- und `PluginPackageInstallerTests.CreateInstaller`-Konstruktoraufrufe anpassen; `InstallAsync_ShouldRemovePartialBackup_WhenBackupCopyFails` auf den provider-basierten Backup-Root umstellen; Discovery-Verifikationstest `PluginManagerTests.InitializeAsync_ShouldIgnoreWorkDirectory` (Nachweis zum Seiteneffekt „Plugin-Discovery": `.work` erzeugt keinen Discovery-Eintrag) sowie `[InlineData(".work")]` in der bestehenden Theory `PluginDirectoryRegistryTests_Names.TryParseVersionedDirectoryName_ShouldReturnFalse_WhenNameIsNotVersioned`.

7. **Unit-Tests: Fehlerpropagation und fachliche Meldung**
   - Voraussetzungen: Schritt 6.
   - Beschreibung: `PluginUpdateServiceTests_CheckSource` — `Failed` mit `WorkDirectoryUnavailable` + `LastError` = Konstante; `PluginPackageValidatorTests` — `PluginWorkspaceException` propagiert; `PluginPackageInstallerTests` — Backup-Workspace-Fehler → `PluginPackageInstallException` (`InstallFailed`, Konstante); `PluginSettingsComponentTests` — `Failed(WorkDirectoryUnavailable)` → lokalisierte Alert-Meldung.

8. **Browser-E2E: Issue-Reproduktion und Fehlermeldung**
   - Voraussetzungen: Schritte 1–7; `GitHubStubServer` um Asset-Download-Endpunkt (ZIP-Bytes) erweitern; `Rezepte.Tests.Browser` erhält `ProjectReference` auf `Rezepte.Tests.PluginFixture` (für die echte Plugin-DLL im ZIP); neue App-Fixture mit `TMP`/`TEMP`/`TMPDIR`-Overrides auf blockierten Pfad.
   - Beschreibung: `PluginWorkspaceBrowserTests` — defektes System-Temp → Update-Check installiert Release erfolgreich über `.work`-Fallback; optional alle Kandidaten defekt → lokalisierte Fehlermeldung im Alert.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `CreateWorkDirectory_ShouldUseConfiguredWorkDirectory` | `PluginWorkspaceProviderTests` (neu) | Gesetzter `WorkDirectory`-Optionswert → Pfad liegt unter dem konfigurierten Verzeichnis |
| `CreateWorkDirectory_ShouldUseSystemTemp_WhenNoConfig` | `PluginWorkspaceProviderTests` | Ohne Konfiguration → Pfad beginnt unter `Path.GetTempPath()` |
| `CreateWorkDirectory_ShouldFallBackToPluginsWork_WhenTempFails` | `PluginWorkspaceProviderTests` | Testnaht `Func<string>` liefert blockierten Temp-Pfad → Pfad liegt unter `{ContentRoot}/plugins/.work` |
| `CreateWorkDirectory_ShouldThrowWorkspaceException_WhenAllCandidatesFail` | `PluginWorkspaceProviderTests` | Alle Kandidaten blockiert → `PluginWorkspaceException` mit `Message == PluginUpdateCheckErrors.WorkDirectoryUnavailable` |
| `CreateWorkDirectory_ShouldCreatePurposeAndGuidLevels` | `PluginWorkspaceProviderTests` | Rückgabepfad hat Struktur `<basis>/<purpose>/<guid>` und existiert |
| `InitializeAsync_ShouldIgnoreWorkDirectory` | `PluginManagerTests` | `{PluginRoot}/.work/<purpose>/<guid>/…` mit Purpose-/GUID-Resten und Fixture-Plugin-DLL ab Tiefe 2 → `InitializeAsync` erzeugt keinen `PluginSetting`-Eintrag aus `.work` (kein `loadfailed:`-/`incompatible:`-Deskriptor); verifiziert den Seiteneffekt „Plugin-Discovery" über `GetPluginRoots` → `GetPluginDirectories` → `GetPluginAssemblyCandidates` |
| `TryParseVersionedDirectoryName_ShouldReturnFalse_WhenNameIsNotVersioned` mit `[InlineData(".work")]` | `PluginDirectoryRegistryTests_Names` (bestehende Theory) | `.work` wird nicht als versionierter Verzeichnisname geparst — Registry-Ebene des Discovery-Nachweises |
| `FakeWorkspaceProvider` / `ThrowingWorkspaceProvider` | `PluginUpdateServiceTestsBase` (Hilfsfake) | Liefern ein reales Temp-Arbeitsverzeichnis bzw. werfen `PluginWorkspaceException` |
| `CheckSourceForUpdatesAsync_ShouldReturnWorkDirectoryUnavailable_WhenWorkspaceFails` | `PluginUpdateServiceTests_CheckSource` | `ThrowingWorkspaceProvider` → `Outcome == Failed`, `Error == PluginUpdateCheckErrors.WorkDirectoryUnavailable`, `source.LastError` = Konstante (nicht die rohe Exception) |
| `ValidateAsync_ShouldPropagateWorkspaceException_WhenWorkDirectoryUnavailable` | `PluginPackageValidatorTests` | Provider wirft → `ValidateAsync` lässt `PluginWorkspaceException` durch (wird nicht zu generischem Extraktionsfehler) |
| `InstallAsync_ShouldThrowInstallFailed_WhenBackupWorkspaceUnavailable` | `PluginPackageInstallerTests` | Registriertes Verzeichnis + Provider wirft im Backup → `PluginPackageInstallException` mit `Status == InstallFailed` und Konstante als `Message` |
| `CheckSourceAsync_ShowsLocalizedWorkDirectoryError` | `PluginSettingsComponentTests` | `Failed(PluginUpdateCheckErrors.WorkDirectoryUnavailable)` → `.alert-danger` enthält den lokalisierten deutschen Text (nicht die englische Konstante) |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `PluginUpdateServiceTestsBase.CreateSut` (und alle abgeleiteten Testklassen) | Neuer Konstruktorparameter `IPluginWorkspaceProvider` — Standard-Fake (reales Temp) bereitstellen, optionalen Parameter für `ThrowingWorkspaceProvider` ergänzen |
| `PluginPackageValidatorTests` (beide bestehenden Tests) | Neuer Konstruktorparameter — Provider-Fake auf `TempWorkspace`-Basis einsetzen |
| `PluginPackageInstallerTests.CreateInstaller` (alle Tests) | Neuer Konstruktorparameter — Provider-Fake auf dediziertem Workspace-Unterverzeichnis |
| `PluginPackageInstallerTests.InstallAsync_ShouldRemovePartialBackup_WhenBackupCopyFails` | Assert-Ziel `{Temp}/rezepte-plugin-backup` gilt nicht mehr — Orphan-Prüfung gegen den injizierten Provider-Backup-Root |

### E2E-Tests (primärer Funktionsnachweis)

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht | Defektes System-Temp (`TMP`/`TEMP`/`TMPDIR` des App-Prozesses zeigen auf einen Pfad unter einer Datei) → Klick „Auf Updates prüfen" → Download des echten `Rezepte.Tests.PluginFixture`-ZIPs vom erweiterten `GitHubStubServer` → Validierung → Installation → Alert „Version {tag} installiert." | `Rezepte.Tests.Browser/PluginWorkspaceBrowserTests.cs` (neu) + neue `PluginWorkspaceAppFixture` | AK 1 + AK 2 (Update funktioniert wieder; Arbeitsverzeichnisse ohne vorhandene Struktur/garantiert beschreibbares Basis) | Reproduziert Issue #239 real prozessübergreifend — `Path.GetTempPath()` des echten App-Prozesses ist aus Unit-Tests nicht manipulierbar |
| Soll | Alle Kandidaten defekt (zusätzlich `PluginUpdates__WorkDirectory` blockiert und `plugins` im Publish-Dir als Datei vorbelegt) → Alert zeigt lokalisierte Meldung statt roher Exception | gleiche Testklasse / Fixture | AK 3 | UI-sichtbarer Fehlerfall; Voraussetzung: verifizieren, dass die App mit `plugins` als Datei noch startet |
| Kann | `PluginUpdates__WorkDirectory` auf gültiges Verzeichnis gesetzt → Update-Check erfolgreich | gleiche Testklasse | AK 2 (konfigurierbare Variante) | Nachweis der konfigurierbaren Option end-to-end |

Vorab-Schritte für die E2E-Umsetzung: `GitHubStubServer` um Asset-Download mit Binär-Body erweitern; `Rezepte.Tests.Browser` → `ProjectReference` auf `Rezepte.Tests.PluginFixture`; Fixture erzeugt die Sperrdatei und setzt die Env-Overrides vor App-Start.

Anpassung bestehender E2E-Tests: Keine — die bestehenden `PluginSettingsBrowserTests` laufen unverändert weiter (Update-Pfad verhält sich bei intaktem Temp identisch).

## Akzeptanzszenarien

| # | Szenario | Auslöser | Ausgangszustand | Schritte | Sichtbare Wirkung | Nachweis |
|---|----------|----------|------------------|----------|-------------------|----------|
| 1 | Happy Path manuelle Prüfung | Admin klickt „Auf Updates prüfen" an aktivierter, vertrauter Quelle | Quelle vorhanden, kein Release-Datensatz, Temp beschreibbar; kein Arbeitsverzeichnis existiert | `CheckSourceForUpdatesAsync` → Provider legt `rezepte-plugin-download/<guid>` unter Temp an → Download → `rezepte-plugin-package/<guid>` → Validierung → `rezepte-plugin-backup/<guid>` bei Same-Tag → Install + Reload | Alert „Version {tag} installiert."; `PluginSourceRelease.Status == Installed`; Plugin wirksam ohne Neustart | E2E `PluginWorkspaceBrowserTests` + `PluginUpdateServiceTests_CheckSource` |
| 2 | System-Temp defekt → Fallback | Wie #1 | `Path.GetTempPath()` zeigt auf nicht anlegbares Verzeichnis; `{ContentRoot}/plugins` beschreibbar | Provider-Kandidat Temp scheitert (Warnlog) → `rezepte-plugin-*/<guid>` wird unter `plugins/.work` angelegt → Rest wie #1 | Gleiche sichtbare Wirkung wie #1 — keine Fehlermeldung, Update installiert | E2E (broken-TMP-Fixture) + `PluginWorkspaceProviderTests` |
| 3 | Kein Arbeitsverzeichnis bereitstellbar | Wie #1 | Konfig-Pfad, Temp und Content-Root alle nicht beschreibbar | `CreateWorkDirectory` wirft `PluginWorkspaceException` (Konstante) → `Failed`/`MarkSourceFailureAsync` → `MapCheckError` → `Localizer` | Alert mit lokalisierter Meldung (kein „Could not find file…"); Statusspalte `LastError` = Konstante; Exception mit Stacktrace im Log | `PluginUpdateServiceTests_CheckSource` + `PluginSettingsComponentTests` (+ Soll-E2E) |
| 4 | Startprüfung mit defektem Temp | Anwendungsstart mit aktivierter Quelle | Wie #2 | `PluginUpdateHostedService` → `CheckForUpdatesAsync` → gleicher Pfad wie #2/#3 | Kein App-Absturz; bei Totalausfall nur `LastError` + Logeintrag statt roher Exception-Meldung | `PluginUpdateServiceTests` (`CheckForUpdatesAsync`-Pfad) |
| 5 | Konfiguriertes Arbeitsverzeichnis | Wie #1 | `PluginUpdates:WorkDirectory` gesetzt | Provider nutzt konfigurierten Pfad als ersten Kandidaten | Arbeitsverzeichnisse liegen unter dem konfigurierten Pfad; Update erfolgreich | `PluginWorkspaceProviderTests` (+ Kann-E2E) |

## Offene Punkte

| # | Offener Punkt | Empfohlener Vorschlag |
|---|---------------|----------------------|
| 1 | Diagnose der betroffenen Installation (requirement „Offene Fragen" 1–5): Container/systemd-Umgebung, `/tmp`-Verfügbarkeit und Env-Variablen, vollständiger Stacktrace der Fehlermeldung, betroffene Release-Version, Reproduzierbarkeit (jede Quelle/jeder Klick vs. sporadisch) | Beim Issue-Melder nachfragen; **nicht umsetzungsblockierend** — die Korrektur (Kandidatenliste + Fallback + fachliche Fehlermeldung) deckt alle Varianten unabhängig von der exakten Fehlerstelle ab |
