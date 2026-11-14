# Anforderung (übersetzt): Update-Prüfung je Pluginquelle in der Pluginverwaltung

Issue: #179, Schritt 4 (vgl. `Docs/projects/task/issue-179-...-aktualisierung-der-plugins/steps.md`)

## Fachliche Zusammenfassung

Die Admin-Einstellungskomponente `PluginSettings` (`/settings`, Bereich „Plugins") wird um eine manuelle Update-Prüfung je GitHub-Pluginquelle erweitert und erklärt dem Anwender sichtbar das Prüfprinzip (automatisch einmalig beim Anwendungsstart für alle aktivierten und vertrauensbestätigten Quellen; manuell je Quelle auf Knopfdruck). Alle Quellen-Aktionen (neu: „Auf Updates prüfen"; bestehend: „Bearbeiten", „Löschen") werden zu einheitlichen Symbolbuttons mit Bootstrap-Icons, lokalisierten Tooltips und `aria-label`s. Alle neuen Texte werden über `UiStrings.resx` lokalisiert.

## Fachlicher Scope

- **Verarbeitungseinheit:** Eine einzelne `PluginSource` (eine konfigurierte GitHub-Pluginquelle) sowie deren manuell ausgelöster Update-Check-Lauf.
- **Auslösende Ereignisse:**
  - Klick auf die Quellen-Aktion „Auf Updates prüfen" → `IPluginSettingsService.CheckSourceForUpdatesAsync(sourceId)` (serverseitig bereits vorhanden, Schritt 2).
  - Kein neuer automatischer Auslöser — die Start-Prüfung (`PluginUpdateHostedService` → `CheckForUpdatesAsync`) bleibt unverändert.
- **Erwartete Wirkung:**
  - Erklärtext zum Prüfprinzip ist in der Quellen-Sektion sichtbar.
  - Nach dem manuellen Check wird das Ergebnis direkt rückgemeldet: „kein Update gefunden" (`PluginUpdateCheckOutcome.NoUpdate`), „Version {Tag} installiert" (`Installed`) oder die Fehlermeldung (`Failed` → `Error`).
  - Die je Quelle angezeigten Statusangaben (`LastCheckedAt`, `LastSuccessfulReleaseTag`, `LastError`) aktualisieren sich nach dem Lauf (Neuladen der Quellenliste).
- **Vorbedingungen der Aktion:** Quelle `Enabled && TrustConfirmed` — sonst ist der Button deaktiviert (identisch zu den serverseitigen Vorbedingungen von `CheckSourceForUpdatesAsync`). Während eines laufenden Checks ist die Aktion gesperrt bzw. als beschäftigt dargestellt.
- **Umgang mit fehlenden Objekten:** Keine neuen fachlichen Objekte nötig; `PluginSourceSettingsItem` liefert bereits alle Statusfelder, `CheckSourceForUpdatesAsync` ist implementiert und admin-geschützt.
- **Scope-Ausschlüsse:**
  - Kein „Alle Quellen prüfen"-Button (die Anforderung verlangt den Check je Quelle).
  - Keine Änderung an Häufigkeit/Zeitpunkt der Start-Prüfung und keine periodische Prüfung.
  - Kein Toast-/Benachrichtigungssystem — Ergebnisrückmeldung erfolgt inline im vorhandenen Alert-Muster.
  - Keine UI für „vermisste Plugins entfernen" (ist Schritt 5).

## Betroffene Klassen und Komponenten

- `Rezepte.Web/Components/Settings/PluginSettings.razor` — Quellen-Tabelle: Erklärtext, neuer Prüf-Button je Zeile, Umbau Bearbeiten/Löschen zu Symbolbuttons, Busy-Deaktivierung, Ergebnismeldung.
- `Rezepte.Web/Resources/UiStrings.resx` — neue Schlüssel (Erklärtext, Tooltip/Aria-Labels, Ergebnismeldungen mit Platzhalter für Release-Tag); vorhandene Schlüssel wie `Edit`, `Delete`, `CheckNow`, `LastChecked` prüfen/wiederverwenden.
- `IPluginSettingsService.CheckSourceForUpdatesAsync` / `PluginUpdateCheckResult` / `PluginUpdateCheckOutcome` — unverändert wiederverwendet (Schritt 2).
- Tests: ggf. neue Browser-Tests (`Rezepte.Tests.Browser`, Admin-UI-Muster existiert über `SecurityTxtBrowserTests`); Service-Tests existieren bereits.

## Implementierungsansatz

- Bestehendes Komponenten-Muster an `ApplicationUpdates.razor` orientieren (`isBusy`-Flag, `message`/`messageClass` mit `alert alert-success|warning|danger`).
- Busy-Zustand clientseitig je Quelle (z. B. ID der laufenden Prüfung); serverseitig serialisiert `PluginUpdateService.CheckGate` ohnehin alle Checks.
- Icons über bereits eingebundene Bootstrap-Icons (`App.razor`, CDN 1.11.3), Muster `<i class="bi bi-…">` bzw. `<span class="bi …" aria-hidden="true">` plus `title`/`aria-label`.

## Konfiguration

Keine Konfigurierbarkeit gefordert.

## Offene Fragen

- „Während eines laufenden Checks gesperrt": Als Mindestinterpretation gilt der durch die UI selbst ausgelöste Lauf. Ob ein serverseitig laufender Check (z. B. noch laufende Start-Prüfung hinter `CheckGate`) den Button ebenfalls als beschäftigt zeigen soll, ist ohne zusätzliche Status-API nicht abfragbar — Annahme: Busy-Anzeige nur für den eigenen ausgelösten Lauf; parallele Server-Checks warten am Gate.
- Platzierung/Umfang des Erklärtextes (Text unter der Quellen-Überschrift vs. Info-Box) — Gestaltungsdetail, keine fachliche Klärung nötig.
