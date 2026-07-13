# Code-Review: Pluginsystem fuer Rezeptimporte

Status: Befunde vorhanden

## Befunde

### 1. Reale Plugin-Builds werden nicht als zusammenhaengender Pluginordner kopiert

Schweregrad: Hoch

Fundstelle:
- `Rezepte.Web/Rezepte.Web.csproj:52`
- `Rezepte.Web/Rezepte.Web.csproj:56`
- `Rezepte.Web/Rezepte.Web.csproj:62`
- `Rezepte.Web/Rezepte.Web.csproj:66`
- `Rezepte.Web/Services/Import/Plugins/PluginManager.cs:95`
- `Rezepte.Web/Services/Import/Plugins/PluginManager.cs:97`
- `Rezepte.Web/Services/Import/Plugins/PluginManager.cs:245`

Die neuen MSBuild-Targets sammeln alle DLLs aus `Rezepte.Import.Plugins.*\bin\...\*.dll`, kopieren sie aber nach `plugins\<DLL-Dateiname>\<DLL-Dateiname>.dll`. Damit landen Abhaengigkeiten nicht neben der jeweiligen Plugin-DLL, sondern in eigenen Ordnern. Beispiel: `Rezepte.Import.Plugins.Chefkoch.dll` landet unter `plugins\Rezepte.Import.Plugins.Chefkoch\...`, eine daneben erzeugte Abhaengigkeit wie `SomeDependency.dll` aber unter `plugins\SomeDependency\SomeDependency.dll`.

Der `PluginManager` sucht nur DLLs direkt unter `plugins` und in direkten Unterordnern. Fuer eine Plugin-DLL im eigenen Ordner verwendet der `AssemblyDependencyResolver` genau diesen Pluginpfad. Abhaengigkeiten aus separaten Ordnern werden dadurch nicht aufgeloest, und die zusaetzlich separat kopierten Abhaengigkeits-DLLs werden als eigene Plugin-Kandidaten gescannt und voraussichtlich als inkompatible Plugins persistiert.

Auswirkung: Sobald echte `Rezepte.Import.Plugins.*`-Projekte mit externen oder eigenen Abhaengigkeiten entstehen, koennen Imports trotz erfolgreichem Build zur Laufzeit wegen fehlender Abhaengigkeiten ausfallen. Zudem verschmutzt die Admin-Liste mit Nicht-Plugin-DLLs.

Empfehlung: Pro Pluginprojekt den kompletten Output in einen gemeinsamen Ordner `plugins\<PluginProjekt>\` kopieren, nicht pro DLL-Dateiname. Danach einen Test ergaenzen, der ein Plugin mit mindestens einer separaten Abhaengigkeits-DLL aus dem build-/publish-Layout laedt.

### 2. Die bestehenden Importquellen sind weiterhin nicht in separate Pluginprojekte ausgelagert

Schweregrad: Hoch

Fundstelle:
- `Rezepte.sln:10`
- `Rezepte.sln:12`
- `Rezepte.Web/Services/Import/Plugins/BuiltInImportPluginCatalog.cs:7`
- `Rezepte.Web/Services/Import/Plugins/BuiltInImportPluginCatalog.cs:9`
- `Rezepte.Web/Services/Import/Plugins/BuiltInImportPluginCatalog.cs:17`
- `Rezepte.Web/Services/Import/BackupImportHandler.cs:13`
- `Rezepte.Web/Services/Import/Url/FourthSourceUrlReceiptImportHandler.cs:12`
- `Rezepte.Web/Services/Import/Url/FourthSourceUrlReceiptImportHandler.cs:119`

Iteration 2 ergaenzt externe Plugin-Erkennung und ein Test-Fixture-Projekt, aber keine Produktiv-Pluginprojekte wie `Rezepte.Import.Plugins.Backup`, `Rezepte.Import.Plugins.Chefkoch`, `Rezepte.Import.Plugins.AIFoto` usw. Die vorhandenen Quellen werden stattdessen weiterhin ueber `BuiltInImportPluginCatalog` als Web-interne Handler registriert.

Damit sind zentrale Akzeptanzkriterien noch nicht erfuellt: pro vorhandener Importquelle eine eigene Klassenbibliothek, Bereitstellung als Plugin-DLL und Entkopplung der quellenabhaengigen Importlogik vom Webprojekt. Besonders sichtbar bleibt das bei `FourthSourceUrlReceiptImportHandler.cs`, das weiterhin SixthSource und FourthSource in einer Datei enthaelt.

Empfehlung: Die vorhandenen Quellen tatsaechlich in Produktiv-Pluginprojekte verschieben. Der Built-in-Katalog kann als Migrationsbruecke bestehen, sollte aber nicht als Erfuellung der Plugin-Auslagerung gelten.

### 3. Plugin-Vertraege erlauben weiterhin keine hostneutrale Rezeptuebergabe

Schweregrad: Mittel

Fundstelle:
- `Rezepte.Import.Abstractions/IImportHandler.cs:7`
- `Rezepte.Import.Abstractions/IImportHandler.cs:9`
- `Rezepte.Import.Abstractions/ImportResult.cs:3`
- `Rezepte.Web/Services/Import/BaseImportHandler.cs:7`
- `Rezepte.Web/Services/Import/Url/BaseUrlReceiptImportHandler.cs:8`
- `Rezepte.Web/Services/Import/Url/BaseUrlReceiptImportHandler.cs:383`
- `Rezepte.Web/Services/Import/Url/BaseUrlReceiptImportHandler.cs:385`
- `Rezepte.Web/Services/Import/BaseAIImportHandler.cs:8`
- `Rezepte.Web/Services/Import/BaseAIImportHandler.cs:135`
- `Rezepte.Web/Services/Import/BackupImportHandler.cs:66`

Das Shared-Projekt enthaelt nur Handler-Interfaces und ein `ImportResult` mit erzeugten Rezept-IDs. Die im Plan vorgesehenen neutralen DTOs (`ImportedRecipe`, `ImportedRecipeStep`, `ImportedIngredient`, `ImportedImage`) und ein hostseitiger Mapper fehlen. Die vorhandenen Handler speichern Rezepte weiterhin selbst ueber `IRecipeService`, und allgemeine Parser-/Basishilfen liegen weiter in `Rezepte.Web`.

Auswirkung: Ein externes Plugin kann fachlich nicht sauber nur Rezeptdaten liefern und die Host-Persistenz dem Webprojekt ueberlassen. Entweder muesste es Host-Services kennen, oder es kann keine vollstaendigen Imports abbilden. Das widerspricht der Architekturentscheidung, dass Plugins hostneutrale Daten liefern und `Rezepte.Web` Persistenz, Bilderablage und Cookbook-Zuordnung verantwortet.

Empfehlung: Den Contract auf neutrale Importdaten erweitern und die Persistenz aus den Handlern in eine Host-Mapping-Schicht verschieben. Danach die Produktiv-Plugins nur gegen `Rezepte.Import.Abstractions` und fachlich noetige externe Pakete bauen.

### 4. Wichtige Fehler- und Orchestrator-Pfade bleiben ungetestet

Schweregrad: Mittel

Fundstelle:
- `Rezepte.Tests/Services/Import/PluginManagerTests.cs:16`
- `Rezepte.Tests/Services/Import/PluginManagerTests.cs:32`
- `Rezepte.Tests/Services/Import/PluginManagerTests.cs:49`
- `Rezepte.Tests/Services/Import/PluginManagerTests.cs:73`
- `Rezepte.Tests/Services/Import/PluginManagerTests.cs:95`
- `Rezepte.Tests/Services/Import/ImportServicePluginTests.cs:13`
- `Rezepte.Web/Services/Import/ImportOrchestrator.cs:63`
- `Rezepte.Web/Services/Import/ImportOrchestrator.cs:84`

Iteration 2 schliesst mehrere Luecken aus dem vorherigen Review: externe DLL direkt unter `plugins`, Unterordner mit nebenliegender `Rezepte.Import.Abstractions.dll`, Reihenfolge/Append, `Missing`-Status und deaktivierte Plugins sind jetzt getestet. Weiterhin fehlen aber Tests fuer fehlerhafte bzw. inkompatible DLLs und fuer den sessionbasierten `ImportOrchestrator`, insbesondere mit interaktiven Plugins und Fehlerpfaden.

Auswirkung: Gerade die laut Plan sichtbaren Fehlerstatus (`LoadFailed`, `Incompatible`) und der Startseiten-Import ueber interaktive AI-Handler koennen regressieren, ohne dass die Tests anschlagen.

Empfehlung: Tests fuer eine defekte DLL, eine DLL ohne `IImportPlugin`, einen Plugin-Typ mit ungueltigem Handler sowie fuer `ImportOrchestrator` mit interaktivem Handler, Reihenfolge und Fehlerabbruch ergaenzen.

## Behobene Punkte aus dem vorherigen Code-Review

- Contract-Assembly-Sharing fuer `Rezepte.Import.Abstractions` ist im `PluginLoadContext` adressiert (`Rezepte.Web/Services/Import/Plugins/PluginManager.cs:240` bis `Rezepte.Web/Services/Import/Plugins/PluginManager.cs:242`).
- Ein Integrationstest mit externer Plugin-DLL und nebenliegender `Rezepte.Import.Abstractions.dll` ist vorhanden (`Rezepte.Tests/Services/Import/PluginManagerTests.cs:32` bis `Rezepte.Tests/Services/Import/PluginManagerTests.cs:47`).
- PluginManager-Tests fuer direkte Plugin-DLLs, Unterordner, stabile Reihenfolge, neue Plugins am Ende, `Missing`-Status und deaktivierte Plugins wurden ergaenzt.

## Fehlende Tests

- Build-/Publish-Layout mit einem echten Produktiv-Pluginprojekt und separater Abhaengigkeits-DLL im selben Pluginordner.
- Fehlerhafte DLL erzeugt `LoadFailed` und wird nicht angesprochen.
- DLL ohne `IImportPlugin` erzeugt `Incompatible` und bleibt in der Admin-UI sichtbar.
- Plugin mit HandlerType ohne `IImportHandler` erzeugt `Incompatible`.
- `ImportOrchestrator` prueft aktivierte Plugins in Reihenfolge.
- `ImportOrchestrator` unterstuetzt interaktive Plugins inklusive Bestaetigung weiterhin.
- `ImportOrchestrator` bricht nach Fehler eines passenden Plugins ab und probiert keine spaeteren passenden Plugins.
- Hostneutrale DTO-Mapping-Schicht, sobald der Contract entsprechend erweitert wird.

## Ausgefuehrte Pruefungen

- `dotnet test Rezepte.sln --no-restore --logger "console;verbosity=minimal"`: fehlgeschlagen, weil fuer das neue Projekt `Rezepte.Tests.PluginFixture` die Datei `obj/project.assets.json` fehlte.
- `dotnet test Rezepte.sln --logger "console;verbosity=minimal"`: erfolgreich. 131 Tests bestanden, 0 fehlgeschlagen, 0 uebersprungen.

## Hinweise

- Der Testlauf meldet weiterhin `NU1903` fuer `SQLitePCLRaw.lib.e_sqlite3` Version `2.1.11` mit hoher Sicherheitsrelevanz. Das ist nicht offensichtlich durch den Plugin-Umbau verursacht, sollte aber separat behoben werden.
- Die Review-Bewertung basiert auf dem aktuellen uncommitted Repository-Zustand nach Iteration 2, den Lifecycle-Artefakten und einem lokalen Testlauf mit Restore.
