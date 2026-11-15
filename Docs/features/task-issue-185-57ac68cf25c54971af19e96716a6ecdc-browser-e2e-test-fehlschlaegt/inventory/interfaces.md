# Interfaces — Bestandsaufnahme

## `IRecipeService`

Datei: `Rezepte.Web/Services/RecipeService.cs` (Zeilen 16–248; Interface und Implementierung
in derselben Datei — `Rezepte.Web/Services/IRecipeService.cs` enthält nur eine
`using`-Direktive, keine Typdeklaration)

Für die Anforderung relevant ist ausschließlich die Suchmethode; die übrigen Member
(CRUD, Kochbuch-/Beilagen-Abfragen, `FindByUri` u. a.) sind nicht betroffen.

| Member | Parameter | Rückgabewert | Zweck |
|--------|-----------|--------------|-------|
| `MaxSearchPageSize` | – (Konstante) | `int` = 100 | Obergrenze für `pageSize`, wird in `RecipeService.SearchAsync` und `RecipeSearch.CurrentPageSize` angewendet |
| `SearchAsync` | `string userId`, `string? q`, `string? tags`, `string? cookbookId`, `int page`, `int pageSize`, `string sort`, `CancellationToken ct` | `Task<SearchResult>` | Volltext-ähnliche Suche mit Paging; aufgerufen von `RecipeSearch.RunSearchAsync` mit `("newest")` als Sortierung |

Keine Änderung am Contract zu erwarten (Scope-Ausschluss der Anforderung).
