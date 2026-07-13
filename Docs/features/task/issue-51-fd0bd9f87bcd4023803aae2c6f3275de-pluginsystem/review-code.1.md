# Code-Review: Pluginsystem fuer Rezeptimporte

Status: Befunde vorhanden

## Befunde

### 1. Externe Plugins koennen mit eigener Abstractions-DLL als inkompatibel erkannt werden

Schweregrad: Hoch

Fundstelle:
- `Rezepte.Web/Services/Import/Plugins/PluginManager.cs:114`
- `Rezepte.Web/Services/Import/Plugins/PluginManager.cs:115`
- `Rezepte.Web/Services/Import/Plugins/PluginManager.cs:231`
- `Rezepte.Web/Services/Import/Plugins/PluginManager.cs:240`

Der `PluginLoadContext` loest jede Assembly-Abhaengigkeit ueber `AssemblyDependencyResolver` aus dem Pluginpfad und laedt sie per `LoadFromAssemblyPath`. Wenn ein Plugin normal gebaut oder veroeffentlicht wird, liegt `Rezepte.Import.Abstractions.dll` typischerweise neben der Plugin-DLL. Dann wird die Contract-Assembly im Plugin-LoadContext ein zweites Mal geladen. Die Plugin-Klasse implementiert dadurch `IImportPlugin` aus dieser zweiten Assembly-Instanz, waehrend die Host-Pruefung `typeof(IImportPlugin).IsAssignableFrom(t)` den Typ aus dem Webprozess verwendet. Das Ergebnis ist: gueltige Plugins werden nicht gefunden bzw. als inkompatibel behandelt.

Empfehlung: Contract-Assemblies explizit aus dem Default-Kontext teilen, z. B. im `Load`-Override fuer `Rezepte.Import.Abstractions` `null` zurueckgeben oder die bereits geladene Host-Assembly verwenden. Dazu einen Integrationstest mit einer echten externen Plugin-DLL plus nebenliegender `Rezepte.Import.Abstractions.dll` ergaenzen.

### 2. Die bestehenden Importquellen sind nicht in separate Pluginprojekte ausgelagert

Schweregrad: Hoch

Fundstelle:
- `Rezepte.Web/Services/Import/Plugins/BuiltInImportPluginCatalog.cs:9`
- `Rezepte.Web/Services/Import/Plugins/BuiltInImportPluginCatalog.cs:17`
- `Rezepte.Web/Services/Import/AIFotoImportHandler.cs:23`
- `Rezepte.Web/Services/Import/AIUrlImportHandler.cs:15`
- `Rezepte.Web/Services/Import/BackupImportHandler.cs:13`
- `Rezepte.Web/Services/Import/Url/ChefkochReceiptImportHandler.cs:15`
- `Rezepte.Web/Services/Import/Url/FourthSourceUrlReceiptImportHandler.cs:12`
- `Rezepte.Web/Services/Import/Url/FourthSourceUrlReceiptImportHandler.cs:119`

Der aktuelle Stand fuehrt die vorhandenen Web-internen Handler nur als Built-in-Plugins auf. Es gibt keine `Rezepte.Import.Plugins.*`-Projekte, und die Handler sowie Basisklassen bleiben im Webprojekt. Damit sind zentrale Akzeptanzkriterien nicht erfuellt: pro vorhandener Importquelle separate Klassenbibliothek, keine Webprojekt-Kopplung der quellenabhaengigen Importlogik und Bereitstellung als Plugin-DLL.

Empfehlung: Entweder die Umsetzung als Zwischenstand markieren oder die vorhandenen Quellen wirklich in Pluginprojekte verschieben. Der Built-in-Katalog kann als Migrationsbruecke nuetzlich sein, sollte aber nicht als Erfuellung der Plugin-Auslagerung gelten.

### 3. Das Shared-Projekt enthaelt keine neutralen Rezept-DTOs und keine Host-Mapping-Grenze

Schweregrad: Mittel

Fundstelle:
- `Rezepte.Import.Abstractions/IImportHandler.cs:3`
- `Rezepte.Import.Abstractions/IImportPlugin.cs:3`
- `Rezepte.Import.Abstractions/ImportResult.cs:3`
- `Rezepte.Web/Services/Import/Url/BaseUrlReceiptImportHandler.cs:8`
- `Rezepte.Web/Services/Import/BaseAIImportHandler.cs:8`

Das neue Abstractions-Projekt enthaelt nur die alten Handler-Vertraege und `ImportResult` mit erzeugten Rezept-IDs. Die geplanten neutralen DTOs wie `ImportedRecipe`, `ImportedIngredient`, `ImportedImage` und die Host-Mapping-Schicht fehlen. Dadurch muessen externe Plugins entweder weiterhin Host-Persistenz selbst erledigen oder koennen keine vollstaendigen Rezeptdaten neutral an den Host zurueckgeben. Das widerspricht der Architekturentscheidung, dass Plugins neutrale Rezeptdaten liefern und `Rezepte.Web` fuer Persistenz, Bilderablage und Cookbook-Zuordnung verantwortlich bleibt.

Empfehlung: Contract auf neutrale Importdaten erweitern und die bestehende Persistenzlogik aus `BaseUrlReceiptImportHandler`, `BaseAIImportHandler` und `BackupImportHandler` in einen hostseitigen Mapper/Adapter verschieben.

### 4. PluginManager-Tests decken die risikoreichsten Pfade nicht ab

Schweregrad: Mittel

Fundstelle:
- `Rezepte.Tests/Services/Import/ImportServicePluginTests.cs:13`
- `Rezepte.Tests/Services/Import/PluginSettingsServiceTests.cs:9`
- `Rezepte.Web/Services/Import/Plugins/PluginManager.cs:88`
- `Rezepte.Web/Services/Import/Plugins/PluginManager.cs:95`
- `Rezepte.Web/Services/Import/Plugins/PluginManager.cs:222`

Die vorhandenen Tests pruefen `ImportService` mit einem Fake-PluginManager und die Settings-Reihenfolge. Nicht getestet sind die eigentlichen PluginManager-Risiken: Laden einer echten DLL direkt unter `plugins`, Laden aus Unterordnern, fehlerhafte/inkompatible DLLs, Beibehaltung bestehender Reihenfolge bei neuen Plugins, `Missing`-Status fuer verschwundene Plugins und Ausschluss deaktivierter Plugins auf Basis persistierter Settings. Genau diese Pfade tragen die neue Funktionalitaet und wuerden Befund 1 sichtbar machen.

Empfehlung: Tests mit temporaeren Plugin-Ausgabeordnern und minimalen Test-Plugin-Assemblies ergaenzen. Zusaetzlich sollte `ImportOrchestrator` mindestens fuer interaktive Plugins und Fehlerpfade gegen den PluginManager getestet werden.

## Fehlende Tests

- Externe Plugin-DLL direkt unter `plugins`.
- Externe Plugin-DLL in `plugins/<PluginName>/` inklusive nebenliegender Abhaengigkeiten.
- Contract-Assembly-Sharing fuer `Rezepte.Import.Abstractions`.
- Fehlerhafte DLL, inkompatible DLL und `Missing`-Status in DB und Admin-Service.
- Deaktivierte Plugins werden vom `PluginManager` nicht instanziiert.
- Persistierte Reihenfolge bleibt bei neu erkannten Plugins stabil.
- `ImportOrchestrator` verwendet Pluginreihenfolge und interaktive Plugins korrekt.
- Admin-UI-Interaktionen fuer Aktivieren, Deaktivieren und Verschieben.

## Ausgefuehrte Pruefungen

- `dotnet build Rezepte.sln --no-restore`: erfolgreich, 2 Warnungen.
- `dotnet test Rezepte.sln --no-build --logger "console;verbosity=minimal"`: erfolgreich, 126 Tests bestanden.

## Hinweise

- Der Build meldet `NU1903` fuer `SQLitePCLRaw.lib.e_sqlite3` Version `2.1.11` mit hoher Sicherheitsrelevanz. Das ist nicht offensichtlich durch den Plugin-Umbau verursacht, sollte aber separat priorisiert werden.
- `rg` ist in der Umgebung nicht installiert; die Suche erfolgte mit PowerShell `Select-String` und Git-Ausgaben.
