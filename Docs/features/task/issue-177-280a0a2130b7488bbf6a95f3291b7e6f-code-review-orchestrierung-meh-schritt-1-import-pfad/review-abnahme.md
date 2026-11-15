# Abnahme-Review (fachlich, unabhängig)

## Ergebnis

**Status:** Abnahme empfohlen — alle Prüfpunkte erfüllt; Beobachtungen ohne Nacharbeitszwang.

Geprüft gegen `requirement.md` anhand des Ist-Stands (HEAD e2fa179) und der dokumentierten Baseline `961e58b` (`inventory/import-pfade.md`).

## Urteile je Prüfpunkt

| # | Prüfpunkt | Urteil |
|---|-----------|--------|
| 1 | Genau ein Ausführungspfad über `ImportOrchestrator` | Erfüllt |
| 2 | Externe API-Signatur & Response-Verhalten der 4 synchronen Endpunkte | Erfüllt |
| 3 | `uri`-Durchreichung bis Handler → `SourceUri` → `FindByUri`-Dedup | Erfüllt |
| 4 | Kein endloses Blockieren bei Interaktionsbedarf; Abbruch + `import-session/*`-Hinweis | Erfüllt (Beobachtung: kein eigenes Timeout bei hängendem Handler) |
| 5 | Collection-Preview/Multi-Import konsistent; Session-Endpunkte unverändert | Erfüllt |
| 6 | DI-Registrierung bereinigt, keine toten Referenzen | Erfüllt |
| 7 | Aussagekraft der Tests | Erfüllt (Beobachtung: bekannte, dokumentierte Lücken) |
| 8 | Englischer Code, Stil, Kommentare | Erfüllt |

## Details

### 1. Einziger Ausführungspfad — erfüllt

- `IImportService.cs`, `ImportService.cs`, `ImportServicePluginTests.cs` entfernt; Volltextsuche nach `IImportService|ImportService` findet im Code nur `ITestRecipeImportService`/`TestRecipeImportService` (`Rezepte.Web/Services/Import/TestRecipeImportService.cs` — liest Test-URLs aus JSON, kein Ausführungspfad) und Doku-Referenzen.
- Einzige produktive Handler-Ausführung: `ImportOrchestrator.StartImportAsync` (`Rezepte.Web/Services/Import/ImportOrchestrator.cs:144-290`); `AcquireActiveHandlersAsync` (151), `TryReadCollectionPreviewAsync` (170), `HandleInteractiveAsync` (216), `HandleAsync` (230), `IImportedRecipePersister` (179, 217, 231) nur dort.
- `DemoDataSeedingJobHandler` erzeugt Rezepte direkt über `IRecipeService` — kein Import-Pfad, expliziter Nicht-Ziel-Bereich (Schritt 2).

### 2. API-Vertrag der synchronen Endpunkte — erfüllt

- `POST {cookbookId}/import` (`CookbooksController.cs:165-195`), `POST {cookbookId}/import-url` (205-217), `POST import` (227-255 inkl. 500-MB-Limits), `POST import-url` (264-272) sowie Helfer `ImportFromUrlAsync` (575-602): identische Validierungs-Reihenfolge und Statuscodes (`Ok{created}`, `BadRequest{message}`, `NotFound`, `499`, `Problem 500`).
- Abweichung nur intern: `[FromServices] ImportOrchestrator` statt `IImportService`; Fehlertext-Inhalte können abweichen (`ImportExceptionHelper.BeautifyExceptionMessage` statt `ex.Message`; neue Interaktions-Fehlermeldung) — Format unverändert; gewünschte Verhaltensänderung: interaktionsbedürftige Inhalte → 400 statt Import.

### 3. URI-Durchreichung — erfüllt

- `CookbooksController.cs:588`: `orchestrator.ImportAndWaitAsync(ms, fetched.FileName, uri.ToString(), ...)` — die zuvor verworfene validierte URI wird durchgereicht.
- Kette verifiziert: `HandleAsync`/`HandleInteractiveAsync`/`TryReadCollectionPreviewAsync` erhalten `uri` → `BaseAIImportHandler.cs:293` setzt `SourceUri = uri` → `ImportedRecipePersister.PersistRecipeAsync` (`:63-65`) dedupliziert per `FindByUri` (Update statt Doppelanlage).
- Datei-Imports übergeben `null` — konsistente Semantik mit `import-session/start-file`.

### 4. Interaktionsabbruch — erfüllt

- `ImportSession.CompletionSource`/`InteractionRequiredSource` (`ImportOrchestrator.cs:41-42`, `RunContinuationsAsynchronously`); `Result`-Setter signalisiert zentral (82-95).
- `ImportAndWaitAsync` (308-327): `Task.WhenAny` + `WaitAsync(ct)` → Result-First-Check → `AbortInteractiveSession` (333-341: `SyncRoot`-Lock, `ReadOnly`, beide TCS `TrySetCanceled`) → Warten auf Terminalzustand (325) → `ImportResult(false, InteractionRequiredMessage)` (330-331, englisch, `import-session/*`-Hinweis).
- Signale korrekt nach TCS-Anlage: `HandleCollectionImportAsync` (488→491), `AskForConfirmationAsync` (672→673). Abbruch landet in terminalen Zuständen (Selection: `Cancelled`/Failed; Confirmation: `Failed` via Exception-Fang).
- Beobachtung: Kein eigenes Timeout — ein Handler, der die TCS-Cancellation ignoriert, blockiert bis zum Request-Abbruch (`ct`). Im Plan dokumentiert; entspricht dem bisherigen Risiko (Handler wurde vorher direkt awaited).

### 5. Konsistenz / Session-Endpunkte — erfüllt

- `import-session/*`-Endpunkte (`CookbooksController.cs:304-617`) unverändert orchestrator-basiert; `uri.ToString()` in `StartImportSessionFromUrlAsync` (616) war bereits vorhanden.
- Collection-Import über synchrone Endpunkte: `SelectionRequired` → Abbruch ohne Teilanlage (Test: `ImportedItemIds` leer). Mehrfach-Import-Verhalten unverändert (nur Session-Pfad).

### 6. DI — erfüllt

- `ServiceCollectionExtensions.cs`: `AddScoped<IImportService, ImportService>` entfernt; `IImportedRecipePersister` (202) und `AddSingleton<ImportOrchestrator>` (229) vorhanden. Keine toten Interfaces/Referenzen.

### 7. Tests — erfüllt mit Beobachtungen

- `ImportOrchestratorTests.cs`: `StartImportAsync_ShouldPassUriToHandler` (328-340, Kette `uri`→Handler→`SourceUri`→Persister via `ReceivedUri`/`ReceivedSourceUris`), `ImportAndWaitAsync_*` (346-415): Happy Path, identische „No suitable import plugin"-Meldung, Abbruch bei Confirmation und Selection (inkl. `import-session`-Assertion, keine Teilanlage), Cancellation → `OperationCanceledException`.
- Keine nachgebauten mehrstufigen Sequenzen; produktive Einstiegspunkte getestet. 703 − 2 + 6 = 707 nachvollziehbar; Laufprotokoll `inventory/test-results/final-dotnet-test.txt` vorhanden.
- Lücken (dokumentiert/pre-existing): `FindByUri`-Dedup nicht direkt unit-getestet (Kette bis Persister-Grenze belegt); `CookbooksController` 0 % Abdeckung (projektweit keine Controller-Tests); Terminal-Zustand der Session nach Abbruch implizit durch interne Warte garantiert, nicht explizit assertiert; Race „Result vs. Interaktionssignal" nicht dediziert getestet.

### 8. Sprache/Stil — erfüllt

- Neuer Code durchgängig englisch (Konvention: XML-Doku, `InteractionRequiredMessage` englisch). Kommentare sparsam und sachlich (z. B. 323-324 Terminal-Warte). Deutsch verbliebene Strings/Kommentare sind pre-existing (Session-Status-Texte, Controller-Kommentare).

## Weitere Beobachtungen

1. **UX-Konsequenz (bestätigter Scope-Ausschluss):** `BaseAIImportHandler` (AIFoto `Rezepte.Import.Plugins.AIFoto`, AIUrl `Rezepte.Import.Plugins.AIUrl`) fragt via `HandleConfirmation` (`BaseAIImportHandler.cs:342-359`, Overrides `AIUrlImportHandler.cs:67-68`, `AIFotoImportHandler.cs:114-115`) **immer** um Bestätigung. KI-Importe über die synchronen Endpunkte — inkl. `CookbookPage.razor:286,322` — werden daher künftig generell mit 400 + `import-session/*`-Hinweis abgelehnt statt wie bisher still über `HandleAsync` importiert. Auf der CookbookPage existiert kein Session-UI; Alternative ist der Rezept-Anlegen-Dialog. Fachlich bestätigt, ggf. Follow-up für nutzerfreundlichere Fehlerdarstellung.
2. **Session-Aufkommen:** Jeder synchrone Import legt nun eine terminale Session in `_sessions` ab (kein Eviction, pre-existing) — neu für diesen Einstieg, gebundenes Wachstum pro Import.
3. **Endzustands-Benennung:** Abbruch bei Confirmation → `Failed` (Exception-Pfad), bei Selection → `Cancelled` — terminale Zustände, kosmetisch inkonsistent.
4. **E2E:** Reale End-to-End-Abnahme des Legacy-Endpunkt-Verhaltens ist in dieser Umgebung nicht sinnvoll durchführbar (URL-Dedup lokal wegen `RemoteUrlGuard` nicht auslösbar; kein import-spezifischer Browser-Test vorhanden). Unit-/Orchestrator-Ebene ist vollständig belegt; dokumentierter Verzicht bzw. manuelle Abnahme obliegt dem Anwender. Hinweis: Die Browser-Test-Infrastruktur selbst (Playwright) ist in dieser Umgebung lauffähig — verifiziert am `RegisterTests`-Lauf.
5. **Flaky Browser-Test** `Register_WithDemoData_CreatesDemoData`: Diff berührt weder Seeding noch Kalender — kein Schritt-1-Problem (vor der finalen Einzelabfrage hatte `WaitForDemoDataAsync` Kalender ≥5 beobachtet; UI-Flakiness beim Nachlesen).

## Nacharbeitspunkte

Keine zwingenden. Optional:

- Explizite Session-Terminal-Assertion in `ImportAndWaitAsync_ShouldAbortAndReturnInteractionError_*` (derzeit implizit durch interne Warte garantiert).
- Follow-up: Fehlermeldung/UX für KI-Importe auf der `CookbookPage` (Produktentscheidung).

## Gesamturteil

**Abnahme empfohlen.** Die Konsolidierung ist vollständig und korrekt umgesetzt: ein einziger Ausführungspfad, `uri`-Durchreichung mit Dedup, sauberer Interaktionsabbruch mit klarem `import-session/*`-Hinweis, unveränderter externer API-Vertrag und bereinigte DI. Verbleibende Punkte sind dokumentierte, bewusst akzeptierte Trade-offs bzw. pre-existing Testlücken.
