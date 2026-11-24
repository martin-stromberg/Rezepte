# Build- und Plugin-Mechanik

Bestandsaufnahme der MSBuild-Mechanik und des Laufzeit-Plugin-Ladens, bezogen auf die TFM-Migration.

## MSBuild-Targets in `Rezepte.Web.csproj`

Datei: `Rezepte.Web/Rezepte.Web.csproj` (Zeilen 53–91)

| Element | Zeilen | Beschreibung |
|---------|--------|--------------|
| `ImportPluginProject`-Items | 54–62 | Deklariert die drei Plugin-Projekte mit `PluginName`-Metadatum |
| `Target BuildImportPlugins` | 65–67 | `BeforeTargets="CopyImportPluginsToOutput;CopyImportPluginsToPublish"`, Condition `'$(BuildingSolutionFile)' != 'true'` (wird bei Solution-Builds **nicht** ausgeführt). Ruft `MSBuild` Task mit `Targets="Build"` auf jedem `ImportPluginProject` auf und übergibt `Configuration=$(Configuration);TargetFramework=$(TargetFramework);RuntimeIdentifier=;SelfContained=false;RezepteWebReferencePath=$(TargetDir)Rezepte.Web.dll` — der TFM wird also an die Plugin-Builds propagiert |
| `Target CopyImportPluginsToOutput` | 69–79 | `AfterTargets="Build"`. Löscht `$(OutputPath)plugins`, sammelt `%(ImportPluginProject.Directory)bin\$(Configuration)\$(TargetFramework)\**\*.*` (ohne `plugins\` und `publish\`-Unterpfade), kopiert nach `$(OutputPath)plugins\%(PluginName)\…`. Zusätzlich `$(ExternalImportPluginsPath)\**\*.*` nur wenn Verzeichnis existiert |
| `Target CopyImportPluginsToPublish` | 81–91 | `AfterTargets="Publish"`, analog nach `$(PublishDir)plugins\%(PluginName)\…` |

Alle Pfade sind über `$(TargetFramework)` TFM-agnostisch formuliert; kein hart codiertes `net10.0` in den Targets. Beobachtung aus dem Baseline-Build: Beim Solution-Build (`BuildingSolutionFile=true`) werden die Plugins als normale Projekte der Solution gebaut; der `BuildImportPlugins`-Target greift nur bei Einzelprojekt-Builds von `Rezepte.Web.csproj`.

## MSBuild-Target in `Rezepte.Tests.Browser.csproj`

Datei: `Rezepte.Tests.Browser/Rezepte.Tests.Browser.csproj` (Zeilen 35–37)

| Element | Beschreibung |
|---------|--------------|
| `Target PublishRezepteWebForBrowserTests` | `AfterTargets="ResolveProjectReferences"`; `Inputs`/`Outputs` auf `..\Rezepte.Web\bin\$(Configuration)\$(TargetFramework)\Rezepte.Web.dll` bzw. `…\publish\Rezepte.Web.dll`; ruft `MSBuild` Task mit `Targets="Publish"` und `Configuration=$(Configuration);TargetFramework=$(TargetFramework)` auf `Rezepte.Web.csproj`. Beim Baseline-Build wurde `Rezepte.Web\bin\Release\net10.0\publish\` erzeugt (verifiziert im Build-Log) |

## Laufzeit-Plugin-Laden

### `PluginManager`

Datei: `Rezepte.Web/Services/Import/Plugins/PluginManager.cs` (534 Zeilen)

| Methode/Bereich | Zeilen | Kurzbeschreibung |
|-----------------|--------|------------------|
| `GetPluginRoots()` | 289–301 | Plugin-Wurzelverzeichnisse: `{ContentRootPath}/plugins` und `{AppContext.BaseDirectory}/plugins` (existierende, dedupliziert) |
| `GetPluginAssemblyCandidates(directory)` | 303–312 | Erwartet `{Verzeichnisname}.dll`, sonst alle `*.dll` im Verzeichnis |
| `DiscoverFromAssembly(path, useCollectibleLoadContext)` | 314 ff. | Erzeugt `PluginLoadContext`, lädt Assembly via `LoadFromAssemblyPath`, sucht `IImportPlugin`-Implementierungen; Assemblies ohne Implementierung → Descriptor mit `PluginStatus.Incompatible`, bekannte Dependency-Assemblies werden übersprungen (`IsKnownDependencyAssembly`, Zeile ~419: Vergleich mit `IImportPlugin`-Assembly-Name) |
| `PluginLoadContext` (private nested class) | 505 ff. | `AssemblyLoadContext`-Subklasse; `isCollectible`-Flag; `Load(AssemblyName)`-Override delegiert die Vertrags-Assembly (`IImportPlugin`-Assembly) an den Default-Kontext zurück, andere Abhängigkeiten über `AssemblyDependencyResolver` |

Weitere Plugin-Komponenten im selben Verzeichnis `Rezepte.Web/Services/Import/Plugins/`:

- `PluginPackageInstaller.cs` / `PluginPackageValidator.cs` — Installation/Validierung von Plugin-Paketen (ZIP), schreibt unter `{ContentRootPath}/plugins`
- `PluginDirectoryRegistry.cs` — verwaltet versionierte Plugin-Verzeichnisse und `.install-state.json`
- `PluginSettingsService.cs`, `PluginUpdateService.cs`, `PluginUpdateHostedService.cs`, `PluginStartupService.cs`, `PluginImportHandler.cs`, `PluginHandlerLease.cs`
- `PluginStatus.cs` — statische Klasse mit Statuskonstanten `Loaded`, `Missing`, `Incompatible`, `LoadFailed`, `RuntimeFailed`
- `BuiltInImportPluginCatalog.cs` — eingebaute Plugins (werden vor externen in `PluginManager` Zeile 254–256 aggregiert)

### `PluginStatus`

Datei: `Rezepte.Web/Services/Import/Plugins/PluginStatus.cs` — kein Enum, sondern `static class` mit `const string`-Werten:

| Wert | Bedeutung laut Verwendung |
|------|---------------------------|
| `Loaded` | Plugin geladen |
| `Missing` | Plugin fehlt |
| `Incompatible` | Assembly ohne `IImportPlugin`-Implementierung |
| `LoadFailed` | Ladefehler |
| `RuntimeFailed` | Laufzeitfehler |

## EF-Core-Migrationen / ModelSnapshot

Verzeichnis: `Rezepte.Web/Migrations/` — 15+ Migrationsdateien (Paare `*.Designer.cs`/`*.cs`), z. B. `20251011163456_InitialCreate`, `…AddAdminStateToUsers`, `…AddCookbooks`, `…AddRecipes`, `…AddUserIdToModels`, `…AddReceiptImages`, `…AddJobQueue`, `…ChangeRecipeCookbookRelationshipToMulti`, `…AddRecipeUri`, `…AddCookbookOrdering` (Auszug).

- `RezepteDbContextModelSnapshot.cs` Zeile 18: `modelBuilder.HasAnnotation("ProductVersion", "10.0.11")` — der Snapshot wurde zuletzt mit EF Core `10.0.11` erzeugt, obwohl die Paketreferenzen auf `10.0.15` stehen.
- Provider: `Microsoft.EntityFrameworkCore.Sqlite` `10.0.15`.

## Contract-Export und Baselines

- `scripts/Export-ImportContract.ps1` baut `Rezepte.Import.Abstractions` und `Rezepte.Import.PluginSdk` in einem Staging-Verzeichnis (`Invoke-ContractBuild`, Zeilen 579–580) und erwartet die Ergebnis-DLLs hart codiert unter `bin/Release/net10.0` (Zeile 585): `"$projectName/bin/Release/net10.0/$projectName.dll"`.
- `contract-baselines/import-contract/` enthält Baselines für Vertragsversionen `0.2.0` und `0.3.0` (je `Rezepte.Import.Abstractions.dll` + `Rezepte.Import.PluginSdk.dll`) für ApiCompat-Vergleiche.
- `ImportContractVersion` = `0.3.0` (`Directory.Build.props` Zeile 6).

## Screenshot-Generierung

- `scripts/screenshots/generate-screenshots.js` Zeile 10: hart codierter Pfad `Rezepte.Web/bin/Release/net10.0/publish/Rezepte.Web.dll` — **nicht** in der Anforderung genannt, aber betroffene Stelle.
