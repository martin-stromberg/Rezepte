# Detail: Tests und Test-Ausgangszustand

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-11-15 ~16:45 (lokale Zeit, UTC+1) — Baseline-Lauf zu Beginn der Bestandsaufnahme
- Branch und Commit-ID: `task/issue-177-280a0a2130b7488bbf6a95f3291b7e6f-code-review-orchestrierung-meh-schritt-2-demo-data-seeder` @ `9780812` (chore: Projekt-Tracking - Schritt 1 abgenommen und gemergt, Schritt 2 in Arbeit)
- Uncommittete Änderungen im getesteten Stand: nur Lifecycle-Artefakte unter `Docs/features/task/…-schritt-2-demo-data-seeder/` (keine Codeänderungen)
- Testumgebung und Runtime-/SDK-Versionen: Windows, .NET SDK `10.0.403`, xUnit + Moq + FluentAssertions
- Ermittelte Testsuiten und Quellen der Testbefehle: `Rezepte.Tests` (xUnit-Unit-/Integrationstests), `Rezepte.Tests.Browser` (Playwright-E2E, separater Lauf)

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Baseline | `dotnet test Rezepte.Tests/Rezepte.Tests.csproj --nologo` | Repo-Root | 0 | 707 | 0 | 0 | [baseline-rezepte-tests.log](test-results/baseline-rezepte-tests.log) |

### Nachgewiesene bestehende Testfehler

Keine — der Baseline-Lauf war vollständig grün (707/707).

### Testlücken und Ausführungsprobleme

- `Rezepte.Tests.Browser` wurde in der Baseline nicht ausgeführt (Playwright-E2E); der Suite ist in dieser Umgebung lauffähig. `RegisterTests.Register_WithDemoData_CreatesDemoData` ist für diesen Schritt der E2E-Nachweis und wird nach der Umsetzung ausgeführt.
- Bekannte Flakiness (aus Projekt-Kontext): Der genannte Browser-Test kann nach bestandenem `WaitForDemoData` beim späteren Kalender-Einzelread 0 finden — bei Fehlschlag einmal wiederholen und Ergebnis dokumentieren.

## Testklassen

### `DemoDataSeedingJobHandlerTests`

Datei: `Rezepte.Tests/Services/BackgroundJobs/DemoDataSeedingJobHandlerTests.cs` (249 Zeilen), Namespace `Rezepte.Tests.Services.BackgroundJobs`.

Baut die **komplette Sequenz-Mock-Landschaft** nach (`HappyPathMocks`-Record mit `IUserService`, `ICookbookService`, `IRecipeService`, `ICalendarService`, `IShoppingListService` + `ServiceProvider` via `ServiceCollection`):

- `HandleAsync_ShouldCreateFiveCookbooks` — verifiziert 5× `ICookbookService.CreateAsync`
- `HandleAsync_ShouldCreateFortyThreeRecipes` — verifiziert 43× `IRecipeService.CreateAsync` (mit `uri == null`)
- `HandleAsync_ShouldCreateFiveCalendarEvents` — verifiziert 5× `ICalendarService.CreateEventAsync` (Tage `Today+1..5`, `TimeSpan.FromHours(18)`, `RecurrenceType.None`, `WeekDays.None`)
- `HandleAsync_ShouldAddIngredientsToDefaultShoppingList` — verifiziert 6× `IShoppingListService.AddItemAsync` in Gruppe `"1"`
- `HandleAsync_ShouldThrow_WhenUserDoesNotExist` — `InvalidOperationException` bei fehlendem Benutzer
- `HandleAsync_ShouldThrow_WhenAnyServiceOperationFails` — `InvalidOperationException` bei fehlschlagendem `CreateAsync` (2. Kochbuch)

Hilfsmethoden: `BuildProvider(...)` (ServiceCollection mit Mocks + `ILogger<DemoDataSeedingJobHandler>`), `CreateJob(userId)` (Job mit `DemoDataSeedPayload(userId).ToJson()`), `CreateHappyPathMocks(userId)`.

### `UserServiceTests` (Registrierungs-/Enqueue-Tests — unverändert)

Datei: `Rezepte.Tests/Services/UserServiceTests.cs`

- `RegisterAsync_ShouldEnqueueDemoDataJob_WhenCreateDemoDataIsTrue` (~Zeile 309) — verifiziert `EnqueueAsync("seed-demo-data", payload enthält user.Id, user.Id, ct)`, Times.Once
- `RegisterAsync_ShouldNotEnqueueDemoDataJob_WhenCreateDemoDataIsFalse` (~Zeile 332) — verifiziert Times.Never

### `RegisterTests` (Playwright-E2E — unverändert)

Datei: `Rezepte.Tests.Browser/Auth/RegisterTests.cs`

- `Register_WithDemoData_CreatesDemoData` (Zeile 50) — registriert über die UI mit Demo-Daten, wartet via `DemoDataWaitHelper.WaitForDemoDataAsync` (Zeile 269) auf das Seeding-Ergebnis und prüft anschließend Kochbücher/Kalender/Einkaufsliste in der UI. E2E-Nachweis der fachlich unveränderten Demo-Daten.
- `WaitForDemoDataAsync` prüft direkt gegen die SQLite-Datenbank (`databasePath`), nicht über UI-Polling.

## Hilfsmethoden

- `HappyPathMocks`-Record + `CreateHappyPathMocks`/`BuildProvider`/`CreateJob` in `DemoDataSeedingJobHandlerTests` — Mock-Landschaft, die beim Umbau großteils in die neuen `DemoDataSeeder`-Tests umzieht (ohne `IUserService`/`ServiceProvider`, da der Seeder die Services per Konstruktor injiziert bekommt).
