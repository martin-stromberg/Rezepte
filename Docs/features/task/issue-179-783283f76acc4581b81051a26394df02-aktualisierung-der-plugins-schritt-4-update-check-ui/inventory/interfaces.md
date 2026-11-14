# Interfaces

## `IPluginSettingsService`

Datei: `Rezepte.Web/Services/Import/Plugins/IPluginSettingsService.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetPluginsAsync` | `ct` | `Task<IReadOnlyList<PluginSettingsItem>>` | Pluginliste für Tabelle |
| `GetSourcesAsync` | `ct` | `Task<IReadOnlyList<PluginSourceSettingsItem>>` | Quellenliste (Admin) |
| `SaveSourceAsync` | `PluginSourceSaveRequest`, `ct` | `Task` | Quelle speichern |
| `SetSourceEnabledAsync` | `sourceId`, `enabled`, `ct` | `Task` | Aktiv-Schalter |
| `DeleteSourceAsync` | `sourceId`, `ct` | `Task` | Quelle löschen |
| **`CheckSourceForUpdatesAsync`** | `sourceId`, `ct` | `Task<PluginUpdateCheckResult>` | **Manueller Check je Quelle (Z. 45) — aus der UI aufzurufen** |
| `SetEnabledAsync` | `pluginId`, `enabled`, `ct` | `Task` | Plugin aktivieren |
| `MoveAsync` | `pluginId`, `direction`, `ct` | `Task` | Reihenfolge |
| `RemovePluginAsync` | `pluginId`, `ct` | `Task` | Vermisstes Plugin entfernen (Schritt 3/5) |

## `IPluginUpdateService`

Datei: `Rezepte.Web/Services/Import/Plugins/IPluginUpdateService.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `CheckForUpdatesAsync` | `ct` | `Task` | Start-Prüfung aller aktivierten + vertrauensbestätigten Quellen (von `PluginUpdateHostedService` aufgerufen) |
| `CheckSourceForUpdatesAsync` | `sourceId`, `ct` | `Task<PluginUpdateCheckResult>` | Einzelquellen-Check mit Vorbedingungen; wird von `PluginSettingsService` delegiert |

Kein Interface nennt einen „Check läuft gerade"-Status — eine Busy-Anzeige für fremd ausgelöste Checks ist ohne Erweiterung nicht abfragbar (siehe offene Frage in `requirement.md`).
