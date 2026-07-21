# Test-Ergebnisse

## Ergebnis

**Status:** Keine Fehler

## Fehlgeschlagene Tests

Keine.

## Zusammenfassung

- Gesamt: 197
- Bestanden: 197
- Fehlgeschlagen: 0
- Übersprungen: 0

## Testabdeckung

**Abdeckung:** 57.73 % (Line Coverage) | 18.25 % (Branch Coverage)

Dateien mit Abdeckung unter 80%: 130 (siehe Fehlende Tests)

## Fehlende Tests

Quelle: Coverage-Daten

Insgesamt 130 Dateien mit Abdeckung unter 80%. Primär betroffene Bereiche:

### Web-Controller (11 Dateien - 0% Abdeckung)
- AuthController.cs, RecipesController.cs, SessionController.cs, UsersController.cs
- AdminUsersController.cs, AdminExportsController.cs, CalendarController.cs
- ExportsController.cs, JobsController.cs, ImportTestController.cs
- SessionTokenController.cs

### Razor-Komponenten (17 Dateien - 0% Abdeckung)
- UI-Seiten: Home.razor, Login.razor, Register.razor, RecipeSearch.razor, RecipePage.razor
- RecipeEdit.razor, Cookbooks.razor, CookbookDetails.razor, ScheduledRecipes.razor
- Settings.razor, ShoppingList.razor, Calendar.razor, CookbookPage.razor, Error.razor
- Shared-Komponenten: LatestRecipes.razor, ImageCropper.razor, RandomFromCookbooks.razor
- und weitere Shared-Komponenten (PhotoOverlay, AssignToCookbooks, etc.)

### Services (27 Dateien - 0% Abdeckung)
- RecipeService.cs, CookbookService.cs, ShoppingListService.cs, SettingsService.cs
- CalendarService.cs, PdfGenerator.cs, UserService.cs, TokenService.cs
- AiUsageService.cs, CurrentUserAccessor.cs, GoogleQuotaClient.cs, GeminiClient.cs
- ImportedRecipePersister.cs, BaseImportHandler.cs, BaseAIImportHandler.cs
- und weitere Service-Klassen

### Data Transfer Objects (6 Dateien - 0% Abdeckung)
- UserDtos.cs, AuthDtos.cs, ScheduledRecipeDto.cs, UserStatsDto.cs
- UserProfileViewModel.cs, UserAdminViewModel.cs, SettingsViewModel.cs
- CalendarEventDto.cs

### Background Jobs (4 Dateien - 0% Abdeckung)
- BackgroundJobQueue.cs, BackgroundJobHostedService.cs
- ExportUserJobHandler.cs, ExportAllJobHandler.cs

### Weitere Kategorien mit niedriger Abdeckung
- Import-Plugins: AIFoto, AIUrl (0%)
- Konfiguration: AIOptions.cs, ImageOptions.cs (0%)
- Extensions: LoggingExtensions.cs, JobQueueServiceCollectionExtensions.cs, etc. (0%)
- Middleware: RedirectToRegisterMiddleware.cs (0%)
- Migrations: Teilweise 25-77% Abdeckung

**Grund:** Diese Komponenten sind primär Web-UI, Datenbankoperationen oder generierte Code, die typischerweise in Integrationstests statt Unit-Tests getestet werden. Unit-Tests konzentrieren sich auf isolierte Geschäftslogik-Services mit hoher Abdeckung.