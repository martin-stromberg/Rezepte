# Code-Review

Basisbranch: `origin/staging` (Merge-Base `061f43b`). Iteration 2 — die Implementierung liegt uncommitted im Working Tree des Branches `task/issue-158-30e99f2ca3e54743bef6edcee9c0e9ca-demo-daten`. Geprüft wurden alle geänderten und neu erstellten Quelldateien (inkl. untracked Dateien, da `git diff` gegen den Basisbranch nur committete Änderungen enthält).

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### DemoDataSource.cs (DemoDataSource)

- **Toter Code / Speculative Generality** — Die statische Property `Recipes` (Zeile 28, `Cookbooks.SelectMany(c => c.Recipes)...`) wird nirgends in der Codebasis aufgerufen; der Handler iteriert ausschließlich über `DemoDataSource.Cookbooks`.

  Empfehlung: Property entfernen, solange kein Aufrufer existiert.

- **Platzhalter-Dokumentation / Primitive Obsession (Data Clump)** — `BuildCookbooks` (Zeilen 32–93) definiert die Demo-Daten als anonymes Tupel-Array `(string Cookbook, string Title, string? Description, int Portions, IReadOnlyList<RecipeCreateStep>? Steps)` mit fünf Positions-Elementen und überwiegend `null`-Literalen; die Struktur `DemoRecipe` existiert bereits, wird aber erst am Ende (Zeile 95) nachträglich gemappt. Das erschwert Lesbarkeit und Erweiterbarkeit (z. B. hat nur ein Eintrag Zutaten, erkennbar nur anhand der Position des fünften Tupel-Elements).

  Empfehlung: Die Rohdaten direkt als `DemoRecipe[]` bzw. pro Kochbuch gruppiert deklarieren (z. B. `new DemoCookbook("Frühstück", new DemoRecipe[]{ ... })`) und den Tupel-Zwischenschritt streichen.

### UserService.cs (UserService)

- **Toter Code** — `using System.Text.Json;` (Zeile 8) wurde hinzugefügt, wird aber nicht verwendet (die Serialisierung läuft über `DemoDataSeedPayload.ToJson()`).

  Empfehlung: Using-Direktive entfernen.

### DemoCookbook.cs / DemoRecipe.cs (DemoCookbook, DemoRecipe)

- **Platzhalter-XML-Kommentare** — Generierter Text ohne Aussage: `/// <param name="Name">The name parameter.</param>`, `/// <returns>The result.</returns>` (DemoCookbook.cs Zeilen 6–8; DemoRecipe.cs Zeilen 6–11). Dasselbe Problem wurde in Iteration 1 an `DemoDataSeedPayload.cs` beanstandet und dort korrigiert — hier besteht es weiter.

  Empfehlung: Kommentare aussagekräftig formulieren (z. B. `<param name="Name">The display name of the demo cookbook.</param>`) oder entfernen; `<returns>` auf einem Record ist ohnehin fehl am Platz.

## Hinweise

- **RaiseUiActionRequested-Regel:** Die Codebasis enthält keinerlei `RaiseUiActionRequested`-Aufrufe (kein ViewModel-Pattern vorhanden); es gibt somit keine fehlenden Handler in Blazor-Seiten/-Komponenten.
- **Befunde aus Iteration 1 — erledigt:** God-Methode `HandleAsync` wurde in `SeedCookbooksAsync`/`SeedRecipesAsync`/`SeedCalendarAsync`/`SeedShoppingListAsync` aufgeteilt; `JobTypeName`- und `CalendarCookbookName`-Konstanten eingeführt; Magic Values (`CalendarEventCount`, `DefaultPortions`, `DinnerTimeOfDay`) benannt; die fragile Titel-Suche durch `SeededRecipe`-Paare ersetzt; `DemoDataSeedPayload.FromJson` wirft nun `InvalidOperationException` mit Kontext; das wirkungslose `[Required]` auf `bool CreateDemoData` wurde entfernt; die Mock-Duplikate in den Handler-Tests sind über `CreateHappyPathMocks`/`BuildProvider` zentralisiert.
- **`RegisterTests.cs` (Zeile 106):** `UniqueUsernameGenerator()` liest sich als Substantiv — `GenerateUniqueUsername()` wäre namenskonformer (geringfügig, kein separater Befund).
- **`AdminUsersController.cs`:** `createDemoData: false` ist jetzt benannt — Hinweis aus Iteration 1 umgesetzt.
- **Untracked Verzeichnis `updates/`:** Enthält weiterhin Laufzeit-/Statusdaten und gehört nicht zur Anforderung — nicht committen bzw. ggf. in `.gitignore` aufnehmen.

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
