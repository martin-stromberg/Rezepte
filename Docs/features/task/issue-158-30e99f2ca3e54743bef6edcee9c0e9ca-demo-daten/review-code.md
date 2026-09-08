# Code-Review

Basisbranch: `origin/staging`. Iteration 3 (final) — die Implementierung liegt uncommitted im Working Tree des Branches `task/issue-158-30e99f2ca3e54743bef6edcee9c0e9ca-demo-daten`. Geprüft wurden alle geänderten und neu erstellten Quelldateien (inkl. untracked Dateien, da `git diff` gegen den Basisbranch nur committete Änderungen enthält). Alle Befunde aus Iterationen 1 und 2 wurden vollständig bearbeitet.

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### DemoRecipe.cs / DemoDataSource.cs (DemoRecipe, DemoDataSource)

- **Toter Code / Datenredundanz** — Das Record-Property `CookbookName` (`DemoRecipe.cs` Zeile 11) wird auf allen 43 Demo-Rezepten in `DemoDataSource.BuildCookbooks` gesetzt, aber nirgends gelesen: `DemoDataSeedingJobHandler.SeedRecipesAsync` iteriert über `DemoCookbook.Recipes` und verwendet ausschließlich `DemoCookbook.Name` (`DemoDataSeedingJobHandler.cs` Zeilen 117, 138). Die Zuordnung Rezept → Kochbuch ist bereits strukturell über die Gruppierung im `DemoCookbook` gegeben; das Feld ist redundante, divergierfähige Information.

  Empfehlung: Parameter `CookbookName` aus `DemoRecipe` entfernen und die Aufrufstellen in `DemoDataSource.cs` entsprechend um das Argument kürzen.

### RegisterTests.cs (RegisterTests)

- **Doppelter Code / fehlende Kapselung** — Die erwarteten Zählerwerte (5 Kochbücher, 43 Rezepte, 5 Termine, >0 Einkaufsliste) sind zweifach hartkodiert: einmal in `DemoDataWaitHelper.WaitForDemoDataAsync` (Zeile 166: `cookbooks >= 5 && recipes >= 43 && calendar >= 5 && shopping > 0`) und einmal in den anschließenden Assertions (Zeilen 64–67: `.Should().Be(5)`, `.Be(43)`, `.Be(5)`). Da der Wait-Helper bei Nichterfüllung bereits eine `TimeoutException` wirft, sind die nachfolgenden Assertions fachlich redundant bzw. prüfen dieselben Werte ein zweites Mal mit leicht anderer Semantik (`>=` vs. `==`). Bei jeder Änderung des Demo-Datensatzes müssen zwei Stellen synchron angepasst werden.

  Empfehlung: Die erwarteten Werte einmal als benannte Konstanten definieren (z. B. `ExpectedCookbookCount = 5`, `ExpectedRecipeCount = 43`, `ExpectedCalendarEventCount = 5`) und sowohl im Wait-Helper als auch in den Assertions verwenden — oder die Assertions nach einem erfolgreichen Wait ganz streichen.

### RegisterPage.cs (RegisterPage)

- **Verantwortlichkeit / Feature Envy** — `LoginAsync` (Zeilen 139–148) gehört fachlich nicht zum Page Object der Registrierungsseite: Die Methode navigiert auf `/login` und bedient die Selektoren `#username`, `#password` und `button.btn-accent[type='submit']` der Login-Seite. Das Page Object `RegisterPage` kennt damit die DOM-Struktur zweier verschiedener Seiten; eine Änderung am Login-Markup müsste in der Register-Page-Klasse nachgezogen werden.

  Empfehlung: Ein eigenes `LoginPage`-Page-Object (oder eine gemeinsame Basisklasse/eine Methode auf einem Auth-Helper) für den Login-Flow einführen und `LoginAsync` dorthin verschieben; `RegisterTests` ruft dann das entsprechende Objekt auf.

## Hinweise

- **RaiseUiActionRequested-Regel:** Die Codebasis enthält keinerlei `RaiseUiActionRequested`-Aufrufe (kein ViewModel-Pattern vorhanden); es gibt somit keine fehlenden Handler in Blazor-Seiten/-Komponenten.
- **`AuthController.RegisterRequestForm` (Zeile 104):** Der Record ist weiterhin deklariert, wird aber nicht gebunden (Form-Daten werden via `Request.ReadFormAsync`/`Request.Form` gelesen) — laut Plan bewusst „für Konsistenz" deklariert; bereits in Iteration 1/2 als bekannt dokumentiert, kein neuer Befund.
- **Form-Feld-Naming:** `Register.razor` verwendet `name="Username"`, `name="Password"`, `name="Email"` (PascalCase), aber `name="createDemoData"` (camelCase). Der Controller liest `form["createDemoData"]` konsistent dazu; funktional korrekt, nur stilistisch uneinheitlich (kein Befund).
- **Untracked Verzeichnis `updates/`:** Enthält weiterhin Laufzeit-/Statusdaten (`status.json`, `pending/`, `staging/`) und gehört nicht zur Anforderung — nicht committen; `.gitignore` ignoriert nur `/Rezepte.Web/updates/`, ggf. Root-Eintrag ergänzen.
- **`IUserService.RegisterAsync`-Signatur:** Der neue `bool createDemoData`-Parameter erhöht die Parameterliste auf vier — noch im akzeptablen Rahmen, kein Befund.
- **`DemoDataSeedPayload.FromJson`:** Liest nur `userId` (camelCase, passend zu `JsonSerializerDefaults.Web`); unbekannte/falsche Property-Namen resultieren in leerer `UserId` und werden im Handler validiert — korrekt behandelt.
- **Fehlerbehandlung:** Exceptions aus dem Handler werden vom `BackgroundJobHostedService` gefangen und der Job als `Failed` markiert — kein endloses Retry; Schlucken von Exceptions tritt nicht auf.

## Geprüfte Dateien

Liste aller geprüften Dateien:
- `Rezepte.Web/Components/Pages/Register.razor`
- `Rezepte.Web/Contracts/AuthDtos.cs`
- `Rezepte.Web/Controllers/AdminUsersController.cs`
- `Rezepte.Web/Controllers/AuthController.cs`
- `Rezepte.Web/Extensions/ServiceCollectionExtensions.cs`
- `Rezepte.Web/Resources/UiStrings.resx`
- `Rezepte.Web/Services/UserService.cs`
- `Rezepte.Web/Services/BackgroundJobs/DemoDataSeedPayload.cs`
- `Rezepte.Web/Services/BackgroundJobs/Handlers/DemoDataSeedingJobHandler.cs`
- `Rezepte.Web/Services/DemoCookbook.cs`
- `Rezepte.Web/Services/DemoDataSource.cs`
- `Rezepte.Web/Services/DemoRecipe.cs`
- `Rezepte.Tests/Controllers/AuthControllerTests.cs`
- `Rezepte.Tests/Services/UserServiceTests.cs`
- `Rezepte.Tests/Services/BackgroundJobs/DemoDataSeedingJobHandlerTests.cs`
- `Rezepte.Tests.Browser/Infrastructure/RezepteAppFixture.cs`
- `Rezepte.Tests.Browser/Infrastructure/RegisterTestsAppFixture.cs`
- `Rezepte.Tests.Browser/Infrastructure/RegisterTestCollection.cs`
- `Rezepte.Tests.Browser/Auth/RegisterPage.cs`
- `Rezepte.Tests.Browser/Auth/RegisterTests.cs`
