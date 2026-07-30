# Export, Backup und BackgroundJobs

## ExportService

`Rezepte.Web/Services/ExportService.cs` definiert `IExportService` mit:

- `ExportUserAsync(string userId, bool includeImages, bool includePdf, CancellationToken ct = default)`
- `ExportAllAsync(string adminUserId, bool includeImages, bool includePdf, CancellationToken ct = default)`
- `RestoreFromZipAsync(Stream zipStream, string adminUserId, CancellationToken ct = default)`

`ExportAllAsync` ist der fachlich naheliegende Einstieg fuer ein Update-Backup. Der Service laedt Benutzer, Kochbuecher und Rezepte inklusive Bilder, Schritte, Zutaten und Cookbook-Zuordnungen und erzeugt einen ZIP-Stream im Speicher.

## Exportinhalt

Der ZIP enthaelt:

- `recipes.json`
- optional `images/{recipeId}/imageNN.ext`
- optional `pdf/{author} - {title}.pdf`
- bei Admin-Export zusaetzlich `metadata.json`

Der ExportRoot enthaelt Cookbooks, Recipes und Users. Der Admin-Export setzt `includeUsers` auf `true`.

## Moegliche Luecke bei "vollstaendig"

Die Datenbank enthaelt weitere Tabellen/Felder, die in der Export-DTO-Struktur nicht eindeutig vollstaendig abgebildet sind:

- `CalendarEvents`
- `ShoppingListGroups`
- `ShoppingListItems`
- `UserSettings`
- `AppSettings`
- `PluginSettings`
- `PluginSources`
- `PluginSourceReleases`
- `AiRequestLogs`
- Rezeptfelder wie `Uri`, `Portions` sind im DTO vorhanden, werden aber im gelesenen Mapping nicht sichtbar gesetzt.
- `RecipeSideDishes` werden nicht sichtbar exportiert.

Fuer die Planung muss geklaert werden, ob die Anforderung "vollstaendiger Datenexport" den vorhandenen fachlichen Export meint oder ob vor Updates wirklich alle persistierten Anwendungsdaten gesichert werden muessen. Akzeptanzkriterium und Risiko sprechen eher fuer eine vollstaendige Sicherung.

## BackgroundJobs

Die Anwendung hat eine persistente Job-Tabelle und einen Channel-basierten Worker:

- `BackgroundJobQueue.EnqueueAsync(...)` legt einen Job in der DB an und schreibt die Job-ID in einen bounded Channel.
- `BackgroundJobHostedService` liest den Channel, markiert Jobs als Running/Succeeded/Failed/Cancelled und ruft den passenden `IBackgroundJobHandler`.
- `ExportAllJobHandler` und `ExportUserJobHandler` rufen `IExportService` auf und speichern die Ergebnisdatei ueber `ExportJobFileStore`.

## ExportJobFileStore

`ExportJobFileStore` schreibt Exportdateien nach:

`Path.Combine(environment.ContentRootPath, "exports")`

Der Pfad wird beim Zugriff angelegt. Dateinamen werden ueber `CreateSafeFileName(prefix, userId, jobId)` erzeugt. `GetPathForFileName` verhindert Pfad-Traversal, indem nur reine Dateinamen akzeptiert und der Vollpfad gegen das Export-Root validiert wird.

## Relevanz fuer Update-Backups

Fuer den Pre-Install-Hook ist der Background-Job-Weg nur eingeschraenkt passend:

- Der Updater muss auf das Backup-Ergebnis warten koennen.
- Ein Enqueue liefert nur eine Job-ID, nicht den fertigen Backup-Pfad.
- Der Channel laeuft innerhalb desselben Prozesses und garantiert nicht, dass vor Installation synchron abgeschlossen wurde.
- Statuspolling waere unnoetig komplex und fehleranfaellig.

Besser ist ein direkter Service-Aufruf:

1. `ExportAllAsync(...)` aufrufen.
2. Stream in konfiguriertes Backup-Verzeichnis schreiben.
3. Optional temp-Datei verwenden und erst nach erfolgreichem Copy final umbenennen.
4. Retention anwenden.
5. Erfolg mit Zielpfad loggen.
6. Fehler loggen und an den Updater weiterreichen, damit die Installation stoppt.

## Bestehende Logging-Punkte

`ExportService`, Controller, Queue, HostedService und JobHandler loggen bereits Start, Erfolg und Fehler. Ein Update-Backup-Service sollte diese Linie fortfuehren und mindestens loggen:

- Start des Pre-Install-Backups
- Backup-Zielpfad
- Exportoptionen, insbesondere Bilder/PDFs
- Dateigroesse, falls guenstig verfuegbar
- Retention-Loeschungen
- Fehler mit Exception
