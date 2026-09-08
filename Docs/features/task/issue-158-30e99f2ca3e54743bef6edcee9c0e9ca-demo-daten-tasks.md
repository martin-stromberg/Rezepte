# Tasks: Demo-Daten bei Registrierung

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Datenmodell | `CreateDemoData`-Eigenschaft zu `RegisterRequest` hinzufügen | Offen | — |
| 2 | Datenmodell | `CreateDemoData`-Eigenschaft zu `RegisterRequestForm` hinzufügen | Offen | — |
| 3 | Datenmodell | `DemoDataSeedPayload`-Record anlegen | Offen | — |
| 4 | Datenmodell | `DemoCookbook`-Record anlegen | Offen | — |
| 5 | Datenmodell | `DemoRecipe`-Record anlegen | Offen | — |
| 6 | Logik | `DemoDataSource`-Klasse mit 43 Demo-Rezepten, Schritten und Zutaten anlegen | Offen | — |
| 7 | Logik | `DemoDataSeedingJobHandler` als `IBackgroundJobHandler` implementieren | Offen | — |
| 8 | Logik | `DemoDataSeedingJobHandler` in `ServiceCollectionExtensions.cs` registrieren | Offen | — |
| 9 | Logik | `IUserService.RegisterAsync`-Signatur um `bool createDemoData` erweitern | Offen | — |
| 10 | Logik | `UserService.RegisterAsync` um `createDemoData` annehmen und Job enqueuen erweitern | Offen | — |
| 11 | Logik | `AuthController.Register` um Auslesen von `createDemoData` aus Formular/JSON erweitern | Offen | — |
| 12 | UI | Checkbox für Demo-Daten in `Register.razor` hinzufügen und an `RegisterModel.CreateDemoData` binden | Offen | — |
| 13 | Tests | `UserServiceTests.RegisterAsync`-Aufrufe auf vier Argumente anpassen | Offen | — |
| 14 | Tests | `UserServiceTests.RegisterAsync_ShouldEnqueueDemoDataJob_WhenCreateDemoDataIsTrue` hinzufügen | Offen | — |
| 15 | Tests | `UserServiceTests.RegisterAsync_ShouldNotEnqueueDemoDataJob_WhenCreateDemoDataIsFalse` hinzufügen | Offen | — |
| 16 | Tests | `AuthControllerTests` Moq-Setup und Aufrufe anpassen | Offen | — |
| 17 | Tests | `AuthControllerTests` für geprüfte/ungewählte Checkbox und JSON-API ergänzen | Offen | — |
| 18 | Tests | `DemoDataSeedingJobHandlerTests` anlegen (5 Kochbücher, 43 Rezepte, 5 Events, Einkaufsliste) | Offen | — |
| 19 | E2E-Tests | `Register_WithDemoData_ShouldCreateDemoData` in Browser-Suite anlegen | Offen | — |
| 20 | E2E-Tests | `Register_WithoutDemoData_ShouldNotCreateDemoData` in Browser-Suite anlegen | Offen | — |
| 21 | Tests | `DemoDataSeedingJobHandlerTests.HandleAsync_ShouldThrow_WhenUserDoesNotExist` anlegen | Offen | — |
| 22 | Tests | `DemoDataSeedingJobHandlerTests.HandleAsync_ShouldThrow_WhenAnyServiceOperationFails` anlegen | Offen | — |
| 23 | E2E-Tests | `RegisterPage` Page-Object in `Rezepte.Tests.Browser/Auth/RegisterPage.cs` anlegen | Offen | — |
| 24 | E2E-Tests | `UniqueUsernameGenerator` Hilfsmethode für eindeutige Testbenutzer anlegen | Offen | — |
| 25 | E2E-Tests | `DemoDataWaitHelper` für Polling des asynchronen Seeding-Jobs anlegen | Offen | — |
