# Umsetzungsplan: Security F-13 - Import-Sessions an Initiator binden

## Zielbild

Import-Sessions werden beim Start serverseitig an die authentifizierte UserId des Initiators gebunden. Alle Folgezugriffe auf Status, Confirm, Selection und Selection-Cancel pruefen diese Besitzbindung, bevor Sessiondetails gelesen oder Sessionzustand mutiert wird.

Fremde und unbekannte Session-IDs werden fuer API-Aufrufer gleich behandelt: `404 NotFound` ohne Sessiondetails. Dadurch kann ein authentifizierter Benutzer aus Fehlerantworten weder Existenz, Besitzer noch Zustand einer fremden Import-Session ableiten.

## Entscheidungen

- Die Besitzbindung wird im vorhandenen in-memory Sessionobjekt `ImportOrchestrator.ImportSession` gespeichert, z. B. als unveraenderliches Feld `InitiatorUserId`.
- Die Besitzerinformation wird ausschliesslich aus der bereits vorhandenen `userId` in `StartImportAsync(...)` abgeleitet.
- Controller verwenden fuer bestehende Sessions ausschliesslich userId-aware Orchestrator-Methoden.
- Fuer fremde Sessions wird dasselbe API-Verhalten wie fuer unbekannte Sessions verwendet: `NotFound()` ohne Fehlerobjekt.
- Die fachlichen `BadRequest`-Antworten fuer falsche Sessionzustaende bleiben nur fuer berechtigte Sessionbesitzer erreichbar.
- Es ist keine Datenbankmigration erforderlich, weil Import-Sessions aktuell nur im Speicher des Singleton-Orchestrators gehalten werden.

## Umsetzung

### 1. Session-Besitzer im Orchestrator speichern

Datei: `Rezepte.Web/Services/Import/ImportOrchestrator.cs`

- `ImportSession` um ein unveraenderliches Besitzerfeld erweitern, z. B. `public string InitiatorUserId { get; init; }`.
- Konstruktion der Session in `StartImportAsync(...)` von `new ImportSession(sessionId)` auf eine Variante mit `userId` umstellen.
- Sicherstellen, dass `InitiatorUserId` nach dem Anlegen nicht mehr veraendert wird.
- Optional eine kleine interne Hilfsmethode einfuehren:
  - Lookup per Session-ID.
  - Vergleich `InitiatorUserId` gegen die aktuelle `userId` mit ordinalem Stringvergleich.
  - Rueckgabe `null` bei unbekannter oder fremder Session.

### 2. UserId-aware Orchestrator-API bereitstellen

Datei: `Rezepte.Web/Services/Import/ImportOrchestrator.cs`

- Statuszugriff ergaenzen, z. B. `GetSessionForUser(string id, string userId)`.
- Confirm auf Besitzer pruefen, z. B. `Confirm(string sessionId, string userId, bool accepted)`.
- Selection auf Besitzer pruefen, z. B. `SubmitSelection(string sessionId, string userId, ImportCollectionSelection selection)`.
- Selection-Cancel auf Besitzer pruefen, z. B. `CancelSelection(string sessionId, string userId)`.
- Bei unbekannter oder fremder Session dieselbe NotFound-Semantik verwenden.
- Die Besitzpruefung muss vor jedem Lesen sensibler Sessiondaten und vor jeder Mutation liegen.
- Bestehende nicht-userId-aware Methoden entweder entfernen und Tests anpassen oder als interne/private Hilfen begrenzen. Controller duerfen diese Methoden nicht mehr fuer geschuetzte Endpunkte verwenden.

### 3. Controller-Endpunkte absichern

Datei: `Rezepte.Web/Controllers/CookbooksController.cs`

- In beiden Status-Endpunkten `GetUserId()` lesen.
  - `null` => `Unauthorized()`.
  - `orchestrator.GetSessionForUser(sessionId, userId)` verwenden.
  - `null` => `NotFound()`.
  - Nur nach erfolgreicher Besitzerpruefung `ToSessionStatus(session)` aufrufen.
- In beiden Confirm-Endpunkten `GetUserId()` lesen.
  - `null` => `Unauthorized()`.
  - userId-aware `Confirm(...)` verwenden.
  - `false` fuer unbekannt/fremd/nicht wartend wie bisher auf `NotFound()` abbilden, sofern der Orchestrator keine differenzierte Rueckgabe einfuehrt.
- In beiden Selection-Endpunkten nach vorhandener UserId- und Zielkochbuchvalidierung userId-aware `SubmitSelection(...)` verwenden.
  - `IsNotFound` => `NotFound()` ohne Meldung.
  - fachlich ungueltig bei berechtigter Session => `BadRequest(new { message = result.Error })`.
- In beiden Selection-Cancel-Endpunkten `GetUserId()` lesen.
  - `null` => `Unauthorized()`.
  - userId-aware `CancelSelection(...)` verwenden.
  - `IsNotFound` => `NotFound()` ohne Meldung.
  - fachlich ungueltig bei berechtigter Session => `BadRequest(new { message = result.Error })`.
- Die Start-Endpunkte behalten ihre bestehende UserId-Ermittlung; sie uebergeben die UserId bereits an `StartImportSessionFromStreamAsync(...)` und `StartImportAsync(...)`.

### 4. Fehlerantworten vereinheitlichen

Dateien:

- `Rezepte.Web/Controllers/CookbooksController.cs`
- `Rezepte.Web/Services/Import/ImportOrchestrator.cs`

- Fremde Sessions duerfen nicht in Codepfade gelangen, die fachliche Sessionfehler erzeugen.
- Fuer fremde oder unbekannte Sessions keine Rueckgabe von:
  - Confirmation-Prompt,
  - Importstatus,
  - Collection-Preview oder Collection-Items,
  - Plugin- oder Importfehlern,
  - Rezept-IDs,
  - Besitzerinformationen.
- Bestehende generische NotFound-Antworten bevorzugen: `return NotFound();`.

### 5. Tests erweitern

Datei: `Rezepte.Tests/Services/Import/ImportOrchestratorTests.cs`

- Bestehende Tests auf neue userId-aware Signaturen anpassen.
- Test: `StartImportAsync` speichert die Initiator-UserId in der Session.
- Test: Besitzer kann seine Session per `GetSessionForUser` lesen.
- Test: fremder Benutzer erhaelt per `GetSessionForUser` keinen Zugriff.
- Test: fremder Benutzer kann eine wartende Confirmation nicht bestaetigen oder ablehnen.
  - Session bleibt `WaitingForConfirmation`.
  - `ConfirmationTcs` wird nicht abgeschlossen.
- Test: fremder Benutzer kann eine Selection nicht einreichen.
  - Ergebnis ist NotFound.
  - Session bleibt `SelectionRequired`.
  - `SelectionTcs` wird nicht abgeschlossen.
- Test: fremder Benutzer kann eine Selection nicht abbrechen.
  - Ergebnis ist NotFound.
  - Session bleibt `SelectionRequired`.
  - `Result` wird nicht auf Cancel gesetzt.
- Bestehende positive Confirm-/Selection-Tests muessen mit Besitzer-UserId weiterhin gruen bleiben.

### 6. Optional gezielte Controller-Tests ergaenzen

Datei: neue oder bestehende Controller-Testdatei unter `Rezepte.Tests`

- Falls die Testinfrastruktur mit direktem ControllerContext ohne hohen Aufwand nutzbar ist, ergaenzen:
  - Status-Endpunkt ohne UserId liefert `Unauthorized`.
  - Status-Endpunkt mit fremder Session liefert `NotFound` und ruft `ToSessionStatus` nicht fuer fremde Sessiondaten auf.
  - Confirm/Selection/Cancel fuer fremde Session liefern informationsarme `NotFound`.
- Wenn Controller-Tests wegen konkreter Orchestrator-Abhaengigkeit unverhaeltnismaessig teuer werden, reicht die Orchestrator-Testabdeckung fuer die Kerninvariante; das HTTP-Verhalten wird dann ueber Code-Review der Controller-Branches abgesichert.

## Verifikation

- `dotnet test`
- Falls die Gesamtsuite wegen bestehender, nicht betroffener Probleme nicht vollstaendig laeuft, mindestens:
  - `dotnet test --filter ImportOrchestratorTests`
- Manuelle Codepruefung:
  - Kein Controller-Sessionzugriff verwendet mehr `GetSession(sessionId)`, `Confirm(sessionId, ...)`, `SubmitSelection(sessionId, ...)` oder `CancelSelection(sessionId)` ohne UserId.
  - `ToSessionStatus(...)` ist nur nach erfolgreicher Besitzerpruefung erreichbar.
  - Fremde Sessions erreichen keine fachlichen `BadRequest`-Fehlerpfade.

## Risiken und Hinweise

- `ImportSession` ist ein Record mit mutable Properties. Das neue Besitzerfeld sollte init-only bleiben, damit spaetere Codepfade es nicht versehentlich veraendern.
- Falls alte nicht-userId-aware Orchestrator-Methoden aus Kompatibilitaetsgruenden erhalten bleiben, duerfen sie nicht aus Controller-Endpunkten erreichbar sein.
- Der Vergleich der UserId sollte exakt erfolgen. Keine Normalisierung aus clientseitigen Daten verwenden.
- Die Signatur von `StartImportAsync` enthaelt `string targetCookbookId`, waehrend Aufrufer teilweise `null` uebergeben. Diese bestehende Nullability-Unstimmigkeit ist nicht Ziel der Sicherheitsaufgabe; nur anfassen, falls Compilerwarnungen durch die geplante Aenderung direkt relevant werden.

## Offene Punkte

