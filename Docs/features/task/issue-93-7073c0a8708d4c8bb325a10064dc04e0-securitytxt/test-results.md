# Test-Ergebnisse

## Ergebnis

**Status:** Keine Fehler

## Fehlgeschlagene Tests

_Keine._

## Zusammenfassung

- Gesamt: 297
- Bestanden: 284
- Fehlgeschlagen: 0
- Übersprungen: 13

### Übersprungene Tests (Browser-Tests — kein Browser verfügbar)

- `Rezepte.Tests.Browser.SecurityTxtBrowserTests.RegularUser_DoesNotSeeSecurityTxtMenuItemInSettings`
- `Rezepte.Tests.Browser.SecurityTxtBrowserTests.GetSecurityTxt_ReturnsNotFound_WhenDisabled`
- `Rezepte.Tests.Browser.SecurityTxtBrowserTests.Admin_CanConfigureSecurityTxtViaUi_AndContentAppearsInPublicEndpoint`
- `Rezepte.Tests.Browser.SecurityTxtBrowserTests.GetSecurityTxt_ReturnsOk_WithoutAuthentication_WhenEnabled`
- `Rezepte.Tests.Browser.LoadingBarVisibilityBrowserTests.LinkClick_WithDelayedResponse_MakesLoadingBarVisible`
- `Rezepte.Tests.Browser.LoadingBarVisibilityBrowserTests.AfterNavigationCompleted_HidesLoadingBar`
- `Rezepte.Tests.Browser.LoadingBarVisibilityBrowserTests.LinkClick_WithDelayedResponse_ActivatesLoadingBar`
- `Rezepte.Tests.Browser.LoadingBarSafetyTimeoutBrowserTests.WhenNavigationNeverCompletes_HidesBarAfterMaxVisibleDuration`
- `Rezepte.Tests.Browser.LoadingBarFormNavigationBrowserTests.SearchSubmit_ShowsLoadingBar`
- `Rezepte.Tests.Browser.LoadingBarFormNavigationBrowserTests.InteractiveFormSubmitWithoutNavigation_DoesNotActivateLoadingBar`
- `Rezepte.Tests.Browser.LoadingBarDisabledBrowserTests.WhenFeatureDisabled_PageContainsNoLoadingBarElement`
- `Rezepte.Tests.Browser.LoadingBarColorBrowserTests.SecondClickDuringRunningAnimation_ChangesColor`
- `Rezepte.Tests.Browser.LoadingBarColorBrowserTests.LinkClick_UsesColorFromConfiguredPalette`

## Testabdeckung

**Abdeckung:** 59,4 %
Quelle: `Coverage-Daten` (XPlat Code Coverage / Cobertura, `Rezepte.Tests`)

Die nachfolgende Tabelle enthält alle Quelldateien mit einer gemessenen Abdeckung unter 80 %. Razor-Komponenten und Migrations-Dateien werden mit aufgeführt, da sie Teil der Abdeckungsmessung sind. Generierte Dateien (`*.g.*`) und `Program.cs` werden ausgeblendet.

| Datei | Abdeckung |
|-------|-----------|
| `Rezepte.Web/ViewModels/UserProfileViewModel.cs` | 0 % |
| `Rezepte.Web/Services/RecipeService.cs` | 0 % |
| `Rezepte.Web/Services/SettingsService.cs` | 0 % |
| `Rezepte.Web/Services/ShoppingListService.cs` | 0 % |
| `Rezepte.Web/Services/TokenService.cs` | 0 % |
| `Rezepte.Web/Middleware/RedirectToRegisterMiddleware.cs` | 0 % |
| `Rezepte.Web/Extensions/WebApplicationExtensions.cs` | 0 % |
| `Rezepte.Web/Services/UserService.cs` | 0 % |
| `Rezepte.Web/Extensions/LoggingExtensions.cs` | 0 % |
| `Rezepte.Web/ViewModels/UserAdminViewModel.cs` | 0 % |
| `Rezepte.Web/Entities/RecipeImage.cs` | 0 % |
| `Rezepte.Web/Entities/CalendarEvent.cs` | 0 % |
| `Rezepte.Web/Dtos/ScheduledRecipeDto.cs` | 0 % |
| `Rezepte.Web/Dto/UserStatsDto.cs` | 0 % |
| `Rezepte.Web/Controllers/UserStatsController.cs` | 0 % |
| `Rezepte.Web/Controllers/UsersController.cs` | 0 % |
| `Rezepte.Web/Controllers/SettingsController.cs` | 0 % |
| `Rezepte.Web/Controllers/SessionTokenController.cs` | 0 % |
| `Rezepte.Web/Controllers/SessionController.cs` | 0 % |
| `Rezepte.Web/Controllers/RecipesController.cs` | 0 % |
| `Rezepte.Web/Controllers/JobsController.cs` | 0 % |
| `Rezepte.Web/Services/Import/ParsedIngredient.cs` | 0 % |
| `Rezepte.Web/Services/PdfGenerator.cs` | 0 % |
| `Rezepte.Web/Controllers/ExportsController.cs` | 0 % |
| `Rezepte.Web/Services/Import/TestRecipeImportService.cs` | 0 % |
| `Rezepte.Web/Services/Import/Plugins/PluginUpdateHostedService.cs` | 0 % |
| `Rezepte.Web/Services/Import/ImportedRecipePersister.cs` | 0 % |
| `Rezepte.Web/Services/Import/GoogleQuotaClient.cs` | 0 % |
| `Rezepte.Web/Services/Import/GeminiClient.cs` | 0 % |
| `Rezepte.Web/Services/Import/BaseAIImportHandler.cs` | 0 % |
| `Rezepte.Web/Services/ExportService.cs` | 0 % |
| `Rezepte.Web/Services/CurrentUserAccessor.cs` | 0 % |
| `Rezepte.Web/Services/CookbookService.cs` | 0 % |
| `Rezepte.Web/Services/CalendarService.cs` | 0 % |
| `Rezepte.Web/Services/BackgroundJobs/Handlers/ExportUserJobHandler.cs` | 0 % |
| `Rezepte.Web/Services/BackgroundJobs/Handlers/ExportAllJobHandler.cs` | 0 % |
| `Rezepte.Web/Services/Import/Plugins/DataProtectionSystemSecretStore.cs` | 0 % |
| `Rezepte.Web/Services/BackgroundJobs/BackgroundJobQueue.cs` | 0 % |
| `Rezepte.Web/Services/BackgroundJobs/BackgroundJobHostedService.cs` | 0 % |
| `Rezepte.Web/Services/ApiClient.cs` | 0 % |
| `Rezepte.Web/Services/ApiAuthHandler.cs` | 0 % |
| `Rezepte.Web/Services/AntiForgeryHandler.cs` | 0 % |
| `Rezepte.Web/Services/AiUsageService.cs` | 0 % |
| `Rezepte.Web/Models/CalendarEventDto.cs` | 0 % |
| `Rezepte.Web/Services/Import/Plugins/PluginManager.cs` | 0 % |
| `Rezepte.Web/Services/Import/Plugins/PluginSettingsService.cs` | 0 % |
| `Rezepte.Web/Services/Import/Plugins/PluginStartupService.cs` | 0 % |
| `Rezepte.Web/Services/Import/Plugins/PluginUpdateService.cs` | 0 % |
| `Rezepte.Web/Controllers/CookbooksController.cs` | 0 % |
| `Rezepte.Web/Controllers/ImportTestController.cs` | 0 % |
| `Rezepte.Web/Controllers/AuthController.cs` | 0 % |
| `Rezepte.Web/Controllers/CalendarController.cs` | 0 % |
| `Rezepte.Web/Components/Pages/Register.razor` | 0 % |
| `Rezepte.Web/Components/Pages/RecipeSearch.razor` | 0 % |
| `Rezepte.Web/Components/Pages/RecipePage.razor` | 0 % |
| `Rezepte.Web/Components/Pages/RecipeEdit.razor` | 0 % |
| `Rezepte.Web/Components/Pages/Login.razor` | 0 % |
| `Rezepte.Web/Components/Pages/Home.razor` | 0 % |
| `Rezepte.Web/Components/Pages/Error.razor` | 0 % |
| `Rezepte.Web/Components/Pages/Cookbooks.razor` | 0 % |
| `Rezepte.Web/Components/Pages/Settings.razor` | 0 % |
| `Rezepte.Web/Components/Pages/CookbookPage.razor` | 0 % |
| `Rezepte.Web/Components/Pages/Calendar.razor` | 0 % |
| `Rezepte.Web/Components/Layout/MainLayout.razor` | 0 % |
| `Rezepte.Web/ApiAuthHandler.cs` | 0 % |
| `Rezepte.Import.PluginSdk/UrlHelpers.cs` | 0 % |
| `Rezepte.Import.PluginSdk/ParsedIngredient.cs` | 0 % |
| `Rezepte.Import.PluginSdk/ImportParserBase.cs` | 0 % |
| `Rezepte.Web/ViewModels/SettingsViewModel.cs` | 0 % |
| `Rezepte.Import.Plugins.AIUrl/AIUrlImportHandler.cs` | 0 % |
| `Rezepte.Import.Plugins.AIFoto/AIFotoImportHandler.cs` | 0 % |
| `Rezepte.Web/Components/Pages/CookbookDetails.razor` | 0 % |
| `Rezepte.Web/Components/Pages/ShoppingList.razor` | 0 % |
| `Rezepte.Web/Components/Pages/ScheduledRecipes.razor` | 0 % |
| `Rezepte.Web/Components/Settings/BackupRestore.razor` | 0 % |
| `Rezepte.Web/Controllers/AdminUsersController.cs` | 0 % |
| `Rezepte.Web/Controllers/AdminExportsController.cs` | 0 % |
| `Rezepte.Web/Contracts/UserDtos.cs` | 0 % |
| `Rezepte.Web/Contracts/AuthDtos.cs` | 0 % |
| `Rezepte.Web/Configuration/ImageOptions.cs` | 0 % |
| `Rezepte.Web/Components/Shared/RecipeSelectDialog.razor` | 0 % |
| `Rezepte.Web/Components/Shared/RandomFromCookbooks.razor` | 0 % |
| `Rezepte.Web/Components/Shared/PhotoOverlay.razor` | 0 % |
| `Rezepte.Web/Components/Shared/MultiAssignToCookbooksOverlay.razor` | 0 % |
| `Rezepte.Web/Components/Shared/LatestRecipes.razor` | 0 % |
| `Rezepte.Web/Components/Settings/AiSettings.razor` | 0 % |
| `Rezepte.Web/Components/Shared/ImageCropper.razor` | 0 % |
| `Rezepte.Web/Services/Import/ImportParserBase.cs` | 0 % |
| `Rezepte.Web/Components/Shared/CalendarEventDialog.razor` | 0 % |
| `Rezepte.Web/Components/Shared/AssignToCookbooksOverlay.razor` | 0 % |
| `Rezepte.Web/Components/Shared/AddRecipeToShoppingListDialog.razor` | 0 % |
| `Rezepte.Web/Components/Settings/UserProfile.razor` | 0 % |
| `Rezepte.Web/Components/Settings/UserAdmin.razor` | 0 % |
| `Rezepte.Web/Components/Settings/UsageStats.razor` | 0 % |
| `Rezepte.Web/Components/Settings/SecurityTxtSettings.razor` | 0 % |
| `Rezepte.Web/Components/Settings/PluginSettings.razor` | 0 % |
| `Rezepte.Web/Components/Settings/ExportData.razor` | 0 % |
| `Rezepte.Web/Components/Shared/CreateRecipeDialog.razor` | 0 % |
| `Rezepte.Web/Services/Import/BaseImportHandler.cs` | 6 % |
| `Rezepte.Tests.PluginFixture/TestImportPlugin.cs` | 14 % |
| `Rezepte.Web/Services/Import/Plugins/PluginSettingsItem.cs` | 23 % |
| `Rezepte.Web/Services/BackgroundJobs/ExportJobFileStore.cs` | 29 % |
| `Rezepte.Web/Services/Import/Plugins/IPluginManager.cs` | 33 % |
| `Rezepte.Import.Abstractions/ImportCollectionModels.cs` | 40 % |
| `Rezepte.Web/Services/BackgroundJobs/BackgroundJob.cs` | 45 % |
| `Rezepte.Web/Services/Import/Plugins/GitHubReleaseClient.cs` | 46 % |
| `Rezepte.Web/Extensions/ServiceCollectionExtensions.cs` | 50 % |
| `Rezepte.Web/Services/Import/Plugins/PluginPackageValidator.cs` | 54 % |
| `Rezepte.Import.Plugins.Backup/BackupImportPlugin.cs` | 59 % |
| `Rezepte.Web/Services/Import/ImportOrchestrator.cs` | 65 % |
| `Rezepte.Web/Services/Import/Plugins/GitHubRepository.cs` | 70 % |
| `Rezepte.Web/Services/Import/ImportService.cs` | 72 % |
| `Rezepte.Web/Services/Import/Plugins/PluginPackageInstaller.cs` | 72 % |
| `Rezepte.Web/Services/Import/ImportExceptionHelper.cs` | 75 % |
| `Rezepte.Web/Services/BackgroundJobs/ExportJobPayload.cs` | 76 % |
| `Rezepte.Web/Entities/ShoppingListGroup.cs` | 78 % |

## Fehlende Tests

Die niedrige Gesamtabdeckung (59,4 %) ist strukturell bedingt: Blazor-Razor-Komponenten, MVC-Controller und serverseitige Infrastruktur-Services sind ausschließlich durch Browser-Tests abgedeckt, die in dieser Umgebung übersprungen werden. Die 13 übersprungenen Browser-Tests (davon 4 für `SecurityTxtSettings`) würden bei Ausführung den Großteil der 0 %-Dateien abdecken.

Folgende feature-relevante Dateien haben 0 % Abdeckung durch Unit-Tests:

- `Rezepte.Web/Components/Settings/SecurityTxtSettings.razor` — 0 % Abdeckung (nur Browser-Tests vorhanden, alle übersprungen)
- `Rezepte.Web/Controllers/SettingsController.cs` — 0 % Abdeckung (keine Testdatei gefunden)
