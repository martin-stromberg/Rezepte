# Tests

## Testklassen

### `PluginManagerTests`
Datei: `Rezepte.Tests/Services/Import/PluginManagerTests.cs`

Umfangreiche Test-Suite für `PluginManager`-Funktionalität.

| Testmethode | Was wird getestet? |
|---|---|
| `InitializeAsync_ShouldDiscoverExternalPluginDirectlyUnderPlugins` | Externe Plugins werden direkt im `plugins/`-Verzeichnis entdeckt |
| `InitializeAsync_ShouldDiscoverExternalPluginFromSubfolderWithAdjacentAbstractionsAssembly` | Externe Plugins werden in Unterordnern mit `IImportPlugin`-Assembly entdeckt |
| `InitializeAsync_ShouldDiscoverExternalPluginFromApplicationBaseDirectory` | Externe Plugins werden im `AppContext.BaseDirectory/plugins/` entdeckt |
| `InitializeAsync_ShouldMarkBrokenDllAsLoadFailed` | Beschädigte DLLs werden als `LoadFailed` gekennzeichnet |
| `InitializeAsync_ShouldIgnoreAdjacentContractAssemblyWithoutPlugin` | Die `IImportPlugin`-Abstraktion selbst wird ignoriert |
| `InitializeAsync_ShouldDiscoverProductiveExternalImportPlugins` | Produktive External-Plugins (AIFoto, AIUrl, Backup) werden entdeckt |
| `InitializeAsync_ShouldLoadPublishedExternalChefkochPluginWithoutAdjacentContractAssembly` | Externe Plugins ohne lokale Abstraktion werden geladen |
| `InitializeAsync_ShouldLoadAllPublishedExternalOnlinePluginsWithoutAdjacentContractAssembly` | Alle veröffentlichten Online-Plugins werden geladen |
| `InitializeAsync_ShouldUseDefaultPriorityForInitialOrder` | Priorität aus `IImportPlugin.DefaultPriority` wird für initiale Reihenfolge verwendet |
| `InitializeAsync_ShouldMarkPluginWithInvalidHandlerTypeAsIncompatible` | Plugins mit ungültigem Handler-Type werden als `Incompatible` gekennzeichnet |
| `InitializeAsync_ShouldKeepExistingOrderAndAppendNewPlugins` | Sortierungsreihenfolge wird beibehalten, neue Plugins werden angehängt |
| `InitializeAsync_ShouldMarkPreviouslyConfiguredPluginAsMissing` | Plugins, die nicht mehr entdeckt werden, werden als `Missing` gekennzeichnet |
| `DiscoverFromDirectory_ShouldNotKeepAssembliesLoadedFromTemporaryDirectory` | Assemblies von kurzfristigen Discoveries werden entladen |
| `GetActiveHandlersAsync_ShouldNotInstantiateDisabledPlugins` | Deaktivierte Plugins werden nicht zu Handlern instantiiert |

**Status:** Tests fokussieren auf Plugin-Discovery und -Loading. Keine Tests für **Nutzbarkeitsprüfung** vorhanden.

---

### `PluginSettingsServiceTests`
Datei: `Rezepte.Tests/Services/Import/PluginSettingsServiceTests.cs`

Test-Suite für `PluginSettingsService`-Funktionalität.

| Testmethode | Was wird getestet? |
|---|---|
| `SetEnabledAsync_ShouldPersistActivation` | Aktivierung/Deaktivierung wird persistiert |
| `MoveAsync_ShouldSwapOrderWithNeighbor` | Sortierungsreihenfolge wird korrekt geändert |
| `SaveSourceAsync_ShouldCanonicalizeGlobalSourceAndStorePatOnlyInSecretStore` | GitHub-Quelle wird normalisiert und PAT sicher gespeichert |
| `SaveSourceAsync_ShouldRequireTrustConfirmationForNewSource` | Neue Quellen benötigen Vertrauensbestätigung |

**Status:** Tests fokussieren auf Settings- und Source-Verwaltung. Keine Tests für **Nutzbarkeitsprüfung**.

---

## Hilfsmethoden und Fixtures

### `PluginWorkspace`
Datei: `Rezepte.Tests/Services/Import/` (in Testdateien)

Hilfklasse für Test-Workspace-Management.

| Methode | Zweck |
|---|---|
| `Create()` | Erstellt einen temporären Workspace mit `plugins/`-Verzeichnis |
| `CreateWithoutPluginRoot()` | Erstellt einen Workspace ohne `plugins/`-Verzeichnis |
| `CopyFixturePlugin(string targetPath)` | Kopiert ein Test-Fixture-Plugin zu einem Zielort |
| `CopyProductivePlugins()` | Kopiert produktive Plugins (AIFoto, AIUrl, Backup) |
| `ExternalPluginRepositoryExists()` | Prüft, ob externes Plugin-Repository vorhanden ist |

---

### `FakeSecretStore`
Hilfklasse für Tests, die ein Secret-Store-Mock bereitstellt.

---

## Zusammenfassung Test-Abdeckung

**Vorhanden:**
- Discovery von Plugins (intern und extern)
- Loading und Instantiation von Plugins
- Fehlerbehandlung beim Plugin-Laden
- Plugin-Settings-Verwaltung
- GitHub-Source-Verwaltung
- Aktivierung/Deaktivierung von Plugins
- Sortierungsreihenfolge

**Nicht vorhanden:**
- Tests für **Nutzbarkeitsprüfung** (`GetUsabilityAsync()`, etc.)
- Tests für Fehlerursachen-Erkennung (fehlende Credentials, deaktivierte Einstellungen)
- Tests für Lösungsvorschläge-Generierung
- Tests für Nutzbarkeits-Persistierung in der Datenbank
