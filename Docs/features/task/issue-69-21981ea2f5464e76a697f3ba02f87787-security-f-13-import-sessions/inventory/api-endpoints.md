# API-Endpunkte

## Controller

Datei: `Rezepte.Web/Controllers/CookbooksController.cs`

Der Controller ist per `[Authorize]` fuer Bearer- und Cookie-Authentifizierung geschuetzt und liest die aktuelle UserId ueber `ClaimTypes.NameIdentifier`.

## Start-Endpunkte

- `POST api/cookbooks/{cookbookId}/import-session/start`
- `POST api/cookbooks/import-session/start`
- `POST api/cookbooks/{cookbookId}/import-session/start-file`
- `POST api/cookbooks/import-session/start-file`

Alle Start-Endpunkte pruefen, ob ein authentifizierter Benutzer vorhanden ist, und rufen am Ende `StartImportSessionFromStreamAsync(..., userId, ct)` auf. Die Methode uebergibt die `userId` weiter an `orchestrator.StartImportAsync(...)`.

Bei den Varianten mit `cookbookId` wird vor dem Start zusaetzlich geprueft, ob das Zielkochbuch dem aktuellen Benutzer gehoert.

## Status-Endpunkte

- `GET api/cookbooks/{cookbookId}/import-session/{sessionId}/status`
- `GET api/cookbooks/import-session/{sessionId}/status`

Aktueller Ablauf:

- `orchestrator.GetSession(sessionId)`
- bei `null`: `NotFound()`
- sonst: `Ok(ToSessionStatus(session))`

Der aktuelle Benutzer wird in diesen beiden Methoden nicht gelesen. Dadurch kann jede authentifizierte Identitaet eine bekannte fremde Session-ID fuer Statusabfragen verwenden.

## Confirm-Endpunkte

- `POST api/cookbooks/{cookbookId}/import-session/{sessionId}/confirm`
- `POST api/cookbooks/import-session/{sessionId}/confirm`

Aktueller Ablauf:

- `orchestrator.Confirm(sessionId, req.Accepted)`
- bei `false`: `NotFound()`
- sonst: `NoContent()`

Der aktuelle Benutzer wird nicht gelesen. Eine fremde Session kann bestaetigt oder abgelehnt werden, sofern sie auf Confirmation wartet.

## Selection-Endpunkte

- `POST api/cookbooks/{cookbookId}/import-session/{sessionId}/selection`
- `POST api/cookbooks/import-session/{sessionId}/selection`

Aktueller Ablauf:

- UserId wird gelesen; ohne UserId: `Unauthorized()`
- Zielkochbuecher aus dem Request werden gegen den aktuellen Benutzer validiert
- `orchestrator.SubmitSelection(sessionId, ToSelection(req))`

Die Zielkochbuchvalidierung verhindert nicht den Zugriff auf fremde Sessions. Ein Benutzer kann mit einer bekannten fremden Session-ID Selection-Daten gegen seine eigenen Kochbuecher einreichen und damit den fremden Importablauf beeinflussen.

## Selection-Cancel-Endpunkte

- `POST api/cookbooks/{cookbookId}/import-session/{sessionId}/selection/cancel`
- `POST api/cookbooks/import-session/{sessionId}/selection/cancel`

Aktueller Ablauf:

- `orchestrator.CancelSelection(sessionId)`
- bei `IsNotFound`: `NotFound(new { message = result.Error })`
- bei fachlich ungueltig: `BadRequest(new { message = result.Error })`
- sonst: `NoContent()`

Der aktuelle Benutzer wird nicht gelesen. Eine fremde Selection-Session kann abgebrochen werden.

## Statusantwort

`ToSessionStatus` gibt unter anderem folgende Daten aus:

- `status`, `state`, `readOnly`
- `waitingForConfirmation`, `confirmationPrompt`
- `result.success`, `result.error`, `result.created`
- `collection` mit Titel, SourceUri und Items
- `items` mit TargetCookbookId, State, Error und RecipeId

Diese Daten duerfen fuer fremde Sessions nicht ausgeliefert werden.
