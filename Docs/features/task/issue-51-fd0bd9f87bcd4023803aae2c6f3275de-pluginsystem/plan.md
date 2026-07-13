# Umsetzungsplan: Pluginsystem fuer Rezeptimporte

## Zielbild

Die bestehende Importstrecke bleibt fachlich erhalten, die quellenabhaengige Importlogik wird aber aus `Rezepte.Web` herausgeloest. Das Webprojekt laedt Import-Plugins beim Programmstart aus dem Ordner `plugins`, synchronisiert die gefundenen Plugins mit einer persistierten Admin-Konfiguration und spricht beim Import nur aktivierte Plugins in der gespeicherten Reihenfolge an.

## Verbindliche Architekturentscheidungen

- Es wird ein neues Shared-Projekt `Rezepte.Import.Abstractions` angelegt.
- Das Shared-Projekt enthaelt die stabilen Pluginvertraege, Import-DTOs, Interaktionsvertraege und allgemeine Parser-/Basishilfen ohne Abhaengigkeit auf `Rezepte.Web`, EF Core oder UI.
- Plugins liefern neutrale Rezeptdaten bzw. Importergebnisse an den Host zurueck; das Webprojekt bleibt fuer Persistenz, Cookbook-Zuordnung, Bilderablage und Benutzerkontext verantwortlich.
- Pro vorhandener Importquelle wird ein eigenes Klassenbibliotheksprojekt angelegt. Die vorhandenen Quellen sind: Backup, Chefkoch, SecondSource, ThirdSource, FourthSource, FifthSource, SixthSource, AI-Foto und AI-URL.
- `FourthSourceUrlReceiptImportHandler.cs` wird vor der Auslagerung aufgetrennt, sodass `FourthSourceUrlReceiptImportHandler` und `SixthSourceUrlRecipeImportHandler` in getrennten Dateien bzw. Pluginprojekten liegen.
- Plugin-Konfiguration wird in einer eigenen Tabelle persistiert, nicht als JSON in `AppSetting`.
- Plugin-Erkennung erfolgt beim Programmstart. Ein Laufzeit-Rescan ist fuer diese Umsetzung nicht Teil des Umfangs.
- Fehlerhafte oder inkompatible Plugin-DLLs werden nicht fuer Imports verwendet, aber mit Status und Fehlermeldung in der Admin-UI angezeigt.
- Wenn kein aktiviertes Plugin die Quelle verarbeiten kann, wird das bisherige Verhalten sinngemaess beibehalten: Der Import endet mit einer fachlichen Fehlermeldung ohne Rezeptanlage.
- Plugin-DLLs gelten als vertrauenswuerdiger lokaler Code. Sandboxing, Signaturen und Rechteisolierung sind nicht Bestandteil dieser Umsetzung.

## Projektstruktur

1. Neues Projekt `Rezepte.Import.Abstractions` erstellen und in `Rezepte.sln` aufnehmen.
2. `Rezepte.Web` referenziert `Rezepte.Import.Abstractions`.
3. `Rezepte.Tests` referenziert zusaetzlich `Rezepte.Import.Abstractions`.
4. Pro Quelle ein Pluginprojekt erstellen, z. B.:
   - `Rezepte.Import.Plugins.Backup`
   - `Rezepte.Import.Plugins.Chefkoch`
   - `Rezepte.Import.Plugins.SecondSource`
   - `Rezepte.Import.Plugins.ThirdSource`
   - `Rezepte.Import.Plugins.FourthSource`
   - `Rezepte.Import.Plugins.FifthSource`
   - `Rezepte.Import.Plugins.SixthSource`
   - `Rezepte.Import.Plugins.AIFoto`
   - `Rezepte.Import.Plugins.AIUrl`
5. Pluginprojekte referenzieren nur `Rezepte.Import.Abstractions` und fachlich noetige externe Pakete. Sie duerfen `Rezepte.Web` nicht referenzieren.

## Shared-Vertraege

Im Shared-Projekt werden mindestens folgende Typen eingefuehrt:

- `IImportPlugin` mit stabiler `Id`, `DisplayName`, optionaler `Description`, `Version` und einer Factory/Registration fuer Handler.
- `IImportHandler` als Pluginvertrag fuer `CanHandleAsync` und `HandleAsync`.
- `IInteractiveImportHandler` und `IImportInteraction` fuer Bestaetigungsdialoge der AI-Imports.
- `ImportResult` als hostneutraler Rueckgabetyp.
- DTOs fuer importierte Rezeptdaten, z. B. `ImportedRecipe`, `ImportedRecipeStep`, `ImportedIngredient`, `ImportedImage`.
- Gemeinsame Basishilfen aus `BaseImportHandler`, sofern sie keine Host-Abhaengigkeiten haben.

Die heutigen Interfaces unter `Rezepte.Web/Services/Import/` werden entfernt oder zu Adaptern umgebaut. Namespaces in Web, Tests und Plugins werden auf `Rezepte.Import.Abstractions` umgestellt.

## PluginManager

Im Webprojekt wird ein `PluginManager` eingefuehrt, der folgende Aufgaben uebernimmt:

1. Beim Start `plugins/*.dll` und `plugins/*/*.dll` suchen.
2. Jede Kandidaten-DLL isoliert laden. Fuer Unterordner wird ein `AssemblyLoadContext` mit `AssemblyDependencyResolver` verwendet, damit Plugin-Abhaengigkeiten neben der Plugin-DLL liegen koennen.
3. Typen finden, die `IImportPlugin` implementieren.
4. Plugin-Metadaten lesen und in ein internes Descriptor-Modell ueberfuehren.
5. Gefundene Plugins mit der Datenbank-Konfiguration synchronisieren.
6. Neue Plugin-IDs mit `Enabled = true` und dem naechsten `OrderIndex` hinten anhaengen.
7. Nicht mehr gefundene Plugins in der Konfiguration behalten und als `Missing` anzeigen.
8. Fehlerhafte DLLs als `LoadFailed` oder `Incompatible` erfassen.
9. Fuer jeden Importlauf frische Handlerinstanzen liefern, damit bestehende Handler-Caches zwischen `CanHandleAsync` und `HandleAsync` nicht parallel geteilt werden.

Der `PluginManager` wird als Singleton registriert, verwaltet aber keine wiederverwendeten Handlerinstanzen. Host-Abhaengigkeiten fuer Persistenz und Recipe-Erstellung werden in scoped Host-Services gehalten und pro Importlauf ueber eine Factory oder einen Importausfuehrungskontext bereitgestellt.

## Persistenz

Neue Entity `PluginSetting` in `Rezepte.Web/Entities/`:

- `PluginId` als Primaerschluessel, maximale Laenge 128.
- `DisplayName`, maximale Laenge 200.
- `AssemblyName`, maximale Laenge 256.
- `TypeName`, maximale Laenge 512.
- `Enabled` als bool.
- `OrderIndex` als int.
- `Status` als enum/string, z. B. `Loaded`, `Missing`, `Incompatible`, `LoadFailed`.
- `Error` als optionaler Text.
- `DiscoveredAt` und `LastSeenAt`.

`RezepteDbContext` erhaelt ein `DbSet<PluginSetting>` und eine Modellkonfiguration mit Index auf `OrderIndex`. Es wird eine EF-Migration fuer die neue Tabelle erstellt.

## Admin-Verwaltung

1. Eine neue Admin-Komponente `Rezepte.Web/Components/Settings/PluginSettings.razor` erstellen.
2. `SettingsViewModel` um einen Admin-only Eintrag "Plugins" erweitern.
3. Einen Service fuer Pluginverwaltung einfuehren, z. B. `IPluginSettingsService`.
4. Die UI zeigt alle persistierten Plugins sortiert nach `OrderIndex`.
5. Pro Plugin werden Name, ID, Status, Aktivierung und ggf. Fehlermeldung angezeigt.
6. Aktivieren/Deaktivieren wird direkt gespeichert.
7. Reihenfolge wird ueber Hoch/Runter-Aktionen oder eine bestehende Reorder-Logik gespeichert.
8. Fehlende oder fehlerhafte Plugins bleiben sichtbar, sind aber beim Import nicht auswaehlbar bzw. werden nicht angesprochen.

## Importumbau

1. `ImportService` erhaelt nicht mehr `IEnumerable<IImportHandler>`, sondern verwendet den `PluginManager`.
2. `ImportOrchestrator` loest Handler nicht mehr direkt aus DI auf, sondern fragt den `PluginManager` nach aktivierten Handlern in Reihenfolge.
3. Beide Importpfade setzen vor jedem `CanHandleAsync` und `HandleAsync` weiterhin die Stream-Position zurueck.
4. Deaktivierte, fehlende und fehlerhafte Plugins werden nicht angesprochen.
5. Sobald ein aktiviertes Plugin `CanHandleAsync = true` liefert, fuehrt dieses Plugin den Import aus.
6. Bei erfolgreichem Plugin-Ergebnis legt der Host die Rezepte an und gibt die erzeugten Rezept-IDs zurueck.
7. Wenn ein passendes Plugin beim Import scheitert, wird der Fehler protokolliert und als fehlgeschlagener Import zurueckgegeben. Weitere passende Plugins werden danach nicht probiert, damit das Verhalten dem bisherigen "erster passender Handler verarbeitet" entspricht.
8. Wenn kein aktiviertes Plugin passt, lautet die Fehlermeldung einheitlich: `No suitable import plugin found for this file or URL.`

## Auslagerung der vorhandenen Handler

1. Gemeinsame Parsinglogik aus `BaseImportHandler` in das Shared-Projekt verschieben.
2. Host-spezifische Persistenzlogik aus `BaseUrlReceiptImportHandler` herausloesen. Pluginprojekte parsen Quellen und liefern neutrale DTOs.
3. Webprojekt erhaelt einen Adapter/Mapper, der neutrale Import-DTOs in bestehende `Recipe`, `RecipeStep`, `RecipeIngredient`, Bilder und Cookbook-Zuordnungen ueberfuehrt.
4. URL-Handler in die jeweiligen Pluginprojekte verschieben.
5. Backup-Import als Plugin auslagern; ZIP-/JSON-Erkennung bleibt im Plugin, Persistenz erfolgt im Host.
6. AI-Handler als interaktive Plugins auslagern. Die bestehende Session-Interaktion ueber `IImportInteraction` bleibt erhalten.
7. Feste `IImportHandler`-Registrierungen in `ServiceCollectionExtensions` entfernen.

## Build und Pluginablage

1. Pluginprojekte werden in der Solution gebaut.
2. Fuer lokale Entwicklung wird eine Build-/Publish-Konfiguration ergaenzt, die Plugin-DLLs unter `Rezepte.Web/bin/<Configuration>/<TargetFramework>/plugins/<PluginName>/` kopiert.
3. Fuer Publish wird dokumentiert oder per MSBuild konfiguriert, dass Plugin-DLLs im Programmverzeichnis unter `plugins/<PluginName>/` liegen.
4. Der PluginManager akzeptiert sowohl DLLs direkt unter `plugins` als auch DLLs in direkten Unterordnern.

## Tests

Neue Tests in `Rezepte.Tests`:

- Plugin-Erkennung findet DLLs direkt unter `plugins`.
- Plugin-Erkennung findet DLLs in direkten Unterordnern.
- Neue Plugins werden an bestehende Konfiguration hinten angehaengt.
- Bestehende Reihenfolge bleibt erhalten, wenn weitere Plugins gefunden werden.
- Deaktivierte Plugins werden beim Import nicht angesprochen.
- Aktivierte Plugins werden exakt nach `OrderIndex` geprueft.
- Das erste passende Plugin liefert das Ergebnis, spaetere passende Plugins werden nicht aufgerufen.
- Kein passendes Plugin liefert die definierte Fehlermeldung.
- Fehlerhafte oder inkompatible DLLs erscheinen als Fehlerstatus und werden nicht angesprochen.
- `ImportService` verwendet den `PluginManager`.
- `ImportOrchestrator` verwendet den `PluginManager` und unterstuetzt interaktive Plugins weiterhin.
- Plugin-Persistenz speichert Aktivierung und Reihenfolge mit EF-InMemory analog zu `SettingsServiceTests`.
- Admin-Service speichert Aktivierung und Reorder korrekt.

Zusaetzlich auszufuehren:

- `dotnet build`
- `dotnet test`

## Umsetzungsschritte

1. Shared-Projekt und Solution-Referenzen anlegen.
2. Shared-Vertraege und neutrale Import-DTOs definieren.
3. Hostseitigen Mapper fuer importierte Rezeptdaten implementieren.
4. `PluginSetting`-Entity, DbContext-Konfiguration und Migration ergaenzen.
5. PluginManager inklusive DLL-Suche, AssemblyLoadContext, Metadatenlesung und Konfigurationssynchronisation implementieren.
6. Pluginverwaltungsservice fuer Admin-UI implementieren.
7. Admin-Komponente und Settings-Navigation ergaenzen.
8. `ImportService` und `ImportOrchestrator` auf PluginManager umstellen.
9. Feste Handler-Registrierungen entfernen.
10. Vorhandene Handler in Pluginprojekte verschieben und dabei Host-Persistenz aus den Handlern herausloesen.
11. Build-/Publish-Kopieren der Plugin-DLLs in `plugins` ergaenzen.
12. Tests fuer PluginManager, Persistenz, Importauswahl und Admin-Service ergaenzen.
13. Build und Tests ausfuehren und Fehler beheben.

## Akzeptanzpruefung

- Shared-Projekt existiert und wird von Webprojekt sowie Pluginprojekten verwendet.
- Alle vorhandenen Importquellen liegen in separaten Pluginprojekten.
- Plugin-DLLs werden direkt unter `plugins` und in Unterordnern erkannt.
- Neue Plugins werden automatisch hinten in die persistierte Liste eingereiht.
- Admins sehen gefundene, fehlende und fehlerhafte Plugins in den Einstellungen.
- Admins koennen Plugins aktivieren, deaktivieren und sortieren.
- Beim Import werden nur aktivierte geladene Plugins in gespeicherter Reihenfolge geprueft.
- Das erste passende Plugin liefert die Rezeptdaten.
- Deaktivierte Plugins beeinflussen den Import nicht.
- Bestehender Startseiten-Import funktioniert weiterhin fuer Datei- und URL-Importe inklusive interaktiver AI-Bestaetigung.

## Risiken und Gegenmassnahmen

- `AssemblyLoadContext` kann bei Plugin-Abhaengigkeiten komplex werden. Gegenmassnahme: Pro Pluginordner einen Resolver verwenden und Tests mit Unterordner-Plugins bauen.
- Bestehende Handler enthalten Persistenzlogik und Cache-Zustand. Gegenmassnahme: Parsing und Persistenz trennen und pro Importlauf frische Handler erzeugen.
- AI-Handler brauchen Host-Services und Benutzerinteraktion. Gegenmassnahme: Interaktionsvertrag ins Shared-Projekt verschieben und Host-Services ueber klar definierte Adapter bereitstellen.
- Plugin-DLLs sind voll vertrauenswuerdiger Code. Gegenmassnahme: In der Dokumentation klar festhalten, dass nur lokal kontrollierte Plugins installiert werden duerfen.
- Der Umbau ist breit. Gegenmassnahme: Erst PluginManager und einen einfachen Test-Pluginpfad stabilisieren, dann vorhandene Handler schrittweise auslagern.

## Offene Punkte
