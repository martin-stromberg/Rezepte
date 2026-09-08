# Tests – Bestandsaufnahme „Demo-Daten bei Registrierung"

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-09-08, Läufe zwischen ca. 08:44 und 08:53 +02:00 (Mitteleuropa, Sommerzeit)
- Branch und Commit-ID: `task/issue-158-30e99f2ca3e54743bef6edcee9c0e9ca-demo-daten`, Commit `061f43b1fb52e5461daacd9a7521d38f1fa926d8` (2026-09-06 10:49:04 +0200)
- Uncommittete Änderungen im getesteten Stand: keine Codeänderungen; lediglich das neue, ungetrackte Dokumentationsverzeichnis `Docs/features/task/issue-158-…-demo-daten/` (requirement.md, inventory-Artefakte). Produktivcode und Tests unverändert.
- Testumgebung und Runtime-/SDK-Versionen: Windows 11 (PowerShell), .NET SDK `10.0.400`, TargetFramework `net10.0`, xunit 2.9.3, Playwright 1.61.0 (Chromium bereits unter `%USERPROFILE%\AppData\Local\ms-playwright` installiert)
- Ermittelte Testsuiten und Quellen der Testbefehle: `.github/workflows/staging-ci.yml` bzw. `pr-staging-ci.yml` führen `dotnet test Rezepte.sln --configuration Release --no-build --logger "trx;LogFileName=test-results.trx"` aus und umfassen die Projekte `Rezepte.Tests` (Unit-/Komponententests, bUnit, EF Core InMemory/Sqlite) und `Rezepte.Tests.Browser` (Playwright-E2E gegen publizierte `Rezepte.Web`).

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| 1 | `dotnet test Rezepte.Tests/Rezepte.Tests.csproj --configuration Release --logger "trx;LogFileName=test-results-unittests.trx"` | Repo-Root | 1 | — (kein Testlauf) | — | — | [Log](test-results/dotnet-test-unittests.log) |
| 2 | `dotnet test Rezepte.Tests/Rezepte.Tests.csproj --configuration Release --logger "trx;LogFileName=test-results-unittests.trx"` | Repo-Root | 0 | 579 | 0 | 0 | [Log](test-results/dotnet-test-unittests-run2.log), [TRX](test-results/test-results-unittests.trx) |
| 3 | `dotnet test Rezepte.Tests.Browser/Rezepte.Tests.Browser.csproj --configuration Release --logger "trx;LogFileName=test-results-browser.trx"` | Repo-Root | 0 | 13 | 0 | 0 | [Log](test-results/dotnet-test-browser.log), [TRX](test-results/test-results-browser.trx) |

### Nachgewiesene bestehende Testfehler

| Test-ID inkl. Testfall | Suite / Dateipfad | Fehlerbild / Fehlermeldung | Lauf und Nachweis |
|-----------------------|------------------|---------------------------|-------------------|
| — | — | Keine Testfehler nachgewiesen. | Läufe 2 und 3: 0 fehlgeschlagene Tests |

### Testlücken und Ausführungsprobleme

- **Lauf 1 (Infrastruktur-/Buildfehler, kein Testfehler):** Der erste `dotnet test`-Aufruf schlug im Build fehl, weil die Plugin-Projekte `Rezepte.Import.Plugins.AIFoto` und `Rezepte.Import.Plugins.AIUrl` die Assembly `Rezepte.Web` per `<Reference><HintPath>..\Rezepte.Web\bin\$(Configuration)\$(TargetFramework)\Rezepte.Web.dll</HintPath></Reference>` einbinden (statt `ProjectReference`) und `Rezepte.Web.dll` zum Zeitpunkt des parallelen Builds noch nicht existierte. Folgefehler: `MSB3245`, `CS0234`, `CS0246`, `CS0535` in `AIFotoImportHandler.cs`/`AIFotoImportPlugin.cs`. Nachdem `Rezepte.Web` einmal gebaut war (DLL lag vor), verliefen alle weiteren Builds fehlerfrei. In der CI tritt das nicht auf, weil dort `Rezepte.Web` vor dem Testprojekt separat gebaut wird. Nachweis: [Log Lauf 1](test-results/dotnet-test-unittests.log).
- Alle 579 Unit-Tests und alle 13 Browser-Tests wurden ausgeführt; keine übersprungenen oder deaktivierten Tests gemeldet.
- Es existieren **keine** Tests für Demo-Daten-Seeding; die Browser-Suite enthält keinen Test für den Registrierungsfluss (nur LoadingBar-/SecurityTxt-Tests).

## Testklassen (für die Anforderung relevant)

### `UserServiceTests`
Datei: `Rezepte.Tests/Services/UserServiceTests.cs` — nutzt EF Core InMemory (`CreateDb`, `CreateSut` mit `new UserService(db, new UsernameValidator())`).

- `RegisterAsync_ShouldCreateFirstUserAsAdmin_WhenNoUsersExist` — erster Benutzer wird Admin
- `RegisterAsync_ShouldFail_WhenUsernameAlreadyExists` — Duplikat-Abweisung
- `RegisterAsync_ShouldFail_WhenUsernameIsReserved` — reservierte Namen
- `LoginAsync_*`, `UpdateProfileAsync_*`, `ChangePasswordAsync_*`, `UpdateUserAsync_*`, `DeleteAsync_*` — übrige Service-Tests (alle rufen `RegisterAsync` mit aktuell 3 Argumenten auf und müssen bei Signaturerweiterung angepasst werden)

### `AuthControllerTests`
Datei: `Rezepte.Tests/Controllers/AuthControllerTests.cs`

- `Register_ShouldRedirectFormPostWithValidationError_WhenUsernameIsRejected` — Moq auf `IUserService.RegisterAsync` (3 Parameter), Form-Post-Context via `CreateFormContext`

### `CookbookServiceTests`
Datei: `Rezepte.Tests/Services/CookbookServiceTests.cs`

- `CreateAsync_ShouldCreate_ForUser`, `GetAllAsync_ShouldReturnOnlyUserCookbooks`, `GetByIdAsync_*`, `UpdateAsync_*`, `DeleteAsync_*` — Owner-Isolation und CRUD

### `RecipeServiceTests` / `RecipeServiceStepValidationTests`
Dateien: `Rezepte.Tests/Services/RecipeServiceTests.cs`, `RecipeServiceStepValidationTests.cs`

- `CreateAsync_ShouldCreate_WithStepsAndIngredients`, Validierung (Titel zu kurz, fehlende Schrittbeschreibung, negative Dauer, fehlender Zutatenname), `GetByCookbookAsync`, `AddExistingToCookbookAsync_*`, Side-Dish-Tests

### `CalendarServiceTests`
Datei: `Rezepte.Tests/Services/CalendarServiceTests.cs`

- `CreateEventAsync_*` (Ablehnung fehlender User, Portions ≤ 0, unbekanntes Rezept, persistiertes Event mit normalisiertem Startdatum), `GetOccurrencesAsync_*` (Wiederholungen)

### `ShoppingListServiceTests`
Datei: `Rezepte.Tests/Services/ShoppingListServiceTests.cs`

- `GetGroupsAsync_ShouldCreateDefaultGroup_WhenListIsEmpty`, `AddItemAsync_*`, `AddRecipeIngredientsAsync_*`, `AddRecipeIngredientGroupsAsync_*`

### `BackgroundJobPersistenceTests`
Datei: `Rezepte.Tests/Services/BackgroundJobs/BackgroundJobPersistenceTests.cs`

- `DbContext_ShouldPersistBackgroundJob` — `BackgroundJob` wird mit Status `Pending` persistiert
- `DbContext_ShouldRoundTripAllBackgroundJobStatuses` — Status-Roundtrip
- `Migrations_ShouldCreateBackgroundJobsTable` — Migration `20260706090000_AddBackgroundJobsTable`

## Hilfsmethoden

### `UserServiceTests` (lokal)
- `CreateDb()` — EF-Core-InMemory-`RezepteDbContext` mit zufälligem DB-Namen
- `CreateSut(db)` — `new UserService(db, new UsernameValidator())`

### `AuthControllerTests` (lokal)
- `CreateFormContext(formBody)` — `DefaultHttpContext` mit `application/x-www-form-urlencoded`-Body

### `Rezepte.Tests/TestHelpers/`
- `RepositoryPaths.cs` — Repo-Pfade (wird auch in `Rezepte.Tests.Browser` gelinkt)
- `LoadingBarServiceTestFactory.cs`, `TestGeminiClient.cs`, `EnvironmentVariableScope.cs`, `GoogleCredentialsEnvironmentCollection.cs` — nicht für diese Anforderung relevant

### `Rezepte.Tests.Browser/Infrastructure/`
- `RezepteAppFixture.cs`, `ConfiguredRezepteAppFixture.cs`, `PlaywrightBrowserFixture.cs`, `BrowserTestCollection.cs`, `LoadingBarBrowserSession.cs`, Page-Objects — Infrastruktur für Playwright-E2E-Tests (startet publizierte `Rezepte.Web`); für einen Registrierungs-E2E-Test wiederverwendbar, ein entsprechendes Page-Object existiert nicht.
