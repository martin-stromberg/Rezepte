# Anforderung: Pluginsystem fuer Rezeptimporte

## Metadaten

| Feld | Wert |
|------|------|
| Aufgaben-ID | fd0bd9f8-7bcd-4023-803a-ae2c6f3275de |
| Branch | task/issue-51-fd0bd9f87bcd4023803aae2c6f3275de-pluginsystem |
| Erstellt | 2026-07-13 |

## Ausgangssituation

Die Serviceklassen fuer den Import neuer Rezepte sind aktuell fest im Hauptprojekt implementiert. Quellenabhaengige Importlogik ist dadurch direkt an das Hauptprojekt gekoppelt.

## Ziel

Die Importlogik fuer Rezeptquellen soll ueber ein Pluginsystem modularisiert werden. Das Hauptprojekt soll Plugins zur Laufzeit aus einem Pluginverzeichnis laden, verwalten und beim Import aktivierte Plugins in einer administrativ festgelegten Reihenfolge ansprechen.

## Fachliche Anforderungen

### Pluginstruktur

- Es soll ein neues Shared-Projekt geben.
- Das Shared-Projekt soll Schnittstellen und Basisklassen fuer Import-Services enthalten.
- Fuer jede Importquelle soll es ein eigenes Klassenbibliotheksprojekt geben.
- Jedes Quellenprojekt soll als Plugin bereitgestellt werden koennen.

### Pluginablage und Erkennung

- Plugin-DLLs sollen im Programmverzeichnis unter einem Ordner `plugins` abgelegt werden.
- Plugin-DLLs duerfen direkt im `plugins`-Ordner liegen.
- Plugin-DLLs duerfen alternativ in jeweils eigenen Unterordnern innerhalb von `plugins` liegen.
- Beim Programmstart soll das Hauptprojekt gefundene Plugins erkennen.
- Wird beim Programmstart ein neues Plugin gefunden, soll es an das Ende der aktuellen Pluginliste gesetzt werden.

### PluginManager

- Im Hauptprojekt soll ein `PluginManager` die Steuerung der Plugins implementieren.
- Der `PluginManager` soll gefundene Plugins laden und fuer Importvorgaenge bereitstellen.
- Der `PluginManager` soll die konfigurierte Aktivierung und Reihenfolge der Plugins beruecksichtigen.

### Administration

- Administratoren sollen jedes gefundene Plugin in den Einstellungen aktivieren koennen.
- Administratoren sollen jedes gefundene Plugin in den Einstellungen deaktivieren koennen.
- Administratoren sollen in den Einstellungen die Reihenfolge der Plugins steuern koennen.
- Die konfigurierte Pluginliste muss dauerhaft gespeichert werden.
- Neu erkannte Plugins duerfen bestehende Reihenfolgen nicht veraendern und werden hinten angehaengt.

### Rezeptimport

- Wird ueber den Dialog auf der Startseite ein Import ausgefuehrt, soll der Import ueber den `PluginManager` erfolgen.
- Der `PluginManager` soll dabei alle aktivierten Plugins in der konfigurierten Reihenfolge ansprechen.
- Jedes aktivierte Plugin soll pruefen koennen, ob es die angegebene Quelle verarbeiten kann.
- Das erste aktivierte Plugin, das meldet, dass es die Quelle verarbeiten kann, soll die Rezeptdaten liefern.
- Deaktivierte Plugins duerfen beim Import nicht angesprochen werden.

## Nichtfunktionale Anforderungen

- Die Plugin-Schnittstellen muessen in einem Shared-Projekt liegen, damit Hauptprojekt und Pluginprojekte dieselben Vertrage verwenden.
- Quellenabhaengige Importlogik soll aus dem Hauptprojekt herausgeloest werden.
- Das System soll erweiterbar sein, ohne fuer neue Quellen Import-Serviceklassen fest im Hauptprojekt implementieren zu muessen.
- Die bestehende Importfunktion auf der Startseite soll fachlich erhalten bleiben und kuenftig ueber Plugins ausgefuehrt werden.

## Akzeptanzkriterien

- Es existiert ein Shared-Projekt mit den Schnittstellen und Basisklassen fuer Rezeptimport-Plugins.
- Fuer die vorhandenen Importquellen existieren separate Klassenbibliotheksprojekte.
- Das Hauptprojekt kann Plugin-DLLs aus `plugins` und aus Unterordnern von `plugins` erkennen.
- Ein `PluginManager` verwaltet Laden, Reihenfolge, Aktivierung und Importauswahl der Plugins.
- Gefundene Plugins sind in den Einstellungen fuer Administratoren sichtbar.
- Administratoren koennen Plugins aktivieren und deaktivieren.
- Administratoren koennen die Pluginreihenfolge in den Einstellungen aendern.
- Ein neu gefundenes Plugin wird automatisch am Ende der bestehenden Liste einsortiert.
- Beim Import werden nur aktivierte Plugins in der gespeicherten Reihenfolge geprueft.
- Das erste passende Plugin liefert die Rezeptdaten.
- Wenn ein Plugin deaktiviert ist, beeinflusst es den Import nicht.

## Offene Punkte

- Es ist nicht angegeben, welche bestehenden Importquellen in eigene Pluginprojekte ausgelagert werden muessen.
- Es ist nicht angegeben, in welchem Speicherort oder Datenmodell die Plugin-Aktivierung und Reihenfolge persistiert werden soll.
- Es ist nicht angegeben, wie sich der Import verhalten soll, wenn kein aktiviertes Plugin die Quelle verarbeiten kann.
- Es ist nicht angegeben, wie fehlerhafte oder inkompatible Plugin-DLLs behandelt und angezeigt werden sollen.
- Es ist nicht angegeben, ob Plugin-DLLs zur Laufzeit nachgeladen werden muessen oder nur beim Programmstart erkannt werden.
