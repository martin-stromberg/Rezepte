# Plan-Review: Pluginsystem fuer Rezeptimporte

Status: Offene Aufgaben vorhanden

## Zusammenfassung

Der Repository-Zustand nach Iteration 3 erfuellt grosse Teile der geplanten Infrastruktur: Shared-Projekt, Plugin-Vertraege, persistierte Plugin-Einstellungen, Admin-UI, Start-Synchronisation, externe DLL-Erkennung, Build-/Publish-Kopierziele, PluginManager-Nutzung in `ImportService` und `ImportOrchestrator` sowie erweiterte Tests sind vorhanden.

Der verbindliche Zielzustand ist trotzdem noch nicht vollstaendig erreicht. Die vorhandenen Importquellen liegen weiterhin im Webprojekt und werden ueber einen Built-in-Katalog als Plugins exponiert, statt in separate Pluginprojekte ausgelagert zu sein. Ausserdem speichern die vorhandenen Handler weiterhin direkt ueber Host-Services, sodass die geplante Trennung "Plugin liefert neutrale Rezeptdaten, Host persistiert" noch nicht umgesetzt ist.

## Umgesetzte Planelemente

- `Rezepte.Import.Abstractions` existiert und ist in der Solution aufgenommen; Web, Tests und das Test-Fixture referenzieren das Shared-Projekt (`Rezepte.sln:10`, `Rezepte.Web/Rezepte.Web.csproj:47`, `Rezepte.Tests/Rezepte.Tests.csproj:25`, `Rezepte.Tests.PluginFixture/Rezepte.Tests.PluginFixture.csproj:9`).
- Shared-Vertraege fuer Plugins, Handler und Interaktion liegen im Shared-Projekt (`Rezepte.Import.Abstractions/IImportPlugin.cs:3`, `Rezepte.Import.Abstractions/IImportHandler.cs:3`, `Rezepte.Import.Abstractions/IInteractiveImportHandler.cs:3`, `Rezepte.Import.Abstractions/IImportInteraction.cs:3`).
- Neutrale DTOs fuer importierte Rezeptdaten wurden ergaenzt (`Rezepte.Import.Abstractions/ImportedRecipe.cs:3`, `Rezepte.Import.Abstractions/ImportedRecipeStep.cs:3`, `Rezepte.Import.Abstractions/ImportedIngredient.cs:3`, `Rezepte.Import.Abstractions/ImportedImage.cs:3`).
- Plugin-Persistenz ist als eigene Tabelle umgesetzt: Entity, `DbSet`, Modellkonfiguration und Migration sind vorhanden (`Rezepte.Web/Entities/PluginSetting.cs:5`, `Rezepte.Web/Data/RezepteDbContext.cs:22`, `Rezepte.Web/Data/RezepteDbContext.cs:245`, `Rezepte.Web/Migrations/20260713182239_AddPluginSettings.cs:14`).
- Admin-Verwaltung fuer Plugins ist vorhanden und erlaubt Aktivieren/Deaktivieren sowie Hoch/Runter-Sortierung (`Rezepte.Web/Components/Settings/PluginSettings.razor:41`, `Rezepte.Web/Components/Settings/PluginSettings.razor:65`, `Rezepte.Web/Services/Import/Plugins/PluginSettingsService.cs:21`, `Rezepte.Web/Services/Import/Plugins/PluginSettingsService.cs:36`).
- `PluginManager` ist registriert, wird beim Start initialisiert und synchronisiert gefundene, neue, fehlende und fehlerhafte Plugins mit der Datenbank (`Rezepte.Web/Extensions/ServiceCollectionExtensions.cs:140`, `Rezepte.Web/Extensions/ServiceCollectionExtensions.cs:141`, `Rezepte.Web/Services/Import/Plugins/PluginManager.cs:37`, `Rezepte.Web/Services/Import/Plugins/PluginManager.cs:192`).
- Externe DLL-Erkennung sucht direkt unter `plugins` und in direkten Unterordnern; Unterordner werden ueber `AssemblyDependencyResolver` geladen (`Rezepte.Web/Services/Import/Plugins/PluginManager.cs:95`, `Rezepte.Web/Services/Import/Plugins/PluginManager.cs:96`, `Rezepte.Web/Services/Import/Plugins/PluginManager.cs:241`, `Rezepte.Web/Services/Import/Plugins/PluginManager.cs:245`).
- Pro Importlauf werden aktive Handler in gespeicherter Reihenfolge frisch erzeugt (`Rezepte.Web/Services/Import/Plugins/PluginManager.cs:51`, `Rezepte.Web/Services/Import/Plugins/PluginManager.cs:67`).
- `ImportService` und `ImportOrchestrator` verwenden den PluginManager und behalten die definierte Fehlermeldung bei, wenn kein Plugin passt (`Rezepte.Web/Services/Import/ImportService.cs:6`, `Rezepte.Web/Services/Import/ImportService.cs:20`, `Rezepte.Web/Services/Import/ImportService.cs:55`, `Rezepte.Web/Services/Import/ImportOrchestrator.cs:8`, `Rezepte.Web/Services/Import/ImportOrchestrator.cs:63`, `Rezepte.Web/Services/Import/ImportOrchestrator.cs:118`).
- Feste `IImportHandler`-Registrierungen wurden aus `ServiceCollectionExtensions` entfernt; sichtbar sind nur noch ImportService, PluginManager, Startup-Service, PluginSettingsService und Orchestrator (`Rezepte.Web/Extensions/ServiceCollectionExtensions.cs:139` bis `Rezepte.Web/Extensions/ServiceCollectionExtensions.cs:151`).
- `FourthSourceUrlReceiptImportHandler` und `SixthSourceUrlRecipeImportHandler` liegen jetzt in getrennten Dateien (`Rezepte.Web/Services/Import/Url/FourthSourceUrlReceiptImportHandler.cs:9`, `Rezepte.Web/Services/Import/Url/SixthSourceUrlRecipeImportHandler.cs:10`).
- Build-/Publish-Kopierlogik fuer `Rezepte.Import.Plugins.*`-Projekte ist im Webprojekt vorbereitet (`Rezepte.Web/Rezepte.Web.csproj:50`, `Rezepte.Web/Rezepte.Web.csproj:52`, `Rezepte.Web/Rezepte.Web.csproj:63`, `Rezepte.Web/Rezepte.Web.csproj:65`).
- Tests fuer Plugin-Erkennung, Unterordner, Reihenfolge, deaktivierte Plugins, fehlerhafte/inkompatible DLLs, ImportService, ImportOrchestrator mit interaktiven Plugins und PluginSettingsService sind vorhanden (`Rezepte.Tests/Services/Import/PluginManagerTests.cs`, `Rezepte.Tests/Services/Import/ImportServicePluginTests.cs`, `Rezepte.Tests/Services/Import/ImportOrchestratorTests.cs`, `Rezepte.Tests/Services/Import/PluginSettingsServiceTests.cs`).

## Offene Aufgaben

- Die geforderten Pluginprojekte pro vorhandener Importquelle fehlen weiterhin. Die Solution enthaelt nur `Rezepte.Web`, `Rezepte.Tests`, `Rezepte.Import.Abstractions` und `Rezepte.Tests.PluginFixture`, aber keine Projekte wie `Rezepte.Import.Plugins.Backup`, `Rezepte.Import.Plugins.Chefkoch`, `Rezepte.Import.Plugins.SecondSource`, `Rezepte.Import.Plugins.ThirdSource`, `Rezepte.Import.Plugins.FourthSource`, `Rezepte.Import.Plugins.FifthSource`, `Rezepte.Import.Plugins.SixthSource`, `Rezepte.Import.Plugins.AIFoto` oder `Rezepte.Import.Plugins.AIUrl` (`Rezepte.sln:6`, `Rezepte.sln:8`, `Rezepte.sln:10`, `Rezepte.sln:12`).
- Die vorhandenen Quellenhandler sind nicht aus `Rezepte.Web` ausgelagert. `BuiltInImportPluginCatalog` registriert alle neun Web-Handler weiterhin direkt als Built-in-Plugins (`Rezepte.Web/Services/Import/Plugins/BuiltInImportPluginCatalog.cs:9` bis `Rezepte.Web/Services/Import/Plugins/BuiltInImportPluginCatalog.cs:17`).
- Die geplante Architektur "Plugins liefern neutrale Rezeptdaten, der Host persistiert" ist noch nicht umgesetzt. Die neutralen DTOs existieren, werden von den bestehenden Importhandlern aber nicht als Rueckgabeweg verwendet.
- Hostseitiges Mapping von neutralen Import-DTOs nach `Recipe`, `RecipeStep`, `RecipeIngredient`, Bildern und Cookbook-Zuordnung fehlt. Stattdessen speichern bestehende Handler weiter direkt ueber `IRecipeService`, z. B. Backup, URL-Imports und AI-Imports (`Rezepte.Web/Services/Import/BackupImportHandler.cs:13`, `Rezepte.Web/Services/Import/BackupImportHandler.cs:66`, `Rezepte.Web/Services/Import/Url/BaseUrlReceiptImportHandler.cs:8`, `Rezepte.Web/Services/Import/Url/BaseUrlReceiptImportHandler.cs:385`, `Rezepte.Web/Services/Import/BaseAIImportHandler.cs:11`, `Rezepte.Web/Services/Import/BaseAIImportHandler.cs:135`).
- Allgemeine Parser-/Basishilfen sind nicht in das Shared-Projekt verschoben. `BaseImportHandler` liegt weiterhin im Webprojekt und verwendet Web-Entity-Typen (`Rezepte.Web/Services/Import/BaseImportHandler.cs:1`, `Rezepte.Web/Services/Import/BaseImportHandler.cs:7`).
- Die Akzeptanzpunkte "Alle vorhandenen Importquellen liegen in separaten Pluginprojekten" und "Pluginprojekte referenzieren nur `Rezepte.Import.Abstractions` und noetige externe Pakete" sind mangels solcher Pluginprojekte nicht erfuellt.
- Die vorbereitete Build-/Publish-Kopierlogik fuer `Rezepte.Import.Plugins.*` greift aktuell faktisch nicht fuer produktive Importquellen, weil solche Projekte noch nicht existieren (`Rezepte.Web/Rezepte.Web.csproj:52`, `Rezepte.Web/Rezepte.Web.csproj:65`).

## Verifikation

- Ausgefuehrt: `dotnet test Rezepte.sln --no-restore --logger "console;verbosity=minimal"`
- Ergebnis: Bestanden, 137 Tests erfolgreich, 0 fehlgeschlagen, 0 uebersprungen.
- Hinweis: Die bekannte NuGet-Warnung `NU1903` fuer `SQLitePCLRaw.lib.e_sqlite3` 2.1.11 mit hoher Schwere wird weiterhin gemeldet.

## Bewertung

Iteration 3 hat mehrere Luecken aus Iteration 2 geschlossen, insbesondere neutrale DTO-Dateien, Fourth/Sixth-Dateitrennung sowie Orchestrator- und Fehler-DLL-Tests. Fuer `Vollstaendig umgesetzt` fehlen aber weiterhin die Kernpunkte des Zielbilds: echte Pluginprojekte fuer alle vorhandenen Quellen, Auslagerung der Handler aus `Rezepte.Web`, Nutzung der neutralen DTOs als Plugin-Rueckgabe und hostseitiges Mapping/Persistenz ausserhalb der Plugins.
