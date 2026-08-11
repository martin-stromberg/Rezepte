# Automatische Programmupdates

Die Anwendung bindet `msTools.Updater` als externe Projektkomponente ein. Der Updater prueft konfigurierte Quellen auf neue Versionen, kann Updates herunterladen und installiert sie nach den Einstellungen unter `ApplicationUpdates`.

Vor jeder Installation abonniert die Anwendung das `BeforeInstall`-Event von `msTools.Updater`. In diesem Schritt wird ein Update-Backup erstellt. Schlaegt das Backup fehl, setzt der Event-Handler die Installation auf abgebrochen.

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
  "EnableAutomaticDownload": true,
  "EnableAutomaticInstallation": false,
  "DownloadPath": "updates",
  "HostedServicesEnabled": true,
  "StopHostAfterScriptStart": false,
  "HealthTimeoutSeconds": 120,
  "UpdateUnitName": "RezepteWebAutoUpdate",
  "RepositoryOwner": "martin-stromberg",
  "RepositoryName": "Rezepte",
  "ManifestAssetName": "update.json"
}
```

- `Enabled`: aktiviert die Update-Pruefung in `msTools.Updater`.
- `EnableAutomaticDownload`: laedt gefundene neue Versionen automatisch herunter.
- `EnableAutomaticInstallation`: installiert heruntergeladene Updates automatisch. Bei `false` kann die Installation ueber die `msTools.Updater`-Kommandos manuell ausgeloest werden.
- `DownloadPath`: lokaler Arbeitsordner fuer Updatepakete, Statusdateien und Locks.
- `HostedServicesEnabled`: aktiviert die Hintergrunddienste von `msTools.Updater`.
- `StopHostAfterScriptStart`: beendet den Host, nachdem das Installationsskript gestartet wurde.
- `HealthTimeoutSeconds`: Timeout fuer Health-/Lock-Bewertungen des Updaters.
- `UpdateUnitName`: eindeutiger Name fuer die systemd-Update-Unit auf Linux.
- `RepositoryOwner`, `RepositoryName`, `ManifestAssetName`: GitHub-Release-Quelle fuer `update.json` und Updatepakete. Sind keine GitHub-Werte gesetzt, kann `LocalSourceDirectory` fuer eine lokale Quelle verwendet werden.

## Pre-Install-Backup

Vor der Installation einer neuen Version loest `msTools.Updater` das `BeforeInstall`-Event aus. Der Event-Handler erstellt ueber `IUpdateBackupService` einen vollstaendigen Systemexport und wartet synchron auf dessen Abschluss, weil das Updater-Event cancellable ist.

Das Backup-Verhalten:

- Die Konfiguration wird vor dem Backup validiert.
- Das Zielverzeichnis wird bei Bedarf erstellt.
- Der Export wird zunaechst in eine temporaere Datei im Backup-Verzeichnis geschrieben.
- Erst nach erfolgreichem Schreiben wird die Datei unter einem finalen Namen wie `update-backup-20260730-1530000000000Z.zip` veroeffentlicht.
- Erfolg und Fehler werden protokolliert, inklusive Zielpfad und Dateigroesse bei erfolgreichen Backups.
- Schlaegt Export, Schreiben, Konfiguration oder Retention fehl, wird `BeforeInstall` abgebrochen und die Installation wird nicht fortgesetzt.

## Retention

Nach einem erfolgreichen Backup wird die Aufbewahrung angewendet. Beruecksichtigt werden nur Dateien im konfigurierten Backup-Verzeichnis, deren Namen dem Muster `update-backup-*.zip` entsprechen.

Die neuesten `UpdateBackups:RetentionCount` Backups bleiben erhalten. Aeltere passende Dateien werden geloescht und die Loeschungen werden protokolliert. Dateien mit anderen Namen im selben Verzeichnis bleiben unberuehrt. Wenn die Retention nicht verlaesslich angewendet werden kann, gilt das Pre-Install-Backup als fehlgeschlagen und die Installation darf nicht weiterlaufen.

## Bedienung in den Einstellungen

Administratoren sehen unter "Einstellungen" den Bereich "Updates". Dort werden der aktuelle Updater-Zustand, die installierte Version, eine gefundene verfuegbare Version, die letzte Pruefung, der Lock-Status sowie die letzten Ergebnisse fuer Pruefung, Download und Installation angezeigt.

Die Aktionen im Bereich:

- "Jetzt pruefen": fragt die konfigurierte Update-Quelle nach einer neuen Version ab.
- "Herunterladen": laedt ein gefundenes Updatepaket herunter.
- "Installieren": startet die Installation mit Downtime-Bestaetigung. Vor der Installation wird automatisch das Pre-Install-Backup erstellt; bei Backupfehlern bricht die Installation ab.
- "Aktualisieren": liest den aktuellen Updater-Status neu ein.
