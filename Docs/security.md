# Daten und Sicherheit

## Authentifizierung und Autorisierung

- Passwörter werden serverseitig mit PBKDF2-HMAC-SHA256 gehasht gespeichert (210.000 Iterationen, Policy-Grenzen 100.000 bis 1.000.000); veraltete Hashes werden beim Login automatisch aktualisiert.
- Benutzernamen werden zentral serverseitig auf Länge, erlaubte Zeichen, reservierte Namen sowie IP-/Domain- und offiziell wirkende Muster geprüft.
- Website-Zugriffe verwenden ein HTTP-only Auth-Cookie.
- Login und Registrierung sind pro Client-IP auf 10 Anfragen pro Minute begrenzt (HTTP 429 bei Überschreitung).
- Das Passwort bei der Registrierung muss mindestens 6 Zeichen lang sein.
- API-Controller sind über JWT abgesichert; Admin-Endpunkte verlangen die Rolle `Admin`.
- Die Registrierung ist nur offen, solange noch kein Benutzer existiert.

## Daten und Medien

- Rezeptbilder werden nur an den Eigentümer des Rezepts ausgeliefert und mit `Cache-Control: private` gekennzeichnet.
- Rezept-, Kochbuch-, Kalender- und Einstellungsdaten sind benutzerbezogen modelliert.
- Session-basierte Importabläufe sind an den initiierenden authentifizierten Benutzer gebunden; fremde oder ungültige Session-IDs legen keine Sessiondetails offen.

## Netzwerk und Import

- Serverseitige Abrufe benutzergelieferter URLs (URL-Import) sind auf öffentliche http(s)-Adressen und die Standardports beschränkt; Loopback-, Link-Local- und private Netzbereiche werden abgelehnt.
- PATs für private GitHub-Pluginquellen verbleiben im geschützten Secret-Speicher des Backends und werden weder an das Frontend ausgegeben noch protokolliert.

## security.txt

- Die Pfade `/security.txt`, `/.well-known/security.txt`, `/.well-known/security.md` und `/.well-known/security.html` sind explizit von der Authentifizierungspflicht ausgenommen und öffentlich erreichbar; bei deaktivierter Funktion (`SecurityTxt.Enabled = false`) antworten alle vier Endpunkte mit HTTP 404.

Details zur security.txt-Konfiguration und zum Betrieb stehen in [Docs/help/security-txt/index.md](help/security-txt/index.md).
