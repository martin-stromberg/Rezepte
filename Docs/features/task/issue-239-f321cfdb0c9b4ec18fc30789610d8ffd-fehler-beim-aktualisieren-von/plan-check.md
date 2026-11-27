# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| AK 1: Update-Prüfung pro Quelle läuft ohne „Could not find file '/tmp/rezepte-plugin-download'" durch; Plugin-Aktualisierung funktioniert wieder | Zentrale Abstraktion `IPluginWorkspaceProvider`/`PluginWorkspaceProvider` mit Kandidatenliste (Schritt 1); Umstellung aller drei `Path.GetTempPath()`-Fundstellen in `PluginUpdateService` (Zeilen 171–172), `PluginPackageValidator` (Zeilen 29–30), `PluginPackageInstaller` (Zeile 245) (Schritte 2–4); DI-Registrierung in `ServiceCollectionExtensions` | Pflicht-E2E `PluginWorkspaceBrowserTests` (defektes System-Temp → echter Download via erweitertem `GitHubStubServer` → Validierung → Installation → Alert „Version {tag} installiert.") + `PluginUpdateServiceTests_CheckSource`-Happy-Path | Abgedeckt |
| AK 2: Arbeitsverzeichnisse ohne Annahme vorhandener Struktur oder konfigurierbares/garantiert beschreibbares Basisverzeichnis | `Directory.CreateDirectory(<kandidat>/<purpose>/<guid>)` legt alle Zwischenebenen und dient als Beschreibbarkeitsprobe; Kandidatenreihenfolge `PluginUpdates:WorkDirectory` (neu, `PluginUpdateOptions.WorkDirectory`) → `Path.GetTempPath()` → `{ContentRoot}/plugins/.work`; beide geforderten Varianten (konfigurierbar + garantiert beschreibbar) umgesetzt | `PluginWorkspaceProviderTests`: `..._ShouldUseConfiguredWorkDirectory`, `..._ShouldUseSystemTemp_WhenNoConfig`, `..._ShouldFallBackToPluginsWork_WhenTempFails`, `..._ShouldCreatePurposeAndGuidLevels` + Pflicht-E2E (Broken-TMP-Fixture) + Kann-E2E (`PluginUpdates__WorkDirectory` gesetzt) | Abgedeckt |
| AK 3: Bei nicht anlegbarem Arbeitsverzeichnis aussagekräftige, fachliche (lokalisierte) Fehlermeldung statt roher Laufzeit-Exception | `PluginWorkspaceException` (direkt von `Exception` abgeleitet — umgeht bewusst den `IOException`-Filter in `ValidateAsync`; `Message` = `PluginUpdateCheckErrors.WorkDirectoryUnavailable`); Transport über alle bestehenden `ex.Message`-Kanäle (`LastError`, `PluginSourceRelease.Error`, `Failed`, `PluginPackageInstallException`); `MapCheckError`-Fall + `UiStrings.resx`-Schlüssel `PluginCheckWorkDirectoryUnavailable` (Schritt 5); technische Exception bleibt mit Stacktrace im `ILogger`-Log | `CheckSourceForUpdatesAsync_ShouldReturnWorkDirectoryUnavailable_WhenWorkspaceFails`, `ValidateAsync_ShouldPropagateWorkspaceException_WhenWorkDirectoryUnavailable`, `InstallAsync_ShouldThrowInstallFailed_WhenBackupWorkspaceUnavailable`, `CheckSourceAsync_ShowsLocalizedWorkDirectoryError` (lokalisierter `.alert-danger`-Text) + Soll-E2E (alle Kandidaten defekt → lokalisierter Alert) | Abgedeckt |

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Manueller Klick „Auf Updates prüfen" mit defektem System-Temp (Issue-#239-Reproduktion) → Download/Validierung/Installation über `.work`-Fallback → Erfolgs-Alert (AK 1 + AK 2) | `PluginWorkspaceBrowserTests` (neu) + `PluginWorkspaceAppFixture` mit `TMP`/`TEMP`/`TMPDIR`-Overrides; Pflicht; Vorab-Schritte benannt (`GitHubStubServer`-Asset-Endpunkt mit Binär-Body, `ProjectReference` auf `Rezepte.Tests.PluginFixture`, Fixture erzeugt Sperrdatei vor App-Start) | Abgedeckt |
| Alle Kandidaten defekt → lokalisierter Alert statt roher Exception (AK 3) | Gleiche Testklasse/Fixture, Priorität Soll; ergänzend bUnit-Test `CheckSourceAsync_ShowsLocalizedWorkDirectoryError` | Abgedeckt |
| `PluginUpdates__WorkDirectory` gesetzt → Update-Check erfolgreich (AK 2, konfigurierbare Variante) | Kann-E2E in derselben Testklasse | Abgedeckt |
| Automatische Startprüfung bei defektem Temp (`PluginUpdateHostedService` → `CheckForUpdatesAsync`) | Kein eigener E2E; Nachweis unit-seitig über den identischen `ProcessSourceAsync`-Pfad in `PluginUpdateServiceTests`; Wirkung (kein Absturz, `LastError` + Log) in Akzeptanzszenario 4 beschrieben | Nicht erforderlich mit Begründung: kein unmittelbar UI-beobachtbarer Nutzerfluss (läuft beim App-Start ohne Nutzerinteraktion); identischer Codepfad bereits E2E-abgedeckt |

## Hinweise

- Die im früheren Check (`plan-check.1.md`) festgestellte einzige Lücke ist geschlossen: Der Discovery-Verifikationstest `PluginManagerTests.InitializeAsync_ShouldIgnoreWorkDirectory` (inkl. `[InlineData(".work")]` in `PluginDirectoryRegistryTests_Names.TryParseVersionedDirectoryName_ShouldReturnFalse_WhenNameIsNotVersioned`) ist jetzt sowohl im Abschnitt `Tests` als auch in der `Umsetzungsreihenfolge` (Schritt 6) verankert.
- `Scope-Klärung` ist vollständig gefüllt (Verarbeitungseinheit, auslösende Ereignisse, erwartete Wirkung, Umgang mit fehlenden Objekten); alle vier Scope-Ausschlüsse tragen Begründung und Status `bestätigt`.
- `Akzeptanzszenarien` enthält mit Szenario 1 einen vollständigen Happy Path vom Admin-Klick bis zur sichtbaren Wirkung und mit Szenario 3 den Fall nicht bereitstellbarer Objekte; Ausgangszustände sind je Szenario benannt.
- Betroffene bestehende Tests sind vollständig benannt (`PluginUpdateServiceTestsBase.CreateSut` inkl. aller abgeleiteten Klassen, `PluginPackageValidatorTests`, `PluginPackageInstallerTests.CreateInstaller`, `InstallAsync_ShouldRemovePartialBackup_WhenBackupCopyFails`); benötigte Testdaten/Fixtures/Hilfsmethoden (`TempWorkspace`, `TestHostEnvironment`, `TempDirectoryCleanup`, `FakeWorkspaceProvider`/`ThrowingWorkspaceProvider`) sind genannt.
- Der Soll-E2E für den Totalausfall aller Kandidaten ist priorisiert, nicht Pflicht. Fiele er aus, bliebe AK 3 über den bUnit-Test `PluginSettingsComponentTests` abgedeckt — der UI-sichtbare Fehlerfall wäre dann aber nicht end-to-end belegt; Priorisierung bei der Umsetzung beachten.
- Die Vorab-Verifikation, dass die App mit blockiertem `TMP`/`TEMP`/`TMPDIR` (und ggf. `plugins` als Datei) noch startet und die Settings-Seite bedienbar bleibt, ist als Risiko benannt; schlägt sie fehl, muss die E2E-Fixture nachgeplant werden.
- Offener Punkt 1 (Umgebungsdiagnose beim Melder, requirement „Offene Fragen" 1–5) ist ausdrücklich nicht umsetzungsblockierend und bereits als Issue-Kommentar gestellt — keine Lücke.
- Scope-Ausschluss 4 (unlokalisierte Konstante in der `LastError`-Statusspalte) ist mit Bestandskonvention begründet und bestätigt — konsistent zur Anforderung, da die fachliche Meldung über den Alert lokalisiert wird.
