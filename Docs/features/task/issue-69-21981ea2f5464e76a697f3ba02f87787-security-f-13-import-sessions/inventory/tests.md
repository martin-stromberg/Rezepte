# Tests und Testluecken

## Vorhandene Tests

Datei: `Rezepte.Tests/Services/Import/ImportOrchestratorTests.cs`

Bestehende Abdeckung:

- aktive Plugins werden in konfigurierter Reihenfolge verwendet,
- interaktive Plugin-Confirmation funktioniert,
- nach fehlgeschlagenem passendem Plugin wird gestoppt,
- Collection-Preview wartet auf Selection und importiert ausgewaehlte Items,
- offene Selection kann spaeter eingereicht werden,
- doppelte Collection-Item-IDs fuehren zu Fehler,
- parallele Selection-Submits akzeptieren nur eine Auswahl.

Diese Tests sind gute Anknuepfungspunkte fuer Besitzerpruefungen auf Orchestrator-Ebene.

## Fehlende Tests zur Anforderung

Es fehlen Negativtests mit zwei authentifizierten Benutzern fuer:

- Status einer fremden Session lesen,
- fremde Session bestaetigen,
- fremde Session abbrechen,
- Selection einer fremden Session einreichen,
- fremde Selection abbrechen,
- keine Preisgabe von Sessiondetails bei fremden Session-IDs.

## Testebenen

### Orchestrator-Tests

Sinnvoll fuer die Kerninvariante:

- Session speichert `OwnerUserId`/`InitiatorUserId` nach `StartImportAsync`.
- Zugriff mit Besitzer-UserId liefert Session.
- Zugriff mit fremder UserId liefert keinen Zugriff.
- Confirm/SubmitSelection/CancelSelection mit fremder UserId mutieren Session nicht.

Diese Tests koennen in `ImportOrchestratorTests.cs` mit vorhandenen Fake-Handlern erweitert werden.

### Controller-Tests

Sinnvoll fuer HTTP-Verhalten und Fehlerantworten:

- fremde Session-ID fuehrt bei Status zu `NotFound` oder gleichwertig informationsarmer Antwort,
- Confirm/Selection/Cancel fuer fremde Session liefert keine Sessiondetails,
- Controller liest `ClaimTypes.NameIdentifier` und reicht ihn an userId-aware Orchestrator-Methoden weiter.

Aktuell gibt es nur minimale Controller-Test-Infrastruktur. `AuthControllerTests` zeigt Tests mit `ControllerContext` und `DefaultHttpContext`; fuer `CookbooksController` koennen aehnliche direkte Controller-Tests erstellt werden. Da `ImportOrchestrator` eine konkrete Klasse ist, sind Orchestrator-Tests fuer den Sicherheitskern einfacher als Mock-basierte Controller-Tests.

## Datenleak-Pruefung

Negativtests sollten nicht nur den Statuscode pruefen, sondern auch sicherstellen, dass keine typischen Sessiondetails in der Antwort stehen:

- keine `confirmationPrompt`,
- kein `collection`,
- keine Collection-Item-Titel oder URLs,
- keine `result.error`,
- keine `created` Rezept-IDs,
- keine Besitzerinformationen.

Bei einer reinen `NotFound()`-Antwort ist diese Pruefung trivial. Bei `NotFound(new { message = ... })` sollte die Meldung generisch bleiben.
