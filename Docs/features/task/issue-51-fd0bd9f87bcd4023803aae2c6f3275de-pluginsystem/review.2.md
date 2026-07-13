# Plan-Review: Pluginsystem fuer Rezeptimporte

Status: Offene Aufgaben vorhanden

## Zusammenfassung

Der Repository-Zustand nach Iteration 2 setzt wesentliche Infrastrukturteile des Plans um: Shared-Projekt, Shared-Interfaces, persistierte Plugin-Einstellungen, Admin-UI, PluginManager mit Start-Synchronisation, externe DLL-Erkennung, Build-/Publish-Kopierziele sowie die Umstellung von `ImportService` und `ImportOrchestrator` auf den PluginManager sind vorhanden.

Der verbindliche Zielzustand ist aber weiterhin nicht vollstaendig erreicht. Die vorhandenen Importquellen wurden nicht in separate Pluginprojekte ausgelagert, die bestehenden Web-Handler speichern weiterhin direkt ueber Host-Services, neutrale Import-DTOs samt hostseitigem Mapper fehlen, und `FourthSourceUrlReceiptImportHandler.cs` enthaelt weiterhin zwei Quellenhandler. Der aktuelle Stand ist damit ein administrierbarer Plugin-/Handler-Katalog mit externer DLL-Erkennung, aber noch nicht das im Plan beschriebene ausgelagerte Quellen-Pluginsystem.

## Umgesetzte Planelemente

- `Rezepte.Import.Abstractions` existiert und ist in der Solution aufgenommen (`Rezepte.sln:10`). Web und Tests referenzieren das Shared-Projekt (`Rezepte.Web/Rezepte.Web.csproj:47`, `Rezepte.Tests/Rezepte.Tests.csproj:25`).
- Shared-Vertraege fuer Import-Plugins und Handler liegen im Shared-Projekt: `IImportPlugin`, `IImportHandler`, `IInteractiveImportHandler`, `IImportInteraction` und `ImportResult` (`Rezepte.Import.Abstractions/IImportPlugin.cs`, `Rezepte.Import.Abstractions/IImportHandler.cs`, `Rezepte.Import.Abstractions/IInteractiveImportHandler.cs`, `Rezepte.Import.Abstractions/IImportInteraction.cs`, `Rezepte.Import.Abstractions/ImportResult.cs`).
- Web nutzt das Shared-Projekt ueber ein globales Using (`Rezepte.Web/GlobalUsings.cs:1`); die alten Web-lokalen Import-Interface-Dateien sind geloescht.
- Plugin-Persistenz ist als eigene Tabelle umgesetzt: `DbSet<PluginSetting>`, Entity, Modellkonfiguration und Migration sind vorhanden (`Rezepte.Web/Data/RezepteDbContext.cs:22`, `Rezepte.Web/Entities/PluginSetting.cs:7`, `Rezepte.Web/Data/RezepteDbContext.cs:245`, `Rezepte.Web/Migrations/20260713182239_AddPluginSettings.cs:14`).
- Admin-Verwaltung ist in den Einstellungen eingehangen und kann Aktivierung sowie Reihenfolge speichern (`Rezepte.Web/ViewModels/SettingsViewModel.cs:32`, `Rezepte.Web/Components/Settings/PluginSettings.razor:41`, `Rezepte.Web/Components/Settings/PluginSettings.razor:65`, `Rezepte.Web/Services/Import/Plugins/PluginSettingsService.cs:21`, `Rezepte.Web/Services/Import/Plugins/PluginSettingsService.cs:36`).
- `PluginManager` ist registriert und wird beim Start initialisiert (`Rezepte.Web/Extensions/ServiceCollectionExtensions.cs:140`, `Rezepte.Web/Extensions/ServiceCollectionExtensions.cs:141`).
- `PluginManager` sucht DLLs direkt unter `plugins` und in direkten Unterordnern, nutzt `AssemblyDependencyResolver`, synchronisiert neue bzw. fehlende Plugins und erzeugt Handler pro Abruf frisch (`Rezepte.Web/Services/Import/Plugins/PluginManager.cs:95`, `Rezepte.Web/Services/Import/Plugins/PluginManager.cs:97`, `Rezepte.Web/Services/Import/Plugins/PluginManager.cs:182`, `Rezepte.Web/Services/Import/Plugins/PluginManager.cs:231`, `Rezepte.Web/Services/Import/Plugins/PluginManager.cs:67`).
- Aktive Plugins werden nach `Enabled`, Status `Loaded` und `OrderIndex` gefiltert (`Rezepte.Web/Services/Import/Plugins/PluginManager.cs:51`).
- `ImportService` und `ImportOrchestrator` verwenden den PluginManager statt einer direkt injizierten `IEnumerable<IImportHandler>` (`Rezepte.Web/Services/Import/ImportService.cs:6`, `Rezepte.Web/Services/Import/ImportService.cs:20`, `Rezepte.Web/Services/Import/ImportOrchestrator.cs:8`, `Rezepte.Web/Services/Import/ImportOrchestrator.cs:63`).
- Feste `IImportHandler`-Registrierungen wurden aus `ServiceCollectionExtensions` entfernt; sichtbar sind nur noch `IImportService`, `IPluginManager`, `PluginStartupService`, `IPluginSettingsService` und `ImportOrchestrator` (`Rezepte.Web/Extensions/ServiceCollectionExtensions.cs:139` bis `Rezepte.Web/Extensions/ServiceCollectionExtensions.cs:151`).
- Build-/Publish-Kopierlogik fuer `Rezepte.Import.Plugins.*`-DLLs ist ergaenzt (`Rezepte.Web/Rezepte.Web.csproj:50`, `Rezepte.Web/Rezepte.Web.csproj:52`, `Rezepte.Web/Rezepte.Web.csproj:60`, `Rezepte.Web/Rezepte.Web.csproj:62`).
- Tests fuer externe Plugin-Erkennung direkt unter `plugins`, Unterordner-Erkennung, Reihenfolge/Anhaengen, fehlende Plugins, deaktivierte Plugins, ImportService-Auswahl und PluginSettingsService sind vorhanden (`Rezepte.Tests/Services/Import/PluginManagerTests.cs`, `Rezepte.Tests/Services/Import/ImportServicePluginTests.cs`, `Rezepte.Tests/Services/Import/PluginSettingsServiceTests.cs`).

## Offene Aufgaben

- Die geforderten Pluginprojekte pro vorhandener Importquelle fehlen weiterhin. Die Solution enthaelt `Rezepte.Import.Abstractions` und ein Test-Fixture-Projekt, aber keine Projekte wie `Rezepte.Import.Plugins.Backup`, `Rezepte.Import.Plugins.Chefkoch`, `Rezepte.Import.Plugins.SecondSource`, `Rezepte.Import.Plugins.ThirdSource`, `Rezepte.Import.Plugins.FourthSource`, `Rezepte.Import.Plugins.FifthSource`, `Rezepte.Import.Plugins.SixthSource`, `Rezepte.Import.Plugins.AIFoto` oder `Rezepte.Import.Plugins.AIUrl` (`Rezepte.sln:10`, `Rezepte.sln:12`).
- Die vorhandenen Quellenhandler sind nicht aus `Rezepte.Web` ausgelagert. `BuiltInImportPluginCatalog` registriert die Web-Handler weiterhin direkt als Built-in-Plugins (`Rezepte.Web/Services/Import/Plugins/BuiltInImportPluginCatalog.cs:9` bis `Rezepte.Web/Services/Import/Plugins/BuiltInImportPluginCatalog.cs:17`).
- Plugins liefern noch keine neutralen Rezeptdaten an den Host. Das Shared-Projekt enthaelt nur Interface-/Result-Dateien und keine DTOs wie `ImportedRecipe`, `ImportedRecipeStep`, `ImportedIngredient` oder `ImportedImage` (`Rezepte.Import.Abstractions/`).
- Hostseitiges Mapping von neutralen Import-DTOs nach `Recipe`, `RecipeStep`, `RecipeIngredient`, Bildern und Cookbook-Zuordnung fehlt. Stattdessen speichern bestehende Handler weiter direkt ueber `IRecipeService`, z. B. `BackupImportHandler` (`Rezepte.Web/Services/Import/BackupImportHandler.cs:13`, `Rezepte.Web/Services/Import/BackupImportHandler.cs:66`), `BaseUrlReceiptImportHandler` (`Rezepte.Web/Services/Import/Url/BaseUrlReceiptImportHandler.cs:8`, `Rezepte.Web/Services/Import/Url/BaseUrlReceiptImportHandler.cs:385`, `Rezepte.Web/Services/Import/Url/BaseUrlReceiptImportHandler.cs:531`) und `BaseAIImportHandler` (`Rezepte.Web/Services/Import/BaseAIImportHandler.cs:11`, `Rezepte.Web/Services/Import/BaseAIImportHandler.cs:135`).
- Allgemeine Parser-/Basishilfen sind nicht in das Shared-Projekt verschoben. `BaseImportHandler` liegt weiterhin im Webprojekt (`Rezepte.Web/Services/Import/BaseImportHandler.cs:7`).
- `FourthSourceUrlReceiptImportHandler.cs` ist weiterhin nicht aufgetrennt. Die Datei enthaelt sowohl `SixthSourceUrlRecipeImportHandler` als auch `FourthSourceUrlReceiptImportHandler` (`Rezepte.Web/Services/Import/Url/FourthSourceUrlReceiptImportHandler.cs:12`, `Rezepte.Web/Services/Import/Url/FourthSourceUrlReceiptImportHandler.cs:119`).
- Die Akzeptanzpunkte "Alle vorhandenen Importquellen liegen in separaten Pluginprojekten" und "Pluginprojekte referenzieren nur `Rezepte.Import.Abstractions` und noetige externe Pakete" sind mangels solcher Pluginprojekte nicht erfuellt.
- Die Tests decken den PluginManager und `ImportService` ab, aber weiterhin nicht den sessionbasierten `ImportOrchestrator` mit interaktiven Plugins. Dieser Planpunkt ist noch offen.
- Fuer fehlerhafte oder inkompatible DLLs ist Implementierungscode vorhanden, aber ein expliziter Testfall mit einer fehlerhaften oder inkompatiblen DLL ist im aktuellen Testbestand nicht ersichtlich.

## Verifikation

- Vorhandenes `test-results.md` fuer Iteration 2 meldet `dotnet test Rezepte.sln --no-restore` als erfolgreich: 126 Tests bestanden, 0 fehlgeschlagen, 0 uebersprungen.
- Dieselbe Datei dokumentiert weiterhin `NU1903` fuer `SQLitePCLRaw.lib.e_sqlite3` 2.1.11 mit hoher Schwere.
- Fuer diesen Plan-Review wurden keine Tests erneut ausgefuehrt; die Bewertung basiert auf Plan, Inventory, vorhandenem `test-results.md` und statischer Pruefung des aktuellen Repository-Zustands.

## Bewertung

Die Iteration 2 hat mehrere Infrastruktur-Luecken aus dem vorherigen Review geschlossen, insbesondere externe Plugin-Erkennungstests und Build-/Publish-Kopierlogik. Fuer `Vollstaendig umgesetzt` fehlen aber weiterhin die Kernpunkte des Zielbilds: echte Pluginprojekte fuer alle bestehenden Quellen, neutrale Import-DTOs, hostseitiges Mapping/Persistenz ausserhalb der Plugins, Verschiebung allgemeiner Basishilfen in das Shared-Projekt, Trennung von Fourth/Sixth und ergaenzende Orchestrator-/Fehler-DLL-Tests.
