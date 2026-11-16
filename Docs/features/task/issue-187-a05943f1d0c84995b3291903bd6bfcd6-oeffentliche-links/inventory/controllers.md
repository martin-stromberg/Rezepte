# Controller / Endpunkte — Bestandsaufnahme

Analysierte Controller im Kontext der Anforderung „Öffentliche Links".

## `ApiControllerBase`
Datei: `Rezepte.Web/Controllers/ApiControllerBase.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `GetUserId()` | protected | `ClaimTypes.NameIdentifier` aus `User` (Z. 15) |
| `CurrentUserId` | protected | `GetUserId() ?? string.Empty` (Z. 21) |
| `TryGetUserId(out string)` | protected | `false` bei fehlendem Claim (Z. 28–33) |

## `RecipesController`
Datei: `Rezepte.Web/Controllers/RecipesController.cs`

Klassenattribute: `[ApiController]`, `[Route("api/[controller]")]`, `[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]` (Z. 21–23). Injektion: `IRecipeService`, `IOptions<ImageOptions>`.

| Endpunkt | Methode | Kurzbeschreibung |
|----------|---------|------------------|
| `GET api/recipes/by-cookbook/{cookbookId}` | `GetByCookbook` | Listeneinträge eines Kochbuchs (Z. 36–53) |
| `GET api/recipes/available-for/{cookbookId}` | `GetAvailableFor` | Zuweisbare Rezepte, paging/search (Z. 65–78) |
| `POST api/recipes/add-existing/{cookbookId}` | `AddExisting` | Rezepte zu Kochbuch (Z. 88–97) |
| `GET api/recipes/{id}` | `GetById` | Vollständiges `RecipeDto` inkl. `Steps`, `SideDishes` (mit Titel/Bild-URL/Portionen), `sourceUri`, `RecipeCookbooks` (Z. 105–148) |
| `GET api/recipes/{id}/side-dishes` | `GetSideDishes` | `RecipeSideDishDto`-Liste des eigenen Rezepts (Z. 157–163) |
| `GET api/recipes/{id}/cookbooks` | `GetCookbooksForRecipe` | Cookbook-Ids des Rezepts (Z. 172–182) |
| `POST api/recipes` | `Create` | Rezept anlegen inkl. `SideDishRecipeIds` (Z. 228–239) |
| `PUT api/recipes/{id}` | `Update` | Rezept aktualisieren (Z. 270–280) |
| `DELETE api/recipes/{id}` | `Delete` | Rezept löschen (Z. 289–296) |
| `POST api/recipes/{id}/image` | `UploadImage` | Bild-Upload mit Größen-/Content-Type-Prüfung (Z. 306–341) |
| `GET api/recipes/{recipeId}/image/{imageId}` | `GetImage` | **JWT + Cookie**-Auth (Z. 352); Besitzprüfung über `GetByIdAsync`, ETag/Cache-Header (Z. 353–387) |
| `GET api/recipes/{recipeId}/images` | `GetImages` | Bildliste mit `Id`/`Url` (Z. 396–411) |
| `DELETE api/recipes/{recipeId}/image/{imageId}` | `DeleteImage` | Bild löschen (Z. 421–428) |
| `GET api/recipes/latest` | `GetLatestForCookbook` | Neueste Rezepte (Z. 460–481) |
| `GET api/recipes/search` | `SearchAsync` | Suche mit Paging-Clamp auf `IRecipeService.MaxSearchPageSize` (Z. 566–596) |

DTO-Records in derselben Datei: `RecipeListItemDto` (Z. 492), `RecipeDto` (Z. 508; `sourceUri`-Parameter kleingeschrieben), `RecipeCookbookDtp` (Z. 516), `RecipeSideDishDto` (Z. 526), `RecipeStepDto` (Z. 538), `RecipeIngredientDto` (Z. 547), Request-Records Z. 189–251.

Hinweise: Kein Share-/Token-Endpunkt; alle Endpunkte erfordern JWT (`GetImage` zusätzlich Cookie). Ein `[AllowAnonymous]`-Endpunkt existiert nicht.

## `SettingsController`
Datei: `Rezepte.Web/Controllers/SettingsController.cs`

Klassenattribute wie `RecipesController` (JWT, Z. 12–14). Injektion: `ISettingsService`, `IGoogleCredentialsProvider`.

| Endpunkt | Methode | Kurzbeschreibung |
|----------|---------|------------------|
| `GET api/settings/me` | `GetMySettings` | Aggregierte Benutzer-+Global-Werte (Z. 38–73) |
| `PUT api/settings/me/ai` | `SetMyAi` | Benutzer-KI-Toggle (Z. 83–88) |
| `PUT api/settings/me/ai/googlevision` | `SetMyGoogleVision` | (Z. 98–103) |
| `PUT api/settings/me/ai/gemini` | `SetMyGemini` | (Z. 113–118) |
| `PUT api/settings/me/ai/confirm` | `SetMyAiConfirm` | (Z. 128–133) |
| `GET api/settings/global` | `GetGlobal` | **`[Authorize(Roles = "Admin")]`** (Z. 141–163) |
| `PUT api/settings/global/ai` | `SetGlobalAi` | Admin (Z. 172–178) |
| `PUT api/settings/global/ai/googlevision` | `SetGlobalGoogleVision` | Admin (Z. 187–193) |
| `PUT api/settings/global/ai/gemini` | `SetGlobalGemini` | Admin (Z. 202–208) |
| `PUT api/settings/global/ai/maxrequestsperhour` | `SetGlobalMaxRequestsPerHour` | Admin; `value <= 0` → `BadRequest` (Z. 217–224) |
| `PUT api/settings/global/ai/maxrequestsperday` | `SetGlobalMaxRequestsPerDay` | Admin; gleiche Validierung (Z. 233–240) |
| `PUT api/settings/global/ai/disableonlimit` | `SetGlobalDisableOnLimit` | Admin (Z. 249–255) |
| `GET api/settings/global/securitytxt` | `GetGlobalSecurityTxt` | Admin (Z. 263–269) |
| `PUT api/settings/global/securitytxt` | `SetGlobalSecurityTxt` | Admin; Validierung Contact/Expires (Z. 278–297) |

Hinweise: Kein Endpunkt für eine Share-Link-Gültigkeitsdauer; das Admin-Muster (`[Authorize(Roles = "Admin")]` + `BadRequest` bei `<= 0`) ist vorhanden.

## `SecurityTxtController`
Datei: `Rezepte.Web/Controllers/SecurityTxtController.cs`

Referenz für **anonyme** Endpunkte: `[ApiController]` ohne `[Authorize]` und ohne Controller-Route — absolute Routen pro Action.

| Endpunkt | Methode | Kurzbeschreibung |
|----------|---------|------------------|
| `GET /security.txt`, `GET /.well-known/security.txt` | `GetSecurityTxt` | `text/plain`, `NotFound` wenn deaktiviert (Z. 34–35) |
| `GET /.well-known/security.md` | `GetSecurityMd` | Markdown (Z. 43–44) |
| `GET /.well-known/security.html` | `GetSecurityHtml` | HTML (Z. 52–53) |

Injektion: `ISettingsService`, `ISecurityTxtRenderer`. Die security.txt-Pfade sind zusätzlich in `RedirectToRegisterMiddleware.IsExcluded` freigegeben (vgl. [logic.md](logic.md)).
