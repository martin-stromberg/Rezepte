# Bestandsaufnahme: Suche nach Rezepten reparieren

## Kontext

Die Anforderung beschreibt, dass eine Suche nach `Honig` das vorhandene Rezept `Honig - Senf - Sojamarinade` nicht in der Ergebnisliste anzeigt. Untersucht wurden die fokussierten Dateien:

- `Rezepte.Web/Controllers/RecipesController.cs`
- `Rezepte.Web/Services/RecipeService.cs`
- `Rezepte.Web/Components/Pages/RecipeSearch.razor`
- Tests unter `Rezepte.Tests`

Detail: [Suchkette und Testlage](inventory/search-flow-and-tests.md)

## Aktueller Suchfluss

1. Die Blazor-Seite `RecipeSearch.razor` ist unter `/recipes/search` erreichbar und uebernimmt den Query-Parameter `q`.
2. Bei Parameterwechsel ruft `RunSearchAsync` den API-Endpunkt `/api/recipes/search?q=...&page=...&pageSize=...` auf.
3. `RecipesController.SearchAsync` delegiert direkt an `IRecipeService.SearchAsync`.
4. `RecipeService.SearchAsync` sucht per `EF.Functions.Like` in Rezepttitel, Beschreibung, Schrittbeschreibung und Zutatennamen.
5. Die API gibt ein Objekt mit `Items` und `Total` zurueck; die Seite zeigt `Items` als Ergebnisliste an.

## Relevante Befunde

### Benutzerkontext fehlt in der Suche

`RecipesController` ist insgesamt JWT-geschuetzt und die meisten Rezept-Endpunkte lesen `GetUserId()` aus den Claims, pruefen `Unauthorized()` und uebergeben `userId` an den Service. `SearchAsync` tut das nicht. Auch `IRecipeService.SearchAsync` hat keinen `userId`-Parameter.

Auswirkung: Die Suche arbeitet nicht entlang derselben Besitzlogik wie `GetByCookbookAsync`, `GetByIdAsync`, `GetLatestAsync` usw. Das ist mindestens inkonsistent und kann sowohl zu falschen Treffermengen als auch zu Datenschutzproblemen fuehren. Fuer die Reparatur sollte die Suche wie die anderen Rezeptoperationen auf den angemeldeten Benutzer eingeschraenkt werden.

### Cookbook-Filter passt nicht zum Datenmodell

Der Search-Endpoint nimmt `cookbookId` als `int?` entgegen. Das Datenmodell speichert `RecipeCookbook.CookbookId` jedoch als `string`, und andere Endpunkte verwenden ebenfalls `string cookbookId`.

Auswirkung: Falls Suchaufrufe mit Kochbuchkontext stattfinden, koennen GUID/string-basierte Kochbuch-IDs nicht korrekt gebunden oder gefiltert werden. Das ist nicht der Kernfall `Honig`, aber ein naheliegender Defekt im selben Suchpfad.

### Suchfelder decken den geforderten Kernfall fachlich ab

Die Service-Suche prueft bereits `r.Title`, `r.Description`, `RecipeStep.Description` und `RecipeIngredient.Name`. Ein Titel `Honig - Senf - Sojamarinade` sollte bei `q=Honig` nach der aktuellen Suchbedingung grundsaetzlich matchen, sofern das Rezept im Query-Scope enthalten ist und der Provider `Like` entsprechend uebersetzt.

Auswirkung: Der wahrscheinlichere Fehler liegt nicht in fehlender Titelsuche, sondern im Scope, API-Vertrag, Datenzugriff oder Testabdeckung.

### UI verschluckt Suchfehler als leere Trefferliste

`RecipeSearch.razor` setzt bei Timeout oder Exception `results` und `totalItems` auf leer bzw. `0` und zeigt danach `Keine Treffer.`. Dadurch koennen API-, Auth-, Serialisierungs- oder Providerfehler fuer Nutzer wie eine erfolglose Suche aussehen.

Auswirkung: Der beobachtete Zustand "keine passenden Ergebnisse" kann auch durch einen Fehler im Suchaufruf verursacht sein. Fuer die eigentliche Reparatur ist wichtig, mindestens mit Service-/Controller-Tests abzusichern, dass der Backend-Suchpfad fuer `Honig` Treffer liefert.

### Tests decken die Rezeptsuche aktuell nicht ab

Unter `Rezepte.Tests` wurden Service-Tests fuer Erstellen, Aktualisieren, Loeschen, Kochbuchzuordnung, Beilagen und Validierung gefunden. Direkte Tests fuer `RecipeService.SearchAsync`, `RecipesController.SearchAsync` oder die konkrete Suche nach `Honig` sind nicht vorhanden.

Auswirkung: Die Anforderung sollte mit einem fokussierten Test abgesichert werden, der ein Rezept `Honig - Senf - Sojamarinade` anlegt und fuer `q=Honig` genau diesen Treffer erwartet.

## Betroffene Stellen

- `Rezepte.Web/Controllers/RecipesController.cs:355`: API-Endpunkt `SearchAsync`
- `Rezepte.Web/Controllers/RecipesController.cs:367`: Delegation an `_recipes.SearchAsync(...)` ohne Benutzerkontext
- `Rezepte.Web/Services/RecipeService.cs:35`: Interface-Signatur `SearchAsync(...)` ohne `userId`
- `Rezepte.Web/Services/RecipeService.cs:428`: Service-Implementierung `SearchAsync(...)`
- `Rezepte.Web/Services/RecipeService.cs:431`: Basisquery auf `_db.Recipes` ohne `UserId`-Filter
- `Rezepte.Web/Services/RecipeService.cs:444`: Titelsuche via `EF.Functions.Like(r.Title, pattern)`
- `Rezepte.Web/Services/RecipeService.cs:454`: Umwandlung von `int? cookbookId` in String
- `Rezepte.Web/Components/Pages/RecipeSearch.razor:120`: UI ruft `/api/recipes/search` auf
- `Rezepte.Web/Components/Pages/RecipeSearch.razor:129`: Abgebrochene Suche wird als leere Trefferliste behandelt
- `Rezepte.Web/Components/Pages/RecipeSearch.razor:130`: Sonstige Exceptions werden als leere Trefferliste behandelt

## Risiken fuer die Umsetzung

- Eine Aenderung der Service-Signatur betrifft alle bestehenden Aufrufer von `SearchAsync`, insbesondere Controller und Komponenten, die DTOs der Suchantwort erwarten.
- Der Cookbook-Parameter ist ein Vertragsproblem. Eine Korrektur von `int?` auf `string?` kann vorhandene Aufrufer betreffen, passt aber zum uebrigen Datenmodell.
- `EF.Functions.Like` kann je nach Datenbank Case-Sensitivity unterschiedlich behandeln. Der konkrete Akzeptanzfall nutzt identische Gross-/Kleinschreibung (`Honig`), sollte also ohne zusaetzliche Normalisierung testbar sein.

## Empfehlung fuer die Planung

- `IRecipeService.SearchAsync` um `userId` erweitern und in der Query mit `r.UserId == userId` filtern.
- `RecipesController.SearchAsync` analog zu den anderen Endpunkten `GetUserId()` pruefen und an den Service weitergeben.
- Den `cookbookId`-Typ im Search-API-Vertrag auf `string?` pruefen und konsistent zur Datenhaltung filtern.
- Einen fokussierten Service-Test fuer `q=Honig` und das Rezept `Honig - Senf - Sojamarinade` ergaenzen; optional einen Controller-Test fuer fehlenden Benutzerkontext bzw. Delegation mit Benutzer-ID.
