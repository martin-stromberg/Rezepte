# Datenexporte und Sicherungen

## Eigene Daten exportieren

Der Datenexport wird in den Einstellungen unter "Datenexport" gestartet. Die Anwendung legt dafuer einen Hintergrundjob an, sodass der Browser nicht auf die komplette Erstellung der Exportdatei warten muss.

Waehrend der Erstellung zeigt die Oberflaeche den aktuellen Fortschritt an. Sobald der Job erfolgreich abgeschlossen ist, erscheint die Exportdatei in der Tabelle "Gespeicherte Exporte". Ueber die Tabelle kann die Datei heruntergeladen oder geloescht werden. Falls der Export fehlschlaegt, wird der Fehlerstatus in der Oberflaeche angezeigt.

Optional koennen Bilder und Rezept-PDFs in den Export aufgenommen werden. Bei grossen Datenmengen kann die Erstellung dadurch laenger dauern.

## Gesamtexport fuer Administratoren

Administratoren starten die Sicherung unter "Sicherung" ueber "Gesamtexport (Admin)". Auch dieser Export laeuft als Hintergrundjob und zeigt den Fortschritt in der Oberflaeche an. Nach erfolgreichem Abschluss erscheint die ZIP-Datei in der Tabelle "Gespeicherte Exporte" und kann dort heruntergeladen oder geloescht werden.

Der Download fertiger Exportdateien ist an den ausloesenden Benutzer gebunden. Administratoren koennen Admin-Exportjobs abrufen.

## Automatische Bereinigung alter Exporte

Erstellte Exportdateien bleiben nur begrenzt im `exports`-Ordner erhalten: Jede ZIP-Datei wird spaetestens einen Tag nach ihrer Erstellung automatisch geloescht. Das gilt sowohl fuer Benutzer- als auch fuer Admin-Exporte. Zusammen mit der Datei wird der zugehoerige Eintrag in der Tabelle "Gespeicherte Exporte" entfernt. Verwaiste ZIP-Dateien im Exportordner, zu denen kein Eintrag mehr existiert, werden ebenfalls nach einem Tag geloescht.

Die Uhrzeit der taeglichen Bereinigung legt der Administrator unter "Sicherung" im Abschnitt "Automatische Bereinigung" fest (Standard: 03:00 Uhr, Serverzeit). Dort ist auch der Zeitpunkt der letzten Bereinigung sichtbar; ueber "Jetzt bereinigen" kann die Bereinigung sofort ausgefuehrt werden.

War die Anwendung zur eingestellten Uhrzeit nicht gestartet, wird die Bereinigung nicht uebersprungen, sondern beim naechsten Start bzw. bei der naechsten Pruefung (im Minutentakt) nachgeholt. Dazu vergleicht die Anwendung den gespeicherten Zeitpunkt der letzten Bereinigung mit dem letzten faelligen Termin.

Schnittstelle fuer Administratoren:

- `GET /api/admin/exports/cleanup` liefert die eingestellte Uhrzeit (`HH:mm`) und den letzten Lauf.
- `PUT /api/admin/exports/cleanup` mit `{ "cleanupTime": "HH:mm" }` speichert die Uhrzeit.
- `POST /api/admin/exports/cleanup/run` fuehrt die Bereinigung sofort aus.

Update-Backups (siehe unten) sind von dieser Bereinigung nicht betroffen; fuer sie gilt weiterhin `UpdateBackups:RetentionCount`.

## Update-Backups

Vor automatischen Programmupdates erstellt die Anwendung einen systemischen Gesamtexport als Pre-Install-Backup. Dieser Export wird nicht ueber die Oberflaeche gestartet, sondern durch den Update-Pre-Install-Callback ausgeloest.

Das Backup wird in das per `UpdateBackups:Directory` konfigurierte Zielverzeichnis geschrieben. Ob Bilder und PDFs enthalten sind, steuern `UpdateBackups:IncludeImages` und `UpdateBackups:IncludePdf`. Die Anzahl der behaltenen Update-Backups richtet sich nach `UpdateBackups:RetentionCount`.

Weitere Details zur Aktivierung und zur `msTools.Updater`-Integration stehen unter [Programmupdates](application-updates.md).

## Wiederherstellung

Die Wiederherstellung erfolgt ueber den Upload einer ZIP-Datei im Bereich "Sicherung". Vor dem Start muss die Wiederherstellung bestaetigt werden, da bestehende Daten ueberschrieben werden koennen.

### Archivvalidierung und Ressourcenlimits

Bevor Daten in die Datenbank uebernommen werden, prueft die Anwendung das hochgeladene Archiv auf Gueltigkeit. Unzulaessige Archive werden abgewiesen, ohne dass Daten veraendert werden.

Folgende Limits werden durchgesetzt:

- Maximale Uploadgroesse: 1,5 GB
- Maximal 10.000 Eintraege im Archiv
- Maximal 2 GB ungepackte Gesamtgroesse aller Eintraege
- Maximal 50 MB fuer `recipes.json`
- Maximal 50 MB pro Bild
- Maximal 1,5 GB Bilddaten insgesamt
- Maximal erlaubtes Kompressionsverhaeltnis von 100:1 pro Eintrag, um ZIP-Bomben zu vermeiden

Zusaetzlich werden ungueltige Pfade oder Pfade mit Verzeichniswechseln (`..`) abgelehnt. Waehrend der Wiederherstellung wird der Vorgang serverseitig seriell ausgefuehrt, sodass nur ein Restore gleichzeitig laeuft. Fehlerhafte oder nicht unterstuetzte Archive fuehren zu einer HTTP-400-Fehlermeldung mit Hinweis auf den Abbruchgrund.
