# Automatische Programmupdates

Die Anwendung bindet `msTools.Updater` als externe Projektkomponente ein. Der Updater prüft konfigurierte Quellen auf neue Versionen, kann Updates herunterladen und installiert sie nach den Einstellungen unter `ApplicationUpdates`.

Vor jeder Installation abonniert die Anwendung das `BeforeInstall`-Event von `msTools.Updater`. In diesem Schritt wird ein Update-Backup erstellt. Schlägt das Backup fehl, setzt der Event-Handler die Installation auf abgebrochen.

## Konfiguration

Die Update-Sicherung wird über `UpdateBackups` konfiguriert:

```json
"UpdateBackups": {
  "Directory": "update-backups",
  "RetentionCount": 5,
  "IncludeImages": true,
  "IncludePdf": false,
  "SystemInitiatorUserId": "system-update-backup"
}
```

- `Directory`: Zielverzeichnis für automatische Update-Backups. Relative Pfade werden gegen das Content-Root der Anwendung aufgelöst; absolute Pfade sind erlaubt.
- `RetentionCount`: Anzahl der aufzubewahrenden Update-Backups. Der Wert muss mindestens `1` sein.
- `IncludeImages`: legt fest, ob Bilder in das Update-Backup aufgenommen werden.
- `IncludePdf`: legt fest, ob Rezept-PDFs in das Update-Backup aufgenommen werden.
- `SystemInitiatorUserId`: technischer Initiator für Protokollierung und Export-Metadaten.

Die Programmupdate-Funktion selbst wird über `ApplicationUpdates` gesteuert:

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

Für Windows muss zusätzlich ein Installationstyp konfiguriert werden. Ab Version `0.7.0-rc.10` wird auch IIS unterstützt:

### Windows-Dienst

```json
"ApplicationUpdates": {
  "ServiceName": "RezepteWeb"
}
```

### Ausführbare Datei

```json
"ApplicationUpdates": {
  "ExecutablePath": "C:\\Services\\Rezepte\\Rezepte.Web.exe"
}
```

### IIS-Application-Pool

```json
"ApplicationUpdates": {
  "AppPoolName": "RezepteWebAppPool",
  "SiteName": "Rezepte"
}
```

- `Enabled`: aktiviert automatische Update-Läufe in `msTools.Updater`. Eine manuell gestartete Prüfung über "Jetzt prüfen" bleibt auch bei `false` möglich.
- `EnableAutomaticDownload`: lädt gefundene neue Versionen automatisch herunter.
- `EnableAutomaticInstallation`: installiert heruntergeladene Updates automatisch. Bei `false` kann die Installation über die `msTools.Updater`-Kommandos manuell ausgelöst werden.
- `DownloadPath`: lokaler Arbeitsordner für Updatepakete, Statusdateien und Locks.
- `HostedServicesEnabled`: aktiviert die Hintergrunddienste von `msTools.Updater`.
- `StopHostAfterScriptStart`: beendet den Host, nachdem das Installationsskript gestartet wurde. Auf Linux muss dieser Wert `true` sein, damit der Hostprozess das Update-Paket nicht weiter blockiert.
- `HealthTimeoutSeconds`: Timeout für Health-/Lock-Bewertungen des Updaters.
- `UpdateUnitName`: eindeutiger Name für die systemd-Update-Unit auf Linux. Die Unit muss existieren und im Systemd-Ziel aktiviert sein, sonst startet das Skript nicht.
- `ServiceName`: Windows: Name des Dienstes, der gestoppt und neu gestartet wird.
- `ExecutablePath`: Windows: Pfad zur ausführbaren Datei, falls kein Dienst verwendet wird.
- `AppPoolName`: Windows (ab `0.7.0-rc.10`): Name des IIS-Application-Pools, der gestoppt und neu gestartet wird.
- `SiteName`: Windows (ab `0.7.0-rc.10`): Optionale IIS-Site, ausschließlich für Logging, wenn `AppPoolName` verwendet wird.
- `RepositoryOwner`, `RepositoryName`, `ManifestAssetName`: GitHub-Release-Quelle für `update.json` und Updatepakete. Sind keine GitHub-Werte gesetzt, kann `LocalSourceDirectory` für eine lokale Quelle verwendet werden.

## Plattform-spezifische Voraussetzungen

### Windows

Einer der folgenden Installationstypen muss konfiguriert sein:

- `ServiceName` – Windows-Dienst.
- `ExecutablePath` – Ausführbare Datei.
- `AppPoolName` – IIS-Application-Pool (ab `0.7.0-rc.10`).

Wird `AppPoolName` gesetzt, verwendet `msTools.Updater` diesen und das optionale `SiteName` nur für Logging. Ansonsten wird `ServiceName` oder `ExecutablePath` verwendet.

#### IIS-Application-Pools

Für `AppPoolName` muss das **`WebAdministration`**-PowerShell-Modul auf dem ausführenden Rechner installiert sein, weil `Rezepte.Web` den `IAutoUpdateProcessRunner` von `msTools.Updater` mit einer Wrapper-Komponente ersetzt, die `WebAdministration`-Cmdlets (`Stop-WebAppPool` / `Start-WebAppPool`) nutzt.

Hintergrund: `msTools.Updater` generiert Scripts mit `Stop-IISApplicationPool` / `Start-IISApplicationPool`. Diese Cmdlets sind im in-box `IISAdministration`-Modul (Version 1.1.0.0) nicht vorhanden, wohl aber im `WebAdministration`-Modul.

Prüfen:

```powershell
Get-Module -ListAvailable -Name WebAdministration
```

Der Service-Account benötigt ausreichende Berechtigungen (Administratoren bzw. Rechte zum Stoppen/Starten des App Pools oder Dienstes).

### Linux

- `UpdateUnitName` muss dem Namen einer echten, aktivierten systemd-Unit entsprechen (z. B. `RezepteWebAutoUpdate.service`).
- `StopHostAfterScriptStart` muss `true` sein, damit der Host stoppt, bevor das Skript die Binärdateien ersetzt.
- Der Service-Account benötigt Rechte, um die Unit zu starten und die Anwendungsdateien zu überschreiben.

## Pre-Install-Backup

Vor der Installation einer neuen Version löst `msTools.Updater` das `BeforeInstall`-Event aus. Der Event-Handler erstellt über `IUpdateBackupService` einen vollständigen Systemexport und wartet synchron auf dessen Abschluss, weil das Updater-Event cancellable ist.

Das Backup-Verhalten:

- Die Konfiguration wird vor dem Backup validiert.
- Das Zielverzeichnis wird bei Bedarf erstellt.
- Der Export wird zunächst in eine temporäre Datei im Backup-Verzeichnis geschrieben.
- Erst nach erfolgreichem Schreiben wird die Datei unter einem finalen Namen wie `update-backup-20260730-1530000000000Z.zip` veröffentlicht.
- Erfolg und Fehler werden protokolliert, inklusive Zielpfad und Dateigröße bei erfolgreichen Backups.
- Schlägt Export, Schreiben, Konfiguration oder Retention fehl, wird `BeforeInstall` abgebrochen und die Installation wird nicht fortgesetzt.

## Retention

Nach einem erfolgreichen Backup wird die Aufbewahrung angewendet. Berücksichtigt werden nur Dateien im konfigurierten Backup-Verzeichnis, deren Namen dem Muster `update-backup-*.zip` entsprechen.

Die neuesten `UpdateBackups:RetentionCount` Backups bleiben erhalten. Ältere passende Dateien werden gelöscht und die Löschungen werden protokolliert. Dateien mit anderen Namen im selben Verzeichnis bleiben unberührt. Wenn die Retention nicht verlässlich angewendet werden kann, gilt das Pre-Install-Backup als fehlgeschlagen und die Installation darf nicht weiterlaufen.

## Bedienung in den Einstellungen

Administratoren sehen unter "Einstellungen" den Bereich "Updates". Dort werden der aktuelle Updater-Zustand, die installierte Version, eine gefundene verfügbare Version, die letzte Prüfung, der Lock-Status sowie die letzten Ergebnisse für Prüfung, Download und Installation angezeigt.

Die Aktionen im Bereich:

- "Jetzt prüfen": fragt die konfigurierte Update-Quelle nach einer neuen Version ab.
- "Herunterladen": lädt ein gefundenes Updatepaket herunter.
- "Installieren": startet die Installation mit Downtime-Bestätigung. Vor der Installation wird automatisch das Pre-Install-Backup erstellt; bei Backupfehlern bricht die Installation ab.
- "Aktualisieren": liest den aktuellen Updater-Status neu ein.
