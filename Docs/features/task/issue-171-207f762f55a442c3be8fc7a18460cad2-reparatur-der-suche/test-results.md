# Test-Ergebnisse

## Ergebnis

**Status:** Fehler vorhanden

## Fehlgeschlagene Tests

### Rezepte.Tests.Browser / RegisterTests.cs

- **Register_WithDemoData_CreatesDemoData** — System.TimeoutException: Demo data state did not match expected value True within 120000ms. Last observed: cookbooks=5, recipes=0, calendar=5, shopping=6; seed job status=Succeeded, error=(none) (RegisterTests.cs:305)

**Einordnung:** Dokumentierter, pre-existing Flake außerhalb des Issue-Scope (bekannte Baustelle, nicht durch die Such-Reparatur verursacht). Die Fehlersignatur wechselt weiterhin zwischen Läufen (`calendar=5` statt `calendar=0` wie im letzten Lauf; konstant bleibt `recipes=0` bei `seed job status=Succeeded`) — dies entspricht exakt dem dokumentierten Flake-Verhalten. Alle geplanten E2E-Szenarien der Suche sind bestanden (siehe unten); alle Such-bezogenen Tests sind grün.

## E2E-Abdeckung

Geplante E2E-Szenarien aus `plan.md` (Abschnitt „E2E-Tests (primärer Funktionsnachweis)"):

| Szenario | Test / Testklasse | Ergebnis |
|----------|-------------------|----------|
| Pflicht: Login → Navbar-Suche nach Begriff im Rezepttitel → Trefferliste zeigt das Rezept | `RecipeSearchBrowserTests.NavbarSearch_ShowsSeededRecipe` | Bestanden |
| Pflicht: Direktaufruf `GET /recipes/search?q=…` (Reload-/Link-Pfad) zeigt Treffer | `RecipeSearchBrowserTests.DirectUrl_WithQuery_ShowsResults` | Bestanden |
| Pflicht: Suche nach nicht vorhandenem Begriff → „Keine Treffer." (`alert-secondary`), kein `SearchFailed` | `RecipeSearchBrowserTests.Search_WithoutMatches_ShowsNoSearchResults` | Bestanden |
| Pflicht: > `pageSize` Treffer → „»"-Button → Seite 2 zeigt Resttreffer und „Seite 2 von …" | `RecipeSearchBrowserTests.Pagination_NextPage_ShowsPageTwo` | Bestanden |
| Soll: Suche über In-Page-Formular `#search-q` auf `/recipes/search` aktualisiert die Trefferliste | `RecipeSearchBrowserTests.InPageSearch_Submit_UpdatesResultList` | Bestanden |

Zusätzlich verifiziert: `LoadingBarFormNavigationBrowserTests.SearchSubmit_ShowsLoadingBar` (in `plan.md` als anzupassender Bestandstest genannt) — Bestanden.

## Zusammenfassung

- Gesamt: 624
- Bestanden: 623
- Fehlgeschlagen: 1
- Übersprungen: 0

Ausgeführt: `dotnet build Rezepte.sln` (0 Fehler, 0 Warnungen) → `dotnet test Rezepte.sln --no-build --collect:"XPlat Code Coverage" --logger "console;verbosity=normal"`. Davon `Rezepte.Tests`: 604 bestanden, `Rezepte.Tests.Browser`: 19 bestanden / 1 fehlgeschlagen.

## Testabdeckung

**Abdeckung:** 67 % (17244/25694 Zeilen, Zeilenabdeckung, beide Coverage-Berichte zeilenweise zusammengeführt)

Hinweis: Die Browser-Tests starten `Rezepte.Web` als eigenen, nicht instrumentierten Prozess; Code, der ausschließlich über Playwright-E2E-Tests ausgeführt wird (insbesondere `.razor`-Komponenten), erscheint daher in den Coverage-Daten mit 0 %, obwohl er E2E-abgedeckt sein kann.

| Datei | Abdeckung |
|-------|-----------|
| `Rezepte.Web/Components/Layout/MainLayout.razor` | 0.0% |
| `Rezepte.Web/Components/Pages/Calendar.razor` | 0.0% |
| `Rezepte.Web/Components/Pages/CookbookDetails.razor` | 0.0% |
| `Rezepte.Web/Components/Pages/CookbookPage.razor` | 0.0% |
| `Rezepte.Web/Components/Pages/Cookbooks.razor` | 0.0% |
| `Rezepte.Web/Components/Pages/Error.razor` | 0.0% |
| `Rezepte.Web/Components/Pages/Home.razor` | 0.0% |
| `Rezepte.Web/Components/Pages/Login.razor` | 0.0% |
| `Rezepte.Web/Components/Pages/RecipeEdit.razor` | 0.0% |
| `Rezepte.Web/Components/Pages/RecipePage.razor` | 0.0% |
| `Rezepte.Web/Components/Pages/Register.razor` | 0.0% |
| `Rezepte.Web/Components/Pages/ScheduledRecipes.razor` | 0.0% |
| `Rezepte.Web/Components/Pages/Settings.razor` | 0.0% |
| `Rezepte.Web/Components/Pages/ShoppingList.razor` | 0.0% |
| `Rezepte.Web/Components/RedirectToLogin.razor` | 0.0% |
| `Rezepte.Web/Components/Settings/AiSettings.razor` | 0.0% |
| `Rezepte.Web/Components/Settings/ApplicationUpdates.razor` | 0.0% |
| `Rezepte.Web/Components/Settings/BackupRestore.razor` | 0.0% |
| `Rezepte.Web/Components/Settings/ExportData.razor` | 0.0% |
| `Rezepte.Web/Components/Settings/ExportFilesList.razor` | 0.0% |
| `Rezepte.Web/Components/Settings/PluginSettings.razor` | 0.0% |
| `Rezepte.Web/Components/Settings/UsageStats.razor` | 0.0% |
| `Rezepte.Web/Components/Settings/UserAdmin.razor` | 0.0% |
| `Rezepte.Web/Components/Settings/UserProfile.razor` | 0.0% |
| `Rezepte.Web/Components/Shared/AddRecipeToShoppingListDialog.razor` | 0.0% |
| `Rezepte.Web/Components/Shared/AssignToCookbooksOverlay.razor` | 0.0% |
| `Rezepte.Web/Components/Shared/CalendarEventDialog.razor` | 0.0% |
| `Rezepte.Web/Components/Shared/CreateRecipeDialog.razor` | 0.0% |
| `Rezepte.Web/Components/Shared/ImageCropper.razor` | 0.0% |
| `Rezepte.Web/Components/Shared/LatestRecipes.razor` | 0.0% |
| `Rezepte.Web/Components/Shared/MultiAssignToCookbooksOverlay.razor` | 0.0% |
| `Rezepte.Web/Components/Shared/PhotoOverlay.razor` | 0.0% |
| `Rezepte.Web/Components/Shared/RandomFromCookbooks.razor` | 0.0% |
| `Rezepte.Web/Components/Shared/RecipeSelectDialog.razor` | 0.0% |
| `Rezepte.Web/Controllers/AdminExportsController.cs` | 0.0% |
| `Rezepte.Web/Controllers/AdminUsersController.cs` | 0.0% |
| `Rezepte.Web/Controllers/CalendarController.cs` | 0.0% |
| `Rezepte.Web/Controllers/CookbooksController.cs` | 0.0% |
| `Rezepte.Web/Controllers/ExportsController.cs` | 0.0% |
| `Rezepte.Web/Controllers/ImportTestController.cs` | 0.0% |
| `Rezepte.Web/Controllers/JobsController.cs` | 0.0% |
| `Rezepte.Web/Controllers/SessionController.cs` | 0.0% |
| `Rezepte.Web/Controllers/SessionTokenController.cs` | 0.0% |
| `Rezepte.Web/Controllers/UpdatesController.cs` | 0.0% |
| `Rezepte.Web/Controllers/UserExportFilesController.cs` | 0.0% |
| `Rezepte.Web/Controllers/UserStatsController.cs` | 0.0% |
| `Rezepte.Web/Controllers/UsersController.cs` | 0.0% |
| `Rezepte.Web/Dto/UserStatsDto.cs` | 0.0% |
| `Rezepte.Web/Dtos/ScheduledRecipeDto.cs` | 0.0% |
| `Rezepte.Web/Models/CalendarEventDto.cs` | 0.0% |
| `Rezepte.Web/Services/AntiForgeryHandler.cs` | 0.0% |
| `Rezepte.Web/Services/ApiAuthHandler.cs` | 0.0% |
| `Rezepte.Web/Services/BackgroundJobs/Handlers/ExportAllJobHandler.cs` | 0.0% |
| `Rezepte.Web/Services/BackgroundJobs/Handlers/ExportUserJobHandler.cs` | 0.0% |
| `Rezepte.Web/Services/Import/GoogleQuotaClient.cs` | 0.0% |
| `Rezepte.Web/Services/Import/ImportedRecipePersister.cs` | 0.0% |
| `Rezepte.Web/Services/Import/TestRecipeImportService.cs` | 0.0% |
| `Rezepte.Web/Services/PdfGenerator.cs` | 0.0% |
| `Rezepte.Web/ViewModels/SettingsViewModel.cs` | 0.0% |
| `Rezepte.Web/Services/Import/BaseImportHandler.cs` | 6.2% |
| `Rezepte.Web/Services/Import/Plugins/DataProtectionSystemSecretStore.cs` | 6.7% |
| `Rezepte.Web/Controllers/RecipesController.cs` | 9.5% |
| `Rezepte.Web/Services/BackgroundJobs/BackgroundJobHostedService.cs` | 19.7% |
| `Rezepte.Web/Services/Import/Plugins/PluginSettingsItem.cs` | 23.1% |
| `Rezepte.Web/Services/Import/BaseAIImportHandler.cs` | 26.0% |
| `Rezepte.Import.Plugins.AIFoto/AIFotoImportHandler.cs` | 34.4% |
| `Rezepte.Web/Middleware/RedirectToRegisterMiddleware.cs` | 37.7% |
| `Rezepte.Web/Controllers/SettingsController.cs` | 43.3% |
| `Rezepte.Import.Plugins.AIUrl/AIUrlImportHandler.cs` | 45.8% |
| `Rezepte.Web/Migrations/20251019143125_AddCalendarEvent.cs` | 50.0% |
| `Rezepte.Web/Contracts/AuthDtos.cs` | 53.3% |
| `Rezepte.Web/Migrations/20251024021342_AddAILogType.cs` | 53.7% |
| `Rezepte.Web/Services/Import/Plugins/PluginPackageValidator.cs` | 53.7% |
| `Rezepte.Web/Services/Import/Plugins/PluginUpdateHostedService.cs` | 56.2% |
| `Rezepte.Web/Migrations/20251014163413_AddRecipeUri.cs` | 58.3% |
| `Rezepte.Web/Services/Import/GeminiClient.cs` | 59.2% |
| `Rezepte.Web/Migrations/20251014031057_ChangeRecipeCookbookRelationshipToMulti.cs` | 59.6% |
| `Rezepte.Web/Migrations/20251011164006_AddAdminStateToUsers.cs` | 60.0% |
| `Rezepte.Web/Migrations/20251015050346_AddCookbookOrdering.cs` | 60.0% |
| `Rezepte.Web/Services/CookbookService.cs` | 60.3% |
| `Rezepte.Web/Migrations/20251013155507_AddJobQueue.cs` | 60.7% |
| `Rezepte.Web/Migrations/20251016194136_AddNumberOfPortionsForRecipe.cs` | 61.5% |
| `Rezepte.Web/Migrations/20251019065746_ExtendUserSettingsAndAppSettings.cs` | 61.5% |
| `Rezepte.Web/Migrations/20251019063026_ExtendUserSettingsAndAppSettings2.cs` | 62.1% |
| `Rezepte.Web/Migrations/20260718131500_AddPluginSourceReleaseReloadState.cs` | 62.1% |
| `Rezepte.Web/Components/Settings/SecurityTxtSettings.razor` | 62.8% |
| `Rezepte.Web/ApplicationVersion.cs` | 63.2% |
| `Rezepte.Web/Migrations/20251012152022_AddUserIdToModels.cs` | 63.2% |
| `Rezepte.Web/Migrations/20251019063026_ExtendUserSettingsAndAppSettings.cs` | 63.6% |
| `Rezepte.Web/Services/ApiClient.cs` | 64.0% |
| `Rezepte.Web/Services/BackgroundJobs/ExportJobFileStore.cs` | 64.3% |
| `Rezepte.Web/Security/RemoteUrlGuard.cs` | 65.6% |
| `Rezepte.Web/Extensions/FormFileExtensions.cs` | 66.7% |
| `Rezepte.Web/Services/Import/Plugins/PluginUpdateService.cs` | 67.5% |
| `Rezepte.Web/Contracts/UserDtos.cs` | 69.2% |
| `Rezepte.Web/Services/Import/Plugins/GitHubRepository.cs` | 69.7% |
| `Rezepte.Web/Services/RecipeService.cs` | 70.0% |
| `Rezepte.Web/Entities/UserExportFile.cs` | 71.4% |
| `Rezepte.Web/Extensions/ServiceCollectionExtensions.cs` | 71.8% |
| `Rezepte.Web/Services/BackgroundJobs/DemoDataSeedPayload.cs` | 72.7% |
| `Rezepte.Web/Services/Updates/ApplicationUpdateSettingsService.cs` | 73.4% |
| `Rezepte.Web/Services/ShoppingListService.cs` | 73.6% |
| `Rezepte.Web/Services/Import/Plugins/PluginSettingsService.cs` | 74.2% |
| `Rezepte.Web/Extensions/WebApplicationExtensions.cs` | 75.0% |
| `Rezepte.Web/Services/Import/ImportExceptionHelper.cs` | 75.0% |
| `Rezepte.Web/Services/Import/Plugins/IPluginManager.cs` | 75.0% |
| `Rezepte.Web/Controllers/AuthController.cs` | 75.9% |
| `Rezepte.Import.Abstractions/ImportCollectionModels.cs` | 76.0% |
| `Rezepte.Web/Services/BackgroundJobs/ExportJobPayload.cs` | 76.0% |
| `Rezepte.Web/Services/Import/Plugins/GitHubReleaseClient.cs` | 77.4% |
| `Rezepte.Web/Services/SettingsService.cs` | 78.3% |
| `Rezepte.Web/Services/Import/ImportService.cs` | 78.4% |
| `Rezepte.Tests.PluginFixture/TestImportPlugin.cs` | 78.6% |

## Fehlende Tests

Quelle: `Coverage-Daten`

Hinweis: `.razor`-Komponenten mit 0 % können dennoch durch Playwright-E2E-Tests ausgeführt worden sein — die ausgelagerte App läuft nicht instrumentiert (siehe Hinweis unter „Testabdeckung").

- `Rezepte.Web/Components/Layout/MainLayout.razor` — 0 % Abdeckung
- `Rezepte.Web/Components/Pages/Calendar.razor` — 0 % Abdeckung
- `Rezepte.Web/Components/Pages/CookbookDetails.razor` — 0 % Abdeckung
- `Rezepte.Web/Components/Pages/CookbookPage.razor` — 0 % Abdeckung
- `Rezepte.Web/Components/Pages/Cookbooks.razor` — 0 % Abdeckung
- `Rezepte.Web/Components/Pages/Error.razor` — 0 % Abdeckung
- `Rezepte.Web/Components/Pages/Home.razor` — 0 % Abdeckung
- `Rezepte.Web/Components/Pages/Login.razor` — 0 % Abdeckung
- `Rezepte.Web/Components/Pages/RecipeEdit.razor` — 0 % Abdeckung
- `Rezepte.Web/Components/Pages/RecipePage.razor` — 0 % Abdeckung
- `Rezepte.Web/Components/Pages/Register.razor` — 0 % Abdeckung
- `Rezepte.Web/Components/Pages/ScheduledRecipes.razor` — 0 % Abdeckung
- `Rezepte.Web/Components/Pages/Settings.razor` — 0 % Abdeckung
- `Rezepte.Web/Components/Pages/ShoppingList.razor` — 0 % Abdeckung
- `Rezepte.Web/Components/RedirectToLogin.razor` — 0 % Abdeckung
- `Rezepte.Web/Components/Settings/AiSettings.razor` — 0 % Abdeckung
- `Rezepte.Web/Components/Settings/ApplicationUpdates.razor` — 0 % Abdeckung
- `Rezepte.Web/Components/Settings/BackupRestore.razor` — 0 % Abdeckung
- `Rezepte.Web/Components/Settings/ExportData.razor` — 0 % Abdeckung
- `Rezepte.Web/Components/Settings/ExportFilesList.razor` — 0 % Abdeckung
- `Rezepte.Web/Components/Settings/PluginSettings.razor` — 0 % Abdeckung
- `Rezepte.Web/Components/Settings/UsageStats.razor` — 0 % Abdeckung
- `Rezepte.Web/Components/Settings/UserAdmin.razor` — 0 % Abdeckung
- `Rezepte.Web/Components/Settings/UserProfile.razor` — 0 % Abdeckung
- `Rezepte.Web/Components/Shared/AddRecipeToShoppingListDialog.razor` — 0 % Abdeckung
- `Rezepte.Web/Components/Shared/AssignToCookbooksOverlay.razor` — 0 % Abdeckung
- `Rezepte.Web/Components/Shared/CalendarEventDialog.razor` — 0 % Abdeckung
- `Rezepte.Web/Components/Shared/CreateRecipeDialog.razor` — 0 % Abdeckung
- `Rezepte.Web/Components/Shared/ImageCropper.razor` — 0 % Abdeckung
- `Rezepte.Web/Components/Shared/LatestRecipes.razor` — 0 % Abdeckung
- `Rezepte.Web/Components/Shared/MultiAssignToCookbooksOverlay.razor` — 0 % Abdeckung
- `Rezepte.Web/Components/Shared/PhotoOverlay.razor` — 0 % Abdeckung
- `Rezepte.Web/Components/Shared/RandomFromCookbooks.razor` — 0 % Abdeckung
- `Rezepte.Web/Components/Shared/RecipeSelectDialog.razor` — 0 % Abdeckung
- `Rezepte.Web/Controllers/AdminExportsController.cs` — 0 % Abdeckung
- `Rezepte.Web/Controllers/AdminUsersController.cs` — 0 % Abdeckung
- `Rezepte.Web/Controllers/CalendarController.cs` — 0 % Abdeckung
- `Rezepte.Web/Controllers/CookbooksController.cs` — 0 % Abdeckung
- `Rezepte.Web/Controllers/ExportsController.cs` — 0 % Abdeckung
- `Rezepte.Web/Controllers/ImportTestController.cs` — 0 % Abdeckung
- `Rezepte.Web/Controllers/JobsController.cs` — 0 % Abdeckung
- `Rezepte.Web/Controllers/SessionController.cs` — 0 % Abdeckung
- `Rezepte.Web/Controllers/SessionTokenController.cs` — 0 % Abdeckung
- `Rezepte.Web/Controllers/UpdatesController.cs` — 0 % Abdeckung
- `Rezepte.Web/Controllers/UserExportFilesController.cs` — 0 % Abdeckung
- `Rezepte.Web/Controllers/UserStatsController.cs` — 0 % Abdeckung
- `Rezepte.Web/Controllers/UsersController.cs` — 0 % Abdeckung
- `Rezepte.Web/Dto/UserStatsDto.cs` — 0 % Abdeckung
- `Rezepte.Web/Dtos/ScheduledRecipeDto.cs` — 0 % Abdeckung
- `Rezepte.Web/Models/CalendarEventDto.cs` — 0 % Abdeckung
- `Rezepte.Web/Services/AntiForgeryHandler.cs` — 0 % Abdeckung
- `Rezepte.Web/Services/ApiAuthHandler.cs` — 0 % Abdeckung
- `Rezepte.Web/Services/BackgroundJobs/Handlers/ExportAllJobHandler.cs` — 0 % Abdeckung
- `Rezepte.Web/Services/BackgroundJobs/Handlers/ExportUserJobHandler.cs` — 0 % Abdeckung
- `Rezepte.Web/Services/Import/GoogleQuotaClient.cs` — 0 % Abdeckung
- `Rezepte.Web/Services/Import/ImportedRecipePersister.cs` — 0 % Abdeckung
- `Rezepte.Web/Services/Import/TestRecipeImportService.cs` — 0 % Abdeckung
- `Rezepte.Web/Services/PdfGenerator.cs` — 0 % Abdeckung
- `Rezepte.Web/ViewModels/SettingsViewModel.cs` — 0 % Abdeckung
