# Risiken und offene technische Punkte

## Offene Punkte

- `msTools.Updater` API ist unbekannt: Paketname, Registrierungs-API, Optionen und Pre-Install-Event muessen verifiziert werden.
- Unklar ist, ob der Pre-Install-Hook asynchron auf ein Backup warten kann.
- Unklar ist, wie der Updater einen Abbruch erwartet, wenn das Backup fehlschlaegt.
- Es ist zu klaeren, ob der vorhandene `ExportAllAsync` als "vollstaendiger Datenexport" ausreicht.
- Fuer automatische Backups fehlt ein technischer Initiator, weil `ExportAllAsync` aktuell `adminUserId` verlangt.
- Backup-Retention ist neu; vorhandene Exportdateien werden aktuell nicht automatisch bereinigt.
- Das bestehende Export-Ziel `<ContentRoot>/exports` ist nicht konfigurierbar und sollte fuer Update-Backups nicht ungeprueft wiederverwendet werden.

## Risiken

- Wenn der Updater-Callback fire-and-forget arbeitet, koennte die Installation starten, bevor das Backup fertig ist.
- Wenn das Backup im selben Prozess laeuft und die Installation den Prozess beendet, muss der Zeitpunkt des Hooks belastbar sein.
- Ein MemoryStream-Export kann bei grossen Datenbestaenden viel Speicher belegen. Der vorhandene `ExportService` baut ZIPs im Speicher.
- Der Export ist moeglicherweise nicht vollstaendig gegenueber dem aktuellen Datenmodell.
- Relative Backup-Pfade koennen je nach Working Directory falsch landen, wenn sie nicht explizit gegen `ContentRootPath` aufgeloest werden.
- Retention-Loeschung darf nur Dateien im konfigurierten Backup-Verzeichnis betreffen und muss Pfadvalidierung nutzen.

## Empfehlungen fuer die Planung

- Vor Implementierung `msTools.Updater` direkt einsehen und API dokumentieren.
- Einen dedizierten `UpdateBackupService` planen, der nur Backup-Erstellung und Retention verantwortet.
- `IExportService` wiederverwenden, aber den Admin-Kontext/technischen Initiator explizit loesen.
- Fuer Backup-Dateischreiben temp-Datei plus atomaren Move verwenden.
- Bei jedem Fehler Exception/Fehlerresultat an den Updater liefern.
- Tests fuer erfolgreiche Backup-Erstellung, fehlschlagenden Export, Retention und Pfadvalidierung vorsehen.
