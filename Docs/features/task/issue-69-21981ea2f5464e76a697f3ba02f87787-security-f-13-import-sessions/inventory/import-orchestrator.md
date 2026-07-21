# Import-Orchestrator und Session-State

## Datei

`Rezepte.Web/Services/Import/ImportOrchestrator.cs`

## Lebensdauer und Speicher

`ImportOrchestrator` wird als Singleton registriert. Sessions werden in einem privaten `ConcurrentDictionary<string, ImportSession>` gehalten. Die Session-ID ist ein zufaelliger GUID-String im Format `n`.

Die Sessions sind damit:

- pro laufender App-Instanz im Speicher vorhanden,
- nicht persistent,
- fuer alle Benutzer im selben Prozess ueber dieselbe Dictionary-Instanz erreichbar,
- aktuell nur durch Kenntnis der `sessionId` adressiert.

## ImportSession-Struktur

`ImportSession` enthaelt aktuell:

- `Id`
- `Status`
- `State`
- `ReadOnly`
- `WaitingForConfirmation`
- `ConfirmationPrompt`
- `ConfirmationTcs`
- `SelectionTcs`
- `CollectionPreview`
- `CollectionItems`
- `Result`

Es gibt kein Feld fuer `UserId`, `OwnerUserId`, `InitiatorUserId` oder eine gleichwertige Besitzbindung.

## StartImportAsync

`StartImportAsync(Stream stream, string fileName, string? uri, string targetCookbookId, string userId, CancellationToken ct = default)` erhaelt die UserId und verwendet sie fuer Pluginhandler und Persistierung. Beim Erstellen der Session wird sie jedoch nicht gespeichert:

- `var sessionId = Guid.NewGuid().ToString("n");`
- `var session = new ImportSession(sessionId);`
- `_sessions[sessionId] = session;`

Damit geht die fuer spaetere Zugriffskontrollen benoetigte Besitzerinformation direkt beim Session-Anlegen verloren.

## Zugriffsmethoden

Aktuelle oeffentliche Zugriffsmethoden:

- `GetSession(string id)`
- `Confirm(string sessionId, bool accepted)`
- `SubmitSelection(string sessionId, ImportCollectionSelection selection)`
- `CancelSelection(string sessionId)`

Keine dieser Methoden nimmt eine `userId` entgegen. Der Orchestrator kann daher derzeit nicht unterscheiden, ob der aktuelle Aufrufer Besitzer der Session ist.

## Nebenlaeufigkeit

`SubmitSelection` und `CancelSelection` verwenden `lock (session.SyncRoot)`. Eine Besitzpruefung sollte vor dem Auslesen oder Mutieren sensibler Sessiondaten erfolgen. Bei Methoden, die danach mutieren, sollte die Pruefung so platziert werden, dass zwischen Lookup und Mutation keine ungewollte Freigabe entsteht. Da `OwnerUserId` unveraenderlich sein sollte, ist eine Pruefung direkt nach dem Dictionary-Lookup ausreichend, sofern das Feld nach Session-Erstellung nicht geaendert werden kann.

## Kompatibilitaetsaspekt

Bestehende Tests rufen die Methoden ohne Benutzerkontext auf. Eine Umsetzung kann entweder:

- bestehende Methoden auf userId-aware Signaturen umstellen und Tests anpassen, oder
- neue Methoden wie `GetSessionForUser`, `ConfirmForUser`, `SubmitSelectionForUser`, `CancelSelectionForUser` einfuehren und alte Methoden nur dort behalten, wo sie intern oder fuer Tests vertretbar sind.

Aus Sicherheitssicht sollten Controller ausschliesslich userId-aware Methoden verwenden.
