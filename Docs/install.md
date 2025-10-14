# Installationsanweisungen

- Veröffentlichung ausführen
  Kontifuration: Release
  Framework net9.0
  Bereitstellungsmodus: Framework-abhängig
  Ziellaufzeit: linux-x64

- Verzeichnisinhalt am Linux-Maschine ablegen
  z.B. /var/www/rezepte

- rezepte.service erstellen
  ```
  [Unit]
  Description=Rezepte API Service
  After=network.target
  [Service]
  WorkingDirectory=/var/www/rezepte
  ExecStart=/usr/bin/dotnet /var/www/rezepte/Rezepte.dll
  Restart=always
  RestartSec=10
  SyslogIdentifier=rezepte-api
  User=www-data
  Environment=ASPNETCORE_ENVIRONMENT=Production
  [Install]
  WantedBy=multi-user.target
  ```
  Datei ablegen unter /etc/systemd/system/rezepte.service

- Dienst installieren und starten:
  ```
  sudo systemctl enable rezepte.service
  sudo systemctl start rezepte.service
  ```