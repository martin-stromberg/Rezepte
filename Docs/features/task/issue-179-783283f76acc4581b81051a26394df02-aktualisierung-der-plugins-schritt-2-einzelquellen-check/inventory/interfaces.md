# Interfaces — Bestandsaufnahme (Schritt 2: Einzelquellen-Check)

Vollständige Plugin-Interface-Liste: [Projekt-inventory/interfaces.md](../../../../projects/task/issue-179-783283f76acc4581b81051a26394df02-aktualisierung-der-plugins/inventory/interfaces.md).

## `IPluginUpdateService`

Datei: `Rezepte.Web/Services/Import/Plugins/IPluginUpdateService.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `CheckForUpdatesAsync` | `CancellationToken ct = default` | `Task` | Start-Lauf über alle aktivierten, vertrauensbestätigten Quellen; einzige vorhandene Methode — kein Einzelquellen-Einstieg, kein Ergebnis. |

**Schritt-2-Befund:** Hier kommt die neue Methode hin (Vorschlag `CheckSourceForUpdatesAsync(string sourceId, CancellationToken)` mit Ergebnis-Rückgabe). Aufrufer heute: nur `PluginUpdateHostedService.StartAsync`.

## `IPluginSettingsService`

Datei: `Rezepte.Web/Services/Import/Plugins/IPluginSettingsService.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetPluginsAsync` | `ct` | `Task<IReadOnlyList<PluginSettingsItem>>` | Plugin-Liste inkl. Usability. |
| `GetSourcesAsync` | `ct` | `Task<IReadOnlyList<PluginSourceSettingsItem>>` | Quellen-Liste (Admin) inkl. Statusfeldern — Sichtbarkeit der Check-Ergebnisse bereits gegeben. |
| `SaveSourceAsync` | `PluginSourceSaveRequest, ct` | `Task` | Quelle anlegen/aktualisieren (Admin). |
| `SetSourceEnabledAsync` | `string sourceId, bool enabled, ct` | `Task` | Quelle (de)aktivieren (Admin). |
| `DeleteSourceAsync` | `string sourceId, ct` | `Task` | Quelle löschen (Admin). |
| `SetEnabledAsync` | `string pluginId, bool enabled, ct` | `Task` | Plugin (de)aktivieren. |
| `MoveAsync` | `string pluginId, int direction, ct` | `Task` | Reihenfolge verschieben. |

**Schritt-2-Befund:** Hier kommt der Admin-Einstieg hin (Vorschlag `CheckSourceForUpdatesAsync(string sourceId, CancellationToken)` → Ergebnis-DTO). Aufrufer der bestehenden Methoden: `Rezepte.Web/Components/Settings/PluginSettings.razor` (Z. 215–338) — UI-Anbindung ist Scope von Schritt 4.

## Angrenzende, unverändert genutzte Interfaces

| Interface | Datei | Für Schritt 2 relevante Member |
|-----------|-------|-------------------------------|
| `IGitHubReleaseClient` | `Rezepte.Web/Services/Import/Plugins/IGitHubReleaseClient.cs` | `GetLatestReleaseAsync(GitHubRepository, string? personalAccessToken, ct) → Task<GitHubReleaseInfo?>`; `DownloadAssetAsync(GitHubReleaseAsset, string targetPath, string? personalAccessToken, ct) → Task`. Registriert als `HttpClient` (`ServiceCollectionExtensions.cs:210–214`). |
| `ISystemSecretStore` | `Rezepte.Web/Services/Import/Plugins/ISystemSecretStore.cs` | `GetAsync(name, ct) → Task<string?>` (PAT-Auflösung), `StoreAsync`, `DeleteAsync`. Scoped-Implementierung `DataProtectionSystemSecretStore`. |
| `IPluginPackageInstaller` | `Rezepte.Web/Services/Import/Plugins/IPluginPackageInstaller.cs` | `InstallAsync(...)`; `InstallWithReloadTrackingAsync(directories, options, beforeReload, ct)` mit **Default-Interface-Implementierung** (Z. 22–26: `beforeReload` dann `InstallAsync`) — Fakes in Tests implementieren nur `InstallAsync`. |
| `IPluginManager` | `Rezepte.Web/Services/Import/Plugins/IPluginManager.cs` | `CoordinateReloadAsync(replacePlugins, ct)` (Default-Impl: replace → `ReloadAsync`); `GetPluginsUsabilityAsync`. Singleton (`ServiceCollectionExtensions.cs:204`). |
| `IPluginPackageValidator` | `Rezepte.Web/Services/Import/Plugins/IPluginPackageValidator.cs` | `ValidateAsync(zipPath, ct) → Task<PluginPackageValidationResult>` — Scope-Ausschluss, unverändert. |

Kein neues eigenständiges Interface für Schritt 2 erforderlich (Anforderung: nur Erweiterung der zwei Bestehenden).
