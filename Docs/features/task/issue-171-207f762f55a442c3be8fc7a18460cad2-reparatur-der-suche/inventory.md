# Bestandsaufnahme: Reparatur der Rezeptsuche (Issue #171)

Bestandsaufnahme des fachlichen Bereichs „Rezeptsuche" (Seite `/recipes/search`, API-Endpunkt `GET /api/recipes/search`, `RecipeService.SearchAsync` und alle Aufrufer) bezogen auf die übersetzte Anforderung in `requirement.md`.

## Zusammenfassung

- Die komplette Suchkette ist vorhanden und funktional implementiert: Navbar-Suchformular (`MainLayout.razor`) → SSR-Seite `RecipeSearch.razor` → serverseitiger HTTP-Self-Call via `ApiClient` (`CircuitAuthHandler` setzt Bearer-JWT aus `TokenService`) → `RecipesController.SearchAsync` → `RecipeService.SearchAsync` (EF Core, `EF.Functions.Like` auf SQLite).
- `RecipeSearch.razor` ist die **einzige inhaltliche routbare Seite ohne `@rendermode InteractiveServer`** (nur `Login.razor`, `Register.razor`, `Error.razor` — bewusst statische Seiten — verzichten ebenfalls darauf). Die `@onclick`-Paginierungsbuttons und die `@onsubmit`-Suche sind damit im ausgelieferten HTML inert; `pageNumber`/`pageSize` werden ohne Query-Werte mit `0` gebunden.
- In `RunSearchAsync` (`RecipeSearch.razor:136-139`) wird `OperationCanceledException` (eigene `CancellationTokenSource`, 10 s Timeout) nur per `LogInformation` protokolliert — die UI zeigt danach `NoSearchResults` („Keine Treffer."), der Fehlerfall ist von einer echten leeren Treffermenge nicht unterscheidbar.
- `RecipeSearch.razor` und `CalendarEventDialog.razor` rufen den Endpunkt mit **absoluter** URI `/api/recipes/search` auf; `RecipeEdit.razor` und `RecipeSelectDialog.razor` verwenden **relative** URIs (`api/…`) — uneinheitlich, relevant bei PathBase/Reverse-Proxy-Betrieb.
- `RecipeService.SearchAsync` durchsucht `Recipe.Title`, `Recipe.Description`, `RecipeStep.Description` und `RecipeIngredient.Name` per `EF.Functions.Like` (nur ASCII case-insensitiv auf SQLite); der Parameter `tags` ist im Service vorhanden, wird aber ignoriert (kein Tag-Modell vorhanden, Code-Kommentar).
- `IRecipeService` ist in `RecipeService.cs` (Zeilen 16–242) definiert; die Datei `Rezepte.Web/Services/IRecipeService.cs` enthält nur eine `using`-Zeile (Leerdatei).
- Test-Ausgangszustand: **Alle Tests grün** — `Rezepte.Tests`: 590 bestanden, `Rezepte.Tests.Browser`: 15 bestanden, 0 Fehler, 0 übersprungen (Nachweis: [tests.md](inventory/tests.md)). Es existieren drei `SearchAsync`-Service-Tests (alle auf dem **InMemory**-Provider); kein Test für `RecipesController.SearchAsync`, kein Komponenten-Test für `RecipeSearch.razor` und kein dedizierter E2E-Test der Suchergebnisse — die Browser-Tests nutzen die Seite nur indirekt (Navbar-Submit, Rezeptzählung). Provider-Unterschiede (InMemory vs. SQLite) sind damit ungetestet.

## Details

- [Datenmodell](inventory/models.md)
- [Logik](inventory/logic.md)
- [Komponenten (UI)](inventory/components.md)
- [Interfaces](inventory/interfaces.md)
- [Tests](inventory/tests.md)
