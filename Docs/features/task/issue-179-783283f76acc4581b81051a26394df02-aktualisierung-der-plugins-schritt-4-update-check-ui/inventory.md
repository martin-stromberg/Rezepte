# Bestandsaufnahme: Update-Prüfung je Pluginquelle in der Pluginverwaltung

Analysiert wurden die Pluginverwaltungs-UI (`PluginSettings.razor`), die Update-Check-Services aus Schritt 2, das Lokalisierungs-/Icon-Setup und die Testinfrastruktur — bezogen auf die Anforderung in `requirement.md`.

## Zusammenfassung

- **Backend vollständig vorhanden:** `IPluginSettingsService.CheckSourceForUpdatesAsync(sourceId)` (Admin-Guard + Delegation an `PluginUpdateService`) liefert `PluginUpdateCheckResult` mit `Outcome` (`NoUpdate`/`Installed`/`Failed`), `ReleaseTag`, `Error`, `AlreadyInstalled`. Serverseitig sind alle Checks über das statische `SemaphoreSlim CheckGate` serialisiert; die Start-Prüfung läuft über `PluginUpdateHostedService` → `CheckForUpdatesAsync` für alle `Enabled && TrustConfirmed`-Quellen.
- **UI fehlt komplett:** `PluginSettings.razor` ruft `CheckSourceForUpdatesAsync` noch nicht auf; es gibt keinen Erklärtext, keinen Prüf-Button und keine Erfolgs-/Ergebnismeldung (nur `alert-danger` für Fehler). Die Aktionen „Bearbeiten"/„Löschen" sind noch Textbuttons ohne Icons; `aria-label` existiert nur an den Aktiv-Schaltern.
- **Statusfelder vorhanden und bereits gerendert:** `PluginSourceSettingsItem` enthält `Enabled`, `TrustConfirmed`, `IsPrivate`, `LastSuccessfulReleaseTag`, `LastError`, `LastCheckedAt`, `LastErrorAt` — Status-Spalte zeigt Release-Tag-Badge, `LastError` und `LastCheckedAt` bereits an (z. T. hartkodiert Deutsch, nicht lokalisiert).
- **Lokalisierung:** einzelne `UiStrings.resx` (deutsch, 219 Einträge, keine Satelliten-Resx), `IStringLocalizer<UiStrings>` via `_Imports.razor`; `AddLocalization()` in `Program.cs`. Wiederverwendbare Schlüssel vorhanden (`Edit`, `Delete`, `CheckNow`, `LastChecked`, `TrustThisSource` u. a.). Neue Schlüssel nötig für Erklärtext, Tooltips/Aria-Labels und Ergebnismeldungen.
- **Icons:** Bootstrap Icons 1.11.3 per CDN in `App.razor:20` eingebunden; Repo-Muster `<i class="bi bi-…">`/`<span class="bi …" aria-hidden="true">` mit `title`-Tooltip (Beispiele in `MainLayout`, `Cookbooks`, `BackupRestore`, `RecipePage`).
- **Feedback-Muster:** kein Toast-System; `ApplicationUpdates.razor` ist das direkte Vorbild (`isBusy`, `message`/`messageClass` mit `alert alert-success|warning|danger`, `RunCommandAsync`). Busy-Spinner-Muster `spinner-border spinner-border-sm` existiert (`CreateRecipeDialog.razor`).
- **Browser-Tests:** Playwright-Setup mit App-Prozess + temp. SQLite (`RezepteAppFixture`), Admin-UI-Testmuster existiert (`SecurityTxtBrowserTests` + `SecurityTxtPageObject`); kein bestehender Test für die Plugin-Verwaltungsseite.

Test-Ausgangszustand: kein neuer Lauf (vorgegeben); Baseline aus Projektdoku — `Rezepte.Tests` 687/687 bestanden (Acceptance Schritt 3); `Rezepte.Tests.Browser` 20/20 im letzten Lauf mit einem bekannten Flake (`Register_WithDemoData_CreatesDemoData`, Timeout im 120-s-Demo-Daten-Wait, siehe [Tests](inventory/tests.md)).

## Details

- [UI-Komponente, Lokalisierung, Icons, Feedback](inventory/ui.md)
- [Logik / Services](inventory/logic.md)
- [Datenmodelle und Ergebnistypen](inventory/models.md)
- [Interfaces](inventory/interfaces.md)
- [Tests](inventory/tests.md)
