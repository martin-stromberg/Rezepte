# Requirement: Security F-13 - Import-Sessions an Initiator binden

## Metadaten

- Aufgaben-ID: `21981ea2-f546-4e76-a697-f3ba02f87787`
- Branch: `task/issue-69-21981ea2f5464e76a697f3ba02f87787-security-f-13-import-sessions`
- Erstellt: `2026-07-21`
- Ursprung: `#54`

## Ausgangslage

Import-Sessions werden derzeit ueber zufaellige Session-IDs gesteuert. Die Pfade fuer Status, Confirm, Cancel und Selection lesen oder veraendern Import-Sessions, ohne dass ein sichtbarer Besitzerabgleich stattfindet. Der Import-Orchestrator speichert keine Initiator-UserId oder gleichwertige Besitzbindung in der Session.

## Problem

Eine Import-Session ist nicht eindeutig an den Benutzer gebunden, der sie gestartet hat. Dadurch reicht die Kenntnis einer fremden Session-ID potenziell aus, um Informationen ueber diese Session abzurufen oder den Importablauf eines anderen Benutzers zu beeinflussen.

## Risiko

Ein authentifizierter Benutzer, der eine fremde Import-Session-ID erlangt, kann unberechtigt:

- den Status einer fremden Import-Session lesen,
- Confirm-Aktionen fuer eine fremde Import-Session ausloesen,
- Cancel-Aktionen fuer eine fremde Import-Session ausloesen,
- Selection-Aktionen fuer eine fremde Import-Session ausloesen oder veraendern.

## Ziel

Import-Sessions muessen an den initiierenden Benutzer gebunden werden. Jeder Zugriff auf eine bestehende Import-Session muss pruefen, ob der aktuelle authentifizierte Benutzer der Besitzer dieser Session ist. Fremde Session-IDs duerfen weder gelesen noch mutiert werden.

## Betroffene Bereiche

- `Rezepte.Web/Controllers/CookbooksController.cs`
- `Rezepte.Web/Services/Import/ImportOrchestrator.cs`

## Funktionale Anforderungen

### Besitzbindung

- Beim Anlegen einer Import-Session muss die UserId des initiierenden Benutzers gespeichert werden.
- Alternativ darf eine gleichwertige Besitzbindung verwendet werden, sofern sie eindeutig, serverseitig vertrauenswuerdig und fuer alle Session-Zugriffe pruefbar ist.
- Die Besitzinformation darf nicht aus clientseitig manipulierbaren Eingaben abgeleitet werden.

### Zugriffsschutz

- Jeder Status-Zugriff auf eine Import-Session muss den aktuellen Benutzer gegen den Session-Besitzer pruefen.
- Jeder Confirm-Zugriff auf eine Import-Session muss den aktuellen Benutzer gegen den Session-Besitzer pruefen.
- Jeder Cancel-Zugriff auf eine Import-Session muss den aktuellen Benutzer gegen den Session-Besitzer pruefen.
- Jeder Selection-Zugriff auf eine Import-Session muss den aktuellen Benutzer gegen den Session-Besitzer pruefen.
- Bei fehlender, unbekannter oder fremder Session-Berechtigung darf die Session nicht gelesen oder veraendert werden.

### Fehlerverhalten

- Fehlerantworten fuer fremde Session-IDs duerfen keine Details der fremden Import-Session preisgeben.
- Fehlerantworten duerfen insbesondere keine Import-Inhalte, Plugin-Ergebnisse, Statusdetails, Auswahloptionen oder Besitzerinformationen fremder Sessions enthalten.
- Das Fehlerverhalten soll fuer nicht vorhandene und nicht zugaengliche Sessions so gestaltet sein, dass daraus keine fremden Session-Details ableitbar sind.

## Testanforderungen

- Es muessen Negativtests mit zwei unterschiedlichen authentifizierten Benutzern existieren.
- Die Negativtests muessen abdecken, dass Benutzer B den Status einer von Benutzer A gestarteten Import-Session nicht lesen kann.
- Die Negativtests muessen abdecken, dass Benutzer B eine von Benutzer A gestartete Import-Session nicht bestaetigen kann.
- Die Negativtests muessen abdecken, dass Benutzer B eine von Benutzer A gestartete Import-Session nicht abbrechen kann.
- Die Negativtests muessen abdecken, dass Benutzer B die Selection einer von Benutzer A gestarteten Import-Session nicht lesen oder veraendern kann.
- Die Tests muessen sicherstellen, dass bei fremden Session-IDs keine Session-Details in der Antwort erscheinen.

## Akzeptanzkriterien

- Import-Sessions speichern die Initiator-UserId oder eine gleichwertige Besitzbindung.
- Jeder Status-, Confirm-, Cancel- und Selection-Zugriff prueft den aktuellen Benutzer gegen den Session-Besitzer.
- Fremde Session-IDs koennen von authentifizierten Benutzern weder zur Einsicht noch zur Steuerung fremder Importablaeufe verwendet werden.
- Negativtests mit zwei Benutzern decken fremde Session-IDs fuer Status, Confirm, Cancel und Selection ab.
- Fehlerantworten verraten keine fremden Session-Details.

## Nicht-Ziele

- Keine Aenderung der fachlichen Importlogik ausserhalb der Besitzpruefung.
- Keine Erweiterung der Importfunktionen oder Plugin-Auswahl ueber den beschriebenen Zugriffsschutz hinaus.
- Keine Umstellung des Authentifizierungssystems, sofern die vorhandene UserId des aktuellen Benutzers zuverlaessig verwendet werden kann.
