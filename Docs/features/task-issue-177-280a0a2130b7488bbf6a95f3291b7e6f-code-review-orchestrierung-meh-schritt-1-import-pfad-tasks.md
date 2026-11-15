# Tasks: Ein einziger Ausführungspfad für „Rezept importieren"

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Orchestrator | `ImportSession` um interne `CompletionSource`/`InteractionRequiredSource` (TCS) erweitern; `Result`-Setter signalisiert Completion | Offen | — |
| 2 | Orchestrator | `ImportAndWaitAsync` (öffentlich, XML-Doku) + `AbortInteractiveSession` + `InteractionRequiredMessage` implementieren | Offen | — |
| 3 | Orchestrator | Interaktionssignale in `HandleCollectionImportAsync` (`SelectionRequired`) und `SessionInteraction.AskForConfirmationAsync` setzen | Offen | — |
| 4 | Controller | `ImportFromFile` (mit/ohne Kochbuch) auf `[FromServices] ImportOrchestrator` + `ImportAndWaitAsync` umstellen | Offen | — |
| 5 | Controller | `ImportFromUrl` (mit/ohne Kochbuch) + `ImportFromUrlAsync`-Helfer umstellen; `uri.ToString()` durchreichen | Offen | — |
| 6 | Cleanup | `IImportService.cs`, `ImportService.cs` und `AddScoped<IImportService, ImportService>` entfernen | Offen | — |
| 7 | Tests | `ImportServicePluginTests.cs` löschen | Offen | — |
| 8 | Tests | `RecordingHandler.ReceivedUri` + `StartImportAsync_ShouldPassUriToHandler` in `ImportOrchestratorTests` ergänzen | Offen | — |
| 9 | Tests | `ImportAndWaitAsync`-Tests (Erfolg, kein Plugin, Bestätigung erforderlich, Auswahl erforderlich, Cancellation) in `ImportOrchestratorTests` ergänzen | Offen | — |
| 10 | Abschluss | `dotnet build Rezepte.sln`, `dotnet test Rezepte.Tests`, ggf. `dotnet format` | Offen | — |
| 11 | Doku | `Docs/help/import-plugins.md` (Importablauf) an konsolidierten Pfad anpassen | Offen | — |
