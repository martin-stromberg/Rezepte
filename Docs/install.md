# Installationsanweisungen

Diese Anleitung beschreibt das manuelle Deployment der Anwendung auf einem Linux-Server mit systemd. Die Anwendung zielt auf `net10.0` und wird typischerweise für `linux-x64` veröffentlicht.

## Runtime auf dem Server prüfen

Bei einem framework-abhängigen Publish (`--self-contained false`) müssen die passenden .NET-10-Shared-Frameworks auf dem Zielserver installiert sein. Prüfe die installierte Runtime vor dem Ausrollen:

```bash
dotnet --info
```

In der Ausgabe müssen beide Shared Frameworks in einer passenden .NET-10-Version vorhanden sein:

- `Microsoft.NETCore.App`
- `Microsoft.AspNetCore.App`

Framework-Assemblies wie `System.Runtime.Serialization.Primitives.dll` müssen bei einem framework-abhängigen Publish nicht im Publish-Verzeichnis liegen. Sie werden aus dem installierten .NET-Shared-Framework des Servers geladen. Fehlt dort eine passende .NET-10-Runtime, kann die Anwendung beim Rendern von Blazor-Komponenten mit `System.IO.FileNotFoundException` abbrechen.

## Publish erzeugen

Verwende framework-abhängiges Deployment, wenn der Server die passenden .NET-10-Shared-Frameworks verlaesslich bereitstellt:

```powershell
dotnet publish Rezepte.Web -c Release -f net10.0 -r linux-x64 --self-contained false
```

Wenn die Server-Runtime nicht kontrollierbar, nicht aktualisierbar oder unklar ist, verwende stattdessen ein self-contained Publish:

```powershell
dotnet publish Rezepte.Web -c Release -f net10.0 -r linux-x64 --self-contained true
```

Kopiere den Inhalt des Publish-Verzeichnisses auf den Linux-Server, zum Beispiel nach `/var/www/rezepte`.

## systemd-Service einrichten

Erstelle `/etc/systemd/system/rezepte.service` für ein framework-abhängiges Deployment:

```ini
[Unit]
Description=Rezepte API Service
After=network.target

[Service]
WorkingDirectory=/var/www/rezepte
ExecStart=/usr/bin/dotnet /var/www/rezepte/Rezepte.Web.dll
Restart=always
RestartSec=10
SyslogIdentifier=rezepte-api
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production

[Install]
WantedBy=multi-user.target
```

Bei einem self-contained Publish kann alternativ der native Host gestartet werden:

```bash
sudo chmod +x /var/www/rezepte/Rezepte.Web
```

```ini
ExecStart=/var/www/rezepte/Rezepte.Web
```

`WorkingDirectory=/var/www/rezepte` muss dabei erhalten bleiben, damit relative Pfade für Datenbank, Logs und statische Dateien zum Deployment-Verzeichnis passen.

## Dienst installieren und starten

```bash
sudo systemctl daemon-reload
sudo systemctl enable rezepte.service
sudo systemctl start rezepte.service
sudo systemctl status rezepte.service
```
