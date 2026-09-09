# Funktionsumfang

Rezepte ist eine deutschsprachige Webanwendung zur Verwaltung von Kochbüchern, Rezepten, Bildern und geplanten Mahlzeiten.

## Benutzer und Berechtigungen

- Benutzerregistrierung, Login und Logout mit Cookie-Authentifizierung für die Weboberfläche.
- Serverseitige Username-Validierung für Registrierung, Profil und Admin-Benutzerverwaltung.
- JWT-Authentifizierung für API-Aufrufe.
- Optionaler Demo-Daten bei der Registrierung: fünf vorbefüllte Kochbücher mit 43 Rezepten, fünf Kalendereinträge für die nächsten fünf Tage und Einkaufslisteneinträge.
- Erster registrierter Benutzer wird automatisch Administrator.
- Admin-Bereich für Benutzerverwaltung und globale Einstellungen.

Details zu Benutzerkonten, Registrierung und Passwort-Policy stehen in [Docs/help/user-accounts.md](help/user-accounts.md).

## Oberfläche und Navigation

- Responsive Navigation mit kompaktem Benutzer- und Einstellungsbereich.
- Visueller Ladebalken bei Navigation (konfigurierbar).
- Suche nach Rezepten und Anzeige neuester bzw. zufälliger Rezepte.

Details zur Navigation und zum Ladebalken stehen in [Docs/help/navigation.md](help/navigation.md) und [Docs/help/loading-bar-configuration.md](help/loading-bar-configuration.md).

## Kochbücher und Rezepte

- Kochbücher mit Sortierung, Detailseiten und Zuordnung mehrerer Rezepte.
- Rezeptverwaltung mit Zutaten, Zubereitungsschritten, Portionsangaben, Bildern und verlinkten Beilagen.
- Bild-Upload mit Validierung, zugeschnittenen Thumbnails und Grossbildansicht.

Details zur Rezeptsuche und zu Beilagen stehen in [Docs/help/recipe-search.md](help/recipe-search.md) und [Docs/help/side-dishes.md](help/side-dishes.md).

## Planung und Listen

- Einkaufsliste mit Gruppen, abhakbaren Zutaten und Übernahme von Rezeptzutaten inklusive gruppierter Beilagenzutaten.
- Kalenderansicht für geplante Rezepte mit optionaler Übernahme hinterlegter Beilagen.

Details zur Einkaufsliste stehen in [Docs/help/shopping-list.md](help/shopping-list.md).

## Import und Export

- Import von Rezepten aus Backups, Dateien, URLs und unterstützten Webseiten.
- Plugin-Framework für Rezeptimporte mit aktivierbarer Reihenfolge in den Admin-Einstellungen.
- Globale GitHub-Pluginquellen in den Admin-Einstellungen mit automatischer Prüfung beim Anwendungsstart.
- Optionale KI-Importe über Gemini für URL-Quellen sowie Google Vision und Gemini für Fotoimporte.
- Export- und Sicherungsfunktionen inklusive Fortschrittsanzeige für Datenexporte sowie validierte Wiederherstellung aus ZIP-Archiven mit Ressourcenlimits.
- Programmupdates über `msTools.Updater` mit Status, Prüfung, Download und Installation in den Admin-Einstellungen sowie Pre-Install-Update-Backups.

Details zum Plugin-Framework stehen in [Docs/help/import-plugins.md](help/import-plugins.md). Details zu Exporten, Sicherungen und Wiederherstellung stehen in [Docs/help/exports.md](help/exports.md). Details zu Programmupdates stehen in [Docs/help/application-updates.md](help/application-updates.md).

## Sicherheit

- `security.txt` gemäss RFC 9116 unter `/security.txt` und `/.well-known/security.txt` mit optionalen Zusatzformaten (`/.well-known/security.md`, `/.well-known/security.html`); Konfiguration durch Administratoren im Einstellungsbereich.

Details zu security.txt stehen in [Docs/help/security-txt/index.md](help/security-txt/index.md).

## CI/CD

- GitHub Actions für Pull-Request-Prüfungen auf `staging`, automatische Promotion- und Sync-PRs sowie automatisierte Release-Artefakte.
- Nutzungs- und KI-Limits über Einstellungen und Protokollierung.

Details zu GitHub Actions und dem Release-Prozess stehen in [Docs/help/github-actions.md](help/github-actions.md).
