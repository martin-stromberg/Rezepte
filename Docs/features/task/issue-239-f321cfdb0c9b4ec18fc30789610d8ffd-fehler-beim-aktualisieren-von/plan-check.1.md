# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan lückenhaft

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| AK 1: Update-Prüfung pro Quelle läuft ohne „Could not find file '/tmp/rezepte-plugin-download'" durch; Plugin-Aktualisierung funktioniert wieder | Zentrale Abstraktion `IPluginWorkspaceProvider`/`PluginWorkspaceProvider` mit Kandidatenliste (Schritt 1); Umstellung aller drei `Path.GetTempPath()`-Fundstellen in `PluginUpdateService`, `PluginPackageValidator`, `PluginPackageInstaller` (Schritte 2–4) | Pflicht-E2E `PluginWorkspaceBrowserTests` (defektes System-Temp → Download → Validierung → Installation → Alert „Version {tag} installiert.") + `PluginUpdateServiceTests_CheckSource` | Abgedeckt |
| AK 2: Arbeitsverzeichnisse ohne Annahme vorhandener Struktur oder konfigurierbares/garantiert beschreibbares Basisverzeichnis | `Directory.CreateDirectory(<kandidat>/<purpose>/<guid>)` legt alle Zwischenebenen an; Kandidatenreihenfolge `PluginUpdates:WorkDirectory` → `Path.GetTempPath()` → `{ContentRoot}/plugins/.work`; beide geforderten Varianten (konfigurierbar + garantiert beschreibbar) umgesetzt | `PluginWorkspaceProviderTests`: `..._ShouldUseConfiguredWorkDirectory`, `..._ShouldUseSystemTemp_WhenNoConfig`, `..._ShouldFallBackToPluginsWork_WhenTempFails`, `..._ShouldCreatePurposeAndGuidLevels` + E2E (Broken-TMP-Fixture) + Kann-E2E (konfigurierter Pfad) | Abgedeckt |
| AK 3: Bei nicht anlegbarem Arbeitsverzeichnis aussagekräftige, fachliche (lokalisierte) Fehlermeldung statt roher Laufzeit-Exception | `PluginWorkspaceException` (direkt von `Exception`, Message = `PluginUpdateCheckErrors.WorkDirectoryUnavailable`); Transport über bestehende `ex.Message`-Kanäle; `MapCheckError`-Fall + `UiStrings.resx`-Schlüssel `PluginCheckWorkDirectoryUnavailable` (Schritt 5); Exception bleibt im `ILogger`-Log | `CheckSourceForUpdatesAsync_ShouldReturnWorkDirectoryUnavailable_WhenWorkspaceFails`, `ValidateAsync_ShouldPropagateWorkspaceException_WhenWorkDirectoryUnavailable`, `InstallAsync_ShouldThrowInstallFailed_WhenBackupWorkspaceUnavailable`, `CheckSourceAsync_ShowsLocalizedWorkDirectoryError` + Soll-E2E (alle Kandidaten defekt → lokalisierter Alert) | Abgedeckt |

## Fehlende oder unvollständige Testanforderungen

- [ ] Der Plan fordert in „Seiteneffekte und Risiken" (plan.md, Abschnitt Plugin-Discovery) ausdrücklich: „Zu verifizieren per Test, dass `.work` keinen Discovery-Eintrag erzeugt." Dieser Nachweis ist jedoch **weder im Abschnitt `Tests` als konkreter Test benannt noch in der `Umsetzungsreihenfolge` als Schritt eingeplant**. Es fehlt z. B. ein Test, der prüft, dass `{ContentRoot}/plugins/.work` keinen Eintrag über `PluginManager.GetPluginRoots()`/`GetPluginAssemblyCandidates`/`PluginDirectoryRegistry.GetExcludedDirectoryNames`/`TryParseVersionedDirectoryName` erzeugt (Bezug: Seiteneffekt „Plugin-Discovery"; sichert indirekt AK 1 ab, da ein Discovery-Eintrag von `.work` den Installations-/Reload-Pfad beeinträchtigen könnte).

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Manueller Klick „Auf Updates prüfen" mit defektem System-Temp (Issue-#239-Reproduktion) → Download/Validierung/Installation über `.work`-Fallback → Erfolgs-Alert (AK 1 + AK 2) | `PluginWorkspaceBrowserTests` (neu) + `PluginWorkspaceAppFixture` mit `TMP`/`TEMP`/`TMPDIR`-Overrides; Pflicht; Vorab-Schritte benannt (`GitHubStubServer`-Asset-Endpoint, `ProjectReference` auf `Rezepte.Tests.PluginFixture`) | Abgedeckt |
| Alle Kandidaten defekt → lokalisierter Alert statt roher Exception (AK 3) | Gleiche Testklasse/Fixture, Priorität Soll; ergänzend bUnit-Test `CheckSourceAsync_ShowsLocalizedWorkDirectoryError` | Abgedeckt |
| `PluginUpdates__WorkDirectory` gesetzt → Update-Check erfolgreich (AK 2, konfigurierbare Variante) | Kann-E2E in derselben Testklasse | Abgedeckt |
| Automatische Startprüfung bei defektem Temp (`PluginUpdateHostedService`) | Kein eigener E2E; Nachweis unit-seitig über den `CheckForUpdatesAsync`-Pfad in `PluginUpdateServiceTests` | Nicht erforderlich mit Begründung: kein unmittelbar UI-beobachtbarer Nutzerfluss (läuft beim App-Start ohne Nutzerinteraktion); Wirkung (kein Absturz, fachlicher `LastError` + Log) ist über Unittests und die identische Codepfad-Abdeckung belegt |

## Fehlende oder unvollständige Planbestandteile

- [ ] Verankerung des in den Risiken angekündigten Discovery-Verifikationstests für `{ContentRoot}/plugins/.work` in `Umsetzungsreihenfolge` und `Tests` (einzige festgestellte Lücke; siehe oben).

## Hinweise

- Der Soll-E2E für den Totalausfall aller Kandidaten ist priorisiert, nicht Pflicht. Fiele er aus, bliebe AK 3 über den bUnit-Test `PluginSettingsComponentTests` abgedeckt — der UI-sichtbare Fehlerfall wäre dann aber nicht end-to-end belegt; Priorisierung bei der Umsetzung beachten.
- Die Vorab-Verifikation, dass die App mit blockiertem `TMP`/`TEMP`/`TMPDIR` (und ggf. `plugins` als Datei) noch startet und die Settings-Seite bedienbar bleibt, ist als Risiko benannt; schlägt sie fehl, muss die E2E-Fixture nachgeplant werden.
- Offener Punkt 1 (Umgebungsdiagnose beim Melder) ist ausdrücklich nicht umsetzungsblockierend und bereits als Issue-Kommentar gestellt — keine Lücke.
- Scope-Ausschluss 4 (unlokalisierte Konstante in der `LastError`-Statusspalte) ist mit Bestandskonvention begründet und bestätigt — konsistent zur Anforderung, da die fachliche Meldung über den Alert lokalisiert wird.
