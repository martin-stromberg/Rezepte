# Anforderung: Entfernen-Aktion für vermisste Plugins (Schritt 5, Issue #179)

## Fachliche Zusammenfassung

In der Pluginliste der Admin-Einstellungsseite (`PluginSettings.razor`) erhalten Einträge mit
Status `PluginStatus.Missing` eine Entfernen-Aktion. Die Aktion wird als Symbolbutton mit
lokalisiertem Tooltip und `aria-label` umgesetzt und löscht den Verwaltungseintrag
(`PluginSetting`-Zeile) über den bereits vorhandenen
`IPluginSettingsService.RemovePluginAsync`. Nach dem Entfernen wird die Liste neu geladen;
wird das Plugin später durch die Discovery wieder gefunden, erscheint es als neuer Eintrag am
Ende der Reihenfolge (bestehendes `PluginManager`-Verhalten, kein neuer Code).

## Fachlicher Scope

- **Verarbeitungseinheit:** Ein Verwaltungseintrag (`PluginSetting`) eines Import-Plugins mit
  Status `Missing`
- **Auslösende Ereignisse:** Klick des Administrators auf den Entfernen-Button in der
  Plugin-Zeile
- **Erwartete Wirkung:** DB-Zeile gelöscht, Pluginliste neu geladen, Erfolgs-Alert sichtbar;
  bei Fehlschlag Danger-Alert, Eintrag bleibt sichtbar
- **Umgang mit fehlenden Objekten:**
  - Plugin-Eintrag zur Klickzeit nicht mehr in der DB → `RemovePluginAsync` wirft
    `InvalidOperationException` → Danger-Alert (verwerfen, kein Neuanlegen)
  - Status zur Laufzeit ≠ `Missing` → Service-Guard wirft → Danger-Alert; UI zeigt den Button
    ohnehin nur bei `Missing`
- **Scope-Ausschlüsse:**
  - Kein Entfernen-Button für andere Stati (`Loaded`, `Incompatible`, `LoadFailed`,
    `RuntimeFailed`) — Service lehnt diese ohnehin ab
  - Kein Bestätigungsdialog — die vorhandene Quellen-Löschaktion hat ebenfalls keinen
  - Wiedererkennung und Neuanlage am Ende der Reihenfolge: bereits durch die
    `PluginManager`-Discovery abgedeckt, kein Code in diesem Schritt
  - Aktivieren-/Verschieben-Buttons für `Missing`-Einträge bleiben unverändert

## Betroffene Klassen und Komponenten

- `Rezepte.Web/Components/Settings/PluginSettings.razor` — Button in der Aktionszelle +
  neuer Handler
- `Rezepte.Web/Resources/UiStrings.resx` — neue lokalisierte Keys
- `Rezepte.Tests/Components/PluginSettingsComponentTests.cs` — bUnit-Tests
- `Rezepte.Tests.Browser/Infrastructure/PluginSettingsAppFixture.cs` — Seed-Hilfsmethode
- `Rezepte.Tests.Browser/Infrastructure/PluginSettingsPageObject.cs` — Page-Object-Erweiterung
- `Rezepte.Tests.Browser/PluginSettingsBrowserTests.cs` — 1 E2E-Test

## Implementierungsansatz

Wiederverwendung des Symbolbutton-Musters aus Schritt 4 (Wrapper-`<span title>` +
Button-`title` + `aria-label` + Bootstrap-Icon) und des Handler-Musters von
`DeleteSourceAsync` (`isSaving`/`message`/`messageClass` + `LoadAsync`). Neue resx-Keys für
Tooltip/aria-label sowie Erfolgs- und Fehlermeldung.

## Konfiguration

Keine.
