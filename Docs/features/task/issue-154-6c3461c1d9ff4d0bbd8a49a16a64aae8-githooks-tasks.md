# Tasks: Git-Hooks aus Pattern-Collection übernehmen und ausgelöste Prüffehler beheben

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Hooks | `RecipeEdit.razor` aus dem Index nehmen und Hooks-Baseline committen (`.githooks/*`, `CLAUDE.md`, Docs-Artefakte) | Offen | — |
| 2 | Konfiguration | `Rezepte.Web.csproj`: `GenerateDocumentationFile` + `WarningsAsErrors>CS1591` ergänzen | Offen | — |
| 3 | Konfiguration | `Microsoft.Extensions.Localization`-Verfügbarkeit prüfen, ggf. `PackageReference` in `Rezepte.Web.csproj` ergänzen | Offen | — |
| 4 | Lokalisierung | Marker-Klasse `Rezepte.Web/Resources/UiStrings.cs` anlegen | Offen | — |
| 5 | Lokalisierung | `Rezepte.Web/Resources/UiStrings.resx` mit validen resx-Headern anlegen | Offen | — |
| 6 | Lokalisierung | `Program.cs`: `builder.Services.AddLocalization()` registrieren | Offen | — |
| 7 | Lokalisierung | `_Imports.razor`: `@using Microsoft.Extensions.Localization` + `@using Rezepte.Web.Resources` ergänzen | Offen | — |
| 8 | XML-Doku | `dotnet build Rezepte.Web`: alle CS1591-Fehler beheben (vollständige `///`-Doku aller öffentlichen Member inkl. der 24 Inventur-Dateien) | Offen | — |
| 9 | razor-usage | BOM entfernen + lokalisieren: `Calendar.razor`, `CookbookDetails.razor`, `CookbookPage.razor`, `Cookbooks.razor`, `Error.razor`, `Home.razor`, `RecipePage.razor`, `Settings.razor` | Offen | — |
| 10 | razor-usage | `Routes.razor`: `typeof(Layout.MainLayout)` → `typeof(MainLayout)` + `@using Rezepte.Web.Components.Layout` | Offen | — |
| 11 | razor-usage | `Settings.razor` `@code`: `typeof(...)`-Liste der 9 Settings-Komponenten aufnehmen; `SettingsViewModel.cs` anpassen | Offen | — |
| 12 | Lokalisierung | `MainLayout.razor` lokalisieren (11 Fundstellen) + BOM entfernen | Offen | — |
| 13 | Lokalisierung | Pages lokalisieren: `Login`, `RecipeEdit` (inkl. „geloescht"-Korrektur), `RecipeSearch`, `Register`, `ScheduledRecipes`, `ShoppingList` | Offen | — |
| 14 | Lokalisierung | Settings-Komponenten lokalisieren: `AiSettings`, `ApplicationUpdates`, `BackupRestore`, `ExportData`, `ExportFilesList`, `PluginSettings`, `SecurityTxtSettings`, `UsageStats`, `UserAdmin`, `UserProfile` | Offen | — |
| 15 | Lokalisierung | Shared-Komponenten lokalisieren: `AddRecipeToShoppingListDialog`, `AssignToCookbooksOverlay`, `CalendarEventDialog`, `CreateRecipeDialog`, `ImageCropper`, `LatestRecipes`, `MultiAssignToCookbooksOverlay`, `PhotoOverlay`, `RandomFromCookbooks`, `RecipeSelectDialog` | Offen | — |
| 16 | Konfiguration | `Rezepte.Tests.csproj` + `Rezepte.Tests.PluginFixture.csproj`: XML-Doc-Konfiguration; CS1591-Buildfehler beider Projekte beheben | Offen | — |
| 17 | Tests | `TestImportPlugin.cs` Z. 31: throw-only-Body umbauen | Offen | — |
| 18 | Tests | `ApplicationUpdatePreInstallHandlerTests.cs` Z. 101/118: throw-only-Bodies umbauen | Offen | — |
| 19 | Tests | `UpdateBackupServiceTests.cs` Z. 162/174/180/183/186: throw-only-Bodies umbauen | Offen | — |
| 20 | Tests | `ImportOrchestratorTests.cs` Z. 389 (`ThrowingHandler`) umbauen + Z. 270 (`CancellingStream`) XML-Doku vervollständigen | Offen | — |
| 21 | Tests | `PluginUpdateServiceTests.cs` Z. 160 (`FailingPackageInstaller`) umbauen | Offen | — |
| 22 | Tests | Enum-Coverage-Tests: `ImportCollectionItemState.Pending`/`.Importing`, `WeekDays.Tuesday`/`.Saturday`, `BackgroundJobStatus.Running`/`.Failed`/`.Cancelled` in `Rezepte.Tests` referenzieren | Offen | — |
| 23 | Konfiguration | `Rezepte.Tests.Browser.csproj`: XML-Doc-Konfiguration; `ConfiguredRezepteAppFixture.cs`-Doku + CS1591-Fehler beheben | Offen | — |
| 24 | Konfiguration | `Rezepte.Import.Abstractions`, `Rezepte.Import.PluginSdk`, `Rezepte.Import.Plugins.{AIFoto,AIUrl,Backup}`, `Rezepte.Updater.TestHost`: XML-Doc-Konfiguration + CS1591-Fehler je Projekt beheben | Offen | — |
| 25 | Verifikation | `dotnet build Rezepte.sln`, `dotnet test`, `Rezepte.Tests.Browser`, `dotnet format --verify-no-changes`, alle Check-Skripte `--all`/`--all --strict` = Exit 0, finaler Commit/Push durchläuft beide Hooks | Offen | — |
