# Datenexporte und Sicherungen

## Eigene Daten exportieren

Der Datenexport wird in den Einstellungen unter "Datenexport" gestartet. Die Anwendung legt dafuer einen Hintergrundjob an, sodass der Browser nicht auf die komplette Erstellung der Exportdatei warten muss.

Waehrend der Erstellung zeigt die Oberflaeche den aktuellen Fortschritt an. Sobald der Job erfolgreich abgeschlossen ist, startet der Download automatisch. Falls der Export fehlschlaegt, wird der Fehlerstatus in der Oberflaeche angezeigt.

Optional koennen Bilder und Rezept-PDFs in den Export aufgenommen werden. Bei grossen Datenmengen kann die Erstellung dadurch laenger dauern.

## Gesamtexport fuer Administratoren

Administratoren starten die Sicherung unter "Sicherung" ueber "Gesamtexport (Admin)". Auch dieser Export laeuft als Hintergrundjob und zeigt den Fortschritt in der Oberflaeche an. Nach erfolgreichem Abschluss wird die ZIP-Datei automatisch heruntergeladen.

Der Download fertiger Exportdateien ist an den ausloesenden Benutzer gebunden. Administratoren koennen Admin-Exportjobs abrufen.

## Wiederherstellung

Die Wiederherstellung erfolgt weiterhin ueber den Upload einer ZIP-Datei im Bereich "Sicherung". Vor dem Start muss die Wiederherstellung bestaetigt werden, da bestehende Daten ueberschrieben werden koennen.
