# Datenexporte und Sicherungen

## Eigene Daten exportieren

Der Datenexport wird in den Einstellungen unter "Datenexport" gestartet. Die Anwendung legt dafÃ¼r einen Hintergrundjob an, sodass der Browser nicht auf die komplette Erstellung der Exportdatei warten muss.

WÃ¤hrend der Erstellung zeigt die Oberflaeche den aktuellen Fortschritt an. Sobald der Job erfolgreich abgeschlossen ist, startet der Download automatisch. Falls der Export fehlschlaegt, wird der Fehlerstatus in der Oberflaeche angezeigt.

Optional kÃ¶nnen Bilder und Rezept-PDFs in den Export aufgenommen werden. Bei grossen Datenmengen kann die Erstellung dadurch laenger dauern.

## Gesamtexport fÃ¼r Administratoren

Administratoren starten die Sicherung unter "Sicherung" Ã¼ber "Gesamtexport (Admin)". Auch dieser Export lÃ¤uft als Hintergrundjob und zeigt den Fortschritt in der Oberflaeche an. Nach erfolgreichem Abschluss wird die ZIP-Datei automatisch heruntergeladen.

Der Download fertiger Exportdateien ist an den ausloesenden Benutzer gebunden. Administratoren kÃ¶nnen Admin-Exportjobs abrufen.

## Update-Backups

Vor automatischen Programmupdates erstellt die Anwendung einen systemischen Gesamtexport als Pre-Install-Backup. Dieser Export wird nicht Ã¼ber die Oberflaeche gestartet, sondern durch den Update-Pre-Install-Callback ausgeloest.

Das Backup wird in das per `UpdateBackups:Directory` konfigurierte Zielverzeichnis geschrieben. Ob Bilder und PDFs enthalten sind, steuern `UpdateBackups:IncludeImages` und `UpdateBackups:IncludePdf`. Die Anzahl der behaltenen Update-Backups richtet sich nach `UpdateBackups:RetentionCount`.

Weitere Details zur Aktivierung und zur `msTools.Updater`-Integration stehen unter [Programmupdates](application-updates.md).

## Wiederherstellung

Die Wiederherstellung erfolgt Ã¼ber den Upload einer ZIP-Datei im Bereich "Sicherung". Vor dem Start muss die Wiederherstellung bestÃ¤tigt werden, da bestehende Daten Ã¼berschrieben werden kÃ¶nnen.

### Archivvalidierung und Ressourcenlimits

Bevor Daten in die Datenbank Ã¼bernommen werden, prÃ¼ft die Anwendung das hochgeladene Archiv auf GÃ¼ltigkeit. UnzulÃ¤ssige Archive werden abgewiesen, ohne dass Daten verÃ¤ndert werden.

Folgende Limits werden durchgesetzt:

- Maximale Uploadgroesse: 500 MB
- Maximal 10.000 Eintraege im Archiv
- Maximal 1 GB ungepackte Gesamtgroesse aller Eintraege
- Maximal 50 MB fÃ¼r `recipes.json`
- Maximal 50 MB pro Bild
- Maximal 500 MB Bilddaten insgesamt
- Maximal erlaubtes KompressionsverhÃ¤ltnis von 100:1 pro Eintrag, um ZIP-Bomben zu vermeiden

ZusÃ¤tzlich werden ungÃ¼ltige Pfade oder Pfade mit Verzeichniswechseln (`..`) abgelehnt. WÃ¤hrend der Wiederherstellung wird der Vorgang serverseitig seriell ausgefuehrt, sodass nur ein Restore gleichzeitig lÃ¤uft. Fehlerhafte oder nicht unterstuetzte Archive fuehren zu einer HTTP-400-Fehlermeldung mit Hinweis auf den Abbruchgrund.
