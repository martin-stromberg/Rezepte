# UI / Routing / Lokalisierung — Bestandsaufnahme

Analysierte Razor-Komponenten, Routing-/Layout-Bausteine und Ressourcen im Kontext der Anforderung „Öffentliche Links".

## `RecipePage.razor` (Rezept-Detailseite)
Datei: `Rezepte.Web/Components/Pages/RecipePage.razor`

- Route `@page "/recipes/{Id}"` (Z. 1), `@rendermode InteractiveServer` (Z. 10), `@attribute [Authorize]` (Z. 11).
- Injektionen: `ApiClient Api`, `NavigationManager Nav`, `IJSRuntime JS`, `AuthenticationStateProvider`, `IOptionsMonitor<ImageOptions>`, `ILogger`, `IStringLocalizer<UiStrings>` (Z. 12–18).
- Kopfbereich (Z. 22–62): „Termin planen"-Button für alle (Z. 26); Besitzer-Button-Gruppe nur bei `isOwner` (Z. 29–61): Einkaufsliste (Z. 32), Bearbeiten-Link `/recipes/{Id}/edit` (Z. 35), Favoriten/Kochbuch-Zuordnung (Z. 38–47), externer Quell-Link `recipe.sourceUri` mit `target="_blank"` (Z. 49–59). **Kein Teilen-Einstieg vorhanden.**
- Darstellung (Z. 72–189): Titelbild bzw. Platzhalter (Z. 77–85), `InputFile`-Upload + `ImageCropper` (Z. 89–94), Bilder-Overlay-Button (Z. 96–101), Titel/Beschreibung, Gesamtminuten/Ruhenächte/Portionen (Z. 105–126), Beilagenliste als Links auf `/recipes/{sideDish.Id}` (Z. 127–138), Schritte-Akkordeon mit Zutaten-Tabelle (Z. 142–188).
- Overlays/Dialoge: `AddRecipeToShoppingListDialog` (Z. 191), `PhotoOverlay` (Z. 197), `AssignToCookbooksOverlay` (Z. 204), `ImageCropper` (Z. 211), `CalendarEventDialog` (Z. 219).
- `@code`: `isOwner` = `recipe.OwnerId == currentUserId` (Z. 252); `currentUserId` aus `sub`-/`NameIdentifier`-Claim (Z. 405–410); Datenabruf über `Api.Http.GetFromJsonAsync<RecipeDto>` (Z. 394 u. a.); private DTO-Records inkl. `RecipeDto` mit `sourceUri` und `SideDishes` (Z. 471–475). JS-Interop-Aufrufe: `console.log`, `alert` via `IJSRuntime` — keine Clipboard-Nutzung.

## `Settings.razor` (Einstellungsbereich)
Datei: `Rezepte.Web/Components/Pages/Settings.razor`

- Route `@page "/settings"`, `[Authorize]` (Z. 5), `InteractiveServer`; injiziert `SettingsViewModel Vm` und `IStringLocalizer` (Z. 7–8).
- `Items`-Liste (Z. 62–73): Einträge vom Typ `SettingsViewModel.Item` mit `Visible`-Flag; Admin-gated Items: `UserAdmin`, `PluginSettings`, `ApplicationUpdates`, `SecurityTxtSettings`, `BackupRestore`; gewähltes Item via `DynamicComponent` (Z. 39).
- Kein Eintrag für eine Link-Gültigkeitsdauer.

## `SecurityTxtSettings.razor` (Muster für Admin-Einstellung)
Datei: `Rezepte.Web/Components/Settings/SecurityTxtSettings.razor`

- Scoped `ApiClient` + `IStringLocalizer<UiStrings>` + `ILogger` (Z. 1–3).
- Lädt via `Client.GetAsync<SecurityTxtSettings>("api/settings/global/securitytxt")` (Z. 156), speichert via `Client.PutAsJsonAsync` (Z. 188); Lade-/Speicher-/Erfolgszustände mit Alerts; Formularfelder via `@bind`/record-`with`-Pattern.
- Referenzmuster für eine neue Admin-Einstellungskomponente.

## `Routes.razor` / Layout-Zuordnung
Datei: `Rezepte.Web/Components/Routes.razor`

- `<Router>` mit `<AuthorizeRouteView RouteData="routeData" DefaultLayout="typeof(MainLayout)">` (Z. 6) — **alle** Seiten erhalten `MainLayout`; `<NotAuthorized>` → `<RedirectToLogin />` (Z. 8); `<FocusOnNavigate>` (Z. 11).
- Kein Mechanismus für seitenindividuelles/abweichendes Layout vorhanden.

## `MainLayout.razor`
Datei: `Rezepte.Web/Components/Layout/MainLayout.razor`

- Navbar mit Brand, NavLinks (Kochbücher, Kalender, Einkaufsliste), Suchformular, `AuthorizeView` mit Settings-Link und Benutzer-Dropdown (Z. 7–88); `<LoadingBar />` (Z. 90); `<main>` mit `@Body` (Z. 92–94); Footer mit Version (Z. 96–100).
- Ein alternatives Layout ohne Navbar existiert nicht (`Components/Layout/` enthält nur `MainLayout` und `LoadingBar`).

## `App.razor`
Datei: `Rezepte.Web/Components/App.razor`

- HTML-Rahmen; `<CascadingAuthenticationState><Routes /></CascadingAuthenticationState>` (Z. 33–35); eingebundene JS: `fileDownload.js`, `dragHelpers.js`, `backupRestore.js`, `randomFromCookbooks.js`, `imageCropper.js`, `loadingBar.js`, `auth.js` sowie Inline-Helfer (`triggerFileInput`, Suchfeld-Sync) — **keine Clipboard-Funktion**.

## Middleware-/Pipeline-Kontext
Datei: `Rezepte.Web/Program.cs`

- `MapRazorComponents<App>().AddInteractiveServerRenderMode()` (Z. 103), `MapControllers()` (Z. 107), `app.UseRedirectToRegisterWhenNoUsers()` (Z. 110) — die Middleware leitet anonyme Aufrufe nicht-freigegebener Pfade auf `/login` um (Details: [logic.md](logic.md)).

## Lokalisierung
Dateien: `Rezepte.Web/Resources/UiStrings.cs` (Marker-Klasse), `Rezepte.Web/Resources/UiStrings.resx`

- `UiStrings.resx` enthält 258 `<data>`-Einträge; Verwendung via `IStringLocalizer<UiStrings>` + `@Localizer["Key"]`.
- Keine Einträge für Teilen-/Share-/öffentliche-Ansichts-Texte vorhanden.
- Vorhandene, thematisch relevante Keys (Beispiele): `ScheduleEvent`, `AddIngredientsToShoppingList`, `Edit`, `AssignRecipeToCookbooks`, `OpenSource`, `RecipeNotFound`, `SettingsPageTitle`, `SecurityTxtTitle`.

## Nicht vorhanden

- Öffentliche Share-Seite (z. B. `/share/{token}`), Share-Dialog/-Overlay, Clipboard-JS-Helfer, alternatives Layout ohne Navbar, Share-bezogene Lokalisierungen.
