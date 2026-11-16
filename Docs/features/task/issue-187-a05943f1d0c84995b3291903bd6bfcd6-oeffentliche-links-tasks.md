# Tasks: Öffentliche Links (Issue #187)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Datenmodell | `RecipeShareLink`-Entität in `Rezepte.Web/Entities/` anlegen (Id, Token, RecipeId, UserId, IncludeSideDishes, CreatedAt, ExpiresAt) | Offen | — |
| 2 | Datenmodell | `DbSet<RecipeShareLink>` und EF-Konfiguration in `RezepteDbContext.OnModelCreating` ergänzen (Unique-Index `Token`, FK `Recipe` Cascade, Index `RecipeId`) | Offen | — |
| 3 | Datenmodell | EF-Migration `AddRecipeShareLinks` erzeugen (`dotnet ef migrations add`) | Offen | — |
| 4 | Logik | `ISettingsService` um `GetShareLinkValidityDaysAsync`/`SetShareLinkValidityDaysAsync` erweitern | Offen | — |
| 5 | Logik | `SettingsService`: `AppSetting`-Key `ShareLinkValidityDays` + Get/Set implementieren (Default 30 bei fehlendem/ungültigem Eintrag) | Offen | — |
| 6 | Logik | `IRecipeShareService`-Interface anlegen (`CreateShareLinkAsync`, `GetSharedRecipeAsync`, `GetSharedSideDishAsync`, `GetSharedImageAsync`) | Offen | — |
| 7 | Logik | Read-Model-Records `SharedRecipeView`, `SharedStepView`, `SharedIngredientView`, `SharedSideDishView` anlegen | Offen | — |
| 8 | Logik | `RecipeShareService` implementieren: Token-Generierung (`RandomNumberGenerator` + Base64url), Besitzprüfung, `ExpiresAt` via `TimeProvider` + `ISettingsService` | Offen | — |
| 9 | Logik | `RecipeShareService`: `GetSharedRecipeAsync` — Token-Validierung, Rezept inkl. Schritte/Zutaten/Bilder/Beilagen ohne `userId` laden, Beilagen nur bei `IncludeSideDishes` | Offen | — |
| 10 | Logik | `RecipeShareService`: `GetSharedSideDishAsync` — nur direkte Beilage des geteilten Rezepts und nur bei Flag | Offen | — |
| 11 | Logik | `RecipeShareService`: `GetSharedImageAsync` — Bild nur für Hauptrezept oder (bei Flag) direkte Beilage | Offen | — |
| 12 | Logik | `IRecipeShareService` in `ServiceCollectionExtensions.AddRezepteServices` scoped registrieren | Offen | — |
| 13 | Controller | `RecipesController`: `IRecipeShareService` in Konstruktor injizieren | Offen | — |
| 14 | Controller | `RecipesController`: `POST api/recipes/{id}/share` (`CreateShareLink`) + Records `CreateShareLinkRequest`/`ShareLinkResponse` | Offen | — |
| 15 | Controller | `ShareController` anlegen: `GET api/share/{token}/image/{imageId}` (anonym, ETag/`If-None-Match`/`Cache-Control` nach `RecipesController.GetImage`-Muster) | Offen | — |
| 16 | Controller | `SettingsController`: `GET api/settings/global/sharelinkvalidity` (`[Authorize(Roles = "Admin")]`) | Offen | — |
| 17 | Controller | `SettingsController`: `PUT api/settings/global/sharelinkvalidity` (Admin, `days <= 0` → `BadRequest`) | Offen | — |
| 18 | Controller | `SettingsController.GetGlobal`: Aggregat-Antwort um `ShareLinkValidityDays` erweitern | Offen | — |
| 19 | UI | `PublicLayout.razor` in `Components/Layout/` anlegen (Layout ohne Navbar) | Offen | — |
| 20 | UI | `SharedRecipePage.razor` anlegen: Routen `/share/{Token}` + `/share/{Token}/sidedish/{RecipeId}`, `@layout PublicLayout`, statisches SSR, Daten via `IRecipeShareService`, Fehlerzustand | Offen | — |
| 21 | UI | `RedirectToRegisterMiddleware.IsExcluded`: `/share`-Segment freigeben (segment-sicher) | Offen | — |
| 22 | UI | `ShareRecipeDialog.razor` in `Components/Shared/` anlegen (Beilagen-Checkbox nur bei `HasSideDishes`, Link-Erzeugung via `ApiClient`, Anzeige + Kopieren) | Offen | — |
| 23 | UI | `RecipePage.razor`: Teilen-Button in `isOwner`-Gruppe + `ShareRecipeDialog`-Einbindung + `showShareDialog`-State | Offen | — |
| 24 | UI | `wwwroot/js/clipboard.js` mit `window.rezepteCopyToClipboard` anlegen (`navigator.clipboard` + Fallback) und in `App.razor` einbinden | Offen | — |
| 25 | UI | `ShareLinkSettings.razor` in `Components/Settings/` anlegen (Get/Put Gültigkeitstage, Alerts — Muster `SecurityTxtSettings`) | Offen | — |
| 26 | UI | `Settings.razor`: `isAdmin`-gesteuertes `SettingsViewModel.Item` für `ShareLinkSettings` eintragen | Offen | — |
| 27 | Lokalisierung | `UiStrings.resx`: Texte für Teilen-Dialog (Titel, Beschreibung, Checkbox, Buttons, Kopiert-/Fehlerhinweise, Gültigkeit) ergänzen | Offen | — |
| 28 | Lokalisierung | `UiStrings.resx`: Texte für öffentliche Seite (Fehlermeldung, Quell-Link, Beilagen-Überschrift) ergänzen | Offen | — |
| 29 | Lokalisierung | `UiStrings.resx`: Texte für Admin-Einstellung (Menü-Titel, Label, Beschreibung, Speicher-Alerts) ergänzen | Offen | — |
| 30 | Tests | `RecipeShareServiceTests` (neu): Erzeugung — Token, `ExpiresAt` Default 30 / konfigurierter Wert, Besitzprüfung | Offen | — |
| 31 | Tests | `RecipeShareServiceTests`: Auflösung — gültig / unbekannt / abgelaufen (`FixedTimeProvider.Advance`) | Offen | — |
| 32 | Tests | `RecipeShareServiceTests`: Beilagen-Flag ein/aus, `GetSharedSideDishAsync` nur direkte Beilage | Offen | — |
| 33 | Tests | `RecipeShareServiceTests`: `GetSharedImageAsync` Hauptrezept / Beilage mit Flag / Fremdbild | Offen | — |
| 34 | Tests | `SettingsServiceTests` erweitern: `ShareLinkValidityDays` Default 30, Roundtrip, ungültiger Wert → 30 | Offen | — |
| 35 | Tests | `RecipesControllerShareTests` (neu): Token/Expiry-Response, `404` bei fremdem Rezept | Offen | — |
| 36 | Tests | `ShareEndpointAuthenticationTests` (neu, `WebApplicationFactory`): anonym `GET /share/{token}` 200 ohne Redirect, unbekanntes Token Fehleranzeige, `POST` share → 401 anonym | Offen | — |
| 37 | Tests | `ShareEndpointAuthenticationTests`: anonymer Bildabruf 200 / 404 unbekanntes Token / 404 Fremdbild | Offen | — |
| 38 | Tests | `SettingsControllerShareLinkValidityTests` (neu): `BadRequest` bei `<= 0`, `NoContent` bei gültig, Admin-Rolle erforderlich | Offen | — |
| 39 | Tests | `RecipesControllerSearchTests` anpassen: `Mock<IRecipeShareService>` im Controller-Konstruktor | Offen | — |
| 40 | Tests | `SharedRecipePageTests` (bUnit): Rendern des Read-Models, Beilagen-Links nur bei Flag, Fehlerzustand bei `null` | Offen | — |
| 41 | Tests | `ShareRecipeDialogTests` (bUnit): Checkbox bedingt sichtbar, POST mit Flag, absolute URL, `VerifyInvoke("rezepteCopyToClipboard")` | Offen | — |
| 42 | Tests | `ShareLinkSettingsTests` (bUnit): Laden/Speichern, Fehler-Alert (Muster `SecurityTxtSettingsTests_ErrorHandling`) | Offen | — |
| 43 | E2E-Tests | `ShareLinkPageObject` in `Rezepte.Tests.Browser/Infrastructure/` anlegen | Offen | — |
| 44 | E2E-Tests | `ShareLinkBrowserTests`: Happy Path — Teilen, Link kopieren, anonymer Kontext sieht Rezept ohne Navbar/Interaktionen | Offen | — |
| 45 | E2E-Tests | `ShareLinkBrowserTests`: Beilage mit Haken anonym erreichbar; ohne Haken keine Liste + Direktaufruf Fehlermeldung | Offen | — |
| 46 | E2E-Tests | `ShareLinkBrowserTests`: unbekanntes Token → anonyme Fehleranzeige; Bild `<img>` lädt (200) | Offen | — |
| 47 | E2E-Tests | `ShareLinkBrowserTests`: Admin setzt Gültigkeit in `/settings`; regulärer Benutzer sieht Menüpunkt nicht | Offen | — |
| 48 | Doku | `Docs/help/public-links.md` anlegen und in `Docs/help/index.md` verlinken | Offen | — |
