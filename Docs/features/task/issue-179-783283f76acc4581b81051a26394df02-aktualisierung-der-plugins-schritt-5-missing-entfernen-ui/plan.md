# Umsetzungsplan: Entfernen-Aktion für vermisste Plugins (Schritt 5)

## Übersicht

Die Plugin-Tabelle in `PluginSettings.razor` erhält für Zeilen mit
`Status == PluginStatus.Missing` einen Entfernen-Symbolbutton, der
`IPluginSettingsService.RemovePluginAsync` aufruft, die Liste neu lädt und eine lokalisierte
Erfolgs- bzw. Fehlermeldung anzeigt. Backend-Code ist nicht nötig — es wird ausschließlich
UI-Komponente, resx und Tests erweitert.

## Scope-Klärung

| Feld | Festlegung |
|------|------------|
| Verarbeitungseinheit | Ein Verwaltungseintrag (`PluginSetting`) eines Import-Plugins mit Status `Missing` |
| Auslösende Ereignisse | Klick des Administrators auf den Entfernen-Button in der Plugin-Zeile |
| Erwartete Wirkung | `PluginSettings`-Zeile gelöscht, Liste neu geladen, Erfolgs-Alert; bei Fehlschlag Danger-Alert, Eintrag bleibt |
| Umgang mit fehlenden Objekten | Eintrag zur Klickzeit nicht in DB oder Status ≠ `Missing` → Service wirft `InvalidOperationException` → Danger-Alert; kein Neuanlegen (verwerfen) |

### Scope-Ausschlüsse

| # | Einschränkung / Ausschluss | Begründung | Status |
|---|----------------------------|------------|--------|
| 1 | Kein Entfernen-Button für Stati ≠ `Missing` | Anforderung + Service-Guard | bestaetigt |
| 2 | Kein Bestätigungsdialog vor dem Löschen | Konsistent mit Quellen-`DeleteSourceAsync` (kein Dialog) | bestaetigt |
| 3 | Wiedererkennung/Neuanlage am Ende der Reihenfolge | Bereits `PluginManager`-Discovery-Verhalten (Status `Missing` → Re-Insert als neuer Datensatz), kein Code hier | bestaetigt |
| 4 | Verschiebe-/Aktivieren-Buttons bei `Missing`-Zeilen bleiben unverändert | Nicht Teil der Anforderung | bestaetigt |

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Button-Platzierung | Eigener `<span class="d-inline-block" title>`-Button **nach** der Order-`btn-group` in derselben `td.text-end`; Spaltenbreite 112px → 160px | Entfernen gehört nicht in die `btn-group` mit `aria-label="PluginOrder"` (semantisch Reihenfolge); gleiche Zelle wie in der Quellen-Tabelle (mehrere Aktionen pro Zeile) |
| Icon/Stil | `btn-outline-danger` + `bi bi-trash` | Konsistent mit Quellen-Löschbutton (Z. 139–148) |
| Label-Texte | Parametrisierter Key `RemovePluginFor` („Plugin-Eintrag {0} entfernen") mit `DisplayName` | Analog `EnablePluginFor` („Plugin {0} aktivieren") — eindeutiges Accessible-Label pro Zeile |
| Busy-Tooltip | Bei `isSaving`: `PluginCheckBusyTooltip` wiederverwenden | Vorhandener, bereits eingeführter Key |
| Erfolg/Fehler | Neue Keys `PluginRemoved` (Erfolg) und `PluginRemoveFailed` (Fehler, Format mit `{0}`) | Parität mit `PluginSaveFailed`-Format; `PluginRemoved` als Alert-Text ohne Platzhalter |

## Programmabläufe

### Plugin-Eintrag entfernen

1. Admin klickt den Entfernen-Button in einer `Missing`-Zeile.
2. `RemovePluginEntryAsync(PluginSettingsItem plugin)` (neuer privater Handler):
   - Guard: `isSaving` oder `plugin.Status != PluginStatus.Missing` → `return`.
   - `isSaving = true; message = null;`
   - `PluginSettingsService.RemovePluginAsync(plugin.PluginId)`
   - Erfolg: `messageClass = "alert alert-success py-2"`, `message = Localizer["PluginRemoved"]`,
     `await LoadAsync()` (lädt Plugins + Quellen neu, Eintrag verschwindet)
   - `catch (Exception ex)`: `messageClass = "alert alert-danger py-2"`,
     `message = Localizer["PluginRemoveFailed", ex.Message]`; Liste wird **nicht** neu geladen,
     Eintrag bleibt sichtbar
   - `finally`: `isSaving = false`
3. Rendern: Success-/Danger-Alert über der Tabelle; Zeile ist nach Reload weg.

Beteiligt: `PluginSettings.razor`, `IPluginSettingsService`/`PluginSettingsService`,
`UiStrings.resx`.

## Neue Klassen

Keine.

## Änderungen an bestehenden Klassen

### `PluginSettings` (`Rezepte.Web/Components/Settings/PluginSettings.razor`, Razor-Komponente)

- **Markup (Aktionszelle der Plugin-Tabelle, ~Z. 225–230):** Spaltenbreite 112px → 160px;
  nach der bestehenden `btn-group` einen bedingten Block
  `@if (plugin.Status == PluginStatus.Missing)` mit `<span class="d-inline-block"
  title="@RemoveButtonTitle()">` + `<button class="btn btn-outline-danger" type="button"
  title aria-label disabled="@isSaving" @onclick="() => RemovePluginEntryAsync(plugin)">`
  + `<i class="bi bi-trash" aria-hidden="true"></i>`
- **Neue Methode `RemovePluginEntryAsync(PluginSettingsItem plugin)`:** Ablauf wie oben,
  Muster `DeleteSourceAsync`
- **Neue Methode `RemoveButtonTitle()`:** `isSaving ? Localizer["PluginCheckBusyTooltip"] :
  Localizer["RemovePluginFor", <DisplayName>]` — parametrisiert, daher Signatur
  `RemoveButtonTitle(PluginSettingsItem plugin)` bzw. mit DisplayName-Parameter
- **Namespace:** `PluginStatus`/`PluginSettingsItem` bereits via
  `@using Rezepte.Web.Services.Import.Plugins` (Z. 1) verfügbar

### `UiStrings.resx` (`Rezepte.Web/Resources/UiStrings.resx`)

Neue Keys (deutsch, `xml:space="preserve"`):

| Key | Wert |
|-----|------|
| `RemovePluginFor` | `Plugin-Eintrag {0} entfernen` |
| `PluginRemoved` | `Plugin-Eintrag entfernt.` |
| `PluginRemoveFailed` | `Plugin-Eintrag konnte nicht entfernt werden: {0}` |

### `PluginSettingsComponentTests` (`Rezepte.Tests/Components/PluginSettingsComponentTests.cs`)

- Neuer Helper `WithPlugins(params PluginSettingsItem[])` — stuht
  `GetPluginsAsync → plugins` (Konstruktor-Default bleibt leeres Array)
- Neue Fabrik `Plugin(string id, string status = PluginStatus.Missing, …)` für
  `PluginSettingsItem`
- Neue Facts (s. Tests)

### `PluginSettingsAppFixture` (`Rezepte.Tests.Browser/Infrastructure/`)

- **Neue Methode `SeedMissingPluginAsync(string pluginId, string displayName)`:** INSERT in
  Tabelle `PluginSettings` mit `Status = "Missing"`, `Enabled = 1`, eigenem `OrderIndex`
  (z. B. `MAX(OrderIndex)+1` bzw. fester hoher Wert), Zeitstempel-Format wie
  `SeedInstalledReleaseAsync` (`yyyy-MM-dd HH:mm:ss.fffffff`)

### `PluginSettingsPageObject` (`Rezepte.Tests.Browser/Infrastructure/`)

- Neue Methoden: `RemovePluginRowAsync(displayName)` (klickt `button:has(i.bi-trash)` in der
  Plugin-Zeile, wartet auf `.alert-success`/`.alert-danger` oder Zeilen-Verschwinden),
  `IsPluginRowVisibleAsync(displayName)`, `GetPluginRemoveButtonTitleAsync(displayName)`
- `PluginRow(displayName)`: `tr:has-text(displayName)` in der Plugin-Tabelle (zweite Tabelle
  / Bereich nach `LocalImportPlugins`-Überschrift)

### `PluginSettingsBrowserTests` (`Rezepte.Tests.Browser/`)

- 1 neuer `SkippableFact` (s. E2E-Tabelle)

## Datenbankmigrationen

Keine.

## Validierungsregeln

Keine neuen Eingaben. Sichtbarkeitsregel: Button nur bei `Status == PluginStatus.Missing`;
Guard im Handler wiederholt die Prüfung (bUnit kann `disabled`-Buttons klicken).

## Konfigurationsänderungen

Keine.

## Seiteneffekte und Risiken

- **`LoadAsync` kollapziert kurz in „Laden…":** gleiches Verhalten wie `SetEnabledAsync`/
  `MoveAsync` — bewusst konsistent, kein Teil-Reload nötig.
- **Spaltenbreite 112 → 160px:** geringes Layout-Risiko, Tabelle ist `table-responsive`.
- **Race: Zeile nach Reload weg, `OrderIndex`-Lücke:** bestehende Reihenfolge-Logik toleriert
  Lücken (`OrderBy(OrderIndex)`); kein Reindexing nötig.

## Umsetzungsreihenfolge

1. **resx-Keys anlegen** (`RemovePluginFor`, `PluginRemoved`, `PluginRemoveFailed`)
   - Voraussetzungen: Keine
2. **`PluginSettings.razor` erweitern** (Button + `RemovePluginEntryAsync` + `RemoveButtonTitle`)
   - Voraussetzungen: Schritt 1
3. **bUnit-Tests schreiben und grün bekommen**
   - Voraussetzungen: Schritt 2
4. **E2E: `SeedMissingPluginAsync` + Page-Object-Erweiterung + 1 Browser-Test**
   - Voraussetzungen: Schritt 2; Test-DB-Schema `PluginSettings` vorhanden

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `RemoveButton_OnlyForMissingStatus` | `PluginSettingsComponentTests` | Seed 1× `Loaded` + 1× `Missing`: nur Missing-Zeile hat `i.bi-trash`-Button mit parametrisiertem `aria-label` („Plugin-Eintrag <Name> entfernen") |
| `RemoveButton_HasLocalizedTooltipAndAriaLabel` | `PluginSettingsComponentTests` | Button `title` + `aria-label` + Wrapper-`span`-`title` stimmen überein (Muster `AssertIconButton`, parametrisierter Text) |
| `RemovePluginAsync_CallsService_AndReloads` | `PluginSettingsComponentTests` | Klick → `RemovePluginAsync(pluginId)` 1×, `GetPluginsAsync` insgesamt 2× (Init + Reload), `.alert-success` „Plugin-Eintrag entfernt.", Zeile verschwindet nach Mock-Umschalten auf leere Liste |
| `RemovePluginAsync_WhenServiceThrows_ShowsDangerAlert` | `PluginSettingsComponentTests` | `RemovePluginAsync` wirft `InvalidOperationException` → `.alert-danger` mit „Plugin-Eintrag konnte nicht entfernt werden:" + Detail; Zeile bleibt |
| `WithPlugins(...)`, `Plugin(...)` | `PluginSettingsComponentTests` (Hilfsmethoden) | Mock-Setup `GetPluginsAsync`, `PluginSettingsItem`-Fabrik |
| `SeedMissingPluginAsync` | `PluginSettingsAppFixture` | INSERT einer `PluginSettings`-Zeile mit `Status='Missing'` |
| `RemovePluginRowAsync`, `IsPluginRowVisibleAsync` | `PluginSettingsPageObject` | Klick + Sichtbarkeits-Locators für Plugin-Zeilen |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `PluginSettingsComponentTests` (Konstruktor-Default) | `GetPluginsAsync` bleibt leer — keine Anpassung nötig; neue Facts nutzen `WithPlugins`. `ActionButtons_HaveLocalizedTooltipAndAriaLabel` unverändert (Quellen-Tabelle). |

Keine bestehenden Tests brechen: Markup-Änderung betrifft nur `Missing`-Zeilen, die bisher
kein Test seeded.

### E2E-Tests (primärer Funktionsnachweis)

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht | Missing-Plugin seeden → Plugins-Panel → Entfernen klicken → Zeile weg + Success-Alert | `PluginSettingsBrowserTests` (neuer Fact `MissingPlugin_CanBeRemoved`) | Eintrag mit Missing-Badge zeigt Entfernen-Button; nach Klick ist die Zeile verschwunden und die Erfolgsmeldung sichtbar | Vollständiger Nutzerfluss (Admin-Klick → Service → DB → Reload → UI) nur im Browser belegbar |
| Optional | Nicht-Missing-Zeile hat keinen Entfernen-Button | gleicher Test mitgeladenes Plugin ggf. schwer seedbar (Discovery) | Button-Sichtbarkeit | bUnit deckt Sichtbarkeit ab; E2E-Seed eines `Loaded`-Plugins ist möglich, aber Mehrwert gering — in denselben Test integrierbar, falls Seed trivial |

Anpassung bestehender E2E-Tests: Keine.

## Akzeptanzszenarien

| # | Szenario | Auslöser | Ausgangszustand | Schritte | Sichtbare Wirkung | Nachweis |
|---|----------|----------|------------------|----------|-------------------|----------|
| 1 | Happy Path | Admin klickt Entfernen-Button | `PluginSetting`-Zeile mit `Status='Missing'` existiert; Admin eingeloggt | Plugins-Panel öffnen → Zeile mit `Missing`-Badge → Entfernen-Button (Trash-Icon, Tooltip „Plugin-Eintrag <Name> entfernen") klicken | Success-Alert „Plugin-Eintrag entfernt.", Zeile aus Liste verschwunden | E2E `MissingPlugin_CanBeRemoved` + bUnit |
| 2 | Sichtbarkeitsregel | Rendern der Tabelle | Zeilen mit `Loaded` und `Missing` | Panel öffnen | Nur Missing-Zeile zeigt Trash-Button | bUnit `RemoveButton_OnlyForMissingStatus` |
| 3 | Fehlerfall | Admin klickt Entfernen-Button | Eintrag zwischenzeitlich aus DB entfernt / Status geändert | Klick → Service wirft | Danger-Alert „Plugin-Eintrag konnte nicht entfernt werden: …", Zeile bleibt sichtbar | bUnit `RemovePluginAsync_WhenServiceThrows_ShowsDangerAlert` |
| 4 | Wiedererkennung | App-Start-Discovery findet Plugin wieder | Verwaltungseintrag wurde zuvor entfernt | Discovery legt neuen `PluginSetting`-Datensatz an | Plugin erscheint als neuer Eintrag am Ende der Reihenfolge | Bestehendes `PluginManager`-Verhalten (Service-Tests Schritt 1–3); kein neuer Test nötig |

## Offene Punkte

| # | Offener Punkt | Empfohlener Vorschlag |
|---|---------------|----------------------|
