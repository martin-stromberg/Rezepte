# Bestandsaufnahme: Entfernen-Aktion für vermisste Plugins (Schritt 5)

Bestandsaufnahme der Plugin-Tabelle in `PluginSettings.razor`, des
`IPluginSettingsService.RemovePluginAsync`-Vertrags und der Test-Infrastruktur, bezogen auf
die Anforderung aus `requirement.md`.

## Zusammenfassung

- `RemovePluginAsync(pluginId)` existiert vollständig: Admin-Gate (`EnsureAdmin`), Missing-Guard,
  harte Löschung der `PluginSettings`-Zeile. **Kein Backend-Code nötig.**
- Die Plugin-Tabelle hat eine Aktionszelle mit einer `btn-group` (nur ↑/↓-Verschiebe-Buttons,
  Textpfeile, `aria-label` „Plugin-Reihenfolge"). Fehlend: jede Form einer Entfernen-Aktion.
- `Missing` wird bereits als `badge text-bg-warning` dargestellt; `plugin.Status` ist ein
  `string`-Vergleich gegen `PluginStatus.Missing`.
- Das Symbolbutton-Muster (Wrapper-`<span title>` + `title` + `aria-label` + `bi-*`-Icon) und
  das Busy-/Alert-Muster (`isSaving`, `message`, `messageClass`) existieren — in der
  Quellen-Tabelle, wiederverwendbar.
- Lokalisierte Keys für Entfernen-Aktion/Erfolg/Fehler fehlen; wiederverwendbar sind
  `PluginCheckBusyTooltip` („Ein Vorgang läuft gerade."), `Actions`, `PluginOrder`.
- **Test-Ausgangszustand:** `PluginSettingsComponentTests` — 12/12 erfolgreich, 0 Fehler
  (Nachweis: [tests.md](inventory/tests.md), [Log](inventory/test-results/dotnet-test-pluginsettingscomponents-2026.log)).

## Details

### Plugin-Tabelle (`Rezepte.Web/Components/Settings/PluginSettings.razor`)

- **Zeilenaufbau** (Z. 178–233): `for`-Schleife über `plugins` (`List<PluginSettingsItem>`).
  - Spalte 1 (Z. 182–191): Enabled-Toggle (`form-check form-switch`),
    `disabled="@isSaving"`, `aria-label` `EnablePluginFor {DisplayName}`,
    `@onchange → SetEnabledAsync(plugin.PluginId, args)`
  - Spalte 2: `DisplayName`, `PluginId`, `Description`, `Error` (danger)
  - Spalte 3 (Z. 204–220): `<span class="@StatusClass(plugin.Status)">@plugin.Status</span>`
    — Status wird als **roher Enum-String** angezeigt, nicht lokalisiert; plus
    `NotUsable`-Badge/Issues
  - Spalte 4: `AssemblyName`, `TypeName`
  - Spalte 5 / Aktionszelle (Z. 225–230): `td.text-end`, `btn-group btn-group-sm`
    `aria-label="PluginOrder"` mit zwei `btn-outline-secondary`-Textbuttons (↑ `MoveUp`,
    ↓ `MoveDown`), `disabled="@(isSaving || i == 0)"` bzw. `i == plugins.Count - 1`,
    Klick → `MoveAsync(plugin.PluginId, ±1)`. **Kein `<span title>`-Wrapper, kein Icon.**
  - Spaltenkopf der Aktionszelle: `@Localizer["PluginOrder"]`, Breite 112px
- **`StatusClass`** (Z. 516–523): `Missing → badge text-bg-warning`, `Loaded → success`,
  `Incompatible`/`LoadFailed → danger`, sonst secondary
- **Busy/Alert-Muster:** `isSaving` (Z. 244), `message`/`messageClass` (Z. 246–247, Alerts
  „alert alert-danger py-2" / „alert alert-success py-2", Z. 11–14)
- **Handler-Muster `DeleteSourceAsync`** (Z. 439–457): `isSaving=true; message=null;` →
  Service → `await LoadAsync()` → catch: `messageClass`-danger + lokalisierte Fehlermeldung
  → finally `isSaving=false`. `LoadAsync` setzt `isLoading=true` (Panel kollapziert kurz in
  „Laden…").
- **Symbolbutton-Muster** (Quellen-Tabelle, Z. 110–149): `<span class="d-inline-block" title="…">`
  umschließt `<button title aria-label …><i class="bi bi-*" aria-hidden="true"></i></button>`;
  Löschen = `btn-outline-danger` + `bi-trash`; Tooltip-Texte über Helper-Methoden
  (`CheckButtonTitle`/`EditButtonTitle`/`DeleteButtonTitle`, Z. 346–374), die bei
  `isSaving || checkingSourceId is not null` den Key `PluginCheckBusyTooltip` liefern.

### Service-Vertrag (`Rezepte.Web/Services/Import/Plugins/`)

- `IPluginSettingsService.RemovePluginAsync` — `IPluginSettingsService.cs:65`:
  `Task RemovePluginAsync(string pluginId, CancellationToken ct = default)`
- Implementierung `PluginSettingsService.cs:256–272`: `EnsureAdmin()` → `FindAsync` →
  `InvalidOperationException` wenn Eintrag fehlt oder `Status != PluginStatus.Missing` →
  `db.PluginSettings.Remove(plugin)` + `SaveChangesAsync`
- `PluginSettingsItem` (Record, `PluginSettingsItem.cs:19`): u. a. `PluginId`, `DisplayName`,
  `Enabled`, `OrderIndex`, `Status` (**string**), `Error`, `Usability`
- `PluginStatus` (`PluginStatus.cs`): statische Klasse mit `const string` — `Loaded`,
  `Missing`, `Incompatible`, `LoadFailed`, `RuntimeFailed`

### Wiederverwendbare resx-Keys (`Rezepte.Web/Resources/UiStrings.resx`)

- `PluginCheckBusyTooltip` = „Ein Vorgang läuft gerade." — Busy-Tooltip
- `Actions` = „Aktionen", `PluginOrder` = „Plugin-Reihenfolge", `Delete` = „Löschen"
- `PluginSaveFailed` = „Plugin konnte nicht gespeichert werden: {0}" — Format-Vorbild
- `PluginsLoadFailed`, `OrderSaveFailed` — weitere Fehlerformat-Vorbilder
- **Fehlen:** Tooltip/aria-label für Plugin-Entfernen, Erfolgsmeldung, Fehlermeldung

### Tests / Infrastruktur

- `Rezepte.Tests/Components/PluginSettingsComponentTests.cs` (328 Z., 12 Facts): Moq auf
  `IPluginSettingsService`; Konstruktor stuht `GetPluginsAsync → Array.Empty` (Z. 27–28);
  Helper `WithSources`, `Render`, `AssertIconButton` (prüft `i.<icon>` → Button
  `title`/`aria-label`/`ParentElement.title`, statischer Text, Z. 294–301); bUnit feuert
  Click auch auf `disabled`-Buttons → Guard im Handler testbar. **Bisher kein Plugin-Seed
  (`WithPlugins`-Pendant fehlt).**
- `Rezepte.Tests.Browser/Infrastructure/PluginSettingsAppFixture.cs`: Seed-Muster über
  `Microsoft.Data.Sqlite` direkt auf `DatabasePath` (`SeedInstalledReleaseAsync`, Z. 57–106)
  — als Vorbild für einen `PluginSettings`-Row-Seed wiederverwendbar. Tabellenschema
  `PluginSettings` (Migration `20260713182239_AddPluginSettings.cs`): `PluginId` (TEXT, PK,
  max 128), `DisplayName` (TEXT, max 200), `Description` (TEXT, null), `AssemblyName` (TEXT),
  `TypeName` (TEXT), `Enabled` (INTEGER), `OrderIndex` (INTEGER), `Status` (TEXT, max 32),
  `Error` (TEXT, null), `DiscoveredAt`/`LastSeenAt` (TEXT, Format `yyyy-MM-dd HH:mm:ss.fffffff`)
- `Rezepte.Tests.Browser/Infrastructure/PluginSettingsPageObject.cs`: nur Quellen-Locators
  (`SourceRow` per `tr:has-text`, `CheckButton` per `button:has(i.bi-*)`, Alerts
  `.alert-success`/`.alert-danger`) — wiederverwendbar, Plugin-Tabellen-Locators fehlen
- `Rezepte.Tests.Browser/PluginSettingsBrowserTests.cs`: `[SkippableFact]` +
  `Skip.IfNot(BrowsersAvailable/ApplicationAvailable)`-Muster; Collection
  `PluginSettingsTestCollection`

## Test-Ausgangszustand

| Lauf | Befehl | Arbeitsverzeichnis | Exit | Erfolgreich | Fehlgeschlagen | Übersprungen |
|------|--------|--------------------|------|-------------|----------------|--------------|
| 1 | `dotnet test Rezepte.Tests/Rezepte.Tests.csproj --filter "FullyQualifiedName~PluginSettingsComponentTests" --nologo -v q` | Repo-Root | 0 | 12 | 0 | 0 |

- Zeitpunkt: 2026-11-14 ~21:02 (lokale Zeit); Branch:
  `task/issue-179-783283f76acc4581b81051a26394df02-aktualisierung-der-plugins-schritt-5-missing-entfernen-ui`;
  Commit `bbb7d2e`; uncommittete Änderungen: nur dieses Feature-Verzeichnis (untracked);
  .NET SDK 10.0.403
- Browser-E2E nicht ausgeführt (Playwright/App-Fixture abhängig; Baseline über bUnit
  ausreichend für diesen Schritt).
