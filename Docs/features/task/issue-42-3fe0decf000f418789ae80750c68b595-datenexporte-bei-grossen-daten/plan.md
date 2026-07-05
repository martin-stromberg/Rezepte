# Umsetzungsplan

## Zielbild

Benutzer- und Admin-Exporte werden ueber die bestehende Background-Job-Queue gestartet. Die UI pollt den Jobstatus und zeigt einen Fortschrittsbalken. Nach erfolgreichem Abschluss wird das Ergebnis ueber einen autorisierten Download-Endpunkt heruntergeladen.

## Umsetzung

1. Gemeinsame Export-Job-Payload definieren.
   - Parameter: `includeImages`, `includePdf`.
   - Optional: aus bestehendem JSON tolerant lesen.

2. Benutzerexport-Jobhandler haerten.
   - `includeImages` aus Payload lesen statt immer `true`.
   - Ergebnis als relativen Dateinamen oder kontrollierten Result-Token speichern.
   - Keine Server-Dateipfade in Statusantworten leaken.

3. Admin-Export-Jobhandler hinzufuegen.
   - JobType `export:all`.
   - `ExportService.ExportAllAsync(...)` im Hintergrund ausfuehren.
   - ZIP in dasselbe Export-Verzeichnis schreiben.
   - Fortschritt und Fehler wie beim Benutzerexport pflegen.

4. Job-API erweitern.
   - `POST /api/jobs/exports/me` nimmt `includeImages` und `includePdf` entgegen.
   - `POST /api/jobs/exports/all` fuer Admins.
   - `GET /api/jobs/{id}` gibt Status, Progress und eine Download-URL nur bei erfolgreichem Export zurueck.
   - `GET /api/jobs/{id}/download` liefert die Datei nur fuer Initiator oder Admin.

5. Sichtbare synchrone Export-Endpunkte umstellen oder kompatibel delegieren.
   - `GET /api/exports/recipes` startet einen Job und liefert `202 Accepted`.
   - `POST /api/admin/exports` startet einen Admin-Job und liefert `202 Accepted`.

6. UI anpassen.
   - `ExportData.razor` startet Job, pollt Status, zeigt Progressbar, startet Download bei Erfolg.
   - `BackupRestore.razor` nutzt denselben Ablauf fuer Admin-Gesamtexport.
   - Fehlerstatus sichtbar anzeigen.

7. Tests ergaenzen.
   - Handler/Payload fuer `includeImages`.
   - Job-Download-Autorisierung beziehungsweise API-Helfer soweit sinnvoll isoliert testbar.

## Offene Punkte

Keine.
