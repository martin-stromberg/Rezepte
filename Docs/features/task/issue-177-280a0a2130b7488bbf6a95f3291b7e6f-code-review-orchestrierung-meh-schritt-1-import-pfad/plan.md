# Umsetzungsplan: Ein einziger Ausführungspfad für „Rezept importieren"

## Übersicht

Die beiden produktiven Import-Ausführungspfade werden auf den `ImportOrchestrator` konsolidiert. Die vier synchronen Endpunkte in `CookbooksController` starten intern eine Import-Session und warten synchron auf deren Endzustand; bei Interaktionsbedarf wird die Session abgebrochen und eine Fehlermeldung mit Hinweis auf `import-session/*` zurückgegeben. `IImportService`/`ImportService` wird samt DI-Registrierung und Direkttests entfernt.

## Scope-Klärung

| Feld | Festlegung |
|------|------------|
| Verarbeitungseinheit | Eine Import-Ausführung des Geschäftsvorfalls „Rezept importieren" (Datei- oder URL-Import, mit/ohne Ziel-Kochbuch) |
| Auslösende Ereignisse | HTTP-POST auf `api/cookbooks/{id}/import`, `api/cookbooks/{id}/import-url`, `api/cookbooks/import`, `api/cookbooks/import-url`; unverändert `import-session/*` |
| Erwartete Wirkung | Synchrone Endpunkte liefern dasselbe Ausführungsverhalten wie die Session-API; `SourceUri` wird bei URL-Importen gesetzt; Duplikatserkennung greift; Interaktionsbedarf → Session-Abbruch + Fehler mit Hinweis auf `import-session/*` |
| Umgang mit fehlenden Objekten | Ziel-Kochbuch fehlt: verwerfen (bisheriges `NotFound` bleibt). Import-Session: intern erzeugen und nach Endzustand verwerfen. Bereits importiertes Rezept gleicher `SourceUri`: wiederverwenden (Dedup-Update über `ImportedRecipePersister`) |

### Scope-Ausschlüsse

| # | Einschränkung / Ausschluss | Begründung | Status |
|---|----------------------------|------------|--------|
| 1 | Keine Interaktionsmöglichkeit in den synchronen Endpunkten: Importe mit Sammlungsauswahl (`ICollectionImportHandler`) oder Bestätigung (`IInteractiveImportHandler`) werden abgebrochen und mit Fehlerhinweis auf `import-session/*` abgelehnt. | Im Entwicklungsschritt ausdrücklich als geklärte Randbedingung vorgegeben. | bestätigt (2026-11-15, im Schritt-Auftrag vorgegeben) |
| 2 | Externe API-Konsumenten, die bisher interaktionsbedürftige Inhalte synchron importiert hätten (stiller Fallback auf nicht-interaktives Verhalten), erhalten künftig eine Fehlerrückmeldung statt eines Imports. | Direkte Folge von Ausschluss 1; bewusste Verhaltensänderung der synchronen API im Fehlerfall. | bestätigt (2026-11-15, im Schritt-Auftrag vorgegeben) |

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Synchroner Einstieg | Neue öffentliche Methode `ImportAndWaitAsync` auf `ImportOrchestrator`, die `StartImportAsync` aufruft und auf den Session-Endzustand wartet | Der Orchestrator bleibt der einzige Ausführungseinstieg (Anforderung); keine zweite Orchestrierung im Controller (dünne-Controller-Konvention) |
| Wartemechanik | Zwei `TaskCompletionSource`-Signale in `ImportSession`: `CompletionSource` (gesetzt, wenn `Result` zugewiesen wird) und `InteractionRequiredSource` (gesetzt beim Eintritt in `SelectionRequired` bzw. `WaitingForConfirmation`); der Wartende nutzt `Task.WhenAny` | Kein Polling, keine Latenz; minimale Änderung — `Result`-Setter signalisiert zentral, Interaktionssignale an den zwei existierenden Übergängen |
| Interaktionsabbruch | `SelectionTcs` und `ConfirmationTcs` per `TrySetCanceled` abbrechen; der Hintergrund-Task führt die Session selbst in den Terminal-Fehlerzustand | Nutzt vorhandene Abbruchmechanik (`CancellationToken.Register`/`CancelSelection`-Muster); kein neuer Abbruch-Codepfad nötig |
| Zweiter Ausführungspfad | `IImportService`/`ImportService` entfernen (inkl. DI-Registrierung und `ImportServicePluginTests`); Endpunkte injizieren `ImportOrchestrator` direkt | Anforderung lässt Entfernen oder Delegation zu; Entfernen ist einfacher — eine reine Delegationshülle wäre ein Middle-Man-Smell ohne Mehrwert |
| `uri`-Durchreichung | URL-Endpunkte übergeben `uri.ToString()` an `ImportAndWaitAsync`; Datei-Endpunkte übergeben `null` (wie `import-session/start-file`) | `uri` ist in `ImportFromUrlAsync` nach `RemoteUrlGuard` bereits validiert vorhanden; Datei-Uploads haben keine Quell-URI |

## Programmabläufe

### Synchroner Datei-Import (`POST {cookbookId}/import` und `POST import`)

1. Endpunkt validiert User, Datei und (bei Variante mit Kochbuch) das Ziel-Kochbuch — unverändert.
2. Endpunkt ruft `orchestrator.ImportAndWaitAsync(ms, file.FileName, null, cookbookId, userId, ct)` auf.
3. `ImportAndWaitAsync` startet via `StartImportAsync` eine Session, wartet auf `CompletionSource` oder `InteractionRequiredSource` und gibt das `ImportResult` zurück.
4. Endpunkt bildet das Result unverändert ab: `Success` → `Ok { created }`; sonst `BadRequest { message }`. `OperationCanceledException` → `StatusCode(499)`; andere Exceptions → `Problem 500`.

Beteiligte Klassen: `CookbooksController`, `ImportOrchestrator`, `ImportSession`, `IPluginManager`, `IImportHandler`, `IImportedRecipePersister`.

### Synchroner URL-Import (`POST {cookbookId}/import-url` und `POST import-url`)

1. Endpunkt validiert User/Request (und Kochbuch) — unverändert.
2. Privater Helfer `ImportFromUrlAsync(url, cookbookId, orchestrator, userId, ct)`: `RemoteUrlGuard.TryValidateAsync` → `_remoteContent.FetchAsync` → `orchestrator.ImportAndWaitAsync(ms, fetched.FileName, uri.ToString(), cookbookId, userId, ct)` — die validierte Quell-URI wird jetzt durchgereicht.
3. Handler setzt `SourceUri = uri` auf dem `ImportedRecipe`; `ImportedRecipePersister.PersistRecipeAsync` findet per `FindByUri` vorhandene Rezepte und aktualisiert statt neu anzulegen.
4. Antwortverhalten unverändert (`Ok { created }` / `BadRequest { message }` / 499 / 500).

Beteiligte Klassen: `CookbooksController`, `RemoteUrlGuard`, `IRemoteContentFetcher`, `ImportOrchestrator`, `BaseAIImportHandler`, `ImportedRecipePersister`, `RecipeService.FindByUri`.

### Interaktionsabbruch über synchrone Endpunkte

1. Die interne Session erreicht `State == "SelectionRequired"` (Collection-Preview) oder `WaitingForConfirmation` (interaktiver Handler) — dabei wird `InteractionRequiredSource` signalisiert.
2. `ImportAndWaitAsync` bricht die Session ab: `SelectionTcs`/`ConfirmationTcs` per `TrySetCanceled` canceln → Hintergrund-Task führt die Session in den Terminal-Fehlerzustand.
3. `ImportAndWaitAsync` gibt `ImportResult(false, <Fehlermeldung mit Hinweis auf import-session/*>, [])` zurück → Endpunkt antwortet `BadRequest { message }`.

Beteiligte Klassen: `ImportOrchestrator`, `ImportSession`, `SessionInteraction`, `HandleCollectionImportAsync`.

### Fehlermeldung bei Interaktionsbedarf

Konstante im Orchestrator, englisch (Konvention der API-Fehlermeldungen), mit ausdrücklichem Hinweis auf die Session-Endpunkte, z. B.:
`"This import requires user interaction (collection selection or confirmation). Use the interactive import endpoints under 'import-session/*' instead."`

## Neue Klassen

Keine.

## Änderungen an bestehenden Klassen

### `ImportOrchestrator.ImportSession` (Record, in `ImportOrchestrator.cs`)

- **Neue interne Member:** `CompletionSource` (`TaskCompletionSource<ImportResult>`, `RunContinuationsAsynchronously`), `InteractionRequiredSource` (`TaskCompletionSource`, `RunContinuationsAsynchronously`) — beide `internal`.
- **Geänderte Eigenschaft:** `Result` — Setter signalisiert `CompletionSource.TrySetResult(value)` bei nicht-null-Zuweisung; alle bestehenden Zuweisungen bleiben unverändert wirksam.

### `ImportOrchestrator` (`Rezepte.Web/Services/Import/ImportOrchestrator.cs`)

- **Neue Methode:** `Task<ImportResult> ImportAndWaitAsync(Stream stream, string fileName, string? uri, string? targetCookbookId, string userId, CancellationToken ct = default)` — öffentlich, XML-Doku. Ablauf: `StartImportAsync` aufrufen → Session aus `_sessions` holen → `Task.WhenAny(CompletionSource.Task, InteractionRequiredSource.Task).WaitAsync(ct)` → bei `Result != null` Result zurückgeben → sonst `AbortInteractiveSession(session)` + `ImportResult(false, InteractionRequiredMessage, [])`.
- **Neue private Methode:** `AbortInteractiveSession(ImportSession session)` — unter `lock (session.SyncRoot)`: `ReadOnly = true`, `SelectionTcs?.TrySetCanceled()`, `ConfirmationTcs?.TrySetCanceled()`.
- **Neue Konstante:** `InteractionRequiredMessage` — Fehlertext mit Hinweis auf `import-session/*` (siehe Programmablauf).
- **Geänderte Stellen:**
  - `HandleCollectionImportAsync`: nach `session.State = "SelectionRequired"` → `session.InteractionRequiredSource.TrySetResult()`.
  - `SessionInteraction.AskForConfirmationAsync`: nach Setzen von `WaitingForConfirmation`/`ConfirmationTcs` → `_session.InteractionRequiredSource.TrySetResult()`.

### `CookbooksController` (`Rezepte.Web/Controllers/CookbooksController.cs`)

- **`ImportFromFile(string cookbookId, ...)` (Zeile 166):** `[FromServices] IImportService importService` → `[FromServices] ImportOrchestrator orchestrator`; Aufruf `orchestrator.ImportAndWaitAsync(ms, file.FileName, null, cookbookId, userId, ct)`.
- **`ImportFromUrl(string cookbookId, ...)` (Zeile 206):** Parameter-Swap; `ImportFromUrlAsync(request.Url, cookbookId, orchestrator, userId, ct)`.
- **`ImportFromFile(IFormFile file)` (Zeile 230):** Parameter-Swap; `orchestrator.ImportAndWaitAsync(ms, file.FileName, null, null, userId, ct)`.
- **`ImportFromUrl(ImportUrlRequest)` (Zeile 265):** Parameter-Swap; `ImportFromUrlAsync(request.Url, null, orchestrator, userId, ct)`.
- **`ImportFromUrlAsync` (Zeilen 575-602):** Signatur `(string url, string? cookbookId, ImportOrchestrator orchestrator, string userId, CancellationToken ct)`; Zeile 588 → `orchestrator.ImportAndWaitAsync(ms, fetched.FileName, uri.ToString(), cookbookId, userId, ct)`.

### `ServiceCollectionExtensions` (`Rezepte.Web/Extensions/ServiceCollectionExtensions.cs`)

- **Entfernt:** Zeile 201 `services.AddScoped<IImportService, ImportService>();`

### Entfernte Dateien

- `Rezepte.Web/Services/Import/IImportService.cs`
- `Rezepte.Web/Services/Import/ImportService.cs`
- `Rezepte.Tests/Services/Import/ImportServicePluginTests.cs`

## Datenbankmigrationen

Keine.

## Validierungsregeln

Keine — Eingabevalidierung der Endpunkte bleibt unverändert (`RemoteUrlGuard`, Datei-/Kochbuch-/Request-Prüfungen).

## Konfigurationsänderungen

Keine.

## Seiteneffekte und Risiken

- **Externe API-Konsumenten:** Interaktionsbedürftige Importe über synchrone Endpunkte liefern künftig `BadRequest` statt eines Imports — gewünschte Verhaltensänderung (Scope-Ausschluss 2).
- **Session-Lebensdauer:** Synchron beendete/abgebrochene Sessions bleiben wie bisher alle Sessions im In-Memory-`_sessions`-Dictionary (kein Eviction vorhanden — kein neues Problem, kein Umbau in diesem Schritt).
- **Endzustand abgebrochener Sessions:** Beim Canceln des `ConfirmationTcs` landet die `TaskCanceledException` im Handler-Fehlerpfad (`Failed`) statt im `Cancelled`-Zweig; bei `SelectionTcs` im `Cancelled`-Pfad. Beides sind Terminal-Fehlerzustände; das synchron zurückgegebene Result ist für den Aufrufer maßgeblich.
- **Hängende Handler:** `ImportAndWaitAsync` wartet ohne eigenes Timeout — Abbruch erfolgt über den Request-`CancellationToken` (wie bisher beim synchronen Pfad; `ct` wird in die Hintergrundverarbeitung durchgereicht und dort beobachtet).
- **`handler.UserId`-Setzung:** Pfad A setzt `handler.UserId = userId` vor `CanHandleAsync` — neu wirksam auch für die synchronen Endpunkte (gewünscht, gleiches Ausführungsverhalten).

## Umsetzungsreihenfolge

1. **`ImportSession` um Warte-Signale erweitern**
   - Voraussetzungen: Keine.
   - Beschreibung: `CompletionSource`/`InteractionRequiredSource` als interne Member; `Result`-Setter signalisiert `CompletionSource`.

2. **`ImportOrchestrator.ImportAndWaitAsync` + `AbortInteractiveSession` + Interaktionssignale**
   - Voraussetzungen: Schritt 1.
   - Beschreibung: Neue öffentliche Methode mit XML-Doku, Konstante `InteractionRequiredMessage`, Signale in `HandleCollectionImportAsync` und `SessionInteraction.AskForConfirmationAsync`.

3. **`CookbooksController` auf Orchestrator umstellen**
   - Voraussetzungen: Schritt 2.
   - Beschreibung: Vier Endpunkt-Signaturen + `ImportFromUrlAsync`-Helfer auf `ImportOrchestrator`/`ImportAndWaitAsync` umstellen; `uri.ToString()` durchreichen.

4. **`IImportService`/`ImportService` und DI-Registrierung entfernen**
   - Voraussetzungen: Schritt 3 (keine Aufrufer mehr).
   - Beschreibung: Beide Dateien löschen, `AddScoped<IImportService, ImportService>` entfernen.

5. **Tests anpassen**
   - Voraussetzungen: Schritte 2-4.
   - Beschreibung: `ImportServicePluginTests.cs` löschen; `ImportOrchestratorTests` um URI-Durchreich-Assertion und `ImportAndWaitAsync`-Tests erweitern.

6. **Build + Testlauf + `dotnet format`**
   - Voraussetzungen: Schritte 1-5.
   - Beschreibung: `dotnet build Rezepte.sln`, `dotnet test Rezepte.Tests/Rezepte.Tests.csproj`, bei Bedarf `dotnet format Rezepte.sln` (Pre-Commit-Hook prüft Format).

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `StartImportAsync_ShouldPassUriToHandler` | `ImportOrchestratorTests` | `RecordingHandler` zeichnet den `uri`-Parameter auf; Assertion, dass `uri` bis `HandleAsync` durchgereicht wird. |
| `ImportAndWaitAsync_ShouldReturnResult_WhenHandlerSucceeds` | `ImportOrchestratorTests` | Synchroner Einstieg liefert das persistierte `ImportResult` ohne manuelles Polling. |
| `ImportAndWaitAsync_ShouldReturnError_WhenNoPluginCanHandle` | `ImportOrchestratorTests` | Gleiche Fehlermeldung wie Session-Pfad („No suitable import plugin found for this file or URL."). |
| `ImportAndWaitAsync_ShouldAbortAndReturnInteractionError_WhenConfirmationRequired` | `ImportOrchestratorTests` | Interaktiver Handler → Result `Success == false`, `Error` enthält `import-session`; Session erreicht Terminal-Fehlerzustand (`Result != null`). |
| `ImportAndWaitAsync_ShouldAbortAndReturnInteractionError_WhenSelectionRequired` | `ImportOrchestratorTests` | Collection-Preview → Result `Success == false` mit `import-session`-Hinweis; Session erreicht Terminal-Fehlerzustand; keine Items importiert. |
| `ImportAndWaitAsync_ShouldHonorCancellation` | `ImportOrchestratorTests` | Bereits gelöschter Token → `OperationCanceledException`. |
| `RecordingHandler.ReceivedUri` | `ImportOrchestratorTests` (Hilfsklasse) | Aufzeichnung des `uri`-Parameters für die Durchreich-Assertion. |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `ImportServicePluginTests` (komplette Datei) | Entfällt — testet den entfernten `ImportService` direkt; inhaltliche Abdeckung (Reihenfolge, Kein-Handler-Fehler) liegt in `ImportOrchestratorTests` vor bzw. wird ergänzt. |

### E2E-Tests (primärer Funktionsnachweis)

Keine neuen E2E-Tests erforderlich — Begründung: Die Anforderung ändert keinen über die UI erreichbaren Benutzerfluss. Die `CookbookPage` ruft dieselben Endpunkte mit demselben Request-/Response-Vertrag auf; Fehlermeldungen werden wie bisher über das vorhandene JS-Alert angezeigt. Es entstehen keine neuen UI-Elemente, Navigationen oder Interaktionen. Es existiert heute kein Browser-E2E-Test für die Import-Endpunkte (geprüft: `Rezepte.Tests.Browser` enthält nur Plugin-Settings-bezogene Import-Fixtures). Der Funktionsnachweis des konsolidierten Ausführungspfads liegt in den `ImportOrchestratorTests` (Handler-Reihenfolge, Interaktion, Collection, `uri`-Durchreichung). Die reale End-to-End-Abnahme (gestartete Anwendung + HTTP-Aufrufe mit Auth) wird in `test-results.md` separat ausgewiesen.

## Akzeptanzszenarien

| # | Szenario | Auslöser | Ausgangszustand | Schritte | Sichtbare Wirkung | Nachweis |
|---|----------|----------|------------------|----------|-------------------|----------|
| 1 | Happy Path Datei-Import mit Kochbuch | `POST api/cookbooks/{id}/import` (z. B. aus `CookbookPage`) | Benutzer + Kochbuch vorhanden; aktives Plugin kann Datei verarbeiten | Multipart-Upload → Session intern gestartet → Handler `HandleAsync` mit `uri = null` → `PersistAsync` | `200 { created: [...] }`; Rezepte persistiert wie über Session-API | `ImportAndWaitAsync_ShouldReturnResult_WhenHandlerSucceeds` + bestehender `StartImportAsync_ShouldUseActivePluginsInConfiguredOrder` |
| 2 | URL-Import setzt `SourceUri`, Re-Import dedupliziert | `POST api/cookbooks/{id}/import-url` | Benutzer + Kochbuch; URL passiert `RemoteUrlGuard`; Handler liefert `ImportedRecipe` mit `SourceUri` | URL validieren → fetchen → `ImportAndWaitAsync` mit `uri` → Handler `SourceUri = uri` → `PersistRecipeAsync` mit `FindByUri` | Erster Import: `Ok { created }`; zweiter Import derselben URL: Update des bestehenden Rezepts statt zweitem Eintrag | `StartImportAsync_ShouldPassUriToHandler` + Persister-Dedup (bestehendes Verhalten, `ImportedRecipePersister` unverändert) |
| 3 | Interaktionsbedarf → Abbruch + Hinweis | `POST api/cookbooks/{id}/import(-url)` mit Collection-Quelle oder interaktivem Handler | Import löst `SelectionRequired` bzw. `WaitingForConfirmation` aus; keine `import-session`-Interaktion möglich | Session startet → Interaktionssignal → `AbortInteractiveSession` → `ImportResult(false, Hinweis)` | `400 { message: "...import-session/*..." }`; Session in Terminal-Fehlerzustand; keine Teilanlage (Collection) | `ImportAndWaitAsync_ShouldAbortAndReturnInteractionError_WhenConfirmationRequired`, `..._WhenSelectionRequired` |
| 4 | Kein passendes Plugin | `POST .../import` mit unbekanntem Dateityp | Kein Handler kann die Datei | Session startet → kein `CanHandleAsync` trifft zu → Fehlerresult | `400 { message: "No suitable import plugin found for this file or URL." }` — identisch zum Session-Pfad | `ImportAndWaitAsync_ShouldReturnError_WhenNoPluginCanHandle` |
| 5 | Request-Abbruch | Client bricht HTTP-Request ab (Token-Cancellation) | Import läuft noch | `WaitAsync(ct)` wirft `OperationCanceledException` | `499` (bisheriges Verhalten) | `ImportAndWaitAsync_ShouldHonorCancellation` |

## Offene Punkte

Keine.
