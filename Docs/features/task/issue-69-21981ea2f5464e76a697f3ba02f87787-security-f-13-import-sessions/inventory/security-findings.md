# Sicherheitsbefund und Risiken

## Befund 1: Keine Besitzbindung in ImportSession

Die Session wird ohne Initiator-UserId angelegt. Die beim Start vorhandene `userId` wird nur fuer Pluginhandler und Persistierung genutzt, aber nicht im Sessionobjekt abgelegt.

Auswirkung:

- Spaetere Requests koennen nicht gegen den Sessionbesitzer geprueft werden.
- Die zufaellige Session-ID ist der einzige Zugriffsschutz.

## Befund 2: Statuszugriff gibt fremde Details preis

Status-Endpunkte lesen Sessions ueber `orchestrator.GetSession(sessionId)` und geben bei Treffer `ToSessionStatus(session)` zurueck.

Die Antwort kann Importstatus, Confirmation-Prompt, Fehlertexte, Collection-Vorschau, URLs, Zielkochbuecher, Rezept-IDs und Item-Status enthalten. Diese Daten fallen direkt unter die in der Anforderung ausgeschlossenen fremden Sessiondetails.

## Befund 3: Confirm kann fremde Sessions steuern

Confirm-Endpunkte rufen `orchestrator.Confirm(sessionId, req.Accepted)` ohne Benutzerkontext auf. Ein authentifizierter Benutzer mit fremder Session-ID kann dadurch eine wartende Confirmation bestaetigen oder ablehnen.

## Befund 4: Selection kann fremde Sessions beeinflussen

Selection-Endpunkte pruefen die Zielkochbuecher gegen den aktuellen Benutzer, nicht aber den Besitzer der Session. Damit kann eine fremde Session manipuliert werden, indem eine Auswahl fuer diese Session eingereicht wird.

## Befund 5: Selection-Cancel kann fremde Sessions abbrechen

Selection-Cancel-Endpunkte rufen `orchestrator.CancelSelection(sessionId)` ohne Benutzerkontext auf. Ein authentifizierter Benutzer mit fremder Session-ID kann eine fremde Auswahlphase abbrechen.

## Befund 6: Fehlerverhalten unterscheidet noch fachliche Sessionzustaende

Fuer unbekannte Sessions wird `NotFound` geliefert. Fuer existierende, aber im falschen Zustand befindliche Sessions werden teilweise `BadRequest` mit fachlicher Meldung geliefert. Sobald Besitzpruefung eingefuehrt wird, duerfen fremde Sessions diese fachlichen Fehler nicht erreichen, weil solche Fehler indirekt Sessionexistenz oder Zustand verraten koennen.

## Erwartete Sicherheitsinvariante

Fuer jeden Zugriff auf eine bestehende Import-Session gilt:

- keine Sessiondetails lesen, bevor die Besitzerpruefung bestanden ist,
- keine Mutation ausfuehren, bevor die Besitzerpruefung bestanden ist,
- fremde und unbekannte Session-IDs aus Sicht des Aufrufers gleich oder mindestens gleich informationsarm behandeln,
- Besitzerinformation ausschliesslich serverseitig beim Start aus der authentifizierten Identitaet ableiten.
