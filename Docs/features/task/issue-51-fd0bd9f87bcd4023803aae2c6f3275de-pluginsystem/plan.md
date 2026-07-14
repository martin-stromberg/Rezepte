# Umsetzungsplan

## Ziel

Die Einstellungen zeigen alle bekannten Importplugins inklusive Backup und Webseitenquellen an und erlauben Aktivieren, Deaktivieren und Sortieren ueber den bestehenden Pluginsettings-Mechanismus.

## Schritte

1. Plugin-Registrierung und Settings-Service analysieren, um zu bestimmen, warum nur AI-Plugins angezeigt werden.
2. Sicherstellen, dass die ausgelagerten Pluginprojekte in der Web-Anwendung als bekannte Pluginquellen verfuegbar sind.
3. Falls noetig den Katalog bekannter Plugins erweitern oder die Projekt-/Assembly-Referenzen korrigieren, damit Backup und Webseitenquellen in der Settings-Liste erscheinen.
4. Tests fuer die Pluginsettings-Liste ergaenzen, sodass Backup und Webseitenquellen sichtbar und sortierbar bleiben.
5. Relevante bestehende Import-Plugin-Tests ausfuehren.
6. Dokumentation nur aktualisieren, falls sich Nutzerverhalten oder Dokumentationsinhalt konkret aendert.

## Akzeptanzkriterien

- Settings-Pluginliste enthaelt `AI-Foto`, `AI-URL`, Backup und die bekannten Webseitenquellen.
- Alle Eintraege besitzen stabile Plugin-IDs, Anzeigenamen, Sortierung und Aktivierungsstatus.
- Aktivieren/Deaktivieren und Verschieben funktioniert fuer ausgelagerte Plugins unveraendert ueber die bestehende Settings-UI.
- Automatisierte Tests sichern den erwarteten Pluginbestand ab.

## Offene Punkte

Keine.
