# Anforderung: Suche nach Rezepten reparieren

## Ausgangssituation

Die Rezeptsuche funktioniert aktuell nicht korrekt. Sie liefert bei Suchanfragen keine passenden Ergebnisse.

## Problem

Ein vorhandenes Rezept wird trotz passendem Suchbegriff nicht in der Ergebnisliste angezeigt. Konkret wird bei der Suche nach "Honig" das Rezept "Honig - Senf - Sojamarinade" nicht gelistet.

## Ziel

Die Suche nach Rezepten soll wieder passende Rezepte finden und anzeigen.

## Funktionale Anforderungen

- Die Rezeptsuche muss bei einem Suchbegriff passende Rezepte in der Ergebnisliste anzeigen.
- Die Suche nach "Honig" muss das Rezept "Honig - Senf - Sojamarinade" als Treffer liefern.
- Wenn ein vorhandenes Rezept den eingegebenen Suchbegriff passend enthaelt, darf die Ergebnisliste nicht leer bleiben.

## Akzeptanzkriterien

- Bei Eingabe des Suchbegriffs "Honig" wird das Rezept "Honig - Senf - Sojamarinade" angezeigt.
- Die Suche liefert fuer vorhandene passende Rezeptdaten wieder Ergebnisse.
- Die Korrektur beschraenkt sich auf die Wiederherstellung der erwarteten Suchfunktion.

## Nicht-Ziele

- Es wird keine neue Suchfunktionalitaet gefordert, die ueber das Finden passender vorhandener Rezepte hinausgeht.
- Es werden keine Aenderungen an Rezeptdaten gefordert.
