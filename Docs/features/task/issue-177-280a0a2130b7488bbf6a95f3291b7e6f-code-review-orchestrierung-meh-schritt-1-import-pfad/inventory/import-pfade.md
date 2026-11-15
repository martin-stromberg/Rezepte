# Import-Pfade — Bestandsaufnahme (Schritt 1)

Verifiziert am 2026-11-15 gegen HEAD `961e58b` (Dateien vollständig gelesen, Fundstellen per `*.cs`-Volltextsuche gegengeprüft).

## Pfad B — synchroner `ImportService` (zu beseitigender Pfad)

### Schnittstelle und Implementierung

- `Rezepte.Web/Services/Import/IImportService.cs:6-17` — `Task<ImportResult> ImportAsync(Stream stream, string fileName, string? targetCookbookId, string userId, CancellationToken ct = default)`. **Kein `uri`-Parameter** — die Schnittstelle kann die Quell-URI strukturell nicht durchreichen.
- `Rezepte.Web/Services/Import/ImportService.cs:14-80` — Konstruktor-Injection: `IPluginManager`, `IServiceProvider`, `IImportedRecipePersister`, `ILogger<ImportService>`.
  - Zeile 41: `AcquireActiveHandlersAsync` → `PluginHandlerLease` über `_serviceProvider` (scoped).
  - Zeilen 42-76: Schleife über `lease.Handlers`; `stream.Seek(0)` (45), `handler.CanHandleAsync` (49, Fehler → warn+continue).
  - Zeile 66: `handler.HandleAsync(stream, fileName, null, targetCookbookId ?? string.Empty, userId, ct)` — **`uri` hart `null`**.
  - Zeile 67: `recipePersister.PersistAsync(res, targetCookbookId, userId, ct)`.
  - Zeilen 71-75: Handler-Exception → `ImportResult(false, ex.Message, [])`.
  - Zeile 78: kein passender Handler → `ImportResult(false, "No suitable import plugin found for this file or URL.", [])` — identische Fehlermeldung wie der Orchestrator-Pfad.
- Nicht vorhanden in Pfad B: Collection-Preview (`ICollectionImportHandler.TryReadCollectionPreviewAsync`), interaktive Handler (`IInteractiveImportHandler`), Item-Status, `handler.UserId`-Setzung.

### Alle `IImportService`-Fundstellen (vollständig, `*.cs`-Suche)

Produktiv:

- `Rezepte.Web/Controllers/CookbooksController.cs:166` — `ImportFromFile(string cookbookId, IFormFile file, [FromServices] IImportService)` → `POST api/cookbooks/{cookbookId}/import`.
- `Rezepte.Web/Controllers/CookbooksController.cs:206` — `ImportFromUrl(string cookbookId, ImportUrlRequest, [FromServices] IImportService)` → `POST api/cookbooks/{cookbookId}/import-url`.
- `Rezepte.Web/Controllers/CookbooksController.cs:230` — `ImportFromFile(IFormFile file, [FromServices] IImportService)` → `POST api/cookbooks/import` (500-MB-Limits, Zeilen 228-229).
- `Rezepte.Web/Controllers/CookbooksController.cs:265` — `ImportFromUrl(ImportUrlRequest, [FromServices] IImportService)` → `POST api/cookbooks/import-url`.
- `Rezepte.Web/Controllers/CookbooksController.cs:575` — privater Helfer `ImportFromUrlAsync(url, cookbookId, IImportService, userId, ct)` (Zeilen 575-602); `RemoteUrlGuard.TryValidateAsync` (577), `_remoteContent.FetchAsync` (583), `importService.ImportAsync` (588) — **`uri` wird nicht weitergegeben**, obwohl validiert vorhanden.

Tests:

- `Rezepte.Tests/Services/Import/ImportServicePluginTests.cs` — instanziiert `ImportService` direkt (`CreateService` Zeilen 50-55), 2 Testfälle + eigene Hilfsklassen (`PassthroughPersister`, `FakePluginManager`, `RecordingHandler`).

### DI-Registrierung

- `Rezepte.Web/Extensions/ServiceCollectionExtensions.cs:201` — `services.AddScoped<IImportService, ImportService>();`
- `Rezepte.Web/Extensions/ServiceCollectionExtensions.cs:203` — `services.AddScoped<IImportedRecipePersister, ImportedRecipePersister>();`
- `Rezepte.Web/Extensions/ServiceCollectionExtensions.cs:230` — `services.AddSingleton<ImportOrchestrator>();` (Kommentar Zeile 229: Singleton erzeugt Scopes für scoped Handler).

## Pfad A — `ImportOrchestrator` (verbleibender einziger Pfad)

`Rezepte.Web/Services/Import/ImportOrchestrator.cs` — `public sealed class` (Zeile 10), Singleton.

### Öffentliche API

- `StartImportAsync(Stream, fileName, string? uri, string? targetCookbookId, userId, ct) → Task<string>` (sessionId): Zeilen 104-278.
- `GetSessionForUser(id, userId) → ImportSession?`: Zeile 92 (delegiert an `TryGetSessionForUser`, 391-402; `InitiatorUserId`-Bindung, Ordinal).
- `Confirm(sessionId, userId, accepted) → bool`: Zeilen 288-295 (`ConfirmationTcs.TrySetResult(accepted)`). Hinweis: `accepted: false` beendet nur die Bestätigungsfrage — der Handler entscheidet selbst über das Ergebnis; **kein Session-Abbruch**.
- `SubmitSelection(sessionId, userId, ImportCollectionSelection) → SelectionSubmitResult`: Zeilen 304-356 (unter `lock (session.SyncRoot)`; erfordert `State == "SelectionRequired"`).
- `CancelSelection(sessionId, userId) → SelectionSubmitResult`: Zeilen 364-389 (`SelectionTcs.TrySetCanceled`, `Result = ImportResult(false, "Import cancelled.")`, `State = "Failed"`).
- `SelectionSubmitResult` record (Accepted/Invalid/NotFound): Zeilen 558-586.

### `ImportSession`-Record (Zeilen 38-81)

`Status`/`State` (Strings, default `"Queued"`), `ReadOnly`, `WaitingForConfirmation`, `ConfirmationPrompt`, `ConfirmationTcs` (`TCS<bool>`), `SelectionTcs` (`TCS<ImportCollectionSelection>`), `CollectionPreview`, `CollectionItems`, `Result` (`ImportResult?`), `SyncRoot`. Sessions in `ConcurrentDictionary _sessions` (Zeile 17) — rein in-memory, kein Eviction.

### Interner Ablauf `StartImportAsync`

- Zeilen 108-119: Stream-Kopie in privaten `MemoryStream`.
- Zeilen 121-123: Session anlegen (Id `Guid.NewGuid().ToString("n")`), in `_sessions` eintragen.
- Zeilen 129-275: `_ = Task.Run(...)` — fire and forget, **kein Completion-TCS**; `ct` nicht an `Task.Run` übergeben, aber im Delegate beobachtet.
- Zeile 135: `IServiceScopeFactory.CreateScope()` — Handler/Persister scoped.
- Zeilen 141-234: Handler-Schleife; `handler.UserId = userId` (145).
  - `handler is ICollectionImportHandler` → `TryReadCollectionPreviewAsync(workStream, fileName, uri, ct)` (155); Preview vorhanden → `HandleCollectionImportAsync` + `break`.
  - `CanHandleAsync` (176); `handler is IInteractiveImportHandler` → `HandleInteractiveAsync(..., uri, ..., interaction, ct)` mit `SessionInteraction` (201).
  - Sonst `HandleAsync(..., uri, ...)` (215); danach `PersistAsync` + `session.Result`.
  - Handler-Exception → `session.Result = ImportResult(false, Beautify(ex))` + `break` (225-233).
- `session.Result == null` nach Schleife → „No suitable plugin found"-Ergebnis (235-247).
- `OperationCanceledException` → `Status = "Cancelled"`, `State = "Failed"`, Result `ImportResult(false, "Cancelled")` (249-255).
- Session-Zustände (Strings): `"Queued"`, `"Checking"`, `"Importing"`, `"SelectionRequired"`, `"Completed"`, `"Failed"`, `"Cancelled"` (Status-Text).

### `session.Result`-Setzung und Warte-Mechanik

`session.Result` wird an den Zeilen 205, 219, 229, 238, 244, 254, 261, 386, 501-507 gesetzt. **Es existiert kein Completion-TCS**; Konsumenten pollen (Controller-Statusendpunkt + Blazor 800 ms; Tests `WaitUntilAsync` 25 ms/Timeout 5 s).

### `HandleCollectionImportAsync` (Zeilen 404-508)

`SelectionTcs` + `State = "SelectionRequired"` (425-427), `ct.Register` → `SelectionTcs.TrySetCanceled` (429-432), `await SelectionTcs.Task` (434 — **blockiert bis `SubmitSelection`/`CancelSelection`/Cancellation**), Item-Status, `ImportCollectionItemAsync` → `PersistRecipeAsync` je Item, Endergebnis 501-507.

### `SessionInteraction` (Zeilen 588-619)

`AskForConfirmationAsync` setzt `ConfirmationPrompt`, `WaitingForConfirmation`, `ConfirmationTcs` und wartet auf `ConfirmationTcs.Task` (604-617) — **blockiert bis `Confirm`-Aufruf**; `ct.Register` → `ConfirmationTcs.TrySetCanceled` (611-614).

## Controller — `Rezepte.Web/Controllers/CookbooksController.cs`

Ctor-Injection (Zeile 24): `ICookbookService`, `IRecipeService`, `IRemoteContentFetcher`. Auth: Bearer + Cookie (Zeile 23).

### Synchrone (Legacy-)Endpunkte — Antwortverhalten, das erhalten bleiben muss

| Endpunkt | Zeilen | Rückgabe |
|---|---|---|
| `POST {cookbookId}/import` (`ImportFromFile`) | 165-195 | `Ok { created }` / `BadRequest { message }` / `NotFound { message: "Cookbook not found." }` (Kochbuch-Prüfung vorab) / `StatusCode(499)` / `Problem 500` |
| `POST {cookbookId}/import-url` (`ImportFromUrl`) | 205-217 | Kochbuch-Prüfung, dann `ImportFromUrlAsync` |
| `POST import` (ohne Kochbuch, 500 MB) | 227-255 | wie `{cookbookId}/import`, ohne NotFound-Pfad |
| `POST import-url` (ohne Kochbuch) | 264-272 | `ImportFromUrlAsync` |

`ImportFromUrlAsync` (575-602): URL-Validierungsfehler → `BadRequest { message: urlError }`; Fetch-Fehler → `BadRequest { message: $"Remote request failed: {StatusCode}" }`; Import-`Success=false` → `BadRequest { message: result.Error }`; Erfolg → `Ok { created: CreatedRecipeIds }`.

### Session-Endpunkte (unverändert)

Mit `{cookbookId}`: `import-session/start` (304), `.../status` (322), `.../confirm` (342), `.../selection` (363), `.../selection/cancel` (385), `import-session/start-file` (509). Ohne `{cookbookId}`: `import-session/start` (406), `.../status` (423), `.../confirm` (442), `.../selection` (462), `.../selection/cancel` (483), `import-session/start-file` (549).

Private Helfer für Sessions: `StartImportSessionFromUrlAsync` (605-617 — reicht `uri.ToString()` durch), `StartImportSessionFromStreamAsync` (620-641 — `orchestrator.StartImportAsync`, `Ok { sessionId }`), `ToSessionStatus` (643-680), `ValidateSelectionCookbooksAsync` (682-703), `ToSelection` (705-710).

### `RemoteUrlGuard` / `IRemoteContentFetcher`

- `Rezepte.Web/Security/RemoteUrlGuard.cs` — SSRF-Validierung; `TryValidateAsync → (bool urlAllowed, string? urlError, Uri? uri)`.
- `IRemoteContentFetcher.FetchAsync(uri, ct)` → `fetched.Success`, `.Content` (Stream), `.FileName`, `.StatusCode`, `.ErrorBody`.

## UI-Aufrufer

- `Rezepte.Web/Components/Pages/CookbookPage.razor` — einziger produktiver UI-Aufrufer der synchronen Endpunkte: `POST api/cookbooks/{Id}/import` (Zeile ~286) und `POST api/cookbooks/{Id}/import-url` (Zeile ~322); kein Session-Polling. **Keine UI-Änderung durch diesen Schritt** (Antwortverhalten bleibt).
- `Rezepte.Web/Components/Shared/CreateRecipeDialog.razor` — Session-Pfad mit Polling/Confirm/Selection; unverändert.

## Plugin-Vertrag (`Rezepte.Import.Abstractions`, eingefroren — keine Änderung)

- `IImportHandler`: `UserId { set; }`, `CanHandleAsync`, `HandleAsync(stream, fileName, string? uri, targetCookbookId, userId, ct)`.
- `IInteractiveImportHandler`: `HandleInteractiveAsync(..., uri, ..., IImportInteraction, ct)`.
- `ICollectionImportHandler`: `TryReadCollectionPreviewAsync(stream, fileName, uri, ct)`, `ImportCollectionItemAsync`.
- `ImportResult(bool Success, string? Error, List<string> CreatedRecipeIds, IReadOnlyList<ImportedRecipe>? ImportedRecipes = null)`.
- `BaseAIImportHandler.cs:293` — `SourceUri = uri` (Basis der KI-Handler).
- `ImportedRecipePersister.PersistRecipeAsync` (Zeilen 59-109): Dedup Zeilen 63-65 — `recipes.FindByUri(userId, imported.SourceUri, ct)` nur bei nicht-leerer `SourceUri`; Treffer → `UpdateAsync`, sonst `CreateAsync`.

## Lücken / zu schaffende Mechanik

1. **Synchrones Warten auf Session-Ende:** Es gibt keinen öffentlichen Wartepunkt auf `session.Result`. Optionen: Completion-TCS im Orchestrator (sauber, kein Polling) oder Poll-Schleife im Controller.
2. **Interaktions-Abbruch:** Für `SelectionRequired` existiert `CancelSelection`. Für `WaitingForConfirmation` (interaktive Handler) existiert kein Abbruch-Einstieg — `Confirm(..., accepted: false)` ist kein Abbruch, sondern eine negative Antwort, deren Interpretation dem Handler obliegt.
