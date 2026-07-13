# Detail: Importarchitektur

## Aktueller Importfluss

Der Import wird aus dem Dialog `CreateRecipeDialog` gestartet. Dateiimporte verwenden die Endpunkte `api/cookbooks/import-session/start-file` bzw. `api/cookbooks/{cookbookId}/import-session/start-file`; URL-Importe verwenden `api/cookbooks/import-session/start` bzw. `api/cookbooks/{cookbookId}/import-session/start`. Danach pollt der Dialog den Sessionstatus und beantwortet bei interaktiven Handlern eine Bestaetigungsabfrage.

Belege:

- Dateiimport im Dialog: `Rezepte.Web/Components/Shared/CreateRecipeDialog.razor:182`, `Rezepte.Web/Components/Shared/CreateRecipeDialog.razor:208`
- URL-Import im Dialog: `Rezepte.Web/Components/Shared/CreateRecipeDialog.razor:320`, `Rezepte.Web/Components/Shared/CreateRecipeDialog.razor:331`
- Polling und Bestaetigung: `Rezepte.Web/Components/Shared/CreateRecipeDialog.razor:362`, `Rezepte.Web/Components/Shared/CreateRecipeDialog.razor:381`, `Rezepte.Web/Components/Shared/CreateRecipeDialog.razor:385`
- Session-Endpunkte mit Cookbook: `Rezepte.Web/Controllers/CookbooksController.cs:304`, `Rezepte.Web/Controllers/CookbooksController.cs:341`, `Rezepte.Web/Controllers/CookbooksController.cs:355`
- Session-Endpunkte ohne Cookbook: `Rezepte.Web/Controllers/CookbooksController.cs:365`, `Rezepte.Web/Controllers/CookbooksController.cs:437`, `Rezepte.Web/Controllers/CookbooksController.cs:451`
- Datei-Session-Endpunkte: `Rezepte.Web/Controllers/CookbooksController.cs:463`, `Rezepte.Web/Controllers/CookbooksController.cs:495`

## Aktuelle Handler-Auswahl

Es gibt zwei Importpfade:

- `ImportService` fuer direkte Imports.
- `ImportOrchestrator` fuer sessionbasierte Imports mit Status und optionaler Benutzerbestaetigung.

Beide Pfade verwenden heute die durch DI bereitgestellten `IImportHandler`.

`ImportService` erhaelt `IEnumerable<IImportHandler>` im Konstruktor und iteriert die Handler in Reihenfolge der DI-Registrierung. Pro Handler wird `CanHandleAsync` aufgerufen; der erste passende Handler verarbeitet mit `HandleAsync`. Falls kein Handler passt, kommt eine Fehlermeldung zurueck.

Belege:

- Konstruktion mit `IEnumerable<IImportHandler>`: `Rezepte.Web/Services/Import/ImportService.cs:5`
- Sequenzielle Iteration: `Rezepte.Web/Services/Import/ImportService.cs:18`
- Eignungspruefung: `Rezepte.Web/Services/Import/ImportService.cs:24`
- Verarbeitung: `Rezepte.Web/Services/Import/ImportService.cs:37`
- Fehler bei keinem passenden Handler: `Rezepte.Web/Services/Import/ImportService.cs:48`

`ImportOrchestrator` erstellt fuer jede Session einen Scope, laedt alle `IImportHandler` aus dem ServiceProvider und prueft sie ebenfalls sequenziell. Interaktive Handler werden ueber `IInteractiveImportHandler` mit Session-Interaktion angesprochen.

Belege:

- Session-Speicher im Arbeitsspeicher: `Rezepte.Web/Services/Import/ImportOrchestrator.cs:13`
- Handler-Aufloesung aus DI: `Rezepte.Web/Services/Import/ImportOrchestrator.cs:61`
- Sequenzielle Iteration: `Rezepte.Web/Services/Import/ImportOrchestrator.cs:65`
- Interaktive Handler: `Rezepte.Web/Services/Import/ImportOrchestrator.cs:90`
- Kein passender Handler: `Rezepte.Web/Services/Import/ImportOrchestrator.cs:123`

## DI-Registrierung

Alle Importhandler sind fest im Webprojekt registriert:

- `IImportService` -> `ImportService`
- `IImportHandler` -> Backup, URL-Quellen, AI-Handler
- `ImportOrchestrator` als Singleton

Belege:

- `Rezepte.Web/Extensions/ServiceCollectionExtensions.cs:139` bis `Rezepte.Web/Extensions/ServiceCollectionExtensions.cs:148`
- `Rezepte.Web/Extensions/ServiceCollectionExtensions.cs:157`

## Konsequenz fuer das Pluginsystem

Ein `PluginManager` muss die Rolle der aktuellen DI-Handlerliste uebernehmen. Er sollte die aktivierten Plugins in persistierter Reihenfolge liefern, damit `ImportService` und `ImportOrchestrator` nicht mehr direkt `IEnumerable<IImportHandler>` aus DI verwenden.

Wichtig fuer den Umbau:

- Beide Importpfade muessen auf denselben Plugin-Auswahlmechanismus gehen.
- `IInteractiveImportHandler` muss erhalten bleiben oder in das neue Shared-Projekt wandern, weil AI-Handler heute Benutzerbestaetigungen nutzen.
- Der aktuelle Session-Speicher ist rein in-memory; das ist fuer das Pluginfeature nicht zwingend zu aendern, aber beim Laden/Neuladen von Plugins relevant.
- Handlerinstanzen koennen zustandsbehaftet sein, z. B. URL-Handler cachen das Ergebnis von `CanHandleAsync` fuer `HandleAsync`. Der PluginManager sollte daher pro Importlauf frische Instanzen oder geeignete Scopes verwenden.

