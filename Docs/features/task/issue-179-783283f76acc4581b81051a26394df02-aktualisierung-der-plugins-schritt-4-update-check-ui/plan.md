# Umsetzungsplan: Update-Prüfung je Pluginquelle in der Pluginverwaltung

## Übersicht

Die Admin-Komponente `PluginSettings.razor` (`/settings` → „Plugins") erhält einen manuellen Update-Check je GitHub-Pluginquelle über den bereits vorhandenen `IPluginSettingsService.CheckSourceForUpdatesAsync`. Die Quellen-Aktionen werden zu einheitlichen Icon-Buttons mit lokalisierten Tooltips/`aria-label`s, ein lokalisierter Erklärtext beschreibt das Prüfprinzip, und das Ergebnis wird inline als Alert gemeldet. Alle hartkodierten deutschen Strings der angefassten Tabelle werden mitlokalisiert (Pre-Commit-Hook `razor-l10n-check.py` erzwingt dies für die gesamte Datei).

## Scope-Klärung

| Feld | Festlegung |
|------|------------|
| Verarbeitungseinheit | Eine einzelne `PluginSource` plus der manuell ausgelöste Check-Lauf |
| Auslösende Ereignisse | Klick auf „Auf Updates prüfen" in der Aktions-Spalte einer Quellenzeile → `IPluginSettingsService.CheckSourceForUpdatesAsync(source.Id)` |
| Erwartete Wirkung | Inline-Alert mit Ergebnis (`NoUpdate`/`Installed`/`Failed`), Status-Spalte (`LastCheckedAt`, `LastSuccessfulReleaseTag`, `LastError`/`LastErrorAt`) aktualisiert sich durch Reload der Quellenliste |
| Umgang mit fehlenden Objekten | Keine neuen fachlichen Objekte. `PluginSourceSettingsItem` liefert alle Statusfelder. Fehlende Voraussetzung (`!Enabled`/`!TrustConfirmed`) → Button deaktiviert + Tooltip-Begründung (kein Klick möglich) |

### Scope-Ausschlüsse

| # | Einschränkung / Ausschluss | Begründung | Status |
|---|----------------------------|------------|--------|
| 1 | Kein „Alle Quellen prüfen"-Button | Anforderung verlangt Check je Quelle | offen |
| 2 | Keine Änderung an Start-Prüfung / keine periodische Prüfung | `PluginUpdateHostedService` bleibt unverändert | offen |
| 3 | Kein Toast-/Benachrichtigungssystem | Ergebnisrückmeldung inline im Alert-Muster (`ApplicationUpdates.razor`) | offen |
| 4 | Keine UI für „vermisste Plugins entfernen" | Ist Schritt 5 des Issues | offen |
| 5 | Busy-Anzeige nur für den selbst ausgelösten Lauf | Kein serverseitiger Lauf-Status abfragbar; fremd ausgelöste Checks warten am `CheckGate` — Projektentscheidung | offen |

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Busy-Zustand | `isCheckingSourceId` (`string?`) — Id der Quelle, deren eigener Klick läuft | Kein „läuft"-Status im Interface; nur der selbst ausgelöste Lauf ist darstellbar. Serverseitige Parallelität regelt `CheckGate` ohne UI-Feedback |
| Ergebnis-Feedback | `message` + `messageClass` (`alert alert-success|danger py-2`) inline über den Sektionen — Muster `ApplicationUpdates.razor` | Einziges Feedback-Muster im Repo; kein Toast-System |
| Alert-Platzierung | Alert **innerhalb** des Haupt-`else`-Zweigs über den Sektionen | Aktuell blendet `else if (message)` den kompletten Inhalt aus — bei Check-Ergebnissen muss die Tabelle sichtbar bleiben |
| Fehler-Mapping | `PluginUpdateCheckResult.Error` (englisch) per exaktem String-Vergleich auf bekannte Vorbedingungs-Fehler mappen; unbekannte Fehler als Detail-Text an die lokalisierte `Failed`-Meldung anhängen | Serverseitige `Error`-Strings sind Englisch; UI-Texte müssen lokalisiert sein |
| Aktions-Buttons | `btn-group btn-group-sm` mit drei Icon-Buttons: `bi-arrow-repeat` (prüfen), `bi-pencil` (edit), `bi-trash` (delete); `title` + `aria-label` lokalisiert; Busy → `spinner-border spinner-border-sm` statt Icon | Bootstrap Icons 1.11.3 bereits via CDN in `App.razor` eingebunden; `btn-group`-Muster aus der Plugin-Reihenfolge-Spalte derselben Datei |
| Deaktivierungs-Begründung | `title`-Tooltip zeigt den Grund (`PluginCheckSourceDisabled` / `PluginCheckSourceNotTrusted`) | Blockierte Aktion muss erklärt werden — Pflicht laut Plan-Regeln |

## Programmabläufe

### Manueller Update-Check je Quelle

1. Klick auf den Prüf-Button einer Zeile → `CheckSourceAsync(source)` (`@code`).
2. Guard: Abbruch wenn `isSaving`, `isCheckingSourceId != null`, `!source.Enabled` oder `!source.TrustConfirmed` (doppelt abgesichert — Button ist dann ohnehin `disabled`).
3. `isCheckingSourceId = source.Id`, `message = null` → Button zeigt `spinner-border`, alle Buttons der Zeile `disabled`.
4. `PluginSettingsService.CheckSourceForUpdatesAsync(source.Id)` → `PluginUpdateCheckResult`.
5. Ergebnis-Mapping (nach dem Reload gesetzt, s. Schritt 7):
   - `PluginUpdateCheckOutcome.NoUpdate` → `messageClass = "alert alert-success py-2"`, `message = Localizer["PluginCheckNoUpdate"]`
   - `Installed` → `alert-success`, `message = Localizer["PluginCheckInstalled", result.ReleaseTag]`
   - `Failed` → `alert-danger`, `message = MapCheckError(result.Error)` — bekannte Fehlertexte auf lokalisierte Keys, sonst `PluginCheckFailed` + `Error` als Detail in Klammern.
6. `catch` → `alert-danger` + `PluginCheckFailed` + `ex.Message`.
7. `finally` → `isCheckingSourceId = null`, `await LoadAsync()` (Status-Spalte aktualisiert), **danach** `message`/`messageClass` setzen — `LoadAsync` setzt `message = null` am Anfang, Ergebnis muss erst nach dem Reload zugewiesen werden.

Beteiligte Klassen/Komponenten: `PluginSettings` (Razor), `IPluginSettingsService`, `PluginUpdateCheckResult`, `PluginUpdateCheckOutcome`, `IStringLocalizer<UiStrings>`.

### Deaktivierte Quellenaktion

1. Zeile rendert Prüf-Button `disabled` wenn `isSaving || isCheckingSourceId != null || !source.Enabled || !source.TrustConfirmed`.
2. `title`/`aria-label` via Helper `CheckButtonTitle(source)`: `!Enabled` → `Localizer["PluginCheckSourceDisabled"]`, `!TrustConfirmed` → `Localizer["PluginCheckSourceNotTrusted"]`, sonst → `Localizer["CheckForUpdates"]`.
3. Gleiche Sperrlogik für Edit-/Delete-Buttons der Zeile während `isCheckingSourceId == source.Id` (bzw. generell während laufendem Check gesperrt, da Reload die Liste ersetzt).

Beteiligte: `PluginSettings` (Razor-Markup + `CheckButtonTitle`-Helper).

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `GitHubStubServer` | Test-Hilfsklasse (`Rezepte.Tests.Browser/Infrastructure`) | `HttpListener`-Stub für `GET /repos/{o}/{r}/releases/latest` — liefert konfigurierbares 404 oder Release-JSON, damit Browser-Tests ohne externes GitHub laufen |
| `PluginSettingsAppFixture` | Test-Fixture (`Rezepte.Tests.Browser/Infrastructure`) | `RezepteAppFixture`-Subklasse; `GetEnvironmentOverrides` setzt `PluginUpdates__GitHubApiBaseUrl` auf den Stub; bietet `SeedInstalledReleaseAsync` (direkter SQLite-Insert in `PluginSourceReleases` über `DatabasePath`) |
| `PluginSettingsTestCollection` | xUnit-Collection-Definition | Analog `RegisterTestCollection`; eigene Fixture-Instanz wegen Env-Override |
| `PluginSettingsPageObject` | Page Object | Login, `/settings`, „Plugins"-Tab, Quelle anlegen/aktivieren, Prüf-Button lokalisieren/klicken, Alert und Status-Spalte abfragen — Vorbild `SecurityTxtPageObject` |

## Änderungen an bestehenden Klassen

### `PluginSettings` (Razor-Komponente, `Rezepte.Web/Components/Settings/PluginSettings.razor`)

- **Neue Felder:** `isCheckingSourceId` (`string?`) — Id der laufenden eigenen Prüfung; `messageClass` (`string`, Default `"alert alert-danger py-2"`) — Alert-Klasse wie `ApplicationUpdates.razor`.
- **Neue Methoden:**
  - `CheckSourceAsync(PluginSourceSettingsItem source)` — Click-Handler gemäß Programmablauf.
  - `CheckButtonTitle(PluginSourceSettingsItem source)` → `string` — lokalisierter Tooltip-Text (Grund oder `CheckForUpdates`).
  - `MapCheckError(string? error)` → `string` — englische `Error`-Strings → lokalisierte Meldung; unbekannt → `PluginCheckFailed` + Detail.
- **Geänderte Methoden:**
  - `LoadAsync` — unveränderte Struktur, aber `message`-Reset bleibt am Anfang; `CheckSourceAsync` setzt die Ergebnismeldung **nach** `LoadAsync()`.
  - `SaveSourceAsync`, `SetSourceEnabledAsync`, `DeleteSourceAsync`, `SetEnabledAsync`, `MoveAsync`, `LoadAsync` — Fehlertexte von hartkodiert Deutsch auf `Localizer`-Keys umstellen (Nutzer-facing).
- **Markup-Änderungen:**
  - Alert-Div von `else if (message)` (blendet alles aus) in den Haupt-`else`-Zweig verschieben: `@if (!string.IsNullOrWhiteSpace(message)) { <div class="@messageClass">@message</div> }` über den Sektionen.
  - Erklärtext `@Localizer["PluginSourcesInfo"]` als `text-muted small` unter der Quellen-Überschrift.
  - Status-Zelle: `LastErrorAt` ergänzen (`text-muted small`, `Localizer["SourceLastErrorAt", datum]`).
  - Aktions-Zelle: `btn-group btn-group-sm` mit Icon-Buttons (Prüfen: `bi-arrow-repeat` / Busy: `spinner-border spinner-border-sm`; Edit: `bi-pencil`; Delete: `bi-trash`), `title` + `aria-label` lokalisiert, `disabled`-Logik oben.
  - Alle hartkodierten Strings der Datei lokalisieren (Hook erzwingt das für die gestagte Datei): Überschriften, Labels („Repository-URL", „Privat", „Aktiv"), „Speichern"/„Abbrechen", Tabellenköpfe, „Privat/Öffentlich", „Secret:", „Geprüft:", Switch-`aria-label`s, „Lade...", Plugin-Tabellen-Beschriftungen.

### `UiStrings.resx` (`Rezepte.Web/Resources/UiStrings.resx`)

Neue Schlüssel (deutsche Werte, einzige Resx im Repo):

| Schlüssel | Wert | Verwendung |
|-----------|------|------------|
| `PluginSourcesInfo` | `Pluginquellen werden beim App-Start automatisch auf Updates geprüft (nur aktivierte und vertrauenswürdige Quellen). Jede Quelle kann zusätzlich manuell geprüft werden.` | Erklärtext über/unter Quellen-Tabelle |
| `CheckForUpdates` | `Auf Updates prüfen` | Prüf-Button `title`/`aria-label` |
| `PluginCheckNoUpdate` | `Kein Update gefunden.` | Ergebnis `NoUpdate` |
| `PluginCheckInstalled` | `Version {0} installiert.` | Ergebnis `Installed` |
| `PluginCheckFailed` | `Prüfung fehlgeschlagen.` | Generischer Fehler (+ `Error`-Detail) |
| `PluginCheckSourceNotFound` | `Quelle wurde nicht gefunden.` | Mapping `"Plugin source was not found."` |
| `PluginCheckSourceDisabled` | `Quelle ist deaktiviert.` | Mapping + Tooltip-Grund |
| `PluginCheckSourceNotTrusted` | `Vertrauen ist nicht bestätigt.` | Mapping + Tooltip-Grund |
| `PluginCheckNoPat` | `Für die private Quelle ist kein PAT hinterlegt.` | Mapping `"Private source has no stored PAT."` |
| `GitHubPluginSources` | `GitHub-Pluginquellen` | Überschrift |
| `RepositoryUrl` | `Repository-URL` | Form-Label |
| `Private` | `Privat` | Checkbox-Label + Badge |
| `Public` | `Öffentlich` | Badge |
| `Active` | `Aktiv` | Checkbox-Label + Tabellenkopf |
| `Name`, `Assembly` | `Name`, `Assembly` | Tabellenköpfe Plugin-Liste |
| `Source` | `Quelle` | Tabellenkopf |
| `Status` | `Status` | Tabellenkopf |
| `Actions` | `Aktionen` | Tabellenkopf |
| `SecretStored` | `Secret: hinterlegt` | Quellen-Statuszeile |
| `SecretMissing` | `Secret: nicht hinterlegt` | Quellen-Statuszeile |
| `SourceLastErrorAt` | `Letzter Fehler: {0}` | Status-Zelle |
| `EnableSourceFor` | `Quelle {0} aktivieren` | Switch `aria-label` |
| `EnablePluginFor` | `Plugin {0} aktivieren` | Switch `aria-label` |
| `Loading` | `Laden…` | Ladeanzeige |
| `PluginsLoadFailed` | `Plugins konnten nicht geladen werden: {0}` | `LoadAsync`-Fehler |
| `SourceSaveFailed` | `Quelle konnte nicht gespeichert werden: {0}` | Save/Toggle-Fehler |
| `SourceDeleteFailed` | `Quelle konnte nicht gelöscht werden: {0}` | Delete-Fehler |
| `PluginSaveFailed` | `Plugin konnte nicht gespeichert werden: {0}` | Toggle-Fehler |
| `OrderSaveFailed` | `Reihenfolge konnte nicht gespeichert werden: {0}` | Move-Fehler |

Wiederverwendete vorhandene Schlüssel: `Edit`, `Delete`, `Save`, `CheckNow` (nur falls Textbutton nötig — Icon-Button nutzt `CheckForUpdates`), `LastChecked` (Status-Zeile „Geprüft"), `UpdatePat`, `TrustThisSource`, `NoGitHubPluginSources`, `NotUsable`, `PluginOrder`, `MoveUp`, `MoveDown`, `NoImportPluginsFound`, `LocalImportPlugins`.

### `RezepteAppFixture` (`Rezepte.Tests.Browser/Infrastructure/RezepteAppFixture.cs`)

- **`RezepteAppFixture.DatabasePath` ist bereits `public`** (`RezepteAppFixture.cs:47`) — keine Fixture-Änderung nötig; `PluginSettingsAppFixture` kann per `Microsoft.Data.Sqlite` (transitiv über EF-Core-Sqlite-Referenz vorhanden) Zeilen in `PluginSourceReleases` einfügen.

## Datenbankmigrationen

Keine.

## Validierungsregeln

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| Prüf-Button (UI) | Klick nur wenn `Enabled && TrustConfirmed` und kein Check/Save läuft | Button `disabled` + Tooltip-Begründung — kein Request möglich |
| `CheckSourceForUpdatesAsync` (Server) | `EnsureAdmin` + `Enabled`/`TrustConfirmed`-Vorbedingung | `UnauthorizedAccessException` bzw. `Failed`-Result — bereits vorhanden |

## Konfigurationsänderungen

Keine produktiven Änderungen. Testseitig: `PluginUpdates__GitHubApiBaseUrl` wird in der Test-Fixture per Environment-Override auf den lokalen Stub gesetzt (Option existiert bereits: `PluginUpdateOptions.GitHubApiBaseUrl`).

## Seiteneffekte und Risiken

- **Alert-Umbau:** Das Verschieben des Alert-Divs ändert die Anzeige bestehender Fehlerfälle (`LoadAsync`-Fehler zeigt Alert + leere Tabellen statt nur Alert). Gewollt und konsistent mit `ApplicationUpdates.razor`.
- **Lokalisierungspflicht:** Sobald `PluginSettings.razor` gestaged ist, prüft `razor-l10n-check.py` die **gesamte** Datei — sämtliche hartkodierten Strings (auch in der unveränderten Plugin-Tabelle und den `@code`-Meldungen) müssen mitlokalisiert werden, sonst schlägt der Commit fehl.
- **`plugin.json`/kein bUnit-Auth nötig:** `PluginSettings` prüft selbst keine Authentifizierung (Admin-Guard liegt im Service) — bUnit-Tests rendern ohne `Authorize()`.
- **Browser-Test-Netzwerk:** Ohne Stub würde der Check `api.github.com` aufrufen — der `Failed`-Pfad „kein PAT" (`IsPrivate`-Quelle ohne Secret) ist deterministisch ohne Netzwerk; der `NoUpdate(alreadyInstalled)`-Pfad braucht Stub + DB-Seed.

## Umsetzungsreihenfolge

1. **resx-Schlüssel anlegen**
   - Voraussetzungen: keine.
   - Alle neuen Schlüssel aus der Tabelle oben in `UiStrings.resx` ergänzen.

2. **`PluginSettings.razor` umbauen**
   - Voraussetzungen: resx-Keys (Schritt 1).
   - Alert-Restrukturierung, Erklärtext, Icon-Buttons + Busy/`disabled`-Logik, `CheckSourceAsync` + `MapCheckError` + `CheckButtonTitle`, `LastErrorAt` in Status-Zelle, vollständige Lokalisierung der Datei.

3. **bUnit-Komponententests `PluginSettingsComponentTests`**
   - Voraussetzungen: `bunit` 1.40.0 + `Moq` sind bereits Paket-Referenzen in `Rezepte.Tests`; Muster `Rezepte.Tests/Components/RecipeSearchComponentTests.cs` (`TestContext`, `AddLocalization`, Service-Mock-Registrierung).
   - Tests siehe Abschnitt „Neue Tests".

4. **Browser-Test-Infrastruktur**
   - Voraussetzungen: `DatabasePath` bereits public — keine Fixture-Änderung; `HttpListener` (BCL); `Microsoft.Data.Sqlite` transitiv verfügbar; Hinweis: `HttpListener` auf Windows braucht ggf. URL-ACL — Fallback auf belegten freien Port prüfen, sonst Test skippen.
   - `GitHubStubServer`, `PluginSettingsAppFixture`, `PluginSettingsTestCollection`, `PluginSettingsPageObject` anlegen — Muster `SecurityTxtPageObject`/`RegisterTestsAppFixture`.

5. **Browser-Tests `PluginSettingsBrowserTests`**
   - Voraussetzungen: Schritt 4 + veröffentlichte App (`PublishRezepteWebForBrowserTests`-Target existiert).
   - Szenarien siehe E2E-Tabelle.

6. **Build + Tests + Hook-Lauf**
   - `dotnet build` → `dotnet test Rezepte.Tests` → Browser-Suite (Release, Chromium) → Pre-Commit-Hooks laufen beim Commit.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `InfoText_IsRendered` | `PluginSettingsComponentTests` (bUnit) | Erklärtext-Key erscheint über der Quellen-Tabelle |
| `CheckButton_Disabled_WhenSourceNotEnabledOrUntrusted` | dto. | `disabled` + `title`-Grund bei `Enabled=false` bzw. `TrustConfirmed=false` |
| `CheckSourceAsync_CallsService_AndShowsInstalled` | dto. | Klick → Service-Aufruf mit `source.Id` → `alert-success` „Version {tag} installiert" |
| `CheckSourceAsync_ShowsNoUpdate` | dto. | `NoUpdate`-Result → „Kein Update gefunden." |
| `CheckSourceAsync_ShowsLocalizedFailed` | dto. | `Failed("Plugin source is disabled.")` → lokalisierte Meldung; unbekannter Error → `PluginCheckFailed` + Detail |
| `CheckSourceAsync_SetsBusy_AndDisablesRow` | dto. | Mock mit `Task.Delay` → während Pending: `spinner-border` + Zeilen-Buttons `disabled`; danach frei |
| `CheckSourceAsync_ReloadsSources` | dto. | `GetSourcesAsync` nach dem Lauf erneut aufgerufen (Status-Spalte aktualisiert) |
| `ActionButtons_HaveLocalizedTooltipAndAriaLabel` | dto. | `bi-pencil`/`bi-trash`/`bi-arrow-repeat` Buttons mit `title` + `aria-label` |
| `LastErrorAt_IsRendered` | dto. | `LastErrorAt`-Zeitstempel in der Status-Zelle |
| `SeedInstalledReleaseAsync` | `PluginSettingsAppFixture` | SQLite-Insert einer `PluginSourceReleases`-Zeile (`Status='Installed'`, Tag + `AssetId` passend zum Stub-Release) |
| `GitHubStubServer` | Infrastruktur | `HttpListener`: `releases/latest` → 404 oder `{id, tag_name, assets:[{id, name:"x.zip", url}]}`, abfragbar |

### Betroffene bestehende Tests

Keine — Service-Tests bleiben unberührt (keine Signatur-/Verhaltensänderung am Backend); bestehender bUnit-Test `RecipeSearchComponentTests` unabhängig.

### E2E-Tests (primärer Funktionsnachweis)

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|---------------------|
| Pflicht | Admin legt Quelle an → Zeile mit Prüf-Button (Icon, `title` „Auf Updates prüfen") erscheint; Erklärtext sichtbar | `PluginSettingsBrowserTests` + `PluginSettingsPageObject` | Button + Erklärtext im echten Settings-Panel | Vollständiger Nutzerfluss über `/settings` |
| Pflicht | Klick auf Prüf-Button einer `IsPrivate`-Quelle ohne PAT → `alert-danger` mit lokalisierter `PluginCheckNoPat`-Meldung; Status-Zelle zeigt `LastError` + `LastCheckedAt` | dto. | Klick → Fehlermeldung + Status-Update | Deterministischer `Failed`-Pfad ohne Netzwerk |
| Pflicht | Quelle deaktiviert (Switch) → Prüf-Button `disabled` mit Grund-Tooltip | dto. | Deaktivierung + Tooltip-Begründung | Blockade-Erklärung ist Pflicht bei deaktivierter Aktion |
| Pflicht | `NoUpdate(alreadyInstalled)` → `alert-success` „Kein Update gefunden." (Stub-Release = geseedeter `Installed`-Release) | dto. + `PluginSettingsAppFixture` + `GitHubStubServer` | Erfolgs-Meldung nach echtem Check-Durchlauf | Erfolgs-Pfad Happy Path, ohne Netzwerk über lokalen API-Stub |
| Optional | Während Klick: Spinner sichtbar, Buttons gesperrt | bUnit (Browser-Timing zu fragil) | Busy-Darstellung | Timing-abhängig → bUnit mit verzögertem Mock |

Angepasste bestehende E2E-Tests: keine (`SecurityTxtBrowserTests` u. a. unberührt; neue Collection wegen Env-Override).

## Akzeptanzszenarien

| # | Szenario | Auslöser | Ausgangszustand | Schritte | Sichtbare Wirkung | Nachweis |
|---|----------|----------|------------------|----------|-------------------|----------|
| 1 | Happy Path Check | Klick „Auf Updates prüfen" | Admin eingeloggt, Quelle `Enabled && TrustConfirmed`; ggf. kein `PluginSourceReleases`-Eintrag | `/settings` → Plugins → Quelle anlegen → Prüf-Button klicken → Warten (Spinner) → Ergebnis | Alert mit Ergebnismeldung; Status-Zeile zeigt neues `LastCheckedAt` | Browser-Test Szenario 4 (`NoUpdate`) + bUnit `Installed` |
| 2 | Fehlschlag ohne PAT | Klick | Private Quelle ohne gespeichertes Secret | Wie 1, aber Quelle `IsPrivate` ohne PAT | `alert-danger` „Für die private Quelle ist kein PAT hinterlegt." + `LastError`/`LastErrorAt` in der Zeile | Browser-Test Szenario 2 |
| 3 | Blockierte Aktion | — | Quelle `Enabled=false` oder `TrustConfirmed=false` | Quellen-Tabelle betrachten | Prüf-Button `disabled`, Tooltip nennt Grund | Browser-Test Szenario 3 + bUnit |
| 4 | Wartezustand | Klick | Beliebige gültige Quelle; evtl. läuft serverseitig ein Start-Check am `CheckGate` | Klick → Spinner bis `CheckGate` frei + Lauf fertig | Zeilen-Buttons gesperrt, Spinner im Prüf-Button; Wiederaufnahme automatisch nach `await` | bUnit `...SetsBusy...` |
| 5 | Unbekannter Server-Fehler | Klick | Check schlägt mit nicht gemapptem `Error` fehl | Klick → `Failed` mit fremdem `Error`-Text | `alert-danger` „Prüfung fehlgeschlagen. (<Error-Detail>)" | bUnit `...ShowsLocalizedFailed` |

## Offene Punkte

Keine — die beiden offenen Fragen aus `requirement.md` sind durch die Projektklärung entschieden (Busy nur für eigenen Lauf; Info-Text-Platzierung = kurzer `text-muted`-Absatz unter der Überschrift).
