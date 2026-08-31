# Updater Troubleshooting

Dieses Dokument beschreibt die aktuelle `msTools.Updater`-Integration mit Version `0.10.0`. Die früheren Befunde zu stillen Quellenfehlern, nicht persistierten Paketen und den `IISAdministration`-Cmdlets sind mit dieser Version überholt.

## 1. Keine Update-Quelle konfiguriert

Wenn weder `ApplicationUpdates:RepositoryOwner` / `ApplicationUpdates:RepositoryName` noch `ApplicationUpdates:LocalSourceDirectory` gesetzt ist, liefert `msTools.Updater 0.10.0` den Ergebniscode `NoUpdateSourceConfigured`.

**Prüfung**

- Für GitHub `RepositoryOwner`, `RepositoryName` und `ManifestAssetName` setzen.
- Für eine lokale Quelle `LocalSourceDirectory` auf ein Verzeichnis mit gültigem `update.json`-Manifest und Updatepaket setzen.

## 2. Installation startet, aber das Ziel wird nicht aktualisiert

Prüfen Sie zunächst den im Status oder Log ausgewiesenen Fehler und die Voraussetzungen des konfigurierten Installationstyps:

- Das Konto muss den IIS-App-Pool oder Windows-Dienst verwalten dürfen.
- Für IIS muss das `WebAdministration`-Modul verfügbar sein.
- Für Linux muss `UpdateUnitName` auf eine vorhandene, aktivierte systemd-Unit zeigen.

Der Testhost kann das generierte Skript weiterhin mit seinem `LoggingAutoUpdateProcessRunner` synchron ausführen und dessen Ausgabe anzeigen.

## 3. `install` nach einem Prozessneustart

In `0.10.0` wird der Paketdeskriptor im `DownloadPath` gespeichert. Ein neuer Prozess kann ein gültiges, heruntergeladenes Paket daher wieder aufnehmen. Wenn das Paket fehlt oder die Prüfsumme nicht mit dem Deskriptor übereinstimmt, muss der Workflow erneut mit `check` und `download` gestartet werden.

## 4. IIS-App-Pool-Cmdlets werden nicht erkannt

Der frühere Fehler mit `Stop-IISApplicationPool` und `Start-IISApplicationPool` gehört zu älteren Updater-Versionen. `msTools.Updater 0.10.0` erzeugt stattdessen ein Skript mit `Stop-WebAppPool` und `Start-WebAppPool` aus dem Modul `WebAdministration`.

Wenn diese Cmdlets fehlen, installieren oder aktivieren Sie `WebAdministration` auf dem Windows-Server und prüfen Sie die Berechtigungen des Update-Kontos.

## 5. Update-Lock bleibt bestehen

Das Installationsskript bereinigt in `0.10.0` den Update-Lock und den persistierten Paketdeskriptor nach erfolgreichem oder fehlgeschlagenem Lauf. Bei einem noch aktiven Prozess darf der Lock nicht manuell entfernt werden. Für einen veralteten Lock greifen die konfigurierte `HealthTimeoutSeconds`-Bewertung und die reguläre Workspace-Wiederherstellung.
