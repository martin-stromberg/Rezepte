# Bestandsaufnahme — Issue #177: Konsolidierung Import-Pfade + Demo-Seeding-Kapselung

Stand: Branch `task/issue-177-280a0a2130b7488bbf6a95f3291b7e6f-code-review-orchestrierung-meh`, HEAD `0fb3524`. Arbeitsverzeichnis sauber bis auf die neuen Projekt-Docs unter `Docs/projects/` (untracked).

Anforderung: `requirement.md` (FA-1: ein einziger Import-Ausführungspfad über `ImportOrchestrator`; FA-2: Service-Einstiegspunkt `IDemoDataSeeder.SeedForUserAsync`).

## Detaildokumente

- [inventory/import-pfade.md](inventory/import-pfade.md) — beide Import-Ausführungspfade, Controller-Endpunkte, UI-Aufrufer, Plugin-Vertrag, Persistierung/Dedup, DI-Registrierungen.
- [inventory/demo-seeding.md](inventory/demo-seeding.md) — `DemoDataSeedingJobHandler` vollständig, Job-Infrastruktur, Auslöser bei Registrierung, `DemoDataSource`, Domänen-Service-Signaturen.
- [inventory/tests-und-build.md](inventory/tests-und-build.md) — betroffene Testdateien mit Inhalt und Anpassungsbedarf, Test-Frameworks, Build-/Test-Kommandos, Doku-Konventionen.

## Kernaussagen in Kürze

- Zwei produktive Import-Pfade existieren faktisch: synchroner `IImportService.ImportAsync` (`Rezepte.Web/Services/Import/ImportService.cs:33-79`, `uri` hart `null` in Zeile 66) vs. Session-basierter `ImportOrchestrator.StartImportAsync` (`ImportOrchestrator.cs:104-278`, `uri` durchgereicht). Einzige produktive Aufrufer von `IImportService` sind die vier Legacy-Endpunkte in `CookbooksController.cs` (Zeilen 166, 206, 230, 265) plus privater Helfer `ImportFromUrlAsync` (Zeile 575-602).
- Der Orchestrator hat **keine Warte-Mechanik auf Session-Ende**: `session.Result` wird im Hintergrund-`Task.Run` gesetzt; Aufrufer pollen (UI alle 800 ms; Tests per `WaitUntilAsync`-Schleife). Ein synchrones Abwarten für die Legacy-Endpunkte müsste neu gebaut werden (Polling auf `session.Result` oder neuer TCS). Bei `SelectionRequired`/interaktiver Bestätigung würde synchrones Warten blockieren — offene Frage der Anforderung.
- `DemoDataSeedingJobHandler` komponiert die fachliche Sequenz Kochbücher → Rezepte+Bilder → Kalender → Einkaufsliste komplett selbst (`HandleAsync`, Zeilen 44-78 + private Methoden bis 317). Alle dafür nötigen Domänen-Services sind scoped registriert und werden über `scopeServices` aufgelöst — ein neuer `IDemoDataSeeder`-Service kann dieselben Abhängigkeiten scoped injizieren; der Handler würde nur noch Payload lesen, User prüfen und `SeedForUserAsync` aufrufen.
- Betroffene Tests: `ImportServicePluginTests` (testet `ImportService` direkt — entfällt oder wandelt), `ImportOrchestratorTests` (12 Tests, bleiben weitgehend bestehen), `DemoDataSeedingJobHandlerTests` (6 Tests bauen die Sequenz nach — müssen auf den neuen Einstiegspunkt umziehen), `UserServiceTests` (Job-Enqueue-Verifikation, unverändert), Browser-`RegisterTests` (fachliches Ergebnis unverändert, keine Anpassung nötig).
