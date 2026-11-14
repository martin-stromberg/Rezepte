# Logik: Update-Check-Services

## `PluginSettingsService`

Datei: `Rezepte.Web/Services/Import/Plugins/PluginSettingsService.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `GetPluginsAsync` | public | Liste `PluginSettingsItem` inkl. Usability-Infos |
| `GetSourcesAsync` | public | `EnsureAdmin`; Liste `PluginSourceSettingsItem` mit allen Statusfeldern (Z. 64-86) |
| `SaveSourceAsync` | public | `EnsureAdmin`; Quelle anlegen/ändern, PAT im SecretStore, Duplikat-/Trust-Prüfung |
| `SetSourceEnabledAsync` | public | `EnsureAdmin`; `Enabled` umschalten |
| `DeleteSourceAsync` | public | `EnsureAdmin`; Quelle + Secret löschen |
| `CheckSourceForUpdatesAsync` | public | `EnsureAdmin`, dann Delegation an `IPluginUpdateService.CheckSourceForUpdatesAsync` (Z. 192-196) |
| `SetEnabledAsync` | public | Plugin aktivieren/deaktivieren |
| `MoveAsync` | public | Reihenfolge (`OrderIndex`) tauschen |
| `RemovePluginAsync` | public | `EnsureAdmin`; Plugin nur bei `Status == Missing` entfernen (Schritt 3) |
| `EnsureAdmin` | private | `UnauthorizedAccessException` ohne authentifizierten Admin (`IsInRole("Admin")` oder Claim `IsAdmin=true`), Z. 274-286 |

## `PluginUpdateService`

Datei: `Rezepte.Web/Services/Import/Plugins/PluginUpdateService.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `CheckForUpdatesAsync` | public | Start-Prüfung: alle `PluginSources` mit `Enabled && TrustConfirmed`, seriell `ProcessSourceAsync`; hinter `CheckGate` (Z. 29-53) |
| `CheckSourceForUpdatesAsync` | public | Einzelquelle: `CheckGate`-Wait, dann Vorbedingungen (`source != null`, `Enabled`, `TrustConfirmed` → sonst `Failed`-Result mit englischer Fehlermeldung) und `ProcessSourceAsync` (Z. 60-87) |
| `ProcessSourceAsync` | private | Setzt `LastCheckedAt`, leert `LastError`/`LastErrorAt` (Z. 91-96); PAT-Auflösung, Release-Abruf, ZIP-Asset, Release-Historie (`PluginSourceReleases`), Skip wenn bereits `Installed` → `NoUpdate(alreadyInstalled: true)`; Fehler → `MarkSourceFailureAsync` |
| `DownloadValidateInstallAsync` | private | Download, Paketvalidierung, Installation, Reload — Ergebnis `Installed(tag)` oder `Failed` |

- Serialisierung: `private static readonly SemaphoreSlim CheckGate = new(1, 1)` — ein manueller Check wartet auf eine laufende Start-Prüfung (und umgekehrt), schlägt nicht fehl. Belegt durch `PluginUpdateServiceTests_Serialization`.
- Fehlertexte im Result sind Englisch (z. B. „Plugin source is disabled.") — die UI muss ggf. eigene lokalisierte Texte für Vorbedingungs-Fehler zeigen bzw. `Error` durchreichen.

## `PluginUpdateHostedService`

Datei: `Rezepte.Web/Services/Import/Plugins/PluginUpdateHostedService.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `StartAsync` | public | Scope öffnen, `IPluginUpdateService.CheckForUpdatesAsync` aufrufen; Fehler nur geloggt (Z. 15-30) — einmalig beim Anwendungsstart |

## `PluginStartupService`

Datei: `Rezepte.Web/Services/Import/Plugins/PluginStartupService.cs` — `IHostedService` für die Plugin-Discovery (`IPluginManager`); nicht Teil dieser Anforderung.
