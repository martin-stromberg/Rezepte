# Automatische Programmupdates

Die Anwendung bereitet automatische Programmupdates ueber eine interne Adapter-Grenze fuer `msTools.Updater` vor. Produktiv duerfen automatische Programmupdates erst aktiviert werden, wenn ein verifizierter Adapter fuer die echte `msTools.Updater`-API angebunden ist.

Solange die Anwendung den internen `DisabledApplicationUpdater` verwendet, darf `ApplicationUpdates:Enabled` nicht produktiv auf `true` gesetzt werden. Die Aktivierung ist erst zulaessig, wenn Paket oder Projekt, DI-Registrierung, Update-Quelle, Update-Check und die awaitbare Pre-Install-Semantik von `msTools.Updater` nachweislich integriert und getestet wurden.

## Konfiguration

Die Update-Sicherung wird ueber `UpdateBackups` konfiguriert:

```json
"UpdateBackups": {
  "Directory": "update-backups",
  "RetentionCount": 5,
  "IncludeImages": true,
  "IncludePdf": false,
  "SystemInitiatorUserId": "system-update-backup"
}
```

- `Directory`: Zielverzeichnis fuer automatische Update-Backups. Relative Pfade werden gegen das Content-Root der Anwendung aufgeloest; absolute Pfade sind erlaubt.
- `RetentionCount`: Anzahl der aufzubewahrenden Update-Backups. Der Wert muss mindestens `1` sein.
- `IncludeImages`: legt fest, ob Bilder in das Update-Backup aufgenommen werden.
- `IncludePdf`: legt fest, ob Rezept-PDFs in das Update-Backup aufgenommen werden.
- `SystemInitiatorUserId`: technischer Initiator fuer Protokollierung und Export-Metadaten.

Die Programmupdate-Funktion selbst wird ueber `ApplicationUpdates` gesteuert:

```json
"ApplicationUpdates": {
  "Enabled": false,
  "CheckOnStartup": false
}
```

- `Enabled`: aktiviert die Registrierung des Updaters und des Pre-Install-Backups. Dieser Wert muss produktiv `false` bleiben, bis ein verifizierter `msTools.Updater`-Adapter angebunden ist.
- `CheckOnStartup`: fuehrt beim Anwendungsstart einen Update-Check aus, sofern `Enabled` aktiv ist.

## Pre-Install-Backup

Vor der Installation einer neuen Version muss der Updater den Pre-Install-Callback ausfuehren. Der Callback erstellt ueber `IUpdateBackupService` einen vollstaendigen Systemexport und wartet auf dessen Abschluss.

Das Backup-Verhalten:

- Die Konfiguration wird vor dem Backup validiert.
- Das Zielverzeichnis wird bei Bedarf erstellt.
- Der Export wird zunaechst in eine temporaere Datei im Backup-Verzeichnis geschrieben.
- Erst nach erfolgreichem Schreiben wird die Datei unter einem finalen Namen wie `update-backup-20260730-1530000000000Z.zip` veroeffentlicht.
- Erfolg und Fehler werden protokolliert, inklusive Zielpfad und Dateigroesse bei erfolgreichen Backups.
- Schlaegt Export, Schreiben, Konfiguration oder Retention fehl, wird der Fehler an den Updater zurueckgegeben und die Installation darf nicht fortgesetzt werden.

## Retention

Nach einem erfolgreichen Backup wird die Aufbewahrung angewendet. Beruecksichtigt werden nur Dateien im konfigurierten Backup-Verzeichnis, deren Namen dem Muster `update-backup-*.zip` entsprechen.

Die neuesten `UpdateBackups:RetentionCount` Backups bleiben erhalten. Aeltere passende Dateien werden geloescht und die Loeschungen werden protokolliert. Dateien mit anderen Namen im selben Verzeichnis bleiben unberuehrt. Wenn die Retention nicht verlaesslich angewendet werden kann, gilt das Pre-Install-Backup als fehlgeschlagen und die Installation darf nicht weiterlaufen.

