# Bestandsaufnahme: Öffentliche Links (Issue #187)

Analyse des Rezepte-Bereichs bezogen auf die Anforderung „Öffentliche Links" (`requirement.md`): zeitlich befristeter, anonymer Lesezugriff auf ein einzelnes Rezept (optional inkl. direkter Beilagenrezepte) über einen nicht erratbaren Share-Link, mit admin-pflegbarer Gültigkeitsdauer.

## Zusammenfassung

**Vorhanden:**

- Rezept-Datenmodell ist vollständig (`Recipe`, `RecipeStep`, `RecipeIngredient`, `RecipeImage`, `RecipeSideDish`) samt EF-Konfiguration und `DbSet`s in `RezepteDbContext` — inklusive externer Quell-URI (`Recipe.Uri`) und Beilagen-Verknüpfung.
- `AppSetting`-Key-Value-Store und `ISettingsService`/`SettingsService` mit etabliertem Muster für globale Admin-Einstellungen (Get/Set-Paare, `AppSetting`-Key-Konstanten, Standardwerte bei fehlendem Eintrag).
- `SettingsController` mit `[Authorize(Roles = "Admin")]`-Endpunkten (`global/*`), inkl. Validierungsmuster (`<= 0` → `BadRequest`) und anonymem Referenz-Controller `SecurityTxtController` ohne `[Authorize]`.
- `RecipePage.razor` mit Besitzer-Erkennung (`isOwner`), vorhandenen Overlay-/Dialog-Mustern und JS-Interop; `Settings.razor` mit `isAdmin`-gesteuerten Einstellungseinträgen (Muster `SecurityTxtSettings.razor`).
- Test-Infrastruktur: xUnit + EF-InMemory-Service-Tests, `WebApplicationFactory<Program>`-Integrationstests, bUnit-Komponententests und Playwright-Browser-Tests (`RezepteAppFixture`, `RecipeSeedHelper`).

**Fehlt / ist noch nicht vorhanden:**

- Keine Share-Link-Entität (z. B. `RecipeShareLink`), kein zugehöriges `DbSet`, keine Migration; keine Token-Erzeugung.
- Kein Share-Service (`IRecipeShareService`) — alle `IRecipeService`-Lesemethoden sind `userId`-gebunden und können anonym nicht wiederverwendet werden.
- Kein anonymer Endpunkt für Rezeptdaten und keine token-gebundene anonyme Bildauslieferung (`GET api/recipes/{recipeId}/image/{imageId}` verlangt JWT- oder Cookie-Auth).
- Keine öffentliche Seite ohne `MainLayout`-Navbar — `Routes.razor` nutzt `AuthorizeRouteView` mit `DefaultLayout = MainLayout` für alle Seiten.
- `RedirectToRegisterMiddleware.IsExcluded` deckt keine öffentliche Share-Route ab; anonyme Aufrufe werden auf `/login` umgeleitet.
- Keine Einstellung für die Link-Gültigkeitsdauer (kein `AppSetting`-Key, keine `ISettingsService`-Methoden, kein Admin-UI-Item, kein Controller-Endpunkt).
- Kein Teilen-Einstieg auf `RecipePage.razor`, keine Clipboard-Hilfsfunktion in `wwwroot/js` (Clipboard-API bisher nicht verwendet).
- Keine lokalisierten Texte für Teilen/öffentliche Ansicht in `Resources/UiStrings.resx` (258 `<data>`-Einträge, kein Share-Bezug).

**Test-Ausgangszustand:** `dotnet test Rezepte.sln` (Debug) am 16.11.2026, Commit `f0551af` — beide Suiten grün: `Rezepte.Tests` 717/717, `Rezepte.Tests.Browser` 27/27 bestanden, 0 Fehlschläge, 0 Übersprungen. Nachweis: [inventory/tests.md](inventory/tests.md).

## Details

- [Datenmodell](inventory/models.md) — bestehende Rezept-/Beilagen-/Bild-/`AppSetting`-Entitäten; keine Share-Link-Entität
- [Logik / Services](inventory/logic.md) — `RecipeService`, `SettingsService`, `ApiClient`, `SettingsViewModel`, `RedirectToRegisterMiddleware`, DI-Registrierung
- [Controller / Endpunkte](inventory/controllers.md) — `RecipesController`, `SettingsController`, `SecurityTxtController`, `ApiControllerBase`
- [Interfaces](inventory/interfaces.md) — `IRecipeService`, `ISettingsService`
- [UI / Routing / Lokalisierung](inventory/ui.md) — `RecipePage.razor`, `Settings.razor`, `SecurityTxtSettings.razor`, `Routes.razor`, `MainLayout.razor`, `App.razor`, `UiStrings`, JS-Helfer
- [Tests](inventory/tests.md) — Test-Ausgangszustand (Nachweis des vollständig grünen Basislaufs) und relevante bestehende Testklassen/Hilfsmethoden
