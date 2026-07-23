# Services und Manager

## `PluginManager`
Datei: `Rezepte.Web/Services/Import/Plugins/PluginManager.cs`

Zentrale Service-Klasse für Plugin-Discovery, -Loading und Handler-Management.

### Öffentliche Methoden (IPluginManager)

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---|---|---|
| `InitializeAsync(CancellationToken ct)` | Public | Initialisiert den Manager durch Plugin-Discovery und -Synchronisierung mit der Datenbank |
| `ReloadAsync(CancellationToken ct)` | Public | Lädt alle Plugins neu (via InitializeAsync) |
| `CoordinateReloadAsync(Func<CancellationToken, Task> replacePlugins, CancellationToken ct)` | Public | Koordiniert Plugin-Reload mit einer Custom-Austausch-Funktion |
| `GetActiveHandlersAsync(IServiceProvider serviceProvider, CancellationToken ct)` | Public | Gibt Liste der aktivierten und geladenen Plugin-Handler zurück |
| `AcquireActiveHandlersAsync(IServiceProvider serviceProvider, CancellationToken ct)` | Public | Gibt eine Lease für aktive Handler mit Lock-Management zurück |
| `DiscoverFromDirectory(string pluginRoot, bool unloadAfterDiscovery)` | Public | Sucht nach Plugins in einem Verzeichnis (für externe/kurzfristige Discovery) |

### Private Methoden

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---|---|---|
| `InitializeCoreAsync(CancellationToken ct)` | Private | Kernlogik: Discover → Synchronize mit DB |
| `DiscoverPlugins()` | Private | Sucht nach integrierten und externen Plugins |
| `DiscoverExternalPlugins(IEnumerable<string> pluginRoots, bool useCollectibleLoadContext)` | Private | Sucht nach externen Plugins in Verzeichnissen |
| `DiscoverFromAssembly(string path, bool useCollectibleLoadContext)` | Private | Lädt Assembly und sucht nach `IImportPlugin`-Implementierungen |
| `CreateActiveHandlersAsync(IServiceProvider serviceProvider, CancellationToken ct)` | Private | Erstellt Handler-Instanzen für aktivierte Plugins |
| `SynchronizeSettingsAsync(RezepteDbContext db, IReadOnlyList<ImportPluginDescriptor> discovered, CancellationToken ct)` | Private Static | Synchronisiert entdeckte Plugins mit PluginSetting-Entities in der DB |
| `MarkRuntimeFailureAsync(IServiceProvider serviceProvider, string pluginId, string error, CancellationToken ct)` | Private Static | Kennzeichnet Plugin als RuntimeFailed und speichert Fehlermeldung |
| `GetPluginRoots()` | Private | Gibt Liste der Verzeichnisse zurück, in denen nach Plugins gesucht wird |
| `IsKnownDependencyAssembly(string path)` | Private Static | Prüft, ob Assembly die `IImportPlugin`-Abstraktion selbst ist |
| `SelectPreferredDescriptor(IEnumerable<ImportPluginDescriptor> descriptors)` | Private Static | Wählt bevorzugte Plugin-Version aus mehreren Versionen |

### Abonnierte Events
Keine.

### Publizierte Events
Keine.

**Status:** Nutzbarkeitsprüfungs-Logik ist **nicht vorhanden**. Nur Status-Tracking vorhanden (Loaded, Missing, Incompatible, LoadFailed, RuntimeFailed).

---

## `PluginSettingsService`
Datei: `Rezepte.Web/Services/Import/Plugins/PluginSettingsService.cs`

Service für die Verwaltung von Plugin-Einstellungen und Plugin-Quellen.

### Öffentliche Methoden (IPluginSettingsService)

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---|---|---|
| `GetPluginsAsync(CancellationToken ct)` | Public | Gibt alle Plugin-Einstellungen sortiert nach OrderIndex und DisplayName zurück |
| `GetSourcesAsync(CancellationToken ct)` | Public | Gibt alle Plugin-Quellen (GitHub-Repositories) zurück; erfordert Admin-Berechtigung |
| `SaveSourceAsync(PluginSourceSaveRequest request, CancellationToken ct)` | Public | Speichert oder aktualisiert eine Plugin-Quelle; erfordert Admin-Berechtigung |
| `SetSourceEnabledAsync(string sourceId, bool enabled, CancellationToken ct)` | Public | Aktiviert/deaktiviert eine Plugin-Quelle; erfordert Admin-Berechtigung |
| `DeleteSourceAsync(string sourceId, CancellationToken ct)` | Public | Löscht eine Plugin-Quelle; erfordert Admin-Berechtigung |
| `SetEnabledAsync(string pluginId, bool enabled, CancellationToken ct)` | Public | Aktiviert/deaktiviert ein Plugin |
| `MoveAsync(string pluginId, int direction, CancellationToken ct)` | Public | Ändert die Sortierungsreihenfolge eines Plugins |

**Status:** Nutzbarkeitsprüfungs-Methoden sind **nicht vorhanden**.

---

## `PluginUpdateHostedService`
Datei: `Rezepte.Web/Services/Import/Plugins/PluginUpdateHostedService.cs`

Background-Service für periodische Plugin-Updates von konfigurierten Quellen.

Wird wahrscheinlich vom `PluginUpdateService` unterstützt.

**Status:** Nur für Plugin-Updates zuständig, nicht für Nutzbarkeitsprüfungen.

---

## `PluginStartupService`
Datei: `Rezepte.Web/Services/Import/Plugins/PluginStartupService.cs`

Service für die Initialisierung des Plugin-Managers beim Anwendungsstart.

**Status:** Nur für Startup zuständig.
