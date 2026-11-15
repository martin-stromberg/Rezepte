# Tasks: Ein einziger Ausführungspfad für „Rezept importieren"

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Orchestrator | `ImportSession` um interne `CompletionSource`/`InteractionRequiredSource` (TCS) erweitern; `Result`-Setter signalisiert Completion | Erledigt | `ImportOrchestratorTests.ImportAndWaitAsync_*` (Warten funktioniert ohne Polling) |
| 2 | Orchestrator | `ImportAndWaitAsync` (öffentlich, XML-Doku) + `AbortInteractiveSession` + `InteractionRequiredMessage` implementieren | Erledigt | `ImportAndWaitAsync_ShouldReturnResult_WhenHandlerSucceeds`, `..._WhenConfirmationRequired`, `..._WhenSelectionRequired`, `..._ShouldHonorCancellation` |
| 3 | Orchestrator | Interaktionssignale in `HandleCollectionImportAsync` (`SelectionRequired`) und `SessionInteraction.AskForConfirmationAsync` setzen | Erledigt | `ImportAndWaitAsync_ShouldAbortAndReturnInteractionError_WhenSelectionRequired`/`_WhenConfirmationRequired` |
| 4 | Controller | `ImportFromFile` (mit/ohne Kochbuch) auf `[FromServices] ImportOrchestrator` + `ImportAndWaitAsync` umstellen | Erledigt | Build; Verhalten durch `ImportAndWaitAsync`-Tests abgesichert |
| 5 | Controller | `ImportFromUrl` (mit/ohne Kochbuch) + `ImportFromUrlAsync`-Helfer umstellen; `uri.ToString()` durchreichen | Erledigt | `StartImportAsync_ShouldPassUriToHandler`, `ImportAndWaitAsync_ShouldReturnResult_WhenHandlerSucceeds` |
| 6 | Cleanup | `IImportService.cs`, `ImportService.cs` und `AddScoped<IImportService, ImportService>` entfernen | Erledigt | Build (keine Restreferenzen, Volltextsuche leer) |
| 7 | Tests | `ImportServicePluginTests.cs` löschen | Erledigt | Testsuite grün (707/707) |
| 8 | Tests | `RecordingHandler.ReceivedUri` + `StartImportAsync_ShouldPassUriToHandler` in `ImportOrchestratorTests` ergänzen | Erledigt | `StartImportAsync_ShouldPassUriToHandler` |
| 9 | Tests | `ImportAndWaitAsync`-Tests (Erfolg, kein Plugin, Bestätigung erforderlich, Auswahl erforderlich, Cancellation) in `ImportOrchestratorTests` ergänzen | Erledigt | 5 neue Tests, alle grün |
| 10 | Abschluss | `dotnet build Rezepte.sln`, `dotnet test Rezepte.Tests`, ggf. `dotnet format` | Erledigt | Build 0 Fehler, 707/707 Tests, `dotnet format --verify-no-changes` Exit 0 |
| 11 | Doku | `Docs/help/import-plugins.md` (Importablauf) an konsolidierten Pfad anpassen | Erledigt | Abschnitt „Importablauf" beschreibt einheitlichen Pfad, `SourceUri`-Dedup und Interaktions-Ablehnung; `changes.log`-Eintrag angelegt |
