# Import-Pfade — Bestandsaufnahme

Zwei produktive Ausführungspfade für „Rezept importieren": Pfad B (synchron, `IImportService`) und Pfad A (Session, `ImportOrchestrator`).

## Pfad B — synchroner `ImportService`

### Schnittstelle und Implementierung

- `Rezepte.Web/Services/Import/IImportService.cs:6-17` — `Task<ImportResult> ImportAsync(Stream stream, string fileName, string? targetCookbookId, string userId, CancellationToken ct = default)`. **Kein `uri`-Parameter** — die Schnittstelle kann die Quell-URI strukturell nicht durchreichen.
- `Rezepte.Web/Services/Import/ImportService.cs:14-80` — Konstruktor-Injection: `IPluginManager`, `IServiceProvider`, `IImportedRecipePersister`, `ILogger<ImportService>`.
  - Zeile 41: `AcquireActiveHandlersAsync` → `PluginHandlerLease` über `_serviceProvider` (scoped).
  - Zeilen 42-76: Schleife über `lease.Handlers`; pro Handler `stream.Seek(0)` (45), `handler.CanHandleAsync` (49, Fehler → warn+continue, 51-55).
  - Zeile 66: `handler.HandleAsync(stream, fileName, null, targetCookbookId ?? string.Empty, userId, ct)` — **`uri` hart `null`**.
  - Zeile 67: `recipePersister.PersistAsync(res, targetCookbookId, userId, ct)`.
  - Zeilen 71-75: Handler-Exception → `ImportResult(false, ex.Message, [])`, Rückgabe.
  - Zeile 78: kein passender Handler → `ImportResult(false, "No suitable import plugin found for this file or URL.", [])`.
- Nicht vorhanden in Pfad B: Collection-Preview (`ICollectionImportHandler.TryReadCollectionPreviewAsync`), interaktive Handler (`IInteractiveImportHandler`), Item-Status, `handler.UserId`-Setzung.

### Alle Aufrufer von `IImportService`/`ImportService`

Produktiv (vollständige Liste via Volltextsuche — keine weiteren Fundstellen in `.razor`/ViewModels):

- `Rezepte.Web/Controllers/CookbooksController.cs:166` — `ImportFromFile(string cookbookId, IFormFile file, [FromServices] IImportService ...)` → `POST api/cookbooks/{cookbookId}/import`.
- `Rezepte.Web/Controllers/CookbooksController.cs:206` — `ImportFromUrl(string cookbookId, ImportUrlRequest, [FromServices] IImportService ...)` → `POST api/cookbooks/{cookbookId}/import-url`.
- `Rezepte.Web/Controllers/CookbooksController.cs:230` — `ImportFromFile(IFormFile file, [FromServices] IImportService ...)` → `POST api/cookbooks/import` (500-MB-Limits, Zeilen 228-229).
- `Rezepte.Web/Controllers/CookbooksController.cs:265` — `ImportFromUrl(ImportUrlRequest, [FromServices] IImportService ...)` → `POST api/cookbooks/import-url`.
- `Rezepte.Web/Controllers/CookbooksController.cs:575-602` — privater Helfer `ImportFromUrlAsync(url, cookbookId, IImportService, userId, ct)`; Aufruf `importService.ImportAsync` Zeile 588 (übergibt **kein** `uri`, obwohl `uri` in Zeile 577 validiert vorliegt).

Tests:

- `Rezepte.Tests/Services/Import/ImportServicePluginTests.cs:50-55` — instanziiert `ImportService` direkt (`CreateService`), 2 Testfälle.

### DI-Registrierung

- `Rezepte.Web/Extensions/ServiceCollectionExtensions.cs:201` — `services.AddScoped<IImportService, ImportService>();`

## Pfad A — `ImportOrchestrator` (Session)

`Rezepte.Web/Services/Import/ImportOrchestrator.cs` — `public sealed class` (Zeile 10).

### Öffentliche API

- `StartImportAsync(Stream, fileName, string? uri, string? targetCookbookId, userId, ct) → Task<string>` (sessionId): Zeilen 104-278.
- `GetSessionForUser(id, userId) → ImportSession?`: Zeile 92 (delegiert an `TryGetSessionForUser`, 391-402; User-Bindung via `InitiatorUserId`, Ordinal-Vergleich).
- `Confirm(sessionId, userId, accepted) → bool`: Zeilen 288-295 (setzt `ConfirmationTcs`).
- `SubmitSelection(sessionId, userId, ImportCollectionSelection) → SelectionSubmitResult`: Zeilen 304-356 (unter `lock (session.SyncRoot)`; erfordert `State == "SelectionRequired"`; Validierung: Items>0, TargetCookbookId gesetzt, ItemId in Preview, URL-Match; `TrySetResult` → einmalig).
- `CancelSelection(sessionId, userId) → SelectionSubmitResult`: Zeilen 364-389 (`SelectionTcs.TrySetCanceled`, `session.Result = ImportResult(false, "Import cancelled.")`).
- `SelectionSubmitResult` record (Accepted/Invalid/NotFound): Zeilen 558-586.

### `ImportSession`-Record (Zeilen 38-81)

`Status`/`State` (Strings, default `"Queued"`), `ReadOnly`, `WaitingForConfirmation`, `ConfirmationPrompt`, `ConfirmationTcs` (TCS<bool>), `SelectionTcs` (TCS<ImportCollectionSelection>), `CollectionPreview`, `CollectionItems`, `Result` (`ImportResult?`), internes `SyncRoot`. Sessions in `ConcurrentDictionary _sessions` (Zeile 17) — rein in-memory, kein Cleanup/Eviction vorhanden.

### Interner Ablauf `StartImportAsync`

- Zeilen 108-119: Kopie des Streams in privaten `MemoryStream` (Fire-and-Forget-Sicherheit).
- Zeilen 121-123: Session anlegen (Id = `Guid.NewGuid().ToString("n")`).
- Zeilen 129-275: `_ = Task.Run(...)` — **kein `await`, kein TCS auf Session-Ende**; Cancellation wird innerhalb des Delegates beobachtet (Kommentar 125-128), Token wird nicht an `Task.Run` übergeben.
- Zeile 135: `IServiceScopeFactory.CreateScope()` — Handler/Persister scoped auflösbar.
- Zeile 136: `AcquireActiveHandlersAsync(scope.ServiceProvider, ct)`.
- Zeilen 141-234: Handler-Schleife; `handler.UserId = userId` (145); Status-Updates.
  - Collection-Pfad: `handler is ICollectionImportHandler` → `TryReadCollectionPreviewAsync(workStream, fileName, uri, ct)` (149-160); bei Preview → `HandleCollectionImportAsync` (164-165) + `break`.
  - `CanHandleAsync` (173-184).
  - Interaktiv: `handler is IInteractiveImportHandler` → `SessionInteraction` → `HandleInteractiveAsync(..., uri, targetCookbookId ?? "", userId, interaction, ct)` (191-208); PersistAsync 202-204; `session.Result` 205.
  - Nicht-interaktiv: `HandleAsync(..., uri, ...)` (215); PersistAsync 216-218; `session.Result` 219.
  - Handler-Exception → `session.Result = ImportResult(false, Beautify(ex))` (225-233).
  - Kein Handler/kein Result → „No suitable plugin found"-Ergebnis (235-247).
- Fehlerbehandlung: `OperationCanceledException` → Status „Cancelled" (249-255); andere Exceptions → Result mit `ex.Message` (256-262); `finally` dispose workStream (263-274).

### `session.Result`-Setzung und Warte-Mechanik

`session.Result` wird an den Zeilen 205, 219, 229, 238, 244, 254, 261, 386 und 503-505 gesetzt. **Es existiert kein Completion-TCS**; Konsumenten pollen: Controller-Statusendpunkt + Blazor-Polling (800 ms), Tests `WaitUntilAsync`-Schleife. Für ein synchrones Abwarten (FA-1) müsste entweder `session.Result` gepollt oder ein neuer Mechanismus ergänzt werden.

### `HandleCollectionImportAsync` (Zeilen 404-508)

Preview-Lookup mit Duplikat-ID-Prüfung (412-419, `TryCreatePreviewLookup` 510-525), `SelectionTcs` + State `"SelectionRequired"` (425-427), Cancellation-Registration (429-432), `await SelectionTcs.Task` (434), Item-Status aufbauen (439-450), Schleife: `ImportCollectionItemAsync` (462) → `persister.PersistRecipeAsync` je `ImportedRecipe` (472-474) → `SetCollectionItemStatus` (527-549); Endergebnis Zeilen 501-507.

### `SessionInteraction` (Zeilen 588-619)

`IImportInteraction`-Implementierung: `ReportStatusAsync` setzt `session.Status`; `AskForConfirmationAsync` setzt `ConfirmationPrompt`, `WaitingForConfirmation`, `ConfirmationTcs` und wartet (`604-617`).

### Session-Zustände (String-Werte)

`"Queued"`, `"Checking"`, `"Importing"`, `"SelectionRequired"`, `"Completed"`, `"Failed"`, `"Cancelled"` (Status-Texte teils deutsch, z. B. „Auswahl erforderlich").

### Registrierung

- `Rezepte.Web/Extensions/ServiceCollectionExtensions.cs:230` — `services.AddSingleton<ImportOrchestrator>();` (Kommentar Zeile 229: Singleton, erzeugt Scopes für scoped Handler).

## Controller — `Rezepte.Web/Controllers/CookbooksController.cs`

Ctor-Injection (Zeile 24): `ICookbookService`, `IRecipeService`, `IRemoteContentFetcher` (`_remoteContent`, Zeile 28). Auth: Bearer + Cookie (Zeile 23).

### Synchrone (Legacy-)Endpunkte

| Endpunkt | Zeilen | Rückgabe |
|---|---|---|
| `POST {cookbookId}/import` (`ImportFromFile`) | 165-195 | `Ok { created }` / `BadRequest { message }` / `NotFound` (Cookbook) / `StatusCode(499)` / `Problem 500` |
| `POST {cookbookId}/import-url` (`ImportFromUrl`) | 205-217 | delegiert an `ImportFromUrlAsync` |
| `POST import` (ohne Cookbook, 500 MB) | 227-255 | wie oben |
| `POST import-url` (ohne Cookbook) | 264-272 | delegiert an `ImportFromUrlAsync` |

### Session-Endpunkte

Mit `{cookbookId}`: `import-session/start` (304-312), `import-session/{sessionId}/status` (322-331), `.../confirm` (342-351), `.../selection` (363-375), `.../selection/cancel` (385-395), `import-session/start-file` (509-536, 500 MB).
Ohne `{cookbookId}`: `import-session/start` (406-414), `.../status` (423-432), `.../confirm` (442-451), `.../selection` (462-474), `.../selection/cancel` (483-493), `import-session/start-file` (549-572).

### Private Helfer

- `ImportFromUrlAsync(url, cookbookId, IImportService, userId, ct)`: Zeilen 575-602 — `RemoteUrlGuard.TryValidateAsync` (577), `_remoteContent.FetchAsync(uri, ct)` (583), `importService.ImportAsync` (588) — **ohne `uri`-Weitergabe**.
- `StartImportSessionFromUrlAsync(url, cookbookId, orchestrator, userId, ct)`: Zeilen 605-617 — gleiche Validierung/Fetch, übergibt `uri.ToString()` an `StartImportSessionFromStreamAsync` (616).
- `StartImportSessionFromStreamAsync(ms, fileName, uri, cookbookId, orchestrator, userId, ct)`: Zeilen 620-641 — `orchestrator.StartImportAsync` (630), `Ok { sessionId }`.
- `ToSessionStatus(session)`: Zeilen 643-680 — anonymes DTO: status, waitingForConfirmation, confirmationPrompt, result{success,error,created}, state, readOnly, collection{...}, items[...].
- `ValidateSelectionCookbooksAsync` (682-703), `ToSelection` (705-710).
- Request-Records: `CreateCookbookRequest` (718), `UpdateCookbookRequest` (725), `ImportUrlRequest` (731), `ConfirmRequest` (737), `ImportCollectionSelectionRequest` (743), `ImportCollectionSelectionItemRequest` (751).

### `RemoteUrlGuard` / `IRemoteContentFetcher`

- `Rezepte.Web/Security/RemoteUrlGuard.cs:10-102` — statische SSRF-Validierung: http/https, nur Ports 80/443, DNS-Auflösung, interne IPs abgelehnt. `TryValidateAsync → (bool, string?, Uri?)` (Zeile 20).
- `_remoteContent` (`IRemoteContentFetcher`, DI `ServiceCollectionExtensions.cs:202` scoped) liefert `fetched.Content` (Stream), `fetched.FileName`, `fetched.Success`, `fetched.StatusCode`, `fetched.ErrorBody`.

## UI-Aufrufer

### `Rezepte.Web/Components/Pages/CookbookPage.razor`

- Verstecktes `InputFile` Zeile 135; URL-Import-Overlay Zeilen 137-159.
- `TriggerImportFile` (268-272) → JS-Klick.
- `OnImportInputFile` (275-296): Multipart-Upload → `POST api/cookbooks/{Id}/import` (Zeile 286); Erfolg → `LoadRecipesAsync`, Fehler → JS-Alert.
- `ImportFromUrlAsync` (309-342): `POST api/cookbooks/{Id}/import-url` mit `{ url }` (Zeile 322); Erfolg → Liste neu laden + Overlay zu.
- → CookbookPage nutzt ausschließlich die **synchronen Legacy-Endpunkte** (Pfad B). Kein Session-Polling hier.

### `Rezepte.Web/Components/Shared/CreateRecipeDialog.razor`

Session-basierter Pfad A, dreimal nahezu identischer Ablauf:

- `OnFileChange` (279-396): `import-session/start-file` (304-306, mit/ohne cookbookId), dann Poll-Schleife `while(true)` mit `Task.Delay(800)` (333) → `import-session/{id}/status` (335-337); `waitingForConfirmation` → `ShowUserConfirmationAsync` + `.../confirm` (352-366); `UpdateCollectionStatus` (349); `result` → Erfolg: `CloseAsync(DialogResult(true,...))` / Fehler: `message` (368-382).
- `ImportFromUrlAsync` (420-534): `import-session/start` mit `{ Url }` (430-435), gleiche Poll-Schleife (460-520).
- Testlauf-Pfad (ca. 779-910): `api/import/test-urls` (779), `api/import/test-delete` (799), `import-session/start` pro URL (814-815), Poll-Schleife (845-880).
- `ShowUserConfirmationAsync` (537-572): eigenes TCS-Overlay (kein `confirm()`).
- `UpdateCollectionStatus(statusJson)` (641+): liest `readOnly`, `state`, `collection`, `items`.
- Selection-Submit: `import-session/{activeSessionId}/selection` (740-741).
- Zustandsfelder: `activeSessionId` (207), `collectionPreview`/`collectionItems`/`availableCookbooks`/`collectionSelection`/`collectionReadOnly`/`collectionSelectionRequired` (208-213).

### Weitere Aufrufer

`ImportTestController` (`api/import/test-urls`, `api/import/test-delete`, Admin-Rolle) — `Rezepte.Web/Controllers/ImportTestController.cs:44-88`; nutzt `ITestRecipeImportService` + `IRecipeService.FindByUri` (Zeilen 79-81), **kein** `IImportService`. Nicht Teil des Geschäftsvorfalls.

## Plugin-Vertrag (`Rezepte.Import.Abstractions`)

- `IImportHandler` (`IImportHandler.cs:6-33`): `string UserId { set; }`, `CanHandleAsync(stream, fileName, ct)`, `HandleAsync(stream, fileName, string? uri, string targetCookbookId, userId, ct) → ImportResult`.
- `IInteractiveImportHandler : IImportHandler` (`IInteractiveImportHandler.cs:6-20`): `HandleInteractiveAsync(..., string? uri, ..., IImportInteraction interaction, ct)`.
- `ICollectionImportHandler : IImportHandler` (`ICollectionImportHandler.cs:6-26`): `TryReadCollectionPreviewAsync(stream, fileName, string? uri, ct) → ImportCollectionPreview?`, `ImportCollectionItemAsync(item, userId, ct) → ImportResult`.
- `ImportResult` (`ImportResult.cs:11-15`): record `(bool Success, string? Error, List<string> CreatedRecipeIds, IReadOnlyList<ImportedRecipe>? ImportedRecipes = null)`.
- `ImportedRecipe` (`ImportedRecipe.cs:6-47`): `SourceUri` (Zeile 21), Title, Description, Portions, WorkTimeMinutes, Ingredients, Steps, Images.
- Collection-Modelle (`ImportCollectionModels.cs`): `ImportCollectionPreview` (11-15), `ImportCollectionItem` (26-31), `ImportCollectionSelection`/`-Item` (38-51), `ImportCollectionItemStatus` (64-71), `ImportCollectionItemState` (76-97).
- `IImportInteraction` (`IImportInteraction.cs`): `ReportStatusAsync`, `AskForConfirmationAsync`.
- `IImportHandler`-Vertrag ist eingefrorener Plugin-Vertrag: `contract-baselines/import-contract/{0.2.0,0.3.0}/*.dll`, `ImportContractVersion 0.3.0` in `Directory.Build.props:7`; ApiCompat-Strenge dokumentiert in `Docs/help/import-plugins.md:145-170`. Anforderung: **kein Vertrags-Change** (Nicht-Ziel).

### `uri`-Verwendung in Handlern

- `Rezepte.Web/Services/Import/BaseAIImportHandler.cs:24` — `BaseAIImportHandler : BaseImportHandler, IInteractiveImportHandler`.
- `BaseAIImportHandler.cs:293` — `SourceUri = uri` in `ToImportedRecipe` (264-306). Basis beider KI-Handler (`Rezepte.Import.Plugins.AIFoto`, `Rezepte.Import.Plugins.AIUrl`).

### Persistierung/Dedup — `ImportedRecipePersister`

`Rezepte.Web/Services/Import/ImportedRecipePersister.cs` (scoped, `ServiceCollectionExtensions.cs:203`):

- `PersistAsync(result, targetCookbookId, userId, ct)` (22-46): iteriert `ImportedRecipes` → `PersistRecipeAsync`; bei Fehlschlag `Success=false`; leert `ImportedRecipes` im Rückgabe-Record.
- `PersistRecipeAsync(imported, targetCookbookId, userId, ct)` (59-109): **Dedup Zeilen 63-65** — `recipes.FindByUri(userId, imported.SourceUri, ct)` nur wenn `SourceUri` nicht leer; Treffer → `UpdateAsync` (70-85), sonst `CreateAsync` (89-105). `uri`-Parameter des Rezepts = `imported.SourceUri`.
- `ToSteps`/`ToIngredient` (111-147), `AddImagesAsync` (149-167).
- `IRecipeService.FindByUri(userId, v, ct) → Task<Recipe?>` — `Rezepte.Web/Services/RecipeService.cs:234` (Interface), Implementierung `:864`.

### Plugin-Manager-Vertrag

- `Rezepte.Web/Services/Import/Plugins/IPluginManager.cs`: `GetActiveHandlersAsync` (26), `AcquireActiveHandlersAsync` Default-Impl → `PluginHandlerLease` (33-34); `PluginManager.AcquireActiveHandlersAsync` mit `LifecycleLockReleaser` (`PluginManager.cs:139-147`).
- `PluginImportHandler`-Record: `Rezepte.Web/Services/Import/Plugins/PluginImportHandler.cs:9` — `(ImportPluginDescriptor Plugin, IImportHandler Handler)`.
- `PluginHandlerLease`: `PluginHandlerLease.cs:9` — `IAsyncDisposable`.
