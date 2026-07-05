# Strukturierte Anforderung

## Ausgangslage

Der Datenexport und die Sicherung schlagen bei grossen Datenmengen fehl. Ursache ist voraussichtlich, dass die Exportdaten synchron im Request-Kontext zusammengestellt werden und dadurch Laufzeit-, Speicher- oder Timeout-Grenzen erreicht werden.

## Ziel

Datenexporte und Sicherungen muessen auch bei grossen Datenmengen zuverlaessig erstellt werden.

## Funktionale Anforderungen

- Die Zusammenstellung eines Exports muss als Hintergrundausfuehrung gestartet werden.
- Der Benutzer soll den Export aus der Weboberflaeche ausloesen koennen, ohne dass der HTTP-Request bis zur Fertigstellung offen bleiben muss.
- Wenn technisch sinnvoll und ohne unverhaeltnismaessigen Aufwand moeglich, soll die Weboberflaeche waehrend der Ausfuehrung einen Fortschrittsbalken anzeigen.
- Nach Abschluss muss das erzeugte Export- oder Sicherungsartefakt heruntergeladen beziehungsweise weiterverwendet werden koennen.
- Fehler waehrend der Hintergrundausfuehrung muessen fuer den Benutzer erkennbar sein.

## Nicht-funktionale Anforderungen

- Die Loesung muss fuer grosse Datenmengen robuster sein als die bisherige synchrone Ausfuehrung.
- Langlaufende Exporte duerfen normale Web-Requests nicht blockieren.
- Der Zustand eines laufenden Exports muss serverseitig nachvollziehbar sein.

## Akzeptanzkriterien

- Ein Export kann gestartet werden und liefert sofort eine Rueckmeldung ueber den gestarteten Hintergrundjob.
- Der Export laeuft serverseitig weiter, auch wenn die urspruengliche Anfrage abgeschlossen ist.
- Der aktuelle Status des Exports kann abgefragt werden.
- Bei vorhandenem Fortschrittswissen wird ein Fortschrittswert ausgegeben, der in der UI als Fortschrittsbalken genutzt werden kann.
- Nach erfolgreichem Abschluss ist der Export als Datei verfuegbar.
- Bei Fehlern wird ein fehlgeschlagener Status mit Fehlerhinweis angezeigt.
