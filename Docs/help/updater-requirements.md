# Anforderungen an `msTools.Updater`

Diese Datei dokumentiert den Status der während der IIS-Update-Fehlersuche festgehaltenen Anforderungen. Die folgenden Lösungen sind im verwendeten Stand `msTools.Updater 0.10.0` enthalten.

## Status in Version 0.10.0

- **Protokollierung:** Der Updater protokolliert den generierten Skriptpfad und dessen Inhalt. Fehler werden über strukturierte `AutoUpdateResult`- und `ErrorOccurred`-Informationen verfügbar gemacht.
- **IIS-App-Pools:** Das generierte Windows-Skript verwendet das `WebAdministration`-Modul mit `Stop-WebAppPool` und `Start-WebAppPool`. Die früher verwendeten, im `IISAdministration`-Modul nicht vorhandenen `Stop-IISApplicationPool`- und `Start-IISApplicationPool`-Cmdlets werden nicht mehr erzeugt.
- **Installationstypen:** `AppPoolName` wird als IIS-Ziel vor `ServiceName` und `ExecutablePath` aufgelöst. Unter Linux wird das konfigurierte systemd-Ziel verwendet.
- **Installation über Prozessgrenzen:** Der `AutoUpdatePackageDescriptor` wird im `DownloadPath` persistiert und bei einem späteren `install`-Aufruf anhand des heruntergeladenen Pakets geprüft und wieder geladen.
- **Fehlende Quellen:** Wenn keine Update-Quelle konfiguriert ist, liefert der Updater den eindeutigen Ergebniscode `NoUpdateSourceConfigured` statt eines irreführenden `NoNewerUpdateAvailable`.
- **Workspace-Bereinigung:** Installationsskripte entfernen bei Abschluss oder Abbruch den Update-Lock und den persistierten Paketdeskriptor. `HealthTimeoutSeconds` bleibt für die Erkennung veralteter Locks relevant.

## Weiterhin erforderliche Voraussetzungen

- Für IIS muss das PowerShell-Modul `WebAdministration` vorhanden sein.
- Das Konto des Updates benötigt ausreichende Rechte zum Stoppen und Starten des App-Pools beziehungsweise Dienstes.
- Für eine aussagekräftige Update-Prüfung muss eine GitHub- oder lokale Update-Quelle konfiguriert sein.
- `HealthTimeoutSeconds` ersetzt keine fehlenden Berechtigungen oder nicht erreichbaren Installationsziele.
