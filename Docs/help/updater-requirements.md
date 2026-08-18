# Anforderungen an `msTools.Updater`

Diese Datei sammelt Befunde und Aufgaben für den `msTools.Updater`-Maintainer, die während der IIS-Update-Fehlersuche aufgetreten sind. Sie soll dorthin weitergegeben werden, wo der Updater weiterentwickelt wird.

---

## 1. Detaillierte Protokollierung über `ILogger`

**Befund**

`msTools.Updater` meldet bei einem Fehler im generierten Installationsskript weder den Fehlertext noch den Exit-Code. Es gibt nur:

```text
Installation started
```

Danach bleibt `update.lock` bestehen und der Benutzer sieht nicht, was im PowerShell- bzw. Shell-Skript schief gelaufen ist.

**Aufgabe**

Wenn die Anwendung einen `ILogger` registriert, muss `msTools.Updater` protokollieren:

- Gefundenes Release und zugeordnetes Package (`AutoUpdatePackageDescriptor`).
- Erfolgreiches Herunterladen des Packages nach `pending/`.
- Vollständigen Inhalt und Dateipfad des generierten Installationsskripts.
- Exakten Startbefehl des Skripts.
- `stdout` und `stderr` während der Skriptausführung.
- `ExitCode` des Skripts.
- Klare Fehlermeldung, wenn das Skript fehlschlägt.

**Akzeptanzkriterien**

- `ILogger` wird konsistent in `AutoUpdateInstaller`, `AutoUpdateOrchestrator`, `AutoUpdateScriptGenerator` und `DefaultAutoUpdateProcessRunner` verwendet.
- Fehlerhafte Installationen enden mit einem `AutoUpdateResult`, das die ursprüngliche Fehlermeldung des Skripts enthält.
- Benutzer sehen ohne externen Test-Host, warum ein Update nicht funktioniert.

---

## 2. IIS-App-Pools: Verwendung existierender Cmdlets

**Befund**

`msTools.Updater 0.7.0-rc.10` generiert PowerShell-Skripte mit:

```powershell
Stop-IISApplicationPool -Name $AppPoolName
Start-IISApplicationPool -Name $AppPoolName
```

Diese Cmdlets sind im in-box `IISAdministration`-Modul (Version 1.1.0.0, PSGallery-Version ebenfalls 1.1.0.0) **nicht enthalten**.

```powershell
Get-Command -Module IISAdministration
# liefert Get-IISAppPool, Start/Stop-IISSite, ... aber KEINE Start/Stop-IISApplicationPool Cmdlets
```

**Aufgabe**

`msTools.Updater` muss entweder:

- das `WebAdministration`-Modul verwenden (`Stop-WebAppPool` / `Start-WebAppPool`), oder
- prüfen, ob das installierte `IISAdministration`-Modul tatsächlich `Stop-IISApplicationPool` / `Start-IISApplicationPool` exportiert, bevor diese Cmdlets ins Skript geschrieben werden.

Eine harte Abhängigkeit vom `IISAdministration`-Modul in einer Version, die diese Cmdlets enthält, ist keine praktikable Lösung, weil diese Version auf aktuellen Windows-Systemen nicht verfügbar ist.

**Akzeptanzkriterien**

- Das generierte IIS-Skript läuft auf einem Standard-Windows-Server mit installiertem IIS und `WebAdministration`-Modul fehlerfrei.
- Es werden keine Cmdlets referenziert, die im installierten Modul nicht existieren.
- Wenn `IISAdministration` verwendet werden soll, wird vorab geprüft, ob die benötigten Cmdlets vorhanden sind, und ein aussagekräftiger Fehler ausgegeben.

---

## 3. Zulässige Installationstypen transparent priorisieren

**Befund**

Wenn `AppPoolName` gesetzt ist, soll laut Dokumentation `IIS` vor `ServiceName` / `ExecutablePath` verwendet werden. Aktuell führt die fehlende bzw. falsch gewählte Zielauflösung aber zu Fehlern wie:

```text
Configure a service name, executable path or app pool name before starting installation.
```

**Aufgabe**

`IAutoUpdateServiceResolver` / `AutoUpdateInstallationTarget` muss klar und prüfbar priorisieren:

1. `AppPoolName` (IIS)
2. `ServiceName` (Windows-Dienst)
3. `ExecutablePath` (eigenständige ausführbare Datei)
4. `UpdateUnitName` (Linux/systemd)

Für jede Konstellation soll ein sauberer Fehler kommen, wenn die erforderlichen Windows/Linux-Voraussetzungen fehlen.

**Akzeptanzkriterien**

- `IIS`-Konfiguration funktioniert, ohne dass `ServiceName` oder `ExecutablePath` ebenfalls gesetzt sein müssen.
- Fehlermeldungen nennen den erkannten Zieltyp, nicht nur eine generische Aufforderung.

---

## 4. `Install` muss heruntergeladene Packages über Prozessgrenzen finden

**Befund**

`IAutoUpdateCommandHandler.InstallAsync` installiert das Package, das in derselben Prozess-Sitzung heruntergeladen wurde. Beendet sich der Host (z. B. Test-Host oder Web-App nach `download`), kennt ein erneuter `install`-Aufruf das Package nicht mehr und meldet:

```text
No update package is ready to install.
```

**Aufgabe**

Das heruntergeladene Package-Deskriptor (`AutoUpdatePackageDescriptor`) muss persistent im `DownloadPath` gespeichert werden, damit ein neuer Prozess `install` ausführen kann, ohne erneut `check` + `download` starten zu müssen.

**Akzeptanzkriterien**

- Nach `download` ist das Package in `pending/` und ein Deskriptor steht im `DownloadPath` bereit.
- `install` rekonstruiert den Deskriptor aus dem persistenten Store.
- Unterbrechungen zwischen `download` und `install` werden ohne `update.lock`-Blockade fortgesetzt.

---

## 5. `ApplicationDirectory` muss für den Host sinnvoll ermittelt werden

**Befund**

`HostAutoUpdateEnvironment` verwendet `IHostEnvironment.ContentRootPath`. Bei einem Test-Host oder Self-Contained-App führt das dazu, dass `msTools.Updater` versucht, Dateien im laufenden Host-Verzeichnis zu überschreiben. Dabei werden geladene DLLs blockiert (z. B. `msTools.Updater.dll`):

```text
Copy-Item : Der Prozess kann nicht auf die Datei ...\msTools.Updater.dll zugreifen, da sie von einem anderen Prozess verwendet wird.
```

**Aufgabe**

`msTools.Updater` sollte `ApplicationDirectory` konfigurierbar machen (z. B. `AutoUpdateOptions.ApplicationDirectory` oder `IAutoUpdateEnvironment` kann injiziert/überschrieben werden), damit Update-Ziel und Host-Prozess getrennt werden können.

**Akzeptanzkriterien**

- `ApplicationDirectory` ist über `AutoUpdateOptions` konfigurierbar.
- Standardwert bleibt `ContentRootPath`, kann aber überschrieben werden.
- Alle Pfade (`DownloadPath`, `pending/`, etc.) werden relativ zum konfigurierten `ApplicationDirectory` aufgelöst.

---

## 6. Fehlende/nicht erreichbare Update-Quellen klar kommunizieren

**Befund**

Weder `RepositoryOwner/Name` noch `LocalSourceDirectory` sind konfiguriert. `msTools.Updater` fällt auf ein leeres `AutoUpdateLocalFolderSource` zurück und meldet:

```text
Outcome = NoUpdate
Code = NoNewerUpdateAvailable
Message = No newer update is available.
```

Das sieht aus wie ein erfolgreicher Check ohne Update, obwohl gar keine Quelle konfiguriert war.

**Aufgabe**

Wenn keine Quelle explizit konfiguriert ist und der Fallback leer ist, soll `msTools.Updater` einen klar erkennbaren Fehler liefern, z. B. `NoUpdateSourceConfigured`.

---

## 7. `update.lock` und hängende Installationen sauber behandeln

**Befund**

Wenn ein Installationsskript fehlschlägt oder der Host während der Installation beendet wird, bleibt `update.lock` bestehen. Der Updater meldet dann weiter `IsLocked` und blockiert neue Checks/Downloads.

**Aufgabe**

- `HealthTimeoutSeconds` sollte verlässlich funktionieren: ein alter Lock muss als stale erkannt werden können.
- Am Ende eines fehlgeschlagenen oder abgeschlossenen Skripts muss der Lock entfernt werden.
- Der Aufrufer sollte einen Weg bekommen, den Lock manuell freizugeben oder ein `Force` anzufordern.
