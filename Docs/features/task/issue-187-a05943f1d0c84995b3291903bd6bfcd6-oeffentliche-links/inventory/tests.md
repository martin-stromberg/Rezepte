# Tests — Bestandsaufnahme

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-11-16, ~21:12–21:15 Uhr +01:00 (CET)
- Branch und Commit-ID: `task/issue-187-a05943f1d0c84995b3291903bd6bfcd6-oeffentliche-links` @ `f0551af03b4a3ea62f51157ebb8eb46e6fc5a3c7` („test: Demo-Daten-Zähler im Registrierungs-E2E-Test stabilisieren (#190)")
- Uncommittete Änderungen im getesteten Stand: keine Code-Änderungen; lediglich das neue, ungetrackte Verzeichnis `Docs/features/task/issue-187-a05943f1d0c84995b3291903bd6bfcd6-oeffentliche-links/` (diese Bestandsaufnahme). `git status --porcelain` zeigte ausschließlich `?? Docs/features/task/.../`
- Testumgebung und Runtime-/SDK-Versionen: .NET SDK 10.0.403, Laufzeit .NET 10.0.15 (xUnit.net VSTest Adapter 3.1.5, 64-bit); Windows, Git Bash; Debug-Konfiguration (impliziter Build durch `dotnet test`). Für die Browser-Tests war Playwright-Chromium bereits installiert und ein Publish-Output `Rezepte.Web/bin/Debug/net10.0/publish/Rezepte.Web.dll` vorhanden (durch den Build erzeugt) — die Browser-Tests liefen real gegen eine gestartete App-Instanz mit temporärer SQLite-Datenbank.
- Ermittelte Testsuiten und Quellen der Testbefehle: Zwei Testprojekte — `Rezepte.Tests` (xUnit: Services, Controller, Komponenten via bUnit, Integration via `Microsoft.AspNetCore.Mvc.Testing`) und `Rezepte.Tests.Browser` (xUnit + Microsoft.Playwright, E2E gegen publizierte `Rezepte.Web.dll`). Testbefehl laut `README.md`/`Docs/testing.md`: `dotnet test`; CI (`.github/workflows/staging-ci.yml`, Job `build-and-test`) führt `dotnet test Rezepte.sln --configuration Release --no-build` nach Playwright-Installation und `dotnet publish` aus. Die Solution enthält keine weiteren Testprojekte.

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| 1 — `Rezepte.Tests` | `dotnet test Rezepte.sln --logger "console;verbosity=normal"` | Repo-Root | 0 | 717 | 0 | 0 | [Console-Log](test-results/dotnet-test-sln-console.log) |
| 1 — `Rezepte.Tests.Browser` | (selber Aufruf, zweite Testdatei) | Repo-Root | 0 | 27 | 0 | 0 | [Console-Log](test-results/dotnet-test-sln-console.log) |

Gesamt: 744 Tests, alle bestanden. Beide Suiten liefen im selben `dotnet test`-Aufruf; die xUnit-Zusammenfassungen („Gesamtzahl Tests: 717 / Bestanden: 717" bzw. „…: 27 / Bestanden: 27") zeigen weder Fehlschläge noch Übersprungene.

### Nachgewiesene bestehende Testfehler

Keine — im Ausgangslauf sind alle 744 Tests bestanden. Es gibt keinen nachgewiesenen preexisting Fehler.

### Testlücken und Ausführungsprobleme

- Keine fehlgeschlagenen, übersprungenen oder nicht ausführbaren Tests im Ausgangslauf; keine Build-, Setup- oder Infrastrukturfehler.
- Hinweis zur Reproduzierbarkeit: Die Browser-Tests überspringen sich selbst (`ApplicationAvailable`/`ApplicationUnavailableSkipReason` in `RezepteAppFixture`), wenn kein Publish-Output unter `Rezepte.Web/bin/<Konfiguration>/<TFM>/publish/` bzw. `REZEPTE_PUBLISH_DIR` liegt oder Playwright-Chromium fehlt. Im dokumentierten Lauf waren beide Voraussetzungen erfüllt — alle 27 Browser-Tests wurden real ausgeführt.
- Für die angeforderte Funktionalität existieren noch **keine** Tests (keine Share-Service-, Share-Controller- oder Share-UI-Tests).

## Testklassen

Bestehende, für den Bereich relevante Testklassen und ihr Prüfumfang:

### `Rezepte.Tests/Services/SettingsServiceTests.cs`
- `GetUserAiEnabledAsync_ShouldReturnTrueByDefault_WhenNoSettingExists`, `SetUserAiEnabledAsync_*` — Benutzer-Settings via `UserSetting`/`AppSetting`
- `GetGlobalAiEnabledAsync_*`, `SetGlobalAiEnabledAsync_*` — globale Bool-Settings mit Default
- `Get/SetGlobalGoogleVisionEnabledAsync_*`, `Get/SetGlobalGeminiEnabledAsync_*` — globale Service-Toggles
- `SetGlobalMaxRequestsPerHourAsync_ShouldPersistValue` / `…_ShouldClearValue_WhenPassedNull` / `GetGlobalMaxRequestsPerHourAsync_ShouldReturnNull_WhenNoSettingExists` — `int?`-Settings inkl. Löschsemantik (Muster für eine numerische Admin-Einstellung)
- `Get/SetGlobalMaxRequestsPerDayAsync_*` (Datei geht über Z. 527 hinaus) — analog
- `GetUserShoppingListEditModeAsync_*`, `SetUserShoppingListEditModeAsync_*` — pro-Benutzer `AppSetting`-Keys
- `Get/SetSecurityTxtSettingsAsync_*` — komplexere Settings-DTO-Roundtrips, Defaults, `null`-Clearing, ungültige Datumswerte

### `Rezepte.Tests/Services/RecipeServiceTests.cs`
- `CreateAsync_ShouldCreate_WithStepsAndIngredients` u. a. — Erzeugen/Lesen von Rezepten mit Schritten, Zutaten, Beilagen (`sideDishRecipeIds`), `userId`-Isolation zwischen `UserA`/`UserB`

### `Rezepte.Tests/Services/RecipeServiceStepValidationTests.cs`
- Validierung von Schritten/Zutaten beim Anlegen/Aktualisieren

### `Rezepte.Tests/Services/RecipeServiceSearchSqliteTests.cs`
- `SearchAsync_*_OnSqlite` — Suche gegen echten SQLite-Provider (statt InMemory), u. a. `ShouldNotReturnForeignUserRecipes` (fremde Benutzerdaten)

### `Rezepte.Tests/Controllers/SettingsControllerSecurityTxtValidationTests.cs`
- `SetGlobalSecurityTxt_ReturnsBadRequest_*` / `ReturnsNoContent_*` — Controller-Validierung mit `Mock<ISettingsService>` (Muster für Admin-Settings-Validierungstests)

### `Rezepte.Tests/Controllers/SecurityTxtControllerTests.cs` und `SecurityTxtControllerTests_Authentication.cs`
- `GetSecurityTxt_RequiresNoAuthentication` — `WebApplicationFactory<Program>`-Integrationstest, der anonymen Zugriff auf einen öffentlichen Endpunkt nachweist (kein Redirect/401/302)
- Weitere SecurityTxt-Tests zu Rendering/Deaktiviert-Verhalten

### `Rezepte.Tests/Controllers/AuthControllerTests.cs`, `SettingsCredentialAvailabilityTests.cs`, `AdminExportCleanupControllerTests.cs`, `RecipesControllerSearchTests.cs`
- Auth-/Admin-/Controller-Muster für spätere Share-Endpunkt-Tests

### `Rezepte.Tests/Components/*` (bUnit)
- `SecurityTxtSettingsTests_ErrorHandling` — Admin-Settings-Komponente (Lade-/Speicher-/Timeout-Fehler)
- `RecipeSearchComponentTests`, `PluginSettingsComponentTests`, `MainLayoutSearchTests`, `LoadingBarRenderingTests`, `Shared/CollectionImportSelectionStateTests` — bUnit-Muster für Komponententests

### `Rezepte.Tests.Browser` (Playwright E2E)
- `SecurityTxtBrowserTests` — `GetSecurityTxt_ReturnsOk_WithoutAuthentication_WhenEnabled`, `…_ReturnsNotFound_WhenDisabled`, `Admin_CanConfigureSecurityTxtViaUi_AndContentAppearsInPublicEndpoint`, `RegularUser_DoesNotSeeSecurityTxtMenuItemInSettings` (Referenz für „anonymer öffentlicher Endpunkt + Admin-gated Settings-UI")
- `RegisterTests`, `RecipeSearchBrowserTests`, `PluginSettingsBrowserTests`, `LoadingBar*BrowserTests`

## Hilfsmethoden

### `Rezepte.Tests` — lokale Fabriken (pro Datei)
- `SettingsServiceTests.CreateDb()` — `RezepteDbContext` mit `UseInMemoryDatabase(Guid.NewGuid())`; `sut = new SettingsService(db, new SecurityTxtSettingsService(db))`
- `RecipeServiceTests.CreateDb()` / `CreateMockEnv()` / `CreateMockHttpContextAccessor(userId)` — InMemory-Db, `IWebHostEnvironment`-Mock, `IHttpContextAccessor` mit `ClaimsPrincipal` (`NameIdentifier`)
- `SettingsControllerSecurityTxtValidationTests.CreateController(settingsMock)` — `SettingsController` mit `Mock<ISettingsService>` + `Mock<IGoogleCredentialsProvider>`
- `SecurityTxtControllerTests_Authentication.SecurityTxtWebApplicationFactory` — `WebApplicationFactory<Program>` mit Production-Environment, In-Memory-Config für `ConnectionStrings:Default` (temporäre SQLite-Datei) und `Jwt:Key`

### `Rezepte.Tests/TestSupport/FixedTimeProvider.cs`
- `FixedTimeProvider(utcNow)` — steuerbarer `TimeProvider` mit `Advance(TimeSpan)`; Produktivseitig ist `TimeProvider.System` registriert (`ServiceCollectionExtensions` Z. 194) — für Ablaufzeit-Tests eines Share-Services nutzbar

### `Rezepte.Tests.Browser/Infrastructure/`
- `RezepteAppFixture` — startet publizierte `Rezepte.Web.dll` als eigenen Prozess mit temporärer SQLite-DB, Test-JWT-Key und erhöhtem Auth-Rate-Limit; registriert Testbenutzer `browsertest`/`BrowserTest!123`; `RestartWithEmptyDatabaseAsync` für Registrierungsflows; Ableitungen `ConfiguredRezepteAppFixture`, `PluginSettingsAppFixture`, `RegisterTestsAppFixture` mit Env-Overrides
- `PlaywrightBrowserFixture` + `BrowserTestCollection`/`PluginSettingsTestCollection`/`RegisterTestCollection` — gemeinsame Playwright-/App-Fixture-Collections
- `RecipeSeedHelper.SeedRecipesAsync(page, baseAddress, titles)` — legt Rezepte über die echte API an (Session-Token aus Cookie → `POST api/recipes`, optional `sideDishRecipeIds`-Feld im Request)
- Page Objects: `LoginPage`, `RegisterPage`, `LoadingBarPageObject`, `PluginSettingsPageObject`, `SecurityTxtPageObject`; `GitHubStubServer` für externe Abhängigkeiten
