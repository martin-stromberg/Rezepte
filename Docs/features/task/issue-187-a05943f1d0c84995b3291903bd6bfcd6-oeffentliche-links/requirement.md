# Anforderungsübersetzung

## Metadaten

- Aufgaben-ID: `a05943f1-d0c8-4995-b329-1903bd6bfcd6` (Issue #187 „Öffentliche Links")
- Branch: `task/issue-187-a05943f1d0c84995b3291903bd6bfcd6-oeffentliche-links`
- Kundenanforderung: „Der Anwender soll bei einem Rezept einen öffentlichen Link über eine Teilen-Funktion generieren können. Andere Leute ohne Benutzerkonto sollen die Rezepte dann über den Link aufrufen können. Bei einem solchen Aufruf darf dann nur das Rezept gezeigt werden, keine Navigation, kein ‚Termin planen', kein ‚Einkaufswagen', kein Bearbeiten, keine Favoritenfunktion. Einzig der Aufruf der originalen Quellseite des Rezepts darf verlinkt sein. […] Gibt es zu dem Rezept verlinkte Beilagenrezepte, so kann bei der Teilen-Funktion ein Haken gesetzt werden, welche den Zugriff auf diese Rezepte ebenfalls ermöglicht. Da wir keinen Mailserver haben, kann der generierte Link nur kopiert werden. Jeder Link ist 30 Tage gültig. Diese Anzahl soll sich aus einer Programmeinstellung geben, welche der Administrator im Einstellungsbereich vornehmen kann."

## Fachliche Zusammenfassung

Die Rezeptverwaltung wird um eine Teilen-Funktion erweitert: Ein angemeldeter Anwender kann auf der Rezept-Detailseite (`RecipePage.razor`) einen zeitlich befristeten, öffentlich zugänglichen Link zu einem eigenen Rezept erzeugen und per Zwischenablage kopieren. Beim Erzeugen kann optional der Zugriff auf die verlinkten Beilagenrezepte (`RecipeSideDish`) mitfreigegeben werden. Über den Link kann ein anonymer Aufrufer ohne Benutzerkonto das Rezept in einer abgespeckten, rein lesenden Ansicht sehen — ohne Navigation, Terminplanung, Einkaufsliste, Bearbeiten und Favoriten; einzig die externe Quellseite des Rezepts (`Recipe.Uri`/`sourceUri`) darf verlinkt sein. Die Gültigkeitsdauer der Links (Standard 30 Tage) ist eine globale Programmeinstellung, die ein Administrator im Einstellungsbereich (`/settings`, `[Authorize(Roles = "Admin")]`) pflegt.

## Fachlicher Scope

- **Verarbeitungseinheit:** Ein „öffentlicher Rezept-Link" (Share-Link) als eigenes fachliches Objekt: er referenziert genau ein Rezept des erzeugenden Benutzers, trägt ein nicht erratbares Token, einen Erstellungszeitpunkt, eine Gültigkeitsdauer sowie ein Flag, ob die direkt verlinkten Beilagenrezepte mitfreigegeben sind.
- **Auslösende Ereignisse:**
  1. Nutzeraktion „Teilen" auf der Rezept-Detailseite eines eigenen Rezepts (inkl. optionalem Setzen des Beilagen-Hakens und Kopieren des erzeugten Links).
  2. Anonymer HTTP-Aufruf des öffentlichen Links durch eine Person ohne Benutzerkonto.
  3. Administrator-Aktion im Einstellungsbereich zum Ändern der Link-Gültigkeitsdauer.
- **Erwartete Wirkung:**
  - Dem erzeugenden Anwender wird ein kopierbarer, absoluter öffentlicher Link angezeigt.
  - Ein anonymer Aufrufer sieht beim Öffnen des Links ausschließlich die Rezeptdarstellung (Titel, Beschreibung, Portionen, Schritte mit Zutaten, Bilder) sowie — sofern vorhanden — den externen Link zur Originalquelle; bei gesetztem Beilagen-Flag sind die Beilagenrezepte als anklickbare, ebenfalls öffentlich lesbare Rezepte verlinkt. Keine App-Navigation (`MainLayout`-Navbar), kein „Termin planen", kein Einkaufswagen, kein Bearbeiten, keine Favoriten-/Kochbuch-Zuordnung, kein Bild-Upload.
  - Bei abgelaufenem oder unbekanntem Token wird kein Rezeptinhalt ausgeliefert (Fehler-/Nicht-gefunden-Anzeige).
  - Die vom Administrator gesetzte Gültigkeitsdauer wird bei der Link-Erzeugung angewendet.
- **Umgang mit fehlenden Objekten:**
  - Share-Link-Datensatz: erzeugen — pro Teilen-Aktion wird ein neuer Datensatz mit frischem Token und berechnetem Ablaufzeitpunkt persistiert (neue Tabelle/`DbSet` in `RezepteDbContext` plus EF-Migration).
  - Token: erzeugen — kryptographisch zufällig (z. B. `RandomNumberGenerator`), nicht erratbar, kein sequenzieller Schlüssel.
  - Gültigkeitseinstellung: wiederverwenden/anlegen — lesen über `ISettingsService` aus `AppSetting` mit Standardwert 30, wenn kein Eintrag existiert.
  - Abgelaufener/unbekannter Link: verwerfen — keine Rezeptdaten ausliefern.
  - Rezept mit Beilagen ohne gesetzten Haken: Beilagen im öffentlichen Kontext nicht als freigegebene Rezepte auflösbar.
- **Scope-Ausschlüsse:**
  1. Kein Versand des Links (E-Mail/Messenger): Der Link wird ausschließlich zum Kopieren angeboten — kein Mailserver vorhanden.
  2. Kein anonymer Zugriff auf irgendeine andere Funktion oder Seite: Weder Navigation noch Suche, Kalender, Einkaufsliste, Bearbeiten, Favoriten/Kochbuch-Zuordnung, Bild-Upload oder andere Rezepte des Besitzers.
  3. Kein Übergang des anonymen Aufrufers in den regulären Rezeptkontext: Beilagen werden — sofern freigegeben — nur innerhalb der öffentlichen Ansicht aufgelöst, nicht über die interne Route `/recipes/{id}`.
  4. Verwaltung bereits erzeugter Links (Liste, Widerruf, vorzeitiges Löschen) ist in der Anforderung nicht genannt → vorerst ausgeschlossen (siehe Offene Fragen).
  5. Keine anonyme Freigabe weiterer Ressourcen außerhalb des Rezeptkontexts (z. B. Kochbücher, Kalender, Einkaufsliste).
  6. Kein benutzerübergreifendes Teilen innerhalb der App (Link-Inhaber muss nicht angemeldet sein; es gibt kein „für Benutzer X freigeben").

## Betroffene Klassen und Komponenten

- Datenmodell:
  - Neue Entität, z. B. `RecipeShareLink` in `Rezepte.Web/Entities/` (Id, Token, `RecipeId`, `UserId` des Erzeugers, `IncludeSideDishes`, `CreatedAt`, `ExpiresAt` bzw. Gültigkeitsdauer) + `DbSet` und Konfiguration in `Rezepte.Web/Data/RezepteDbContext.cs` + neue EF-Migration unter `Rezepte.Web/Migrations/`.
  - `Rezepte.Web/Entities/AppSetting.cs` — unverändert, Wiederverwendung des Key-Value-Stores für die Gültigkeitsdauer.
- Logik/Services:
  - Neuer Service, z. B. `IRecipeShareService`/`RecipeShareService` in `Rezepte.Web/Services/`: Erzeugen des Links (Token-Generierung, Ablaufberechnung aus Einstellung), Auflösen/Validieren eines Tokens (inkl. Ablaufprüfung), Laden des freigegebenen Rezepts sowie — bei `IncludeSideDishes` — der Beilagenrezepte ohne `userId`-Kontext (bestehende `IRecipeService`-Methoden sind durchgehend `userId`-gebunden).
  - `ISettingsService`/`SettingsService` (`Rezepte.Web/Services/`): neue Get/Set-Methoden für die Link-Gültigkeitsdauer nach Muster von `GetGlobalMaxRequestsPerDayAsync`/`SetGlobalMaxRequestsPerDayAsync` (AppSetting-Key, Standard 30).
- Interfaces: `IRecipeShareService` (neu); `ISettingsService` (erweitert).
- Enums: keine erkennbar nötig.
- Controller/Endpunkte:
  - Authentifizierter Endpunkt zum Erzeugen des Links (z. B. in `RecipesController` oder neuem `ShareController`), JWT-Schema wie üblich (`[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]`), Besitzerprüfung (`recipe.UserId == GetUserId()`).
  - Anonymer Lesendpunkt (`[AllowAnonymous]`) für Rezeptdaten per Token; analog anonymer Bildabruf, da `GET api/recipes/{recipeId}/image/{imageId}` heute JWT- oder Cookie-Authentifizierung verlangt — freigegebene Bilder müssen token-gebunden (nicht global anonym) abrufbar sein.
  - `SettingsController` (`Rezepte.Web/Controllers/SettingsController.cs`): Admin-Endpunkte Get/Set für die Gültigkeitsdauer mit `[Authorize(Roles = "Admin")]` nach Muster der `global/*`-Endpunkte.
- UI-Komponenten:
  - `Rezepte.Web/Components/Pages/RecipePage.razor`: Teilen-Einstieg (Button) für den Besitzer; Dialog/Overlay zum Erzeugen mit Beilagen-Checkbox (nur anbieten, wenn `recipe.SideDishes` vorhanden), Anzeige des erzeugten Links und Kopieren per `IJSRuntime` (Clipboard-API, analog zu bestehenden JS-Interop-Aufrufen).
  - Neue öffentliche Seite, z. B. `Components/Pages/SharedRecipe.razor` mit Route `/share/{token}` ohne `[Authorize]`; Darstellung ausschließlich Rezeptinhalt + externer Quell-Link, ohne `MainLayout`-Navigation (eigenes/minimales Layout).
  - `Rezepte.Web/Components/Routes.razor` bzw. Layout-Zuordnung prüfen: `AuthorizeRouteView` mit `DefaultLayout = MainLayout` würde die Navbar einblenden — für die öffentliche Ansicht ist ein Layout ohne Navbar nötig.
  - `Rezepte.Web/Components/Pages/Settings.razor` + neue Einstellungskomponente unter `Components/Settings/` (Muster: `SecurityTxtSettings.razor`) für die Gültigkeitsdauer, sichtbar nur für `isAdmin`.
  - `Rezepte.Web/Middleware/RedirectToRegisterMiddleware.cs`: `IsExcluded` muss die öffentliche Share-Route (z. B. `/share`) freigeben, sonst werden anonyme Aufrufer auf `/login` umgeleitet.
  - `Resources/UiStrings(.resx)`: neue lokalisierte Texte (Teilen-Dialog, Admin-Einstellung, öffentliche Ansicht).
  - DI-Registrierung in `Rezepte.Web/Extensions/ServiceCollectionExtensions.cs`.
- Tests:
  - `Rezepte.Tests`: Service-Tests (Token-Erzeugung, Ablauf, Beilagen-Flag, Standardwert 30), Controller-Tests für anonymen Zugriff/Ablaufverhalten und Admin-Settings (Muster: `Controllers/SettingsControllerSecurityTxtValidationTests.cs`, `Controllers/SecurityTxtControllerTests_Authentication.cs`), ggf. bUnit-Komponententests für Teilen-Dialog und öffentliche Ansicht (Muster: `Components/…`).
  - `Rezepte.Tests.Browser`: E2E-Test für anonymen Link-Aufruf inkl. Ausblenden der geschützten Elemente (Infrastruktur: `RezepteAppFixture`, `RecipeSeedHelper`).

## Implementierungsansatz

- Share-Link als neue persistierte Entität mit eigenem `DbSet`; Token kryptographisch zufällig erzeugen, `ExpiresAt` beim Erzeugen aus `CreatedAt + Gültigkeitstage` (gelesen über `ISettingsService`, Standard 30) berechnen und fix speichern.
- Erzeugen-Endpunkt prüft Besitz (`GetByIdAsync(userId, …)` liefert nur eigene Rezepte) und persistiert den Link inkl. `IncludeSideDishes`-Flag; Beilagen-Checkbox nur anbieten, wenn das Rezept Einträge in `SideDishes` hat.
- Anonyme Auflösung: dedizierter Service-/Controller-Pfad ohne `userId`-Bindung (bestehende `IRecipeService`-Methoden erzwingen `userId`); Validierung = Token vorhanden, `ExpiresAt > now`; Datenhaltung als DTO analog `RecipeDto`/`RecipeStepDto`/`RecipeIngredientDto`/`RecipeSideDishDto` aus `RecipesController`, aber ohne `RecipeCookbooks`/`OwnerId`-abhängige Aktionen. Beilagen-Links innerhalb der öffentlichen Ansicht müssen auf die Share-Route zeigen (Auflösung über dasselbe Token bzw. token-gebundene Beilagen-IDs), nicht auf `/recipes/{id}`.
- Bilder: token-gebundener anonymer Bildabruf nötig (z. B. `GET api/share/{token}/image/{imageId}` bzw. Beilagen-Bilder nur bei `IncludeSideDishes`), da `RecipeImage.Url` heute auf den authentifizierten Endpunkt `api/recipes/{recipeId}/image/{imageId}` zeigt.
- Öffentliche Seite: eigene Razor-Component mit `@page "/share/{token}"`, ohne `[Authorize]`; Zugriff entweder über anonymen API-Endpunkt (scoped `HttpClient` ohne Auth ist bereits registriert, `ServiceCollectionExtensions` Zeilen 172–182) oder direkt über den scoped Share-Service. Eigenes Layout ohne `MainLayout`-Navbar; `RedirectToRegisterMiddleware.IsExcluded` um die Share-Route erweitern (`/_blazor` ist bereits ausgenommen, Interactive Server bleibt möglich — Alternativ statische SSR-Seite).
- Teilen-Dialog auf `RecipePage.razor` nach Muster der vorhandenen Overlays (`CalendarEventDialog`, `AssignToCookbooksOverlay`, `AddRecipeToShoppingListDialog`); Link-Kopieren via `IJSRuntime`/Clipboard-JS (neues `wwwroot/js/*` oder vorhandene Helfer).
- Admin-Einstellung: `ISettingsService`-Methoden + `SettingsController`-Endpunkte + Einstellungsitem in `Settings.razor` mit `isAdmin`-Sichtbarkeit (Muster: `SecurityTxtSettings`-Item).

## Konfiguration

- Globale Programmeinstellung (anwendungsweit, nicht benutzerspezifisch): Gültigkeitsdauer öffentlicher Links in Tagen, Standardwert **30**. Ablage als `AppSetting`-Key-Value-Eintrag (z. B. `ShareLinkValidityDays`) über `ISettingsService`/`SettingsService`; Pflege ausschließlich durch Administratoren im Einstellungsbereich (`/settings`, Admin-gated UI + `[Authorize(Roles = "Admin")]`-Endpunkte). Validierung: positive ganze Zahl ≥ 1 (Muster: `SetGlobalMaxRequestsPerHour` lehnt `<= 0` ab).
- Keine neue `appsettings`-Sektion nötig; kein Benutzer-Opt-out in der Anforderung gefordert.

## Akzeptanzkriterien

1. Auf der Rezeptseite eines eigenen Rezepts kann ein öffentlicher Link erzeugt und in die Zwischenablage kopiert werden; bei Rezepten mit Beilagen ist vor dem Erzeugen ein Haken „Beilagen freigeben" wählbar, bei Rezepten ohne Beilagen entfällt er.
2. Ein anonymer Aufruf des Links zeigt ausschließlich das Rezept (inkl. Bilder und externem Quell-Link, falls `sourceUri` gesetzt) — ohne Navbar/Navigation, ohne „Termin planen", ohne Einkaufswagen, ohne Bearbeiten/Favoriten/Upload.
3. Mit gesetztem Beilagen-Haken sind die direkt verlinkten Beilagenrezepte über die öffentliche Ansicht ebenfalls anonym lesbar; ohne den Haken nicht.
4. Der Link ist standardmäßig 30 Tage gültig; die Dauer ist eine globale Admin-Einstellung und wird bei der Erzeugung angewendet. Abgelaufene oder unbekannte Tokens liefern keinen Rezeptinhalt.
5. Nicht-Besitzer können keinen Link für fremde Rezepte erzeugen; Rezeptdaten anderer Benutzer bleiben anonym unzugänglich.
6. Die Einstellung kann nur von Administratoren gelesen/geändert werden; ungültige Werte (≤ 0, nicht numerisch) werden abgelehnt.
7. Solution baut, Testsuite (`Rezepte.Tests`, ggf. `Rezepte.Tests.Browser`) läuft grün; Feature-Doku unter `Docs/help/` wird ergänzt.

## Explizite Nicht-Ziele

- Kein E-Mail-/Messenger-Versand des Links.
- Keine Link-Verwaltung (Auflisten, Widerrufen, Verlängern, Löschen bestehender Links) — nicht gefordert.
- Keine anonyme Schreib- oder Interaktionsfunktion (kein Bearbeiten, kein Upload, keine Einkaufsliste, kein Kalender, keine Favoriten).
- Keine Freigabe ganzer Kochbücher oder beliebiger Rezeptmengen — Verarbeitungseinheit ist ein einzelnes Rezept (plus optional dessen direkte Beilagen).
- Kein Login-/Registrierungsangebot oder sonstige App-Navigation für anonyme Aufrufer.

## Offene Fragen

1. **Widerruf:** Soll der Besitzer einen erzeugten Link vorzeitig ungültig machen oder bestehende Links einsehen können? (Anforderung schweigt — vorerst als Nicht-Ziel behandelt.)
2. **Transitivität der Beilagen:** Gilt die Freigabe nur für die direkt verlinkten Beilagen oder auch für Beilagen der Beilagen? (Annahme bis zur Klärung: nur direkte Beilagen.)
3. **Gültigkeitsänderung:** Wirkt eine vom Administrator geänderte Gültigkeitsdauer nur auf neu erzeugte Links (bei `ExpiresAt`-Persistierung der Fall) oder auch auf bestehende? (Annahme: nur neue Links.)
4. **Verhalten bei gelöschtem Rezept:** Wird ein Rezept gelöscht, dessen Link noch gültig ist — reicht die generische „nicht gefunden"-Anzeige? (Annahme: ja.)
5. **Anzahl Links pro Rezept:** Darf beliebig oft geteilt werden (pro Klick neuer Link) oder soll ein bestehender gültiger Link wiederverwendet werden? (Annahme: pro Teilen-Aktion neuer Link.)
6. **Fehlerseite:** Soll bei abgelaufenem Link eine explizite „Link abgelaufen"-Anzeige erscheinen oder generisch „nicht gefunden"? (UX-Detail, in Planung festzulegen.)
