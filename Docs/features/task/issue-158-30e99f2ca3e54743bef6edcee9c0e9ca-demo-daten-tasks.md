# Tasks: Demo-Daten bei Registrierung

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Datenmodell | `CreateDemoData`-Eigenschaft zu `RegisterRequest` hinzufügen | Erledigt | `AuthControllerTests.Register_ShouldCallUserServiceWithCreateDemoDataFromJson_WhenProvided` |
| 2 | Datenmodell | `CreateDemoData`-Eigenschaft zu `RegisterRequestForm` hinzufügen | Erledigt | Kein direkter Test (Record wird deklariert, nicht vom Controller gebunden) |
| 3 | Datenmodell | `DemoDataSeedPayload`-Record anlegen | Erledigt | `UserServiceTests.RegisterAsync_ShouldEnqueueDemoDataJob_WhenCreateDemoDataIsTrue` (serialisierte Payload wird enqueued); `DemoDataSeedingJobHandlerTests` (Deserialisierung via `FromJson`) |
| 4 | Datenmodell | `DemoCookbook`-Record anlegen | Erledigt | `DemoDataSeedingJobHandlerTests.HandleAsync_ShouldCreateFiveCookbooks` |
| 5 | Datenmodell | `DemoRecipe`-Record anlegen | Erledigt | `DemoDataSeedingJobHandlerTests.HandleAsync_ShouldCreateFortyThreeRecipes` |
| 6 | Logik | `DemoDataSource`-Klasse mit 43 Demo-Rezepten, Schritten und Zutaten anlegen | Erledigt | `DemoDataSeedingJobHandlerTests.HandleAsync_ShouldCreateFortyThreeRecipes` |
| 7 | Logik | `DemoDataSeedingJobHandler` als `IBackgroundJobHandler` implementieren | Erledigt | `DemoDataSeedingJobHandlerTests` (alle Tests) |
| 8 | Logik | `DemoDataSeedingJobHandler` in `ServiceCollectionExtensions.cs` registrieren | Erledigt | Kein direkter Test (`RegisterTests.Register_WithDemoData_CreatesDemoData` würde fehlschlagen, wenn der Handler nicht registriert wäre) |
| 9 | Logik | `IUserService.RegisterAsync`-Signatur um `bool createDemoData` erweitern | Erledigt | `UserServiceTests.RegisterAsync_ShouldEnqueueDemoDataJob_WhenCreateDemoDataIsTrue` |
| 10 | Logik | `UserService.RegisterAsync` um `createDemoData` annehmen und Job enqueuen erweitern | Erledigt | `UserServiceTests.RegisterAsync_ShouldEnqueueDemoDataJob_WhenCreateDemoDataIsTrue` / `RegisterAsync_ShouldNotEnqueueDemoDataJob_WhenCreateDemoDataIsFalse` |
| 11 | Logik | `AuthController.Register` um Auslesen von `createDemoData` aus Formular/JSON erweitern | Erledigt | `AuthControllerTests.Register_ShouldCallUserServiceWithCreateDemoDataTrue_WhenFormCheckboxChecked` / `...False_WhenFormCheckboxUnchecked` / `...FromJson_WhenProvided` |
| 12 | UI | Checkbox für Demo-Daten in `Register.razor` hinzufügen und an `RegisterModel.CreateDemoData` binden | Erledigt | `RegisterTests.Register_WithDemoData_CreatesDemoData` (E2E, Page-Object `CheckCreateDemoDataAsync`) |
| 13 | Tests | `UserServiceTests.RegisterAsync`-Aufrufe auf vier Argumente anpassen | Erledigt | Alle `UserServiceTests` kompilieren und laufen mit neuer Signatur |
| 14 | Tests | `UserServiceTests.RegisterAsync_ShouldEnqueueDemoDataJob_WhenCreateDemoDataIsTrue` hinzufügen | Erledigt | Test selbst |
| 15 | Tests | `UserServiceTests.RegisterAsync_ShouldNotEnqueueDemoDataJob_WhenCreateDemoDataIsFalse` hinzufügen | Erledigt | Test selbst |
| 16 | Tests | `AuthControllerTests` Moq-Setup und Aufrufe anpassen | Erledigt | `AuthControllerTests` (alle Register-Tests nutzen `It.IsAny<bool>()` bzw. konkrete bool-Werte) |
| 17 | Tests | `AuthControllerTests` für geprüfte/ungewählte Checkbox und JSON-API ergänzen | Erledigt | `Register_ShouldCallUserServiceWithCreateDemoDataTrue_WhenFormCheckboxChecked`, `...False_WhenFormCheckboxUnchecked`, `...FromJson_WhenProvided` |
| 18 | Tests | `DemoDataSeedingJobHandlerTests` anlegen (5 Kochbücher, 43 Rezepte, 5 Events, Einkaufsliste) | Erledigt | `HandleAsync_ShouldCreateFiveCookbooks`, `...FortyThreeRecipes`, `...FiveCalendarEvents`, `...AddIngredientsToDefaultShoppingList` |
| 19 | E2E-Tests | `Register_WithDemoData` in Browser-Suite anlegen | Erledigt | `RegisterTests.Register_WithDemoData_CreatesDemoData` |
| 20 | E2E-Tests | `Register_WithoutDemoData` in Browser-Suite anlegen | Erledigt | `RegisterTests.Register_WithoutDemoData_LeavesListsEmpty` |
| 21 | Tests | `DemoDataSeedingJobHandlerTests.HandleAsync_ShouldThrow_WhenUserDoesNotExist` anlegen | Erledigt | Test selbst |
| 22 | Tests | `DemoDataSeedingJobHandlerTests.HandleAsync_ShouldThrow_WhenAnyServiceOperationFails` anlegen | Erledigt | Test selbst |
| 23 | E2E-Tests | `RegisterPage` Page-Object in `Rezepte.Tests.Browser/Auth/RegisterPage.cs` anlegen | Erledigt | Wird von `RegisterTests` verwendet (`GotoAsync`, `Fill*Async`, `CheckCreateDemoDataAsync`, `SubmitAsync`, `GetErrorAsync`, `LoginAsync`) |
| 24 | E2E-Tests | `UniqueUsernameGenerator` Hilfsmethode für eindeutige Testbenutzer anlegen | Erledigt | `RegisterTests.UniqueUsernameGenerator` (`demo_e2e_{guid}_{timestamp}`) |
| 25 | E2E-Tests | `DemoDataWaitHelper` für Polling des asynchronen Seeding-Jobs anlegen | Erledigt | `RegisterTests.DemoDataWaitHelper.WaitForDemoDataAsync` (Polling der UI-Listen) |
