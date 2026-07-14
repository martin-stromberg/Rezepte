# Anforderung

## Zusammenfassung

In den Einstellungen sollen alle ausgelagerten Importplugins sichtbar sein, nicht nur die beiden AI-Plugins `AI-Foto` und `AI-URL`.

## Fachlicher Kontext

Das Projekt besitzt ein Pluginsystem fuer Rezeptimporte. Neben den AI-Importen gibt es ausgelagerte Importplugins fuer Backup-Importe und bekannte Webseitenquellen. Im Praxistest werden in der Einstellungsseite aktuell nur `AI-Foto` und `AI-URL` angezeigt.

## Erwartetes Verhalten

- Die Pluginliste in den Einstellungen enthaelt die ausgelagerten Importplugins fuer Backup und bekannte Webseitenquellen.
- Diese Plugins koennen in den Einstellungen aktiviert und deaktiviert werden.
- Diese Plugins koennen in den Einstellungen sortiert werden.
- Bestehende AI-Plugins bleiben weiterhin sichtbar und konfigurierbar.

## Nicht-Ziele

- Keine neue Importquelle erfinden.
- Keine grundlegende Umgestaltung der Einstellungsseite.
- Keine Aenderung des Importformats der bestehenden Plugins, sofern fuer die Sichtbarkeit nicht erforderlich.

## Ausfuehrungshinweis

Die Lifecycle-Schritte werden lokal ausgefuehrt, weil in dieser Codex-Umgebung keine separaten Unteragenten verfuegbar sind.
