# Detail: Suchkette und Testlage

## Controller

`RecipesController` ist auf Klassenebene mit JWT-Authentifizierung geschuetzt. Viele Methoden folgen diesem Muster:

- `GetUserId()` aus `ClaimTypes.NameIdentifier` lesen
- bei fehlendem Benutzer `Unauthorized()` zurueckgeben
- `userId` an den `IRecipeService` uebergeben

Der Such-Endpunkt bei `RecipesController.cs:355` weicht davon ab. Er prueft keinen Benutzer und ruft bei `RecipesController.cs:367` `_recipes.SearchAsync(q, tags, cookbookId, page, pageSize, sort, ct)` auf. Die Service-Signatur erlaubt daher aktuell keinen benutzerbezogenen Suchscope.

## Service

`RecipeService.SearchAsync` startet bei `RecipeService.cs:431` mit `_db.Recipes.AsNoTracking()` und inkludiert Bilder, Schritte, Zutaten sowie Kochbuchzuordnungen. Danach wird bei vorhandenem Suchbegriff ein `Like`-Pattern aus dem getrimmten Query gebildet.

Durchsuchte Felder:

- `Recipe.Title`
- `Recipe.Description`
- `RecipeStep.Description`
- `RecipeIngredient.Name`

Nicht gefiltert wird aktuell:

- `Recipe.UserId`
- Tags, weil keine Tag-Entity modelliert ist

Der Cookbook-Filter nutzt `int? cookbookId` aus der API und vergleicht `cookbookId.Value.ToString()` mit `RecipeCookbook.CookbookId`. Das passt nur fuer numerische Kochbuch-IDs, waehrend die Entitaet `RecipeCookbook.CookbookId` als `string` definiert ist.

## UI

`RecipeSearch.razor` ruft bei `RunSearchAsync` die API per `GetFromJsonAsync<SearchResponseDto>` auf. Bei Exceptions wird keine Fehlermeldung angezeigt; die Seite setzt die Treffer auf leer und zeigt dadurch `Keine Treffer.`.

Der globale Einstieg ueber `MainLayout.razor` navigiert ebenfalls nach `/recipes/search?q=...`. Weitere Suchverwender existieren in `RecipeEdit.razor` und `CalendarEventDialog.razor`; beide sprechen denselben API-Endpunkt an und erwarten ein Antwortobjekt mit `Items` und `Total`.

## Tests

Vorhandene Rezepttests unter `Rezepte.Tests/Services` decken mehrere `RecipeService`-Operationen ab, aber keine Rezeptsuche:

- `CreateAsync`
- `UpdateAsync`
- `DeleteAsync`
- `GetByCookbookAsync`
- `GetAvailableForCookbookAsync`
- `AddExistingToCookbookAsync`
- Beilagenvalidierung und Reihenfolge
- Schritt- und Zutatenvalidierung

Fuer die Anforderung fehlt ein Test mit Datenaufbau:

- Benutzer A
- Rezepttitel `Honig - Senf - Sojamarinade`
- Suchbegriff `Honig`
- Erwartung: Ergebnisliste enthaelt dieses Rezept und `TotalCount` ist mindestens `1`

Ein zusaetzlicher Negativtest mit Benutzer B waere sinnvoll, falls die Reparatur den Benutzerfilter ergaenzt.
