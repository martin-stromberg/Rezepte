# Interfaces — Rezeptsuche

## `IRecipeService`

Datei: `Rezepte.Web/Services/RecipeService.cs` (Zeilen 16–242) — die Interface-Definition steht in derselben Datei wie die Implementierung. Die Datei `Rezepte.Web/Services/IRecipeService.cs` existiert, enthält aber nur eine `using`-Zeile (faktisch leer).

Nur die suchrelevanten Contracts; die übrigen Member sind in [logic.md](logic.md) bei `RecipeService` aufgeführt.

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `SearchAsync` | `string userId, string? q, string? tags, string? cookbookId, int page, int pageSize, string sort, CancellationToken ct` | `Task<SearchResult>` | Suchvertrag; implementiert von `RecipeService`, konsumiert von `RecipesController.SearchAsync` (einziger Aufrufer im Produktivcode) |
| `GetAvailableForCookbookAsync` | `string userId, string cookbookId, CancellationToken ct` | `Task<List<Recipe>>` | Rezeptauswahl für `GET api/recipes/available-for/{cookbookId}` (`RecipeSelectDialog`) |
| `GetByIdAsync` | `string userId, string id, CancellationToken ct` | `Task<Recipe?>` | Direktaufruf aus `RecipeEdit.LoadRecipeSearchResultByIdAsync` (über API) |
| `GetByCookbookAsync`, `GetSideDishesAsync`, `CreateAsync` (2×), `UpdateAsync` (2×), `DeleteAsync`, `AddExistingToCookbookAsync`, `RemoveFromCookbookAsync`, `SetImageAsync`, `AddImageAsync`, `GetImageAsync`, `GetImages`, `GetImageCountAsync`, `DeleteImageAsync`, `GetLatestAsync`, `FindByUri` | s. Signatur in Datei | s. Signatur in Datei | nicht suchrelevant |

Hinweis: `tags` ist Teil des Contracts, wird aber in `RecipeService.SearchAsync` ignoriert (kein Tag-Modell; Kommentar Zeilen 905–906).

## `ITokenService`

Datei: `Rezepte.Web/Services/TokenService.cs` (Zeilen 12–28)

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `CreateToken` | `string userId, string username, bool isAdmin = false` | `string` | erzeugt Bearer-JWT für den Self-Call; aufgerufen von `CircuitAuthHandler` |
| `GetToken` | `string userId` | `string?` | gecachten JWT lesen; aufgerufen von `CircuitAuthHandler` |

## `IJwtSigningKeyProvider`

Datei: `Rezepte.Web/Security/JwtSigningKeyProvider.cs` (Zeilen 9–19)

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `Key` (Property) | — | `byte[]` | HMAC-Signaturschlüssel für `TokenService` und JWT-Validierung |
| `Issuer` (Property) | — | `string` | Token-Issuer (`Jwt:Issuer`, Default `rezepte`) |
| `Audience` (Property) | — | `string` | Token-Audience (`Jwt:Audience`, Default `rezepte.api`) |
