# Vertrags-Interfaces (Import-Plugin-Vertrag)

Bestandsaufnahme der Vertrags-Assemblies `Rezepte.Import.Abstractions` und `Rezepte.Import.PluginSdk`, bezogen auf die Frage der Plugin-Kompatibilität unter .NET 11. Beide Assemblies sind versioniert über `$(ImportContractVersion)` = `0.3.0` und Gegenstand des ApiCompat-Contract-Exports (`scripts/Export-ImportContract.ps1`, Baselines unter `contract-baselines/import-contract/`).

## `Rezepte.Import.Abstractions`

Dateien: `Rezepte.Import.Abstractions/*.cs`

### `IImportPlugin`

Datei: `Rezepte.Import.Abstractions/IImportPlugin.cs`

| Member | Typ | Zweck |
|--------|-----|-------|
| `Id` | `string` (get) | eindeutige Plugin-ID |
| `DisplayName` | `string` (get) | Anzeigename |
| `Description` | `string?` (get) | optionale Beschreibung |
| `Version` | `string` (get) | Plugin-Version |
| `HandlerType` | `Type` (get) | Typ des Import-Handlers |
| `DefaultPriority` | `int` (get, Default-Implementierung `0`) | Priorität bei mehreren passenden Plugins |
| `CheckUsabilityAsync(IServiceProvider, CancellationToken)` | `Task<PluginUsabilityResult>` (Default: `Usable`) | Usability-Prüfung in der Anwendungsumgebung |

### Weitere Vertragsarten (Dateinamen-Ebene)

- `IImportHandler.cs`, `ICollectionImportHandler.cs`, `IInteractiveImportHandler.cs`, `IImportInteraction.cs` — Handler- und Interaktionsverträge
- `ImportResult.cs`, `ImportedImage.cs`, `ImportedIngredient.cs`, `ImportedRecipe.cs`, `ImportedRecipeStep.cs`, `ImportCollectionModels.cs` — DTOs
- `PluginUsabilityResult.cs`, `PluginUsabilityIssue.cs` — Usability-Ergebnistypen

## `Rezepte.Import.PluginSdk`

Dateien: `Rezepte.Import.PluginSdk/*.cs`

- `ImportParserBase.cs` — Basisklasse für Parser-Implementierungen
- `ParsedIngredient.cs` — DTO
- `UrlHelpers.cs` — Hilfsfunktionen

Das Projekt referenziert `Rezepte.Import.Abstractions` und erbt seinen TFM aus `Directory.Build.props` (kein eigener `<TargetFramework>`-Eintrag).

## Verwendung im Produktivcode

- `PluginManager` (`Rezepte.Web/Services/Import/Plugins/PluginManager.cs`) lädt Plugin-Assemblies zur Laufzeit in einem eigenen `AssemblyLoadContext` (`PluginLoadContext`, collectible optional) und prüft `typeof(IImportPlugin).IsAssignableFrom(t)`; die Vertrags-Assembly selbst wird über den `Load`-Override an den Default-Kontext delegiert (Typidentität zwischen Host und Plugin).
- Die drei Plugin-Projekte (`Backup`, `AIFoto`, `AIUrl`) implementieren den Vertrag; AIFoto/AIUrl referenzieren zusätzlich `Rezepte.Web.dll` per `HintPath` mit `<Private>false</Private>` (Typen aus dem Host werden zur Laufzeit geteilt).
