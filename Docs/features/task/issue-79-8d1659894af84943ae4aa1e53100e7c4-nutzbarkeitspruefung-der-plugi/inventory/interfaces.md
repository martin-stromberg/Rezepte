# Interfaces und Abstraktion

## `IImportPlugin`
Datei: `Rezepte.Import.Abstractions/IImportPlugin.cs`

| Eigenschaft / Methode | Typ | Beschreibung |
|---|---|---|
| `Id` | `string` (Property) | Eindeutige Kennung des Plugins |
| `DisplayName` | `string` (Property) | Anzeigebezeichnung des Plugins |
| `Description` | `string?` (Property) | Optionale Beschreibung des Plugins |
| `Version` | `string` (Property) | Versionsnummer des Plugins |
| `HandlerType` | `Type` (Property) | Typ des Import-Handlers, der das Plugin implementiert |
| `DefaultPriority` | `int` (Property, default 0) | Standard-Priorität des Plugins |

**Status:** Nutzbarkeitsprüfungs-Methode `GetUsabilityAsync()` oder ähnlich ist **nicht vorhanden**.

---

## `IImportHandler`
Datei: `Rezepte.Import.Abstractions/IImportHandler.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---|---|---|---|
| `UserId` | `set` | `void` | Setter zur Festlegung der Benutzer-ID |
| `CanHandleAsync` | `Stream stream, string fileName, CancellationToken ct = default` | `Task<bool>` | Prüft, ob der Handler einen Stream verarbeiten kann |
| `HandleAsync` | `Stream stream, string fileName, string? uri, string targetCookbookId, string userId, CancellationToken ct = default` | `Task<ImportResult>` | Verarbeitet einen Stream und importiert Rezepte |

**Status:** Nutzbarkeitsprüfungs-Funktionalität ist **nicht vorhanden**.

---

## `IPluginManager`
Datei: `Rezepte.Web/Services/Import/Plugins/IPluginManager.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---|---|---|---|
| `InitializeAsync` | `CancellationToken ct = default` | `Task` | Initialisiert den Plugin-Manager durch Discovery und Loading |
| `DiscoverFromDirectory` | `string pluginRoot, bool unloadAfterDiscovery = false` | `IReadOnlyList<ImportPluginDescriptor>` | Sucht nach Plugins in einem spezifischen Verzeichnis (Default-Implementierung gibt leere Liste) |
| `GetActiveHandlersAsync` | `IServiceProvider serviceProvider, CancellationToken ct = default` | `Task<IReadOnlyList<PluginImportHandler>>` | Gibt eine Liste der aktiv aktivierten Plugin-Handler zurück |
| `AcquireActiveHandlersAsync` | `IServiceProvider serviceProvider, CancellationToken ct = default` | `async Task<PluginHandlerLease>` | Gibt eine Lease (Sperre) für aktive Handler zurück (Default-Implementierung) |
| `ReloadAsync` | `CancellationToken ct = default` | `Task` | Lädt Plugins neu (Default-Implementierung ruft InitializeAsync auf) |
| `CoordinateReloadAsync` | `Func<CancellationToken, Task> replacePlugins, CancellationToken ct = default` | `async Task` | Koordiniert Reload mit einer Austausch-Funktion (Default-Implementierung) |

**Status:** Nutzbarkeitsprüfungs-Methode ist **nicht vorhanden**.

---

## `IPluginSettingsService`
Datei: `Rezepte.Web/Services/Import/Plugins/IPluginSettingsService.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---|---|---|---|
| `GetPluginsAsync` | `CancellationToken ct = default` | `Task<IReadOnlyList<PluginSettingsItem>>` | Gibt alle gespeicherten Plugin-Einstellungen ab |
| `GetSourcesAsync` | `CancellationToken ct = default` | `Task<IReadOnlyList<PluginSourceSettingsItem>>` | Gibt alle konfigurieren Plugin-Quellen (GitHub) ab |
| `SaveSourceAsync` | `PluginSourceSaveRequest request, CancellationToken ct = default` | `Task` | Speichert oder aktualisiert eine Plugin-Quelle |
| `SetSourceEnabledAsync` | `string sourceId, bool enabled, CancellationToken ct = default` | `Task` | Aktiviert oder deaktiviert eine Plugin-Quelle |
| `DeleteSourceAsync` | `string sourceId, CancellationToken ct = default` | `Task` | Löscht eine Plugin-Quelle |
| `SetEnabledAsync` | `string pluginId, bool enabled, CancellationToken ct = default` | `Task` | Aktiviert oder deaktiviert ein Plugin |
| `MoveAsync` | `string pluginId, int direction, CancellationToken ct = default` | `Task` | Ändert die Reihenfolge eines Plugins (Direction: -1 = oben, +1 = unten) |

**Status:** Nutzbarkeitsprüfungs-Methode ist **nicht vorhanden**.
