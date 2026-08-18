# Anforderungen an `msTools.Updater`

Diese Datei sammelt Verbesserungen und Anforderungen an die `msTools.Updater`-Bibliothek, die aus der Fehlersuche mit IIS-App-Pools gewachsen sind.

## 1. Detaillierte Protokollierung über `ILogger`

**Anforderung**

Wenn die Anwendung einen `ILogger` für `msTools.Updater` registriert, soll die Bibliothek den gesamten Update-Ablauf protokollieren:

- Gefundenes Release und zugeordnetes Package (`AutoUpdatePackageDescriptor`).
- Erfolgreiches Herunterladen des Packages nach `pending/`.
- Den vollständigen Inhalt und Dateipfad des generierten Installationsskripts (PowerShell / Shell).
- Den exakten Befehl, mit dem das Skript gestartet wird.
- `stdout` und `stderr` des Skripts während der Ausführung.
- `ExitCode` des Skripts.
- Klar formulierte Fehler, wenn das Skript fehlschlägt (z. B. `Stop-IISApplicationPool` unbekannt, fehlende Berechtigungen).

**Begründung**

Aktuell gibt `msTools.Updater` nur `Installation started` aus und verliert die Skriptausgabe. Bei einem Fehler im PowerShell-Skript bleibt die Ursache verborgen, weil der Standard-`IAutoUpdateProcessRunner` das Skript detached startet und `stdout`/`stderr` nicht abfängt.

**Akzeptanzkriterien**

- `ILogger` wird konsistent in `AutoUpdateInstaller`, `AutoUpdateOrchestrator`, `AutoUpdateScriptGenerator` und `DefaultAutoUpdateProcessRunner` verwendet.
- Fehlerhafte Installationen enden mit einem `AutoUpdateResult`, das die ursprüngliche Fehlermeldung des Skripts enthält.
- Benutzer sehen ohne externen Test-Host, warum ein Update nicht funktioniert.
