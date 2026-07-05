# Bestandsaufnahme

## Relevante Architektur

- ASP.NET Core / Blazor Server Anwendung in `Rezepte.Web`.
- Persistenz ueber EF Core und `RezepteDbContext`.
- Es existiert bereits eine persistierte Background-Job-Infrastruktur:
  - `Services/BackgroundJobs/BackgroundJob.cs`
  - `BackgroundJobQueue.cs`
  - `BackgroundJobHostedService.cs`
  - `IBackgroundJobHandler.cs`
  - `JobsController.cs`
- `BackgroundJob` enthaelt Status, Fortschritt, Ergebnistext und Fehler.
- `AddBackgroundJobQueue()` registriert Queue und Hosted Service.

## Export-Istzustand

- `ExportsController.ExportMyRecipes` erzeugt den Benutzerexport synchron im Request und liefert direkt Dateiinhalt zurueck.
- `AdminExportsController.ExportAll` erzeugt den Admin-Gesamtexport synchron im Request und liefert direkt ZIP zurueck.
- `ExportService` baut ZIP-Dateien in MemoryStreams. Fuer grosse Datenmengen ist mindestens der synchrone Request-Kontext kritisch.
- `ExportUserJobHandler` existiert bereits fuer `export:user`, schreibt eine Exportdatei nach `ContentRootPath/exports` und pflegt Job-Fortschritt.
- Es gibt keinen Admin-Export-Jobhandler.
- `JobsController` kann Benutzerexporte enqueuen und Jobstatus liefern, aber kein Ergebnis sicher herunterladen.
- `ExportUserJobHandler` schreibt derzeit einen lokalen `file://...` Pfad in `ResultMessage`; dieser ist fuer Browser-Clients nicht direkt nutzbar und leakt Serverpfade.

## UI-Istzustand

- `Components/Settings/ExportData.razor` startet den Benutzerexport synchron ueber `GET api/exports/recipes`.
- `Components/Settings/BackupRestore.razor` startet den Admin-Gesamtexport synchron ueber `POST api/admin/exports`.
- Beide Komponenten zeigen nur einen einfachen Busy-Text, keinen Jobstatus und keinen Fortschrittsbalken.
- Download wird ueber `wwwroot/js/fileDownload.js` aus einem Stream gestartet.

## Betroffene Dateien

- `Rezepte.Web/Controllers/ExportsController.cs`
- `Rezepte.Web/Controllers/AdminExportsController.cs`
- `Rezepte.Web/Controllers/JobsController.cs`
- `Rezepte.Web/Services/BackgroundJobs/Handlers/ExportUserJobHandler.cs`
- Neue oder erweiterte Jobhandler fuer Admin-Export.
- `Rezepte.Web/Extensions/ServiceCollectionExtensions.cs`
- `Rezepte.Web/Components/Settings/ExportData.razor`
- `Rezepte.Web/Components/Settings/BackupRestore.razor`
- Tests unter `Rezepte.Tests`.

## Risiken

- Bestehende direkte Download-Endpunkte koennten von externen Clients genutzt werden. Rueckwaertskompatibilitaet sollte moeglichst erhalten bleiben oder sauber durch Accepted-Responses ergaenzt werden.
- Ergebnisdateien duerfen nur vom Job-Initiator oder von Admins heruntergeladen werden.
- Lokale Dateipfade duerfen nicht in API-Antworten fuer Endanwender sichtbar sein.
- Fortschritt bleibt grob, solange `ExportService` keine feingranularen Progress-Callbacks besitzt.
