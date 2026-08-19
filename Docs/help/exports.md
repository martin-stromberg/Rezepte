# Datenexporte und Sicherungen

## Eigene Daten exportieren

Der Datenexport wird in den Einstellungen unter "Datenexport" gestartet. Die Anwendung legt dafür einen Hintergrundjob an, sodass der Browser nicht auf die komplette Erstellung der Exportdatei warten muss.

Während der Erstellung zeigt die Oberflaeche den aktuellen Fortschritt an. Sobald der Job erfolgreich abgeschlossen ist, startet der Download automatisch. Falls der Export fehlschlaegt, wird der Fehlerstatus in der Oberflaeche angezeigt.

Optional können Bilder und Rezept-PDFs in den Export aufgenommen werden. Bei grossen Datenmengen kann die Erstellung dadurch laenger dauern.

## Gesamtexport für Administratoren

Administratoren starten die Sicherung unter "Sicherung" über "Gesamtexport (Admin)". Auch dieser Export läuft als Hintergrundjob und zeigt den Fortschritt in der Oberflaeche an. Nach erfolgreichem Abschluss wird die ZIP-Datei automatisch heruntergeladen.

Der Download fertiger Exportdateien ist an den ausloesenden Benutzer gebunden. Administratoren können Admin-Exportjobs abrufen.

## Update-Backups

Vor automatischen Programmupdates erstellt die Anwendung einen systemischen Gesamtexport als Pre-Install-Backup. Dieser Export wird nicht über die Oberflaeche gestartet, sondern durch den Update-Pre-Install-Callback ausgeloest.

Das Backup wird in das per `UpdateBackups:Directory` konfigurierte Zielverzeichnis geschrieben. Ob Bilder und PDFs enthalten sind, steuern `UpdateBackups:IncludeImages` und `UpdateBackups:IncludePdf`. Die Anzahl der behaltenen Update-Backups richtet sich nach `UpdateBackups:RetentionCount`.

Weitere Details zur Aktivierung und zur `msTools.Updater`-Integration stehen unter [Programmupdates](application-updates.md).

## Wiederherstellung

Die Wiederherstellung erfolgt über den Upload einer ZIP-Datei im Bereich "Sicherung". Vor dem Start muss die Wiederherstellung bestätigt werden, da bestehende Daten überschrieben werden können.

### Archivvalidierung und Ressourcenlimits

Bevor Daten in die Datenbank übernommen werden, prüft die Anwendung das hochgeladene Archiv auf Gültigkeit. Unzulässige Archive werden abgewiesen, ohne dass Daten verändert werden.

Folgende Limits werden durchgesetzt:

- Maximale Uploadgroesse: 500 MB
- Maximal 10.000 Eintraege im Archiv
- Maximal 1 GB ungepackte Gesamtgroesse aller Eintraege
- Maximal 50 MB für `recipes.json`
- Maximal 50 MB pro Bild
- Maximal 500 MB Bilddaten insgesamt
- Maximal erlaubtes Kompressionsverhältnis von 100:1 pro Eintrag, um ZIP-Bomben zu vermeiden

Zusätzlich werden ungültige Pfade oder Pfade mit Verzeichniswechseln (`..`) abgelehnt. Während der Wiederherstellung wird der Vorgang serverseitig seriell ausgefuehrt, sodass nur ein Restore gleichzeitig läuft. Fehlerhafte oder nicht unterstuetzte Archive fuehren zu einer HTTP-400-Fehlermeldung mit Hinweis auf den Abbruchgrund.
