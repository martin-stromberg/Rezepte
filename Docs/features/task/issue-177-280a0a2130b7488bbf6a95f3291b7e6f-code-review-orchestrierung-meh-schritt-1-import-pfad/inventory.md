# Bestandsaufnahme — Schritt 1: Ein einziger Ausführungspfad für „Rezept importieren"

Stand: Branch `task/issue-177-280a0a2130b7488bbf6a95f3291b7e6f-code-review-orchestrierung-meh-schritt-1-import-pfad`, HEAD `961e58b`. Arbeitsverzeichnis sauber bis auf die neuen Feature-Docs unter `docs/features/` (untracked).

Anforderung: `requirement.md` — Konsolidierung der beiden produktiven Import-Ausführungspfade auf den `ImportOrchestrator` als einzigen Einstiegspunkt; synchrone Endpunkte starten intern eine Session und warten auf deren Endzustand.

## Detaildokumente

- [inventory/import-pfade.md](inventory/import-pfade.md) — beide Ausführungspfade, alle `IImportService`-Fundstellen, Controller-Endpunkte, Session-Wartemechanik, DI-Registrierungen, UI-Aufrufer.
- [inventory/tests.md](inventory/tests.md) — Test-Ausgangszustand (703/703 grün), betroffene Testdateien mit Anpassungsbedarf, Build-/Test-Kommandos, Konventionen.

## Zusammenfassung

- Zwei produktive Ausführungspfade existieren faktisch: synchroner `IImportService.ImportAsync` (`Rezepte.Web/Services/Import/ImportService.cs:33-79`, `uri` hart `null` in Zeile 66) vs. Session-basierter `ImportOrchestrator.StartImportAsync` (`ImportOrchestrator.cs:104-278`, `uri` durchgereicht bis `HandleAsync`/`HandleInteractiveAsync`/`TryReadCollectionPreviewAsync`).
- Vollständige Fundstellen von `IImportService` im Produktivcode (verifiziert per Volltextsuche `*.cs`): `ImportService.cs`, `IImportService.cs`, `ServiceCollectionExtensions.cs:201` (`AddScoped`), `CookbooksController.cs` Zeilen 166, 206, 230, 265, 575. Keine weiteren Referenzen (auch nicht in `Program.cs`, Razor-Dateien oder anderen Services).
- Der Orchestrator hat **keinen Completion-Wartemechanismus**: `session.Result` wird im Hintergrund-`Task.Run` gesetzt; Konsumenten pollen (UI 800 ms, Tests `WaitUntilAsync` 25 ms). Für synchrones Abwarten muss ein Mechanismus ergänzt werden. Bei `SelectionRequired`/`WaitingForConfirmation` würde synchrones Warten unbegrenzt blockieren — die Anforderung schreibt dafür Session-Abbruch plus Fehlermeldung vor.
- Session-Abbruch für `SelectionRequired` existiert bereits: `CancelSelection` setzt `SelectionTcs.TrySetCanceled` und `session.Result = ImportResult(false, "Import cancelled.")`. Für `WaitingForConfirmation` gibt es kein Abbruch-API (nur `Confirm(sessionId, userId, accepted)`; `accepted: false` ließe den Handler-Import weiterlaufen und wäre kein Abbruch).
- Die synchronen Endpunkte geben heute `Ok { created }` bei Erfolg, `BadRequest { message }` bei `Success == false`, `NotFound` bei fehlendem Kochbuch, `StatusCode(499)` bei `OperationCanceledException`, `Problem 500` bei sonstigen Exceptions — dieses Antwortverhalten muss erhalten bleiben.
- Betroffene Tests: `ImportServicePluginTests` (testet `ImportService` direkt — entfällt oder zieht um), `ImportOrchestratorTests` (12 Tests, Referenz; `uri`-Durchreichung heute nicht explizit assertiert). Keine Controller-Tests für die Import-Endpunkte vorhanden.
- Test-Ausgangszustand: **703/703 Tests bestanden** (`dotnet test Rezepte.Tests/Rezepte.Tests.csproj`), Nachweis in [inventory/tests.md](inventory/tests.md). `Rezepte.Tests.Browser` baut, ist aber in dieser Umgebung nicht ausführbar (Playwright-Browser nicht installiert).
