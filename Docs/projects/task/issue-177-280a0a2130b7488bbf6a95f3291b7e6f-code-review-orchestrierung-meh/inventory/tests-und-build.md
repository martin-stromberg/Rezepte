# Tests, Build & Konventionen — Bestandsaufnahme

## Testprojekte und Frameworks

- `Rezepte.Tests/Rezepte.Tests.csproj`: `net10.0`; Pakete: xUnit 2.9.3 + `xunit.runner.visualstudio` 3.1.5, FluentAssertions 8.10.0, Moq 4.20.72, bUnit 1.40.0, EFCore InMemory 10.0.11, `Microsoft.AspNetCore.Mvc.Testing` 10.0.9, coverlet. Projekt-Referenzen auf `Rezepte.Web`, alle Import-Plugin-Projekte, `Rezepte.Tests.PluginFixture`.
- `Rezepte.Tests.Browser/`: Playwright-E2E (eigene Fixtures: `RezepteAppFixture`, `PlaywrightBrowserFixture`, diverse Test-Collections; startet veröffentlichte App gegen eigene SQLite-DB).
- `Rezepte.Tests.PluginFixture/TestImportPlugin.cs` — Test-Plugin-Fixture.
- Konventionen: `GenerateDocumentationFile` + `WarningsAsErrors=CS1591` (fehlende XML-Doku öffentlicher Member = Build-Fehler), auch in Testprojekten; strukturell spiegelt `Rezepte.Tests/` die Ordner `Services/`, `Controllers/`, `Components/` der App.
- `Directory.Build.props`: `net10.0`, `Nullable enable`, `ImplicitUsings`, `ImportContractVersion 0.3.0`.

## Betroffene Testdateien

### `Rezepte.Tests/Services/Import/ImportServicePluginTests.cs` (105 Zeilen)

- Testet `ImportService` **direkt** (`CreateService` Zeilen 50-55: `FakePluginManager` + `PassthroughPersister` + `NullLogger`).
- 2 Tests: `ImportAsync_ShouldUseFirstMatchingPluginInConfiguredOrder` (20-34), `ImportAsync_ShouldReturnPluginError_WhenNoPluginCanHandleInput` (40-48).
- Hilfscode: `PassthroughPersister` (57-68), `FakePluginManager : IPluginManager` (70-84), `RecordingHandler` (86-104).
- **Anpassung:** entfällt bei Entfernen von `ImportService` oder muss auf den Orchestrator-Einstieg umziehen; inhaltlich bereits durch `ImportOrchestratorTests.StartImportAsync_ShouldUseActivePluginsInConfiguredOrder` abgedeckt.

### `Rezepte.Tests/Services/Import/ImportOrchestratorTests.cs` (510 Zeilen)

- 12 Tests (Zeilen 23-322): Plugin-Reihenfolge, `InitiatorUserId`/Session-Isolation, interaktive Bestätigung (`Confirm`), Collection-Selection/Item-Status/Cancel/Duplikat-IDs, Concurrent-Submit (nur 1 akzeptiert), Cancellation vor Start.
- Hilfscode: `CreateOrchestrator` (324-338: `ServiceCollection` mit Persister-Singleton, `IServiceScopeFactory`), `CancellingStream` (347-354), `PassthroughPersister` mit `TargetCookbookIds`-Liste (356-370), **`WaitUntilAsync`/`WaitForResultAsync` = Polling-Schleife 25 ms / Timeout 5 s** (372-395), `FakePluginManager` (397-411), `RecordingHandler`/`InteractiveRecordingHandler`/`ThrowingHandler`/`CollectionRecordingHandler` (413-509).
- **Anpassung:** bleibt der Referenztest des konsolidierten Pfads; ggf. erweitern um `uri`-Durchreich-Assertion (heute nicht explizit verifiziert, `HandleAsync`-Signatur enthält `uri`).

### `Rezepte.Tests/Services/BackgroundJobs/DemoDataSeedingJobHandlerTests.cs` (249 Zeilen)

- 6 Tests: 5 Kochbücher (124-133), 43 Rezepte (139-156), 5 Kalenderevents je Folgetag 18:00 (162-183), 6 Einkaufslisten-Items in Default-Gruppe (189-204), Throw bei fehlendem User (210-222), Throw bei Service-Fehler (228-248).
- Baut die komplette Sequenz-Mock-Landschaft selbst (`HappyPathMocks` 27-33, `BuildProvider` 35-50, `CreateHappyPathMocks` 66-118: Moq-Setups für `IUserService`, `ICookbookService`, `IRecipeService`, `ICalendarService`, `IShoppingListService`, `ILogger<DemoDataSeedingJobHandler>`).
- **Anpassung:** Diese Tests prüfen heute die im Handler liegende Fachsequenz — genau das vom Review beanstandete Muster („Tests bauen mehrstufige Sequenzen nach"). Bei Kapselung in `IDemoDataSeeder` ziehen die Sequenz-Assertions in Tests des neuen Services um; der Handler-Test verifiziert nur noch Auslösung (`SeedForUserAsync` aufgerufen, Fehler → Exception → Job `Failed`).

### `Rezepte.Tests/Services/UserServiceTests.cs`

- Zeilen 306-327: `RegisterAsync_ShouldEnqueueDemoDataJob...` verifiziert `queue.EnqueueAsync("seed-demo-data", <payload mit userId>, userId, ct)` via Moq.
- Zeilen 329-345: Negativfall `createDemoData=false` → kein Enqueue.
- **Anpassung:** keine — Job-Anlage bleibt fachlich unverändert (FA-2).

### `Rezepte.Tests/Controllers/AuthControllerTests.cs`

- Zeilen 43, 66, 89: Registrierung übergibt `createDemoData` korrekt an `IUserService.RegisterAsync`. **Unverändert relevant.**

### `Rezepte.Tests/Components/PluginSettingsComponentTests.cs`

- bUnit-Tests der Plugin-Einstellungs-UI (CheckButton, CheckSourceAsync, Localizer-Texte; `using Rezepte.Web.Services.Import.Plugins` Zeile 7). Kein Bezug zu `IImportService`/Orchestrator. **Keine Anpassung nötig** (Nennung in der Anforderung mutmaßlich wegen Plugin-Nähe).

### `Rezepte.Tests/Components/Shared/CollectionImportSelectionStateTests.cs`

- Tests des UI-Zustandshelfers `CollectionImportSelectionState` (CreateRecipeDialog-Selection). **Keine Anpassung erwartet.**

### `Rezepte.Tests.Browser` — Register-Tests

- `Rezepte.Tests.Browser/Auth/RegisterTests.cs` (xUnit + Playwright, Collection `RegisterTestCollection`):
  - `Register_WithDemoData_CreatesDemoData` (50-87): Registrierung mit Checkbox → Warten via `DemoDataWaitHelper.WaitForDemoDataAsync` (polled UI-Counts + prüft neueste `seed-demo-data`-Job-Row in SQLite, Timeout 120 s, Zeilen 22-24 + 255-330) → erwartet 5 Kochbücher, 43 Rezepte, 5 Kalenderevents, >0 Shopping-Items.
  - `Register_WithoutDemoData_LeavesListsEmpty` (94-122).
  - `RegisterTestsAppFixture.cs:12` — `SeedSharedTestUser => false`; App-Restart pro Test wegen „nur bei leerer DB"-Registrierung.
- **Anpassung:** keine — fachliches Ergebnis bleibt gleich; der Test ist gleichzeitig E2E-Nachweis, dass der gekapselte Seeder identisch liefert. Laufzeit teuer (App-Build+Start).

### Sonstige geprüfte Testdateien ohne Änderungsbedarf

`RemoteUrlGuardTests`, `BackgroundJobPersistenceTests`, `CookbookServiceTests`, `RecipeServiceTests`, `CalendarServiceTests`, `ShoppingListServiceTests`, `CollectionImportSelectionStateTests`, `ContractExportScriptTests` (betrifft nur `contract-baselines/import-contract/*` — Plugin-Vertrag unverändert).

## Build- und Test-Kommandos

- `Docs/testing.md:6` — `dotnet test`; Browser-Tests `dotnet build Rezepte.Tests.Browser/... -c Release` + Playwright-Install + `dotnet test ... --no-build` (Zeilen 15-19).
- CI `.github/workflows/pr-staging-ci.yml`: `dotnet build Rezepte.sln --configuration Release --no-restore -p:TreatWarningsAsErrors=true` (Zeile 101), Einzelbuilds Web/Tests/Tests.Browser (122-128), `dotnet test Rezepte.sln --configuration Release --no-build` (148).
- `staging-ci.yml` analog (92-135); Release-Workflow `release.yml:97-130`.
- Branch-/PR-Fluss: Feature-Branch → `staging` (PR-Checks `pr-staging-ci.yml` + `verify-pr-source.yml`), Promotion staging→main via `staging-to-main-promotion.yml`.
- Lokal: `dotnet build Rezepte.sln` / `dotnet test Rezepte.sln` (Debug ausreichend; CI baut Release).

## Weitere Konventionen

- `CLAUDE.md` (Repo-Root): Programmcode auf Englisch (Identifier, Kommentare, Exceptions, Logs); faktenbasiertes Arbeiten.
- `.github/copilot-instructions.md` (243 Zeilen): Services als Scoped bei Kontextabhängigkeit (51-56), Singletons injizieren Scoped nur via `IServiceScopeFactory` (53), dünne Controller (146), Razor schlank — Business in Services (137), Transaktionen auf Service-Ebene bündeln (88), Informations-Log am Methodenstart (72).
- Git-Hooks (`.githooks/`): `pre-commit` u. a. `csproj-xmldoc-check.py`, `razor-l10n-check.py`, `dotnet format --verify-no-changes`, Encoding-Check; `pre-push` `no-notimplemented-check`, `razor-usage-check`, `enum-coverage-check` (`Docs/testing.md:31-51`).
- UI-Texte in Razor über `IStringLocalizer<UiStrings>` (`Docs/testing.md:49`) — in `CreateRecipeDialog.razor`/`CookbookPage.razor` teils hartkodierte deutsche Strings vorhanden (Ist-Zustand).
- Feature-Doku-Konvention `Docs/help/`: Import-Verhalten in `import-plugins.md` („Importablauf" Zeilen 87-96), Seeding in `user-accounts.md` (Zeilen 28-44) — beide Abschnitte beschreiben Ist-Zustände, die sich durch die Umstellung ändern.
