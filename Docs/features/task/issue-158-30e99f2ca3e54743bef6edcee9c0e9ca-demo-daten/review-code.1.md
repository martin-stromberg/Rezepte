# Code-Review

Basisbranch: `origin/staging`. Geprüft wurden die im Working Tree geänderten bzw. neu erstellten Quelldateien des Branches `task/issue-158-30e99f2ca3e54743bef6edcee9c0e9ca-demo-daten`.

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### DemoDataSeedingJobHandler.cs (DemoDataSeedingJobHandler)

- **God-Methode** — `HandleAsync` (Zeilen 24–140, ~115 Zeilen) erledigt vier konzeptuell getrennte Aufgaben hintereinander: Cookbooks anlegen, Rezepte anlegen, Kalendereinträge erzeugen, Einkaufsliste befüllen.

  Empfehlung: In private Methoden auslagern, z. B. `SeedCookbooksAsync`, `SeedRecipesAsync`, `SeedCalendarAsync`, `SeedShoppingListAsync`; `HandleAsync` orchestriert nur noch.

- **Hardcodierte Werte / Magic Strings** — Der Job-Typ `"seed-demo-data"` ist als Literal sowohl in `DemoDataSeedingJobHandler.JobType` (Zeile 15) als auch in `UserService.cs` (Zeile 158) dupliziert. Der Kochbuchname `"Abendessen"` wird in Zeilen 86 und 117 hartkodiert, um Kalender- und Einkaufsdaten auszuwählen — eine Umbenennung in `DemoDataSource` bricht den Handler stillschweigend bzw. lässt die Kalender-/Einkaufsliste-Logik leer laufen. Zudem sind `Take(5)` (Zeile 93), `TimeSpan.FromHours(18)` (Zeile 101) und die Fallback-Portionszahl `2` (Zeile 102) unbenannte Magic Values.

  Empfehlung: Job-Typ als gemeinsame Konstante (z. B. `DemoDataSeedingJobHandler.JobTypeName` oder eine Konstantenklasse neben `IBackgroundJobHandler`) verwenden. Den Namen des Kalender-Kochbuchs als Konstante in `DemoDataSource` definieren und im Handler referenzieren. `5`, `18:00` und `2` als benannte Konstanten (z. B. `CalendarEventCount`, `DinnerTime`, `DefaultPortions`) deklarieren.

- **Fragile Zuordnung über Titel** — Zeilen 112–119: Das für die Einkaufsliste benötigte `DemoRecipe` wird über `r.Title == firstCalendarRecipe.Title` wieder aus `DemoDataSource` gesucht, obwohl die Zuordnung `DemoRecipe` → erstelltes `Recipe` beim Anlegen (Zeilen 70–90) bereits bekannt war. Titel-Duplikate oder Umbenennungen erzeugen falsche Zutaten.

  Empfehlung: Beim Erstellen der Rezepte Paare `(DemoRecipe, Recipe)` in einer Liste/Dictionary merken und für Kalender und Einkaufsliste direkt verwenden, statt per Titel erneut zu suchen.

### DemoDataSeedPayload.cs (DemoDataSeedPayload)

- **Dokumentation / Namenskonvention** — Die XML-Kommentare sind generierter Platzhaltertext ohne Aussage: „tos the json." (Zeile 15), „froms the json." (Zeile 21), `/// <returns>The result.</returns>` auf dem Record (Zeile 9). Zwar existiert dieser Stil bereits in `ExportJobPayload.cs`, für neuen Code sollten die Kommentare dennoch aussagekräftig oder weggelassen werden.

  Empfehlung: Kommentare korrigieren (z. B. `/// <summary>Serializes the payload to JSON.</summary>`) oder entfernen.

- **Fehlende Fehlerbehandlung bei ungültigem JSON** — `FromJson` (Zeile 32) wirft bei malformed `payloadJson` eine `JsonException` ohne Kontext. Der Handler bekommt so keinen Hinweis, dass das Payload-Format fehlerhaft ist.

  Empfehlung: `JsonDocument.Parse` in try/catch fassen und als `InvalidOperationException`/`FormatException` mit aussagekräftiger Meldung (inkl. Job-Kontext) weiterwerfen — analog zur bestehenden Prüfung auf leere `UserId` im Handler.

### AuthDtos.cs (RegisterRequest)

- **Wirkungsloses Validierungsattribut** — `[param: Required] bool CreateDemoData` (Zeile ~17): `Required` auf einem nicht-nullable `bool` ist bedeutungslos (bools können nicht `null` sein) und suggeriert eine Pflichtvalidierung, die es nicht gibt.

  Empfehlung: Attribut entfernen; `bool CreateDemoData = false` reicht als optionaler Parameter.

### DemoDataSeedingJobHandlerTests.cs (DemoDataSeedingJobHandlerTests)

- **Doppelter Code** — Die Mock-Setups für `IUserService`, `ICookbookService`, `IRecipeService`, `ICalendarService` und `IShoppingListService` sind in den Tests `HandleAsync_ShouldCreateFiveCookbooks`, `...FortyThreeRecipes`, `...FiveCalendarEvents` und `...AddIngredientsToDefaultShoppingList` nahezu identisch über je ~60 Zeilen wiederholt (insgesamt ~240 Zeilen Duplikat).

  Empfehlung: Gemeinsame Hilfsmethode (z. B. `CreateDefaultMocks(userId)` bzw. einen `HappyPath`-Fixture-Builder) einführen, die vorkonfigurierte Mocks zurückgibt; Tests verdrahten nur noch die jeweils relevante Verifikation.

## Hinweise

- **RaiseUiActionRequested-Regel:** Die Codebasis enthält keinerlei `RaiseUiActionRequested`-Aufrufe (kein ViewModel-Pattern vorhanden); die Regel ist damit erfüllt, keine fehlenden Handler.
- **Untracked Verzeichnis `updates/`:** Enthält Laufzeit-/Statusdaten (`status.json`, `pending/`, `staging/`) und gehört nicht zur Anforderung — sollte nicht committet und ggf. in `.gitignore` aufgenommen werden.
- **`AdminUsersController.cs` (Zeile 53):** `RegisterAsync(dto.Username, dto.Password, false, ct)` — das `false`-Literal ist ohne benanntes Argument schwer lesbar; `createDemoData: false` wäre klarer (geringfügig, kein separater Befund).

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
- `Rezepte.Tests.Browser/Auth/RegisterPage.cs`
- `Rezepte.Tests.Browser/Auth/RegisterTests.cs`
