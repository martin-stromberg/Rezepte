# Test-Ergebnisse

## Ergebnis

**Status:** Keine Fehler

## E2E-Abdeckung

Geplante E2E-Szenarien aus `plan.md` (Abschnitt „E2E-Tests", beide Priorität „Pflicht"):

| Szenario | Test / Testklasse | Ergebnis |
|----------|-------------------|----------|
| Registrierung mit aktivierter Demo-Daten-Checkbox → 5 Kochbücher, 43 Rezepte, 5 Kalendereinträge, Einkaufslisteneinträge sichtbar | `Rezepte.Tests.Browser.RegisterTests.Register_WithDemoData_CreatesDemoData` | Bestanden |
| Registrierung ohne Demo-Daten-Checkbox → keine Demo-Daten sichtbar | `Rezepte.Tests.Browser.RegisterTests.Register_WithoutDemoData_LeavesListsEmpty` | Bestanden |

## Zusammenfassung

Ausgeführte Befehle:

- `dotnet build Rezepte.sln` — erfolgreich, 0 Warnungen, 0 Fehler
- `dotnet test Rezepte.Tests --no-build` — 590 Tests, alle bestanden (Dauer ~40 s)
- `dotnet test Rezepte.Tests.Browser --no-build` — 15 Tests, alle bestanden (Dauer ~18 s)

| Testprojekt | Gesamt | Bestanden | Fehlgeschlagen | Übersprungen |
|-------------|--------|-----------|----------------|--------------|
| Rezepte.Tests | 590 | 590 | 0 | 0 |
| Rezepte.Tests.Browser | 15 | 15 | 0 | 0 |
| **Gesamt** | **605** | **605** | **0** | **0** |

## Testabdeckung

**Abdeckung:** 66,0 % Zeilenabdeckung (gemessen nur für `Rezepte.Tests` via `--collect:"XPlat Code Coverage"`; `Rezepte.Tests.Browser` wurde ohne Coverage ausgeführt, daher sind UI-/Razor-Anteile in dieser Messung nicht abgedeckt).

Coverage-Datei: `Rezepte.Tests/TestResults/cb1e78fa-cbe0-48a1-a90f-9c8674f202df/coverage.cobertura.xml`

Feature-relevante Dateien (unter 80 %):

| Datei | Abdeckung |
|-------|-----------|
| `Rezepte.Web/Services/BackgroundJobs/Handlers/DemoDataSeedingJobHandler.cs` | 50,0–100,0 % je Klasse (Kernpfad ≥ 82 %) |
| `Rezepte.Web/Services/BackgroundJobs/DemoDataSeedPayload.cs` | 72,7 % |
| `Rezepte.Web/Services/DemoDataSource.cs` | 100,0 % |
| `Rezepte.Web/Services/UserService.cs` | 65,2–100,0 % je Klasse |
| `Rezepte.Web/Controllers/AuthController.cs` | 0–100 % je Klasse (teilweise nur via Browser-Tests abgedeckt) |
| `Rezepte.Web/Components/Pages/Register.razor` | 0 % (nur via Browser-Tests abgedeckt, nicht in dieser Messung) |
| `Rezepte.Web/Middleware/RedirectToRegisterMiddleware.cs` | 29,5–100 % je Klasse |

## Fehlende Tests

Quelle: `Coverage-Daten`

Insgesamt 518 Klassen weisen in der Unit-Test-Messung 0 % Zeilenabdeckung auf. Davon betroffen sind überwiegend UI-Schichten, die ausschließlich über `Rezepte.Tests.Browser` (Playwright) getestet werden und daher in dieser Messung nicht erfasst sind:

- Alle Razor-Komponenten unter `Rezepte.Web/Components/**` (u. a. `Register.razor`, `Calendar.razor`, `Cookbooks.razor`, `RecipeSearch.razor`, `ShoppingList.razor`, alle Settings- und Shared-Dialoge) — 0 % Abdeckung, keine korrespondierenden Unit-Testdateien
- Die meisten Controller unter `Rezepte.Web/Controllers/**` (u. a. `AuthController`, `JobsController`, `RecipesController`, `CalendarController`, `CookbooksController`, `SettingsController`, `UsersController`) — 0 % in der Unit-Test-Messung; Abdeckung erfolgt indirekt über Browser-Tests
- `Rezepte.Import.Plugins.AIFoto/AIFotoImportHandler.cs`, `Rezepte.Import.Plugins.AIUrl/AIUrlImportHandler.cs` — 0 % Abdeckung, keine Unit-Tests
- Diverse DTOs/Records und Middleware ohne eigene Testdatei

Hinweis: Die für dieses Feature neuen Komponenten (`DemoDataSource`, `DemoDataSeedingJobHandler`, `DemoDataSeedPayload`, `UserService.RegisterAsync`, `AuthController.Register`) sind durch Unit-Tests und die beiden Pflicht-E2E-Tests abgedeckt.
