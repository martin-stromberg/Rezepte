# Anforderung

## Titel

Automatisierte Programmupdates

## Ausgangssituation

Die Webanwendung wird derzeit nicht automatisiert aktualisiert. Updates muessen manuell eingespielt werden. Dadurch entsteht zusaetzlicher Betriebsaufwand und es besteht das Risiko, dass vor einer Aktualisierung keine vollstaendige Datensicherung erstellt wird.

## Ziel

Die Webanwendung soll mithilfe der externen Komponente `msTools.Updater` automatisiert auf den jeweils neuesten verfuegbaren Stand aktualisiert werden. Vor der Installation einer neuen Version muss automatisch ein vollstaendiger Datenexport als Backup erstellt und abgelegt werden.

## Externe Komponente

- Repository: `https://github.com/martin-stromberg/msTools.Updater.git`
- Zweck: Automatisiertes Aktualisieren einer Webanwendung
- Einzubinden ist die aktuelle Version der Komponente.

## Funktionale Anforderungen

### Automatisiertes Update

- Die Komponente `msTools.Updater` wird in die Webanwendung eingebunden.
- Die Webanwendung nutzt den Updater, um verfuegbare neue Versionen automatisiert zu erkennen und einzuspielen.
- Die Integration orientiert sich an den vom Updater bereitgestellten Mechanismen und Schnittstellen.

### Backup vor Installation

- Das vom Updater bereitgestellte Event vor der Installation einer neuen Version wird angebunden.
- Beim Ausloesen dieses Pre-Install-Events wird automatisch ein vollstaendiger Datenexport gestartet.
- Fuer den Datenexport wird die bereits vorhandene Exportfunktion der Anwendung verwendet.
- Das Update darf erst nach erfolgreichem Abschluss des Backups mit der Installation fortfahren.
- Schlaegt das Backup fehl, darf die neue Version nicht installiert werden.

### Backup-Ablage

- Der erzeugte Datenexport wird als Backup in einem konfigurierbaren Zielverzeichnis abgelegt.
- Der Speicherpfad fuer Update-Backups wird ueber `appSettings` konfiguriert.
- Die Anzahl der im Backup-Verzeichnis aufzubewahrenden Backups wird ueber `appSettings` konfiguriert.
- Aeltere Backups werden gemaess der konfigurierten Aufbewahrungsanzahl entfernt oder nicht weiter behalten.

## Nichtfunktionale Anforderungen

- Die Update-Integration darf keine bestehenden Exportfunktionen duplizieren, sondern muss die vorhandene Exportfunktion wiederverwenden.
- Die Backup-Erstellung muss nachvollziehbar protokolliert werden, insbesondere Erfolg, Zielpfad und Fehlerfaelle.
- Fehler beim Update- oder Backup-Prozess muessen so behandelt werden, dass kein vermeidbarer Datenverlust entsteht.
- Konfigurationswerte fuer Backup-Pfad und Aufbewahrungsanzahl muessen ohne Codeaenderung anpassbar sein.

## Konfiguration

Folgende Einstellungen muessen in den `appSettings` verfuegbar sein:

- Speicherpfad fuer automatische Update-Backups
- Anzahl der aufzubewahrenden automatischen Update-Backups

Die konkreten Namen der Konfigurationsschluessel sind im Rahmen der Umsetzung passend zur bestehenden Konfigurationsstruktur der Anwendung festzulegen.

## Akzeptanzkriterien

- Die Webanwendung bindet `msTools.Updater` ein.
- Der Updater kann fuer automatisierte Programmupdates der Webanwendung verwendet werden.
- Vor der Installation einer neuen Version wird automatisch ein vollstaendiger Datenexport ausgefuehrt.
- Der Datenexport nutzt die bereits vorhandene Exportfunktion.
- Das Backup wird im per `appSettings` konfigurierten Zielpfad abgelegt.
- Die Anzahl der behaltenen Backups richtet sich nach der per `appSettings` konfigurierten Aufbewahrungsanzahl.
- Bei fehlgeschlagenem Backup wird die Installation der neuen Version nicht fortgesetzt.
- Die relevanten Erfolgs- und Fehlerfaelle sind protokolliert.

## Offene Punkte

- Es ist zu pruefen, in welcher Form `msTools.Updater` eingebunden werden soll, sofern das Repository kein direkt nutzbares Paket bereitstellt.
- Es ist zu pruefen, welche konkrete Signatur und Semantik das Pre-Install-Event des Updaters besitzt.
- Es ist zu pruefen, welche bestehende Exportfunktion den vollstaendigen Datenexport bereitstellt und wie sie programmatisch aufgerufen wird.
