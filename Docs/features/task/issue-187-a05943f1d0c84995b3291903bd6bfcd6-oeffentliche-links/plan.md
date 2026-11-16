# Umsetzungsplan: Öffentliche Links (Issue #187)

## Übersicht

Die Rezeptverwaltung erhält eine Teilen-Funktion: Ein angemeldeter Besitzer erzeugt auf der Rezept-Detailseite einen zeitlich befristeten, öffentlichen Link zu einem einzelnen Rezept — optional inklusive seiner direkt verlinkten Beilagenrezepte. Anonyme Aufrufer ohne Benutzerkonto sehen über den Link ausschließlich eine Nur-Lesen-Ansicht des Rezepts (ohne Navigation, Terminplanung, Einkaufsliste, Bearbeiten, Favoriten, Upload; einzig der externe Quell-Link `Recipe.Uri` darf verlinkt sein). Die Gültigkeitsdauer (Standard 30 Tage) ist eine globale, admin-pflegbare Programmeinstellung im `AppSetting`-Store.

Betroffen: Datenmodell (neue Entität `RecipeShareLink` + EF-Migration), Services (`IRecipeShareService` neu, `ISettingsService` erweitert), Controller (`RecipesController` erweitert, neuer anonymer `ShareController`, `SettingsController` erweitert), UI (`RecipePage`-Teilen-Dialog, neue öffentliche Seite `/share/{token}` mit eigenem Layout ohne Navbar, neue Admin-Einstellungskomponente), `RedirectToRegisterMiddleware`, Lokalisierung (`UiStrings.resx`), Tests (Unit, Integration, bUnit, Playwright-E2E) und Feature-Doku unter `Docs/help/`.

## Scope-Klärung

| Feld | Festlegung |
|------|------------|
| Verarbeitungseinheit | Ein „öffentlicher Rezept-Link" (`RecipeShareLink`): referenziert genau ein Rezept des erzeugenden Benutzers, trägt nicht erratbares Token, `CreatedAt`, `ExpiresAt` und `IncludeSideDishes`-Flag |
| Auslösende Ereignisse | 1. Nutzeraktion „Teilen" auf `RecipePage.razor` eines eigenen Rezepts (inkl. optionalem Beilagen-Haken und Kopieren des Links). 2. Anonymer HTTP-Aufruf `GET /share/{token}` (bzw. `/share/{token}/sidedish/{recipeId}` und `api/share/{token}/image/{imageId}`). 3. Admin-Aktion „Gültigkeitsdauer ändern" im Einstellungsbereich `/settings` |
| Erwartete Wirkung | Erzeugender Anwender sieht kopierbaren absoluten Link mit Gültigkeitsdatum; anonymer Aufrufer sieht ausschließlich Rezeptinhalt (Titel, Beschreibung, Portionen, Schritte mit Zutaten, Bilder, ggf. externer Quell-Link, bei Flag Beilagen-Links) ohne App-Navigation und ohne interaktive Funktionen; unbekannte/abgelaufene Tokens liefern keinen Rezeptinhalt; Admin-Einstellung wird bei Erzeugung angewendet |
| Umgang mit fehlenden Objekten | Share-Link-Datensatz: **erzeugen** — pro Teilen-Aktion neuer Datensatz mit frischem Token + berechnetem `ExpiresAt`. Token: **erzeugen** — kryptographisch zufällig (`RandomNumberGenerator`, 32 Bytes, Base64url), kein sequenzieller Schlüssel. Gültigkeitseinstellung: **wiederverwenden/anlegen** — `ISettingsService` liest `AppSetting`-Key `ShareLinkValidityDays`, Default 30 bei fehlendem/ungültigem Eintrag. Ablaufzeitpunkt: **fix persistiert** — Admin-Änderung wirkt nur auf neu erzeugte Links. Abgelaufener/unbekannter Link: **verwerfen** — kein Rezeptinhalt, generische Fehleranzeige. Rezept mit Beilagen ohne Haken: Beilagen im öffentlichen Kontext **nicht** auflösbar. Gelöschtes Rezept: Share-Link wird per FK-Cascade mitgelöscht → danach generische „nicht gefunden"-Anzeige |

### Scope-Ausschlüsse

| # | Einschränkung / Ausschluss | Begründung | Status |
|---|----------------------------|------------|--------|
| 1 | Kein Versand des Links (E-Mail/Messenger); Link nur zum Kopieren | Kein Mailserver vorhanden; explizit in der Kundenanforderung | offen |
| 2 | Kein anonymer Zugriff auf andere Funktionen/Seiten (Navigation, Suche, Kalender, Einkaufsliste, Bearbeiten, Favoriten/Kochbuch-Zuordnung, Bild-Upload, andere Rezepte des Besitzers) | Anforderung verlangt ausdrücklich reine Rezeptansicht | offen |
| 3 | Kein Übergang des anonymen Aufrufers in den regulären Rezeptkontext; Beilagen werden nur innerhalb der öffentlichen Ansicht unter demselben Token aufgelöst, nicht über `/recipes/{id}` | Interne Route ist `userId`-gebunden und `[Authorize]`-geschützt | offen |
| 4 | Keine Verwaltung erzeugter Links (Liste, Widerruf, vorzeitiges Löschen, Verlängern) | In der Anforderung nicht genannt (vgl. Offene Punkte #1) | offen |
| 5 | Keine anonyme Freigabe weiterer Ressourcen (Kochbücher, Kalender, Einkaufsliste, Beilagen der Beilagen) | Anforderung nennt nur das Rezept plus dessen direkte Beilagen | offen |
| 6 | Kein benutzerübergreifendes In-App-Teilen („für Benutzer X freigeben") | Anforderung beschreibt nur anonyme Aufrufer ohne Konto | offen |

> **Eskalationsvermerk (2026-11-16):** Alle Scope-Ausschlüsse und offenen Punkte wurden dem Anwender vollständig per Issue-Kommentar vorgelegt (https://github.com/martin-stromberg/Rezepte/issues/187#issuecomment-6928315067). Nach ~80 Minuten ohne Rückmeldung wurde die Umsetzung mit den dokumentierten Lösungsvorschlägen fortgesetzt; der Anwender kann Abweichungen weiterhin per Issue-/PR-Kommentar einfordern. Ausschlüsse 1, 2, 3, 5 und 6 sind direkte Wiederholungen der Kundenanforderung; Ausschluss 4 ist die einzige eigenständige Einschränkung (Verzicht auf Link-Verwaltung, da in der Anforderung nicht genannt).

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Öffentliche Seite / Layout | `SharedRecipePage.razor` als statisch gerenderte SSR-Seite (kein `@rendermode`, Vorbild `Login.razor`) mit `@layout PublicLayout` (neues minimales Layout, nur `@Body`) | `@layout` auf der Seitenkomponente überschreibt `DefaultLayout` des `AuthorizeRouteView` — `Routes.razor`/`MainLayout` bleiben unverändert; rein lesende Ansicht braucht keinen Blazor-Circuit, kein `/_blazor`-Websocket für anonyme Aufrufer |
| Datenbeschaffung öffentliche Seite | Direkte Injektion des scoped `IRecipeShareService` in `SharedRecipePage` — **kein** anonymer JSON-Endpunkt für Rezeptdaten | Seite läuft serverseitig; ein HTTP-DTO-Weg wäre redundanter Serialisierungsaufwand und zusätzliche öffentliche API-Fläche; der Service liefert direkt ein Read-Model |
| Token-gebundene Beilagen | Route `/share/{token}/sidedish/{recipeId}` auf derselben `SharedRecipePage` (zweite `@page`-Direktive, optionaler `[Parameter] RecipeId`); Service prüft `IncludeSideDishes` + direkte Beilagen-Mitgliedschaft | Token bleibt die Capability für das gesamte freigegebene Bündel — keine separaten Tokens pro Beilage, keine Transitivität, Auflösung bleibt an einer Stelle (Specification-artige Prüfung im Service) |
| Bildauslieferung | Anonymer Endpunkt `GET api/share/{token}/image/{imageId}` im neuen `ShareController`; der Service prüft, ob `image.RecipeId` zum Hauptrezept oder (bei `IncludeSideDishes`) zu einer direkten Beilage gehört | `<img>`-Requests laufen browserseitig — HTTP-Endpunkt zwingend nötig; `RecipeImage.Url` ist fest auf den authentifizierten Endpunkt verdrahtet und wird **nicht** geändert; das Read-Model liefert eigene token-gebundene URLs |
| Erzeugen-Endpunkt | `POST api/recipes/{id}/share` auf dem bestehenden `RecipesController` (JWT, Klassenattribut greift); anonymer Teil in eigenem `ShareController` ohne `[Authorize]` (Muster `SecurityTxtController`) | „Rezept teilen" gehört fachlich zur Rezept-API; Trennung authentifizierter vs. anonymer Controller hält die Authorization-Fläche übersichtlich |
| Share-Link-Modell | Eigene persistierte Entität `RecipeShareLink` mit Klartext-Token und Unique-Index (PoEAA: Entity + Table Data Gateway über `RezepteDbContext`) | Capability-URL-Modell (wer den Link kennt, darf lesen) — übliche Praxis; Unique-Index für O(1)-Auflösung; `ExpiresAt` fix gespeichert statt Laufzeitberechnung → Admin-Änderungen wirken nur auf neue Links (eindeutig, nachvollziehbar) |
| Zeitquelle | `TimeProvider` (bereits als `TimeProvider.System` registriert, `ServiceCollectionExtensions` Z. 194) für `CreatedAt`/`ExpiresAt` und Ablaufprüfung | Ermöglicht deterministische Ablauf-Tests mit `Rezepte.Tests/TestSupport/FixedTimeProvider.cs` |
| Gültigkeitsdauer | `AppSetting`-Key `ShareLinkValidityDays` über `ISettingsService`-Get/Set-Paar, `int`, Default 30 | Exaktes Bestandsmuster (`GetGlobalMaxRequestsPerDayAsync` u. a.); keine neue `appsettings`-Sektion, keine eigene Tabelle nötig |
| Fehleranzeige öffentliche Seite | Eine generische, lokalisierte Meldung „Link ungültig oder abgelaufen" innerhalb `PublicLayout` (kein Rezeptinhalt) | Deckt unbekannt/abgelaufen/gelöschtes Rezept/nicht freigegebene Beilage ab; verrät Angreifern nichts über Token-Existenz (vgl. Offene Punkte #6) |
| UI-Muster Teilen-Dialog | `ShareRecipeDialog.razor` als Overlay nach Muster `AssignToCookbooksOverlay` (`overlay-backdrop`/`overlay-content`, `[Parameter] Show`/`OnClose`, `ApiClient`-Aufrufe) | Vorhandenes, im Projekt etabliertes Dialog-Muster; Kopieren via neuem `clipboard.js` + `IJSRuntime` analog bestehender JS-Interop-Aufrufe |
| Admin-UI | `ShareLinkSettings.razor` nach Muster `SecurityTxtSettings.razor` (scoped `ApiClient`, Get/Put, Lade-/Speicher-/Fehler-Alerts) + `isAdmin`-gesteuertes `SettingsViewModel.Item` in `Settings.razor` | Etabliertes Muster für admin-gegatete Einstellungen |

## Programmabläufe

### Link erzeugen und kopieren (Teilen-Dialog)

1. Besitzer öffnet `RecipePage.razor` (`/recipes/{Id}`); `isOwner` ist `true` → Teilen-Button in der Besitzer-Button-Gruppe (Z. 29–61) wird sichtbar.
2. Klick auf „Teilen" → `ShareRecipeDialog` (`Show = true`); die Checkbox „Beilagen freigeben" wird nur gerendert, wenn `HasSideDishes` (`recipe.SideDishes?.Count > 0`, Z. 127) `true` ist; bei Rezepten ohne Beilagen entfällt sie vollständig.
3. Klick auf „Link erstellen" → `Api.Http.PostAsJsonAsync("api/recipes/{Id}/share", new { includeSideDishes })` (authentifiziert via `CircuitAuthHandler`/JWT).
4. `RecipesController.CreateShareLink` → `IRecipeShareService.CreateShareLinkAsync(userId, recipeId, includeSideDishes, ct)`: lädt das Rezept mit `UserId`-Filter (fremdes oder nicht vorhandenes Rezept → `null` → `NotFound`), generiert Token via `RandomNumberGenerator` (32 Bytes → Base64url, ~43 Zeichen), liest Gültigkeitstage via `ISettingsService.GetShareLinkValidityDaysAsync` (Default 30), setzt `CreatedAt`/`ExpiresAt` über `TimeProvider.GetUtcNow()`, speichert `RecipeShareLink`.
5. Antwort `{ token, expiresAt }` → Dialog baut absolute URL (`NavigationManager.BaseUri` + `share/{token}`), zeigt sie in einem readonly Eingabefeld samt Gültigkeitsdatum an.
6. Klick auf „Kopieren" → `IJSRuntime.InvokeVoidAsync("rezepteCopyToClipboard", url)` → `clipboard.js` nutzt `navigator.clipboard.writeText` mit Fallback auf `document.execCommand("copy")`; Erfolg/Fehlschlag wird als lokalisierter Hinweis im Dialog angezeigt.
7. Schlägt der POST fehl (z. B. Rezept zwischenzeitlich gelöscht → 404), zeigt der Dialog eine lokalisierte Fehlermeldung — der Nutzer sieht keine wirkungslose Aktion.

Beteiligte Klassen/Komponenten: `RecipePage`, `ShareRecipeDialog`, `ApiClient`, `RecipesController`, `IRecipeShareService`, `ISettingsService`, `TimeProvider`, `RezepteDbContext`, `IJSRuntime`, `clipboard.js`

### Anonymer Aufruf des öffentlichen Links (Hauptrezept)

1. Browser-`GET /share/{token}` → `RedirectToRegisterMiddleware.IsExcluded` gibt das `/share`-Segment frei (neu) → kein Redirect auf `/login`.
2. `MapRazorComponents` rendert `App` → `Routes` → `AuthorizeRouteView`; die Seite trägt kein `[Authorize]` → `SharedRecipePage` wird statisch mit `PublicLayout` (keine Navbar) gerendert.
3. `SharedRecipePage.OnInitializedAsync` → `IRecipeShareService.GetSharedRecipeAsync(token, ct)`: Link per Token laden; unbekannt oder `ExpiresAt <= now` → `null` → die Seite zeigt die generische Fehlermeldung (Status 404 über `HttpContext`, siehe Designentscheidung „Fehleranzeige").
4. Gültig: `Recipe` inkl. `Steps`→`Ingredients`, `Images`, `SideDishes`→`SideDishRecipe` **ohne** `userId`-Bindung laden → `SharedRecipeView` mit Bild-URLs `/api/share/{token}/image/{imageId}`.
5. Rendern: Titel, Beschreibung, Portionen/Gesamtminuten, Schritte mit Zutaten, Titelbild/Galerie, externer Quell-Link `sourceUri` (`target="_blank"`, nur wenn gesetzt); bei `IncludeSideDishes`: Beilagenliste als Links auf `/share/{token}/sidedish/{id}`; **keine** Beilagenliste ohne Flag, keine Navbar/Buttons/Upload.
6. `<img>`-Requests → `GET api/share/{token}/image/{imageId}` (siehe Ablauf „Anonymer Bildabruf").

Beteiligte Klassen/Komponenten: `SharedRecipePage`, `PublicLayout`, `RedirectToRegisterMiddleware`, `IRecipeShareService`, `RezepteDbContext`, `ShareController`

### Anonymer Aufruf eines freigegebenen Beilagenrezepts

1. Klick auf Beilagen-Link `/share/{token}/sidedish/{recipeId}` → dieselbe `SharedRecipePage` mit gesetztem `RecipeId`-Parameter.
2. `IRecipeShareService.GetSharedSideDishAsync(token, recipeId, ct)`: Link validieren (vorhanden, nicht abgelaufen), `IncludeSideDishes` prüfen und `recipeId` muss eine **direkte** Beilage des geteilten Rezepts sein (`RecipeSideDishes`-Eintrag mit `RecipeId == link.RecipeId` und `SideDishRecipeId == recipeId`) → sonst `null` → generische Fehlermeldung.
3. Darstellung wie Hauptrezept; die Beilagenliste des Beilagenrezepts selbst wird **nicht** gerendert (nur direkte Beilagen sind freigegeben — Transitivität ausgeschlossen).

Beteiligte Klassen/Komponenten: `SharedRecipePage`, `IRecipeShareService`, `RezepteDbContext`

### Anonymer Bildabruf

1. `GET api/share/{token}/image/{imageId}` → `ShareController.GetSharedImage` (`[ApiController]`, `[Route("api/share")]`, kein `[Authorize]` — Muster `SecurityTxtController`; `/api`-Präfix ist in der Middleware ohnehin freigegeben).
2. `IRecipeShareService.GetSharedImageAsync(token, imageId, ct)`: Link validieren; erlaubte Rezept-Ids = `{link.RecipeId}` ∪ (nur bei `IncludeSideDishes`: Ids der direkten Beilagen); `RecipeImage` nur liefern, wenn `Id == imageId` und `RecipeId` in der erlaubten Menge → sonst `NotFound`.
3. Antwort `File(image.Data, contentType, image.FileName)` mit ETag-/`If-None-Match`-/`Cache-Control`-Logik nach Muster `RecipesController.GetImage` (Z. 353–387, `ImageOptions.CacheMaxAgeSeconds`).

Beteiligte Klassen/Komponenten: `ShareController`, `IRecipeShareService`, `IOptions<ImageOptions>`

### Admin ändert die Gültigkeitsdauer

1. Admin öffnet `/settings`; neues `SettingsViewModel.Item` „Öffentliche Links" ist nur bei `Vm.IsAdmin` sichtbar → `DynamicComponent` rendert `ShareLinkSettings`.
2. Laden: `Client.GetAsync<int>("api/settings/global/sharelinkvalidity")` → Anzeige im numerischen Eingabefeld.
3. Speichern: `Client.PutAsJsonAsync("api/settings/global/sharelinkvalidity", days)` → `SettingsController.SetGlobalShareLinkValidity` (`[Authorize(Roles = "Admin")]`): `days <= 0` → `BadRequest`; sonst `ISettingsService.SetShareLinkValidityDaysAsync` → `AppSetting`-Key `ShareLinkValidityDays` schreiben.
4. Nicht-Admins: Menüpunkt unsichtbar; direkte API-Aufrufe scheitern mit 401/403.
5. Erfolg/Fehler als Alert (Muster `SecurityTxtSettings`); der Wert wirkt ab der nächsten Link-Erzeugung.

Beteiligte Klassen/Komponenten: `Settings`, `SettingsViewModel`, `ShareLinkSettings`, `SettingsController`, `ISettingsService`, `ApiClient`

### Fehlerfall: unbekanntes oder abgelaufenes Token

1. `GET /share/{token}` mit unbekanntem/abgelaufenem Token oder Aufruf eines nicht freigegebenen Beilagenrezepts → Service liefert `null`.
2. `SharedRecipePage` rendert innerhalb `PublicLayout` ausschließlich die lokalisierte Meldung „Link ungültig oder abgelaufen" — kein Rezeptinhalt, keine Navigation, kein Hinweis auf den Grund; Bild-Endpunkt liefert `404`.

Beteiligte Klassen/Komponenten: `SharedRecipePage`, `IRecipeShareService`, `ShareController`

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `RecipeShareLink` | Datenmodellklasse (Entity, `Rezepte.Web/Entities/RecipeShareLink.cs`) | Persistierter Share-Link: `Id`, `Token` (unique), `RecipeId` + Navigation, `UserId` (Erzeuger), `IncludeSideDishes`, `CreatedAt`, `ExpiresAt` |
| `IRecipeShareService` | Interface (`Rezepte.Web/Services/IRecipeShareService.cs`) | Contract: `CreateShareLinkAsync`, `GetSharedRecipeAsync`, `GetSharedSideDishAsync`, `GetSharedImageAsync` |
| `RecipeShareService` | Klasse (`Rezepte.Web/Services/RecipeShareService.cs`) | Service-Layer: Token-Generierung (`RandomNumberGenerator`), Ablaufberechnung (`TimeProvider` + `ISettingsService`), Besitzprüfung, token-gebundene anonyme Auflösung ohne `userId` |
| `SharedRecipeView` | Datenmodellklasse (Read-Model-Record) | Flache öffentliche Rezeptsicht: `Title`, `Description`, `Portions`, `SourceUri`, `ImageUrls` (token-gebunden), `Steps`, `SideDishes` |
| `SharedStepView` | Datenmodellklasse (Record) | Schritt: `Title`, `Description`, `DurationMinutes`, `RequiresOvernightRest`, `Ingredients` |
| `SharedIngredientView` | Datenmodellklasse (Record) | Zutat: `Amount`, `Unit`, `Name` |
| `SharedSideDishView` | Datenmodellklasse (Record) | Beilagen-Eintrag: `RecipeId`, `Title`, `Portions`, `ImageUrl`, `Url` (→ `/share/{token}/sidedish/{id}`) |
| `CreateShareLinkRequest` / `ShareLinkResponse` | DTO-Records (in `RecipesController.cs`, Konvention: DTO-Records in Controller-Datei) | Request `{ IncludeSideDishes }`; Response `{ Token, ExpiresAt }` |
| `ShareController` | Controller (`Rezepte.Web/Controllers/ShareController.cs`) | Anonymer Endpunkt `GET api/share/{token}/image/{imageId}` (kein `[Authorize]`) |
| `PublicLayout` | Layout-Komponente (`Rezepte.Web/Components/Layout/PublicLayout.razor`) | Minimales Layout ohne Navbar/`LoadingBar` für die öffentliche Ansicht |
| `SharedRecipePage` | Page-Komponente (`Rezepte.Web/Components/Pages/SharedRecipePage.razor`) | Öffentliche Nur-Lesen-Ansicht; Routen `/share/{Token}` und `/share/{Token}/sidedish/{RecipeId}`; `@layout PublicLayout` |
| `ShareRecipeDialog` | Overlay-Komponente (`Rezepte.Web/Components/Shared/ShareRecipeDialog.razor`) | Teilen-Dialog: Beilagen-Checkbox, Link-Erzeugung, Anzeige + Kopieren |
| `ShareLinkSettings` | Settings-Komponente (`Rezepte.Web/Components/Settings/ShareLinkSettings.razor`) | Admin-Pflege der Gültigkeitsdauer (Muster `SecurityTxtSettings`) |
| `clipboard.js` | JS-Datei (`Rezepte.Web/wwwroot/js/clipboard.js`) | `window.rezepteCopyToClipboard` — `navigator.clipboard.writeText` + `execCommand`-Fallback |

## Änderungen an bestehenden Klassen

### `RezepteDbContext` (DbContext, `Rezepte.Web/Data/RezepteDbContext.cs`)

- **Neue Eigenschaften:** `DbSet<RecipeShareLink> RecipeShareLinks` — Share-Link-Tabelle
- **Geänderte Methoden:** `OnModelCreating` — Konfiguration: `Token` required max. 64 mit Unique-Index, `RecipeId` required, `UserId` required max. 64, FK `Recipe` mit `OnDelete(Cascade)` (Link verfällt mit dem Rezept), Index auf `RecipeId`

### `ISettingsService` (Interface, `Rezepte.Web/Services/ISettingsService.cs`)

- **Neue Methoden:** `GetShareLinkValidityDaysAsync(ct)` → `Task<int>` — Gültigkeitsdauer öffentlicher Links in Tagen (Default 30); `SetShareLinkValidityDaysAsync(days, ct)` → `Task` — persistiert den Wert

### `SettingsService` (Klasse, `Rezepte.Web/Services/SettingsService.cs`)

- **Neue Methoden:** `GetShareLinkValidityDaysAsync` — `AppSetting`-Lookup (`FindAsync`), `int.TryParse`, fehlender/ungültiger/kleiner-1-Wert → `30`; `SetShareLinkValidityDaysAsync` — schreibt via `WriteString`-Helfer
- **Neue Konstante:** `ShareLinkValidityDaysKey = "ShareLinkValidityDays"` (bei Z. 52–60)

### `RecipesController` (Controller, `Rezepte.Web/Controllers/RecipesController.cs`)

- **Geänderte Konstruktor-Injektion:** zusätzlich `IRecipeShareService` (bestehende Injektion `IRecipeService` + `IOptions<ImageOptions>` bleibt)
- **Neue Methoden:** `CreateShareLink(id, CreateShareLinkRequest, ct)` → `POST api/recipes/{id}/share` (JWT via Klassenattribut); ruft `IRecipeShareService.CreateShareLinkAsync(GetUserId(), id, request.IncludeSideDishes, ct)`; `null` → `NotFound`; sonst `Ok(new ShareLinkResponse(token, expiresAt))`
- **Neue DTO-Records:** `CreateShareLinkRequest`, `ShareLinkResponse` (im bestehenden DTO-Abschnitt der Datei)

### `SettingsController` (Controller, `Rezepte.Web/Controllers/SettingsController.cs`)

- **Neue Methoden:** `GetGlobalShareLinkValidity` → `GET api/settings/global/sharelinkvalidity`, `[Authorize(Roles = "Admin")]`, liefert `Ok(int)`; `SetGlobalShareLinkValidity([FromBody] int days)` → `PUT api/settings/global/sharelinkvalidity`, Admin, `days <= 0` → `BadRequest` (Muster Z. 217–224)
- **Geänderte Methoden:** `GetGlobal` — Aggregat-Antwort um `ShareLinkValidityDays` ergänzen (Konsistenz mit den übrigen globalen Einstellungen; zusätzliche JSON-Property ist abwärtskompatibel)

### `RedirectToRegisterMiddleware` (`Rezepte.Web/Middleware/RedirectToRegisterMiddleware.cs`)

- **Geänderte Methoden:** `IsExcluded` — `path.Equals("/share", …) || path.StartsWith("/share/", …)` freigeben (segment-sicher, damit nicht versehentlich andere Pfade wie `/sharexyz` mitfreigegeben werden)

### `ServiceCollectionExtensions` (`Rezepte.Web/Extensions/ServiceCollectionExtensions.cs`)

- **Geänderte Methoden:** `AddRezepteServices` — `services.AddScoped<IRecipeShareService, RecipeShareService>()` im Application-Services-Block (nach Z. 189)

### `RecipePage` (Page, `Rezepte.Web/Components/Pages/RecipePage.razor`)

- **UI-Erweiterung:** Teilen-Button (`bi-share`-Icon, Titel lokalisiert) in der `isOwner`-Button-Gruppe (nach dem Herz-Button, vor dem Quell-Link, Z. 38–59)
- **Neue Komponenten-Einbindung:** `<ShareRecipeDialog Show="showShareDialog" RecipeId="@Id" HasSideDishes="@(recipe.SideDishes?.Count > 0)" OnClose="…" />` im Overlay-Block (Z. 191–219)
- **Neue Methoden:** `OpenShareDialog` — setzt `showShareDialog = true`

### `Settings` (Page, `Rezepte.Web/Components/Pages/Settings.razor`)

- **UI-Erweiterung:** neues `SettingsViewModel.Item` (Titel `Localizer["PublicLinksSettings"]`, Icon `🔗`, `isAdmin`, `typeof(ShareLinkSettings)`) in `Items` (Z. 62–73)

### `App` (Komponente, `Rezepte.Web/Components/App.razor`)

- **UI-Erweiterung:** `<script src="js/clipboard.js"></script>` einbinden (bei den übrigen `js/*`-Scripten, Z. 21–24 bzw. 41–45)

### `UiStrings.resx` (Ressource, `Rezepte.Web/Resources/UiStrings.resx`)

- **Neue `<data>`-Einträge:** u. a. `Share`, `ShareDialogTitle`, `ShareDialogDescription`, `ShareIncludeSideDishes`, `ShareCreateLink`, `ShareLinkValidUntil`, `ShareCopy`, `ShareCopied`, `ShareCopyFailed`, `ShareCreateFailed`, `SharedRecipeNotFound`, `SharedRecipeSource`, `SharedRecipeSideDishes`, `PublicLinksSettings`, `ShareLinkValidityDaysLabel`, `ShareLinkValidityDaysDescription`, `ShareLinkValiditySaved`, `ShareLinkValiditySaveFailed`

### `index.md` (Doku, `Docs/help/index.md`)

- **Geändert:** Eintrag „Öffentliche Links" mit Verweis auf neue `Docs/help/public-links.md` in der Funktionsbereich-Liste

## Datenbankmigrationen

| Migrationsname | Betroffene Tabellen/Spalten | Beschreibung der Änderung |
|----------------|----------------------------|---------------------------|
| `AddRecipeShareLinks` | Neue Tabelle `RecipeShareLinks` | Spalten `Id` (PK, TEXT), `Token` (TEXT, required, max. 64, Unique-Index), `RecipeId` (TEXT, required, FK → `Recipes` Cascade, Index), `UserId` (TEXT, required, max. 64), `IncludeSideDishes` (INTEGER/bool), `CreatedAt` (TEXT/DateTime), `ExpiresAt` (TEXT/DateTime) — erzeugt via `dotnet ef migrations add AddRecipeShareLinks` (EF-Tools sind im Repo etabliert, 27 bestehende Migrationen) |

## Validierungsregeln

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| `token` (Route) | Muss vorhandenen, nicht abgelaufenen `RecipeShareLink` referenzieren (`ExpiresAt > TimeProvider.GetUtcNow()`) | Service → `null` → generische Fehleranzeige; Bild-Endpunkt → `404` |
| `recipeId` (Share-Erzeugung) | Rezept muss existieren und `UserId == GetUserId()` | `404 NotFound` (kein Unterschied fremd/nicht vorhanden — keine Existenz-Preisgabe) |
| `includeSideDishes` | Serverseitig unkritisch akzeptiert; UI blendet die Checkbox nur bei vorhandenen `SideDishes` ein; Beilagen-Freigabe wird serverseitig erzwungen | — (Checkbox allein ist keine Sicherheitsgrenze) |
| `recipeId` (Beilagen-Route) | Nur direkte Beilage des geteilten Rezepts **und** `IncludeSideDishes == true` | `null` → generische Fehleranzeige |
| `imageId` (Bild-Endpunkt) | `RecipeImage.RecipeId` muss zum geteilten Rezept oder (bei Flag) zu einer direkten Beilage gehören | `404` |
| `days` (Admin-PUT) | Ganze Zahl `>= 1` | `<= 0` → `BadRequest`; nicht-numerischer/ungültiger JSON-Body → automatisches `400` durch `[ApiController]`-Modelbinding |

## Konfigurationsänderungen

| Eintrag | Typ | Standardwert | Zweck |
|---------|-----|--------------|-------|
| `AppSetting`-Key `ShareLinkValidityDays` | `int` (als String persistiert) | `30` | Gültigkeitsdauer öffentlicher Links in Tagen; global, nur per Admin-Endpunkt pflegbar; bei fehlendem/ungültigem Eintrag Default |

Keine `appsettings`-Änderungen erforderlich.

## Seiteneffekte und Risiken

- **`RedirectToRegisterMiddleware`:** Die `/share`-Freigabe macht alle Pfade unter `/share` anonym erreichbar (aktuell existiert dort nur die Share-Seite); die Freigabe erfolgt segment-sicher (`/share` und `/share/…`), damit zukünftige gleichnamige Präfixe nicht versehentlich geöffnet werden. Vor der Middleware-Freigabe ist die Seite für anonyme Aufrufer unerreichbar (Redirect auf `/login`) — Middleware- und Seiten-Schritt gehören zusammen getestet.
- **`RecipesController`-Konstruktor:** Die zusätzliche `IRecipeShareService`-Injektion bricht `RecipesControllerSearchTests` (direkte Instanziierung, Z. 28) — Anpassung als betroffener Test eingeplant.
- **`GetGlobal`-Aggregat:** Zusätzliche Property `ShareLinkValidityDays`; `System.Text.Json`-Deserialisierung ignorierter Felder macht das abwärtskompatibel.
- **Bild-Caching:** Anonymer Bild-Endpunkt übernimmt `Cache-Control: private, max-age=<ImageOptions.CacheMaxAgeSeconds>` + ETag vom authentifizierten Vorbild — zulässig, da der Cache browserlokal ist.
- **Token-Speicherung im Klartext:** Wer die DB liest, kennt gültige Links — bewusstes Capability-URL-Modell; bei späterem Widerrufs-Feature kann auf Hashing umgestellt werden.
- **Mehrfaches Teilen:** Jede Teilen-Aktion erzeugt einen neuen, unabhängig gültigen Link (Annahme 5, Offene Punkte) — kein Überschreiben, keine Deduplizierung.
- **Privatsphäre:** `SharedRecipeView` darf keine internen Daten ausliefern (kein `OwnerId`, keine `RecipeCookbooks`, keine internen Ids außer Beilagen-`RecipeId` und Bild-Ids, die nur im Token-Kontext auflösbar sind). Benutzereingaben werden über `@(...)` escaped gerendert — kein `MarkupString`.
- **Statisches SSR der Share-Seite:** `FocusOnNavigate` (Selector `h1`) und `ImportMap` funktionieren; die Seite benötigt keinen Circuit — `/_blazor` ist für sie optional.
- **`RecipeImage.Url`** bleibt unverändert auf den authentifizierten Endpunkt zeigend; die öffentliche Ansicht verwendet ausschließlich token-gebundene URLs aus dem Read-Model — kein Seiteneffekt auf die interne Ansicht.

## Umsetzungsreihenfolge

1. **Entität `RecipeShareLink` + `DbSet`/EF-Konfiguration in `RezepteDbContext`**
   - Voraussetzungen: Keine
   - Beschreibung: Entity-Klasse nach Muster `Recipe` (GUID-Id `ToString("n")`, UTC-Defaults) anlegen; `DbSet` und `OnModelCreating`-Konfiguration (Unique-Index `Token`, FK `Recipe` Cascade) ergänzen.

2. **EF-Migration `AddRecipeShareLinks`**
   - Voraussetzungen: Schritt 1; `dotnet-ef`-Tool bzw. bestehender Migration-Workflow (im Repo etabliert)
   - Beschreibung: Migration erzeugen; `RezepteDbContextModelSnapshot` wird aktualisiert.

3. **`ISettingsService`/`SettingsService` um `ShareLinkValidityDays` erweitern**
   - Voraussetzungen: Keine (`AppSetting`-Store und Get/Set-Muster vorhanden)
   - Beschreibung: Key-Konstante, `GetShareLinkValidityDaysAsync` (Default 30), `SetShareLinkValidityDaysAsync`.

4. **`IRecipeShareService` + `RecipeShareService` + Read-Model-Records**
   - Voraussetzungen: Schritte 1–3 (`DbSet`, Einstellung); `TimeProvider.System` bereits registriert
   - Beschreibung: Interface + Implementierung (Token-Generierung, Besitzprüfung, `ExpiresAt`-Berechnung, Auflösung/Validierung, Beilagen- und Bild-Freigabeprüfung) sowie `SharedRecipeView`/`SharedStepView`/`SharedIngredientView`/`SharedSideDishView`.

5. **DI-Registrierung `IRecipeShareService`**
   - Voraussetzungen: Schritt 4
   - Beschreibung: `AddScoped<IRecipeShareService, RecipeShareService>()` in `ServiceCollectionExtensions`.

6. **`RecipesController`: `POST api/recipes/{id}/share` + DTOs**
   - Voraussetzungen: Schritt 5
   - Beschreibung: Konstruktor-Injektion erweitern, `CreateShareLink`-Action, `CreateShareLinkRequest`/`ShareLinkResponse`.

7. **`ShareController`: anonymer Bild-Endpunkt**
   - Voraussetzungen: Schritt 5; `IOptions<ImageOptions>` (vorhanden)
   - Beschreibung: Neuer Controller `api/share`, `GET {token}/image/{imageId}` mit ETag/Cache-Header nach `RecipesController.GetImage`-Muster.

8. **`PublicLayout` + `SharedRecipePage` (beide Routen, Fehlerzustand)**
   - Voraussetzungen: Schritt 5
   - Beschreibung: Minimales Layout ohne Navbar; Seite lädt Read-Model direkt über `IRecipeShareService`, rendert Rezept/Beilage bzw. Fehlermeldung, `@layout PublicLayout`, kein `@rendermode` (statisches SSR).

9. **`RedirectToRegisterMiddleware.IsExcluded`: `/share`-Segment freigeben**
   - Voraussetzungen: Keine
   - Beschreibung: Segment-sichere Freigabe von `/share` und `/share/*`.

10. **`ShareRecipeDialog` + `RecipePage`-Teilen-Button + `clipboard.js` + `App.razor`-Script**
    - Voraussetzungen: Schritt 6; Overlay-Muster `AssignToCookbooksOverlay` (vorhanden)
    - Beschreibung: Overlay-Komponente (Checkbox, Erzeugen, Anzeige, Kopieren), Button in `isOwner`-Gruppe, JS-Helfer, Script-Tag.

11. **Admin-Einstellung: `SettingsController`-Endpunkte + `ShareLinkSettings` + `Settings.razor`-Item**
    - Voraussetzungen: Schritt 3
    - Beschreibung: `GET`/`PUT api/settings/global/sharelinkvalidity` (Admin, Validierung `<=0`→`BadRequest`), `GetGlobal` erweitern, Komponente nach `SecurityTxtSettings`-Muster, `isAdmin`-Item.

12. **Lokalisierung `UiStrings.resx`**
    - Voraussetzungen: Schritte 8, 10, 11 (finaler Key-Bedarf)
    - Beschreibung: Alle neuen lokalisierten Texte ergänzen.

13. **Unit-/Integrationstests (`Rezepte.Tests`)**
    - Voraussetzungen: Schritte 3–9
    - Beschreibung: Service-Tests (InMemory + `FixedTimeProvider`), Controller-Tests (Moq + `WebApplicationFactory`), anonymer-Zugriff-Integrationstests.

14. **bUnit-Komponententests**
    - Voraussetzungen: Schritte 8, 10, 11
    - Beschreibung: `SharedRecipePage`, `ShareRecipeDialog`, `ShareLinkSettings`.

15. **E2E-Tests (`Rezepte.Tests.Browser`)**
    - Voraussetzungen: Schritte 6–11; Playwright-Chromium + Publish-Output (vorhanden), `RezepteAppFixture`, `RecipeSeedHelper`
    - Beschreibung: `ShareLinkBrowserTests` + Page Object (siehe E2E-Tabelle).

16. **Feature-Doku `Docs/help/public-links.md` + `index.md`-Eintrag**
    - Voraussetzungen: Schritte 1–15
    - Beschreibung: Dokumentation von Teilen-Dialog, öffentlicher Ansicht, Admin-Einstellung, Link-Verhalten.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `CreateDb()`-Hilfsmethode (InMemory `RezepteDbContext` + `FixedTimeProvider` + `SettingsService`) | `Rezepte.Tests/Services/RecipeShareServiceTests.cs` (neu) | Testfixture für den Share-Service; `sut = new RecipeShareService(db, new SettingsService(db, …), fixedTimeProvider)` |
| `CreateShareLink_PersistsLink_WithFreshToken_AndExpiryFromSetting` | `RecipeShareServiceTests` | Token nicht leer/erratbar, `ExpiresAt == now + 30d` bei fehlendem `AppSetting`-Eintrag |
| `CreateShareLink_UsesConfiguredValidityDays` | `RecipeShareServiceTests` | `AppSetting`-Wert (z. B. 5) wird angewendet |
| `CreateShareLink_ReturnsNull_ForForeignOrMissingRecipe` | `RecipeShareServiceTests` | Besitzprüfung (`userId`-Filter) |
| `GetSharedRecipe_ReturnsView_ForValidToken` | `RecipeShareServiceTests` | Vollständiges Read-Model (Schritte, Zutaten, Bild-Ids, Quell-URI) ohne `userId` |
| `GetSharedRecipe_ReturnsNull_ForUnknownToken` | `RecipeShareServiceTests` | Unbekanntes Token → `null` |
| `GetSharedRecipe_ReturnsNull_AfterExpiry` | `RecipeShareServiceTests` | `FixedTimeProvider.Advance(31d)` → `null` |
| `GetSharedRecipe_OmitsSideDishes_WithoutFlag` / `…IncludesSideDishes_WithFlag` | `RecipeShareServiceTests` | `IncludeSideDishes`-Flag steuert Beilagenliste |
| `GetSharedSideDish_ReturnsView_OnlyForDirectSideDish_WithFlag` / `…ReturnsNull_WithoutFlag` / `…ReturnsNull_ForNonSideDish` | `RecipeShareServiceTests` | Direkte Beilagen-Mitgliedschaft, keine Transitivität |
| `GetSharedImage_AllowsMainRecipeImage` / `…AllowsSideDishImage_OnlyWithFlag` / `…RejectsForeignImage` | `RecipeShareServiceTests` | Bild-Freigabemenge (Hauptrezept ∪ direkte Beilagen bei Flag) |
| `GetShareLinkValidityDays_Returns30ByDefault` / `Set…_PersistsValue` / `Get…_Returns30_WhenStoredValueInvalid` | `Rezepte.Tests/Services/SettingsServiceTests.cs` (erweitert) | Default-/Roundtrip-/Korruptwert-Verhalten (Muster `GetGlobalMaxRequestsPerDayAsync`-Tests) |
| `CreateShareLink_ReturnsTokenAndExpiry` / `…Returns404_ForForeignRecipe` | `Rezepte.Tests/Controllers/RecipesControllerShareTests.cs` (neu) | Controller-Mapping über `Mock<IRecipeShareService>` |
| `PostShare_Returns401_WithoutAuthentication` | `Rezepte.Tests/Controllers/ShareEndpointAuthenticationTests.cs` (neu, `WebApplicationFactory<Program>`-Muster `SecurityTxtControllerTests_Authentication`) | JWT-Pflicht des Erzeugen-Endpunkts |
| `GetSharePage_ReturnsOk_WithoutAuthentication` / `…ShowsError_ForUnknownToken` | `ShareEndpointAuthenticationTests` | Middleware-Freigabe `/share` + anonymer Seitenaufruf (Rezept/Link via scoped `RezepteDbContext` in Test-DB seeden) |
| `GetSharedImage_ReturnsOk_WithoutAuthentication` / `…Returns404_ForUnknownToken` / `…Returns404_ForForeignImage` | `ShareEndpointAuthenticationTests` | Token-gebundene anonyme Bildauslieferung |
| `SetShareLinkValidity_ReturnsBadRequest_WhenZeroOrNegative` / `…ReturnsNoContent_WhenValid` / `…RequiresAdminRole` | `Rezepte.Tests/Controllers/SettingsControllerShareLinkValidityTests.cs` (neu, Muster `SettingsControllerSecurityTxtValidationTests` + Auth-Tests) | Admin-Validierung und Rollenprüfung |
| `SharedRecipePage_RendersRecipe` / `…RendersSideDishLinks_OnlyWhenFlagged` / `…ShowsError_WhenServiceReturnsNull` | `Rezepte.Tests/Components/SharedRecipePageTests.cs` (neu, bUnit) | Rendering des Read-Models; `IRecipeShareService`-Mock via `TestContext.Services` |
| `ShareRecipeDialog_HidesSideDishCheckbox_WithoutSideDishes` / `…PostsCreateRequest_WithFlag` / `…ShowsAbsoluteLink` / `…InvokesClipboardJs_OnCopy` | `Rezepte.Tests/Components/ShareRecipeDialogTests.cs` (neu, bUnit) | Bedingte Checkbox, POST-Body, absolute URL, `JSInterop.VerifyInvoke("rezepteCopyToClipboard")` (Loose-Modus-Muster `RecipeSearchComponentTests`) |
| `ShareLinkSettings_LoadsAndSaves` / `…ShowsError_OnFailure` | `Rezepte.Tests/Components/ShareLinkSettingsTests.cs` (neu, bUnit) | Get/Put + Fehler-Alerts (Muster `SecurityTxtSettingsTests_ErrorHandling`) |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `Rezepte.Tests/Controllers/RecipesControllerSearchTests.cs` (Z. 28) | `RecipesController`-Konstruktor erhält zusätzlichen Parameter `IRecipeShareService` → `Mock<IRecipeShareService>` ergänzen |

### E2E-Tests (primärer Funktionsnachweis)

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht | Besitzer öffnet eigenes Rezept → Teilen → Link wird angezeigt → anonymer Browser-Kontext (ohne Cookies) öffnet `/share/{token}` → Rezept sichtbar, **keine** Navbar/„Termin planen"/Einkaufswagen/Bearbeiten/Favoriten/Upload | `Rezepte.Tests.Browser/ShareLinkBrowserTests.cs` + `ShareLinkPageObject` | K1, K2 | Der anonyme, layout-freie Aufruf (Middleware + Routing + statisches SSR) ist nur über realen Browser-Request ohne Session nachweisbar |
| Pflicht | Rezept mit Beilage: Teilen **mit** Haken → Beilagen-Link in öffentlicher Ansicht → Klick → Beilagenrezept anonym lesbar | `ShareLinkBrowserTests` | K3 | Token-gebundene Beilagen-Navigation über zwei anonyme Seitenaufrufe |
| Pflicht | Rezept mit Beilage: Teilen **ohne** Haken → keine Beilagenliste; direkter Aufruf `/share/{token}/sidedish/{id}` → Fehleranzeige | `ShareLinkBrowserTests` | K3 | Serverseitige Flag-Durchsetzung im anonymen Pfad |
| Pflicht | Unbekanntes Token → anonymer Aufruf zeigt Fehlermeldung, kein Rezeptinhalt | `ShareLinkBrowserTests` | K4 | Anonymer Fehlerpfad inkl. Layout |
| Pflicht | Admin setzt Gültigkeitsdauer in `/settings` (Menüpunkt sichtbar); regulärer Benutzer sieht den Menüpunkt nicht (Muster `RegularUser_DoesNotSeeSecurityTxtMenuItemInSettings` — zweiter Benutzer via `SecurityTxtPageObject.RegisterUserAsync`) | `ShareLinkBrowserTests` | K4, K6 | Admin-gatete UI-Sichtbarkeit nur über echte Rollen/Auth prüfbar |
| Empfohlen | Rezept mit Bild: `<img>`-Request auf `api/share/{token}/image/{imageId}` liefert 200 im anonymen Kontext | `ShareLinkBrowserTests` | K2 | Token-gebundener Bildabruf über echte HTTP-Antwort |

Welche bestehenden E2E-Tests müssen angepasst werden? — Keine.

## Akzeptanzszenarien

| # | Szenario | Auslöser | Ausgangszustand | Schritte | Sichtbare Wirkung | Nachweis |
|---|----------|----------|------------------|----------|-------------------|----------|
| 1 | Happy Path: Link erzeugen + anonym aufrufen | Klick „Teilen" auf eigener Rezeptseite | Angemeldeter Besitzer, Rezept ohne Beilagen, kein `ShareLinkValidityDays`-Eintrag (Default 30), keine bestehenden Links | Teilen-Button → Dialog (ohne Beilagen-Checkbox) → „Link erstellen" → absoluter Link + „gültig bis"-Datum → „Kopieren" → anonymer Browser-Kontext öffnet `/share/{token}` | Rezept (inkl. Bilder, ggf. Quell-Link) ohne Navbar/Interaktionen sichtbar; Link in Zwischenablage | E2E `ShareLinkBrowserTests` (Happy Path) |
| 2 | Beilagen mit Freigabe | Teilen mit gesetztem Haken | Rezept mit direkter Beilage | Dialog mit Checkbox → Link erzeugen → anonym öffnen → Beilagen-Link klicken | Beilagenrezept anonym lesbar; dessen eigene Beilagen (falls vorhanden) nicht freigegeben | E2E Beilagen-Variante |
| 3 | Beilagen ohne Freigabe | Teilen ohne Haken | Wie #2 | Link öffnen; direkten `/share/{token}/sidedish/{id}`-Aufruf versuchen | Keine Beilagenliste; Direktaufruf → Fehlermeldung | E2E ohne-Flag-Variante |
| 4 | Abgelaufener Link (Wartesituation/Zeitverlauf) | Anonymer Aufruf nach `ExpiresAt` | Link mit z. B. 1 Tag Gültigkeit erzeugt, Zeit danach | Link aufrufen | Zustand „gültig bis Datum X" → nach Ablauf generische Fehleranzeige; Wiederaufnahme nur durch Erzeugen eines **neuen** Links durch den Besitzer (kein Verlängern) | Service-Test `…_AfterExpiry` + E2E unbekanntes Token (gleiche Anzeige) |
| 5 | Fremdes Rezept teilen | `POST api/recipes/{id}/share` mit fremder Rezept-Id | Angemeldeter Benutzer B, Rezept von Benutzer A | Request absenden | `404`, kein Link-Datensatz | Controller-Test `…Returns404_ForForeignRecipe` |
| 6 | Admin ändert Gültigkeit | Speichern in `/settings` | Admin-Session, z. B. Wert 5 | Wert ändern → speichern → neuen Link erzeugen | Neuer Link hat `ExpiresAt = Erzeugung + 5 Tage`; bestehende Links unverändert | Service-Test + Controller-Test; E2E Admin-UI |
| 7 | Nicht-Admin / ungültiger Wert | Regulärer Benutzer öffnet `/settings`; PUT mit `0` | Reguläre Session | Menüpunkt-Prüfung; direkter PUT | Menüpunkt unsichtbar; `BadRequest` bei `<= 0` | bUnit/Integration + E2E Sichtbarkeit |

## Offene Punkte

| # | Offener Punkt | Empfohlener Vorschlag |
|---|---------------|----------------------|
| 1 | **Widerruf/Verwaltung:** Soll der Besitzer erzeugte Links einsehen oder vorzeitig ungültig machen können? | Vorerst Nicht-Ziel beibehalten (Anforderung schweigt). Das Datenmodell (`Id`, `RecipeId`, `UserId`) lässt eine spätere Verwaltung ohne Migration-Bruch zu. |
| 2 | **Transitivität der Beilagen:** Gilt die Freigabe auch für Beilagen der Beilagen? | Nein — nur direkte Beilagen (wie geplant). Die Beilagenliste des Beilagenrezepts wird in der öffentlichen Ansicht nicht gerendert. |
| 3 | **Gültigkeitsänderung:** Wirkt eine geänderte Dauer auf bestehende Links? | Nein — `ExpiresAt` wird bei Erzeugung fix persistiert; Änderungen wirken nur auf neue Links (bereits so geplant). |
| 4 | **Gelöschtes Rezept:** Verhalten bei gültigem Link zu gelöschtem Rezept? | FK-Cascade löscht den Share-Link mit; danach gilt generische „nicht gefunden"-Anzeige — generisch und ausreichend. |
| 5 | **Anzahl Links pro Rezept:** Neuer Link pro Teilen-Aktion oder Wiederverwendung eines gültigen Links? | Pro Teilen-Aktion neuer Link (einfach, keine Such-/Wiederverwendungslogik); mehrere gültige Links pro Rezept sind möglich. |
| 6 | **Fehlerseite:** Explizites „Link abgelaufen" vs. generisch „nicht gefunden"? | Generische lokalisierte Meldung „Link ungültig oder abgelaufen" für alle Fälle (verrät nichts über Token-Existenz; eine Anzeige deckt unbekannt/abgelaufen/gelöscht/nicht freigegebene Beilage ab). |
