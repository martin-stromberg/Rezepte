# Anforderungskatalog – Kochrezepte-Verwaltungssystem (Blazor Server, .NET 9, SQLite)

| Kennung   | Status        | Anforderung                                                                  | Umsetzungsbeschreibung |
|-----------|---------------|------------------------------------------------------------------------------|------------------------|
| FR-001    | ✅ Erledigt   | Datenpersistenz mit SQLite                                                   | `RezepteDbContext` mit SQLite, Schema via EF‑Migrationen beim Start (wenn vorhanden). |
| FR-002    | ✅ Erledigt   | Passwort-Hashing                                                             | PBKDF2 (`PasswordHasher`) eingesetzt. |
| FR-003    | ✅ Erledigt   | Cookie-Authentifizierung für Website                                         | Cookie‑Auth in `Program.cs` konfiguriert (`rezepte.auth`), `UseAuthentication/UseAuthorization`. |
| FR-004    | ✅ Erledigt   | JWT-Bearer-Authentifizierung für API                                         | `JwtBearer` konfiguriert; `ITokenService` erzeugt HS256‑Token; Key auf 256 Bit normalisiert. |
| FR-005    | ✅ Erledigt   | Automatische Authorization-Header bei API-Requests                           | `ApiAuthHandler` als `DelegatingHandler`, hängt `Authorization: Bearer <token>` anhand aktuellem Benutzer an. |
| FR-006    | ✅ Erledigt   | Login-Endpunkt                                                               | `POST /api/session/login` (Form‑POST, Anti‑Forgery ignoriert), setzt Cookie und erstellt JWT; Redirect via `LocalRedirect`. |
| FR-007    | ✅ Erledigt   | Logout-Endpunkt                                                              | `POST /api/session/logout` (Form‑POST), löscht Cookie, Redirect via `LocalRedirect`. |
| FR-008    | ✅ Erledigt   | Login-Seite                                                                  | `Components/Pages/Login.razor` als `application/x-www-form-urlencoded`‑Formular (Browser‑POST, ReturnUrl‑Support). |
| FR-009    | ✅ Erledigt   | Registrierung-Endpunkt                                                       | `POST /api/auth/register` akzeptiert Form‑ oder JSON‑Requests; bei Form‑POST Redirect zu `/login`. |
| FR-010    | ✅ Erledigt   | Registrierungsseite                                                          | `Components/Pages/Register.razor` als Browser‑Form; E‑Mail optional; Redirect nach Erfolg zu `/login`. |
| FR-011    | ✅ Erledigt   | Redirect-Logik global                                                        | Middleware `RedirectToRegisterMiddleware`: bei 0 Nutzern Redirect zu `/register`, sonst bei anonym `/login`; statische/Framework‑Pfade ausgenommen. |
| FR-012    | ✅ Erledigt   | Navbar abhängig vom Anmeldestatus                                            | `AuthorizeView` in `MainLayout.razor`: Gäste sehen „Anmelden“, angemeldete Nutzer Begrüßung + „Abmelden“. |
| FR-013    | ✅ Erledigt   | Kein direkter Link zur Registrierung bei bestehenden Benutzern               | Link entfernt; Middleware blockiert Zugriff auf `/register`, wenn Nutzer existieren. |
| FR-014    | ✅ Erledigt   | Logging/Diagnostik im Development                                            | `UseDeveloperExceptionPage`, Blazor `DetailedErrors`, angehobene LogLevel in `appsettings.Development.json`. |
| FR-015    | ✅ Erledigt   | DB-Migrationen                                                               | Automatische Migration bei Programmstart (falls Migrationen vorhanden). |
| FR-016    | ✅ Erledigt   | Erste Registrierung als Admin markieren                                      | `IUserService.RegisterAsync` setzt `IsAdmin = true` für den ersten registrierten Benutzer (`Entities.User.IsAdmin`). |
| FR-017    | ✅ Erledigt   | Admin-Setup-Seite                                                            | Benutzerverwaltung als Einstellung "Benutzer" (`Components/Settings/UserAdmin.razor`), nur für Admins sichtbar; API: `GET/POST/PUT/DELETE /api/admin/users` (Bearer + Rolle Admin). |
| FR-018    | ✅ Erledigt   | Form-Handling                                                                | Login/Registrierung senden `x-www-form-urlencoded`; Controller erkennen Form/JSON und liefern bei Form‑POST `LocalRedirect`. |
| FR-019    | ✅ Erledigt   | Internationalisierung (Deutsch)                                              | UI‑Strings Deutsch; Erweiterbarkeit vorbereitet. |
| NFR-001   | ✅ Erledigt   | Sicherheit Cookie‑Einstellungen                                              | Cookie `HttpOnly`, `SameSite=Lax`, `SecurePolicy=SameAsRequest`; HTTPS empfohlen. |
| NFR-002   | ✅ Erledigt   | JWT‑Schlüsselstärke                                                          | Secret wird via SHA‑256 auf 256 Bit gebracht (HS256‑Anforderung). |
| AUTH-001  | ✅ Erledigt   | Registrierung nur möglich, wenn keine Benutzer existieren                    | Zugriff auf `/register` wird durch `RedirectToRegisterMiddleware` nur erlaubt, wenn 0 Nutzer existieren. |
| AUTH-002  | ✅ Erledigt   | Erster Benutzer wird Administrator                                           | `IUserService.RegisterAsync` setzt `IsAdmin = true` beim ersten Benutzer. |
| AUTH-003  | ✅ Erledigt   | Administrator kann weitere Benutzer anlegen                                  | Admin‑Panel `UserAdmin.razor` und `AdminUsersController` mit `GET/POST/PUT/DELETE /api/admin/users`. |
| AUTH-004  | ✅ Erledigt   | Login/Logout für Benutzer                                                    | `POST /api/session/login` und `POST /api/session/logout`; Website‑Cookie + JWT für API (`TokenService`, `ApiAuthHandler`). |
| DB-001    | ✅ Erledigt   | Verwendung von SQLite als Datenbank                                          | EF Core mit SQLite Provider, `RezepteDbContext`, Migrationen/EnsureCreated bei Start. |
| BOOK-001  | ✅ Erledigt   | Benutzer kann beliebig viele Kochbücher erstellen                            | Kochbuch‑Entity mit Benutzerreferenz; UI/Endpoints zum Erstellen vorhanden (`Cookbooks.razor`, `POST /api/cookbooks`). |
| BOOK-002  | ✅ Erledigt   | Rezepte können mehreren Kochbüchern zugeordnet werden                        | Many‑to‑Many Beziehung zwischen Rezept und Kochbuch; UI: Mehrfachzuweisung (Multi‑Assign Overlay) und API‑Endpoints vorhanden. |
| BOOK-003  | ✅ Erledigt   | Reihenfolge der Kochbücher ändern (Drag & Drop)                              | UI: Drag‑Handle + clientseitiges Reordering (`Cookbooks.razor`, `dragHelpers.js`); Persistenz: `OrderIndex` in `Cookbook`‑Entity, Service: `ICookbookService.ReorderAsync`, API: `POST /api/cookbooks/reorder`. (EF‑Migration erforderlich) |
| RECIPE-001| ✅ Erledigt   | Rezept hat Titel                                                             | `Recipe`‑Entity besitzt `Title` (CLR‑Property), Mapping im DbContext vorhanden; DB‑Migration ggf. angewendet. |
| RECIPE-002| ✅ Erledigt   | Rezept hat beliebig viele Bilder                                             | Bilder als separate Entity (`RecipeImage`) mit FK zum `Recipe`; Controller‑Endpoints und Service‑Methoden für Upload/Download/Delete implementiert; Upload‑Validierung, Max‑Size und Cache/ETag ergänzt. |
| RECIPE-003| ✅ Erledigt   | Rezept hat beliebig viele Zubereitungsschritte                               | Schritte als Collection in der Rezept‑Entity. |
| STEP-001  | ✅ Erledigt   | Schritt hat optionalen Titel                                                 | Property `Title` (nullable) in Schritt‑Entity. |
| STEP-002  | ✅ Erledigt   | Schritt hat Beschreibung                                                     | Property `Description` in Schritt‑Entity. |
| STEP-003  | ✅ Erledigt   | Schritt hat Zutatenliste                                                     | Zutaten als Collection in Schritt‑Entity. |
| STEP-004  | ✅ Erledigt   | Schritt hat Zubereitungsdauer                                                | Property `DurationMinutes` in Schritt‑Entity. |
| STEP-005  | ✅ Erledigt   | Schritt kann Ruhezeit über Nacht enthalten                                   | Boolean‑Flag `RequiresOvernightRest` in Schritt‑Entity. |
| CAL-001   | ✅ Erledigt   | Jeder Benutzer hat einen Kalender                                            | Kalender‑View (`Components/Pages/Calendar.razor`) und API (`CalendarController`) sind implementiert; Ereignisse sind benutzergebunden. |
| CAL-002   | ✅ Erledigt   | Rezepte können im Kalender eingeplant werden                                 | Kalender-Ereignisse unterstützen Rezept-Zuordnung, Portionen, Wiederholung; UI zum Erstellen/Bearbeiten vorhanden. |
| CAL-003   | 🕓 Offen      | Vorbereitungen an Vortagen werden automatisch erkannt                        | Algorithmus zur Rückrechnung basierend auf Dauer und Ruhezeit. (noch offen) |
| PLAN-001  | 🕓 Offen      | Arbeitsplan kombiniert mehrere Rezepte                                       | Arbeitsplan‑Entity mit Rezeptreferenzen. |
| PLAN-002  | 🕓 Offen      | Schritte werden zeitlich optimiert (z. B. Dessert vor Hauptgericht)           | Sortierlogik nach Zubereitungszeit und Rezepttyp. |
| SHOP-001  | 🕓 Offen      | Zutaten aus Arbeitsplan können in Einkaufsliste übernommen werden            | `Components/Pages/ShoppingList.razor` implementiert; API: `ShoppingListsController` (`GET /api/shoppinglists`, `POST /api/shoppinglists`, `PUT /api/shoppinglists/{id}`, `DELETE /api/shoppinglists/{id}`) vorhanden. Rezept‑Integration: `POST /api/shoppinglists/{id}/groups/from-recipe` erstellt/benennt Gruppe aus Rezept‑Zutaten. |
| SHOP-002  | 🕓 Offen      | Zutaten können als erledigt abgehakt werden                                  | UI: `ShoppingList.razor` mit Gruppen/Items und CRUD; Status per Zutat per UI möglich (Checkbox‑Status vorgesehen); Persistenz via `PUT /api/shoppinglists/{id}`. |
| SHOP-003  | ✅ Erledigt   | Zutaten aus Rezepten können in Einkaufsliste übernommen werden               | `Components/Pages/ShoppingList.razor` implementiert; API: `ShoppingListsController` (`GET /api/shoppinglists`, `POST /api/shoppinglists`, `PUT /api/shoppinglists/{id}`, `DELETE /api/shoppinglists/{id}`) vorhanden. Rezept‑Integration: `POST /api/shoppinglists/{id}/groups/from-recipe` erstellt/benennt Gruppe aus Rezept‑Zutaten. |
| FR-025    | ✅ Erledigt   | Einkaufsliste (Seite)                                                        | Neue Seite `Components/Pages/ShoppingList.razor` (Route `/shopping`) implementiert; Auth‑geschützt; unterstützt Simple/Advanced Mode, Erstellung/Umbenennung/Löschen von Listen, Gruppenverwaltung, Item‑CRUD, Export in Zwischenablage und Persistenz über `ShoppingListsController`. Integration: `RecipePage.razor` bietet „Zur Einkaufsliste“ Button, mappt Zutaten und erstellt bzw. erweitert Listen über API (siehe `CreateListWithRecipeGroupAsync` und `AddGroupToListAsync`). |
| KI-001    | ✅ Erledigt   | Rezepterfassung per KI aus Fotos/Webseiten (langfristig)                      | Infrastruktur: `GeminiClient` / `ImageAnnotatorClient` vorhanden; Einstellungen & Laufzeit‑Guards implementiert (siehe KI‑002/KI‑003). Extraktion/Parser/Prompts in Weiterentwicklung. |
| KI-002    | ✅ Erledigt   | Benutzerbezogene KI‑Einstellung (User kann KI für eigenes Konto aktivieren)  | `UserSetting` Entity, `SettingsService`, API `GET /api/settings/me` und `PUT /api/settings/me/ai` sowie Blazor‑Component `AiSettings.razor` implementiert. |
| KI-003    | ✅ Erledigt   | Globale KI‑Deaktivierung durch Admin                                         | `AppSetting` Entity, `SettingsService`, Admin‑API `GET /api/settings/global` und `PUT /api/settings/global/ai`; UI: Admin sieht globalen Switch in `AiSettings`. |
| KI-004    | ✅ Erledigt   | Kostenbegrenzung: Max Anfragen pro Stunde / Tag + Option bei Erreichen KI deaktivieren | UI: `AiSettings.razor` (Admin) ergänzt; API: neue Admin‑PUTs `api/settings/global/ai/maxrequestsperhour`, `.../maxrequestsperday`, `.../disableonlimit`; `SettingsService` gespeichert in `AppSetting`. |
| KI-005    | ✅ Erledigt   | AI‑Nutzungsprotokollierung und Limit‑Durchsetzung                            | `AiRequestLog` Entity (+ `Type`), `AiUsageService` (limit check `TryRecordRequestAsync`, `RecordRequestAsync`), Tests `AiUsageServiceTests` und DI‑Registrierung implementiert. |
| IMG-001   | ✅ Erledigt   | Bild‑Zuschneiden vor Upload (Clientseitig)                                   | `ImageCropper` Component (Cropper.js + JS Interop) + direct fetch upload (`imageCropper.uploadCroppedBlob`) implementiert; verhindert große SignalR/Base64‑Transfers. |
| IMG-002   | ✅ Erledigt   | iOS Safari kompatibler File‑Trigger                                          | Label‑trigger für verstecktes `InputFile` (1×1 px, nicht display:none) zur zuverlässigen Öffnung des Dateidialogs. |
| IMG-003   | ✅ Erledigt   | Einheitliche quadratische Thumbnails + Großbild‑Overlay                      | `Components/Shared/PhotoOverlay.razor` zeigt Bilder in einem Grid mit quadratischen Thumbnails (`PhotoOverlay.razor.css`: `aspect-ratio: 1/1`, `object-fit: cover`). Klick auf ein Thumbnail öffnet eine separate Großbild‑Overlay (`large-backdrop` / `large-content`) mit höherem `z-index`. `Delete`‑Button nutzt `@onclick:stopPropagation`. Empfehlung: bUnit/E2E‑Test für Klick→Overlay Verhalten. |
| SYS-001   | ✅ Erledigt   | AI‑Aufrufe runtime‑guard                                                     | AI‑Handler (z. B. `BaseAIImportHandler`, `AIFotoImportHandler`) prüfen vor externen API‑Aufrufen zuerst `SettingsService.GetGlobalAiEnabledAsync` und `GetUserAiEnabledAsync(userId)`; zusätzlicher `AiUsageService` sorgt für Limit‑Prüfung und optionales globales Deaktivieren. |
| DB-002    | ✅ Erledigt   | Settings‑Tabellen und Mapping                                                | `UserSetting` und `AppSetting` in `RezepteDbContext` registriert; Modell‑Konfiguration und Indexe hinzugefügt; Migration erstellt/applied empfohlen. |
| INF-001   | ✅ Erledigt   | Tokenbereitstellung für Browser‑Uploads                                      | `ITokenService` + `ApiAuthHandler` sorgen für gültige JWT; ImageCropper liest serverseitigen Token bei Blazor Server und übergibt an Fetch. |
| FR-020    | 🕓 Offen      | Benutzer‑Export: Anwender kann eigene Rezeptesammlung exportieren            | API: `GET /api/exports/me?format=zip|json` startet Export‑Job; Backend: `IExportService.ExportUserAsync(userId, format, ct)`. |
| FR-021    | 🕓 Offen      | Administrator‑Export: Admin kann vollständigen Datenexport durchführen       | API: `POST /api/admin/exports` (Admin‑Role) startet asynchronen Export‑Job. |
| FR-022    | ✅ Erledigt   | Importfunktion: Upload von Exportformat zur Erstellung neuer Rezepte         | API: session‑basierte Import‑Flows implementiert. Endpunkte: URL‑Start `POST /api/cookbooks/import-session/start`, File‑Start `POST /api/cookbooks/import-session/start-file` (je mit/ohne `cookbookId` Varianten). `ImportOrchestrator` verwaltet Import‑Sessions (waiting/confirm/result). Client (Blazor) pollt Status, zeigt interaktive Bestätigungs‑Overlay; Fehler werden freundlich (lokalisiert, gekürzt) angezeigt. |
| FR-023    | ✅ Erledigt   | Suche nach Rezepten                                                          | UI & API zum Finden von Rezepten anhand mehrerer Kriterien. (siehe Detailbeschreibung weiter unten) |
| FR-024    | ✅ Erledigt   | Zufallsrezepte aus Kochbüchern anzeigen                                      | `Components/Shared/RandomFromCookbooks.razor` zeigt pro Cookbook ein zufälliges Rezept (responsive, skeletons während Laden). Nutzt API `GET /api/cookbooks` und `GET /api/recipes/by-cookbook/{id}`; clientseitiges JS‑Interop `wwwroot/js/randomFromCookbooks.js` zur Spaltenberechnung. Empfehlung: UI‑Tests / E2E‑Test für Responsive‑Verhalten und Skeleton‑Count. |
| AUTH-005  | 🕓 Offen      | Autorisierung für Export/Import                                              | Benutzer‑Export: eigener Nutzer. Admin‑Export/All‑Data‑Import: nur `IsAdmin==true`. |
| NFR-010   | 🕓 Offen      | Performance / Skalierung für Exporte                                         | Große Exporte asynchron; Streaming/Chunked ZIP‑Erzeugung; Rate‑Limit/Queue für Admin‑Exporte. |
| NFR-011   | 🕓 Offen      | Sicherheit / Datenschutz beim Export                                         | PII minimieren; Optionale Verschlüsselung für Admin‑Export; Audit‑Log aller Aktionen. |
| NFR-012   | 🕓 Offen      | Import‑Validierung & Safety                                                  | `dryRun` gibt Schema‑und Konflikt‑Report; Upload‑Limits und Quotas. |
| NFR-013   | 🕓 Offen      | Kompatibilität & Upgrade‑Sicherheit                                          | Export enthält `formatVersion` im Manifest; Import ignoriert unbekannte Felder. |

## Ergänzungen / Hinweise (neu)
- Import‑Orchestrator:
  - `ImportOrchestrator` implementiert in `Rezepte.Web.Services.Import` und als Singleton registriert (__ServiceCollectionExtensions__). Verwaltet in‑memory Sessions mit Status, interaktiven Bestätigungen und Resultaten.
  - Handlers (`IImportHandler`) bleiben Scoped; Orchestrator erzeugt Scopes beim Ausführen.
- API‑Änderungen (Import Session):
  - Neue helper‑Endpunkte: `POST /api/cookbooks/import-session/start` (URL), `POST /api/cookbooks/import-session/start-file` (File, multipart) — jeweils mit optionaler `/{cookbookId}` Variante.
  - Gemeinsame Hilfsmethode `StartImportSessionFromStreamAsync` zentralisiert Stream→Session Start im `CookbooksController`.
- Client‑(Blazor) Änderungen:
  - `CreateRecipeDialog` (komponente) zeigt interaktive Bestätigungs‑Overlay statt browser `confirm()`; Overlay ist fokussiert, z‑Index/CSS angepasst.
  - URL‑Input erhält Fokus beim Öffnen; Datei‑Uploads starten nun Session‑Flow statt direkten Sync‑Import.
  - `Components/Shared/PhotoOverlay.razor` zeigt nun quadratische Thumbnails in einem Grid; Klick öffnet ein eigenständiges Großbild‑Overlay (`large-backdrop` / `large-content`). Scoped CSS liegt in `PhotoOverlay.razor.css`. `OnShowLargeImage` EventCallback bleibt verfügbar.
  - `Components/Shared/RandomFromCookbooks.razor` wurde hinzugefügt: wählt responsive eine Anzahl Kochbücher aus und lädt pro Cookbook ein zufälliges Rezept. Implementiert Skeleton‑Platzhalter, responsive Spaltenberechnung via `wwwroot/js/randomFromCookbooks.js` und Resize‑Handler. Empfehlung: optionaler Server‑Endpoint, der die zufällige Auswahl serverseitig liefert, würde Roundtrips reduzieren.
  - `Components/Pages/ScheduledRecipes.razor` zeigt geplante Rezepte auf der Startseite (heute + 2 Tage) im selben Karten‑Layout wie `RandomFromCookbooks`.
  - Shopping‑Liste:
    - Neue Seite `Components/Pages/ShoppingList.razor` (Route `/shopping`) implementiert; Auth‑geschützt.
    - UI unterstützt Simple/Advanced Mode (per Benutzer‑Einstellung `api/settings/me/shoppinglistsimplemode`), Listen‑/Gruppen‑/Item‑CRUD, Merge von Listen beim Wechsel in SimpleMode, Export in Zwischenablage.
    - Recipe‑Integration über `RecipePage.razor`: Button „Zur Einkaufsliste“ mappt `RecipeStep.Ingredients` → `ShoppingItem` und nutzt API‑Endpunkt `POST /api/shoppinglists/{id}/groups/from-recipe` oder erstellt bei Bedarf via `POST /api/shoppinglists`.
    - Server: `ShoppingListsController` stellt die erforderlichen Endpunkte (`GET`, `POST`, `PUT`, `DELETE`, `POST /{id}/groups/from-recipe`) bereit; Controller validiert und persistiert via `IShoppingListService`.
- Fehler‑UX:
  - Technische Exceptions (z. B. Google/Gemini errors) werden serverseitig durch `ImportExceptionHelper.BeautifyExceptionMessage` aufbereitet; volle Details bleiben in Logs, Benutzer sieht kurze, lokalisierte Meldung.
- Tests / Wartung:
  - `AiUsageService` Unit‑Tests (`AiUsageServiceTests`) hinzugefügt; Dummy `TestGeminiClient` zur Isolation. Bitte bestehende Import‑Tests anpassen: Orchestrator (singleton) erfordert Scoping in Tests; Handler‑Mocks bleiben Scoped.
  - Empfohlen: Integrationstest für Session‑Flow (start → poll → confirm → result).
  - UI‑Komponenten (z. B. `PhotoOverlay`, `RandomFromCookbooks`, `ShoppingList`) mit bUnit testen; E2E‑Szenarios für Klick→Großansicht, Responsive‑Skeletons und schnelle Item‑Eingabe empfohlen.
- Security:
  - Endpoints weiterhin autorisiert (Bearer/Cookie). Externe HTTP‑Calls setzen browser‑like Header (UserAgent, Accept, Referrer) um 403‑Risiken zu reduzieren.
- Migration / Ops:
  - `AiRequestLog` um `Type` erweitert und neue Indexe hinzugefügt — Migration __erforderlich__. Führe __dotnet ef migrations add yyyyMMddHHmm_AddAiRequestLogTypeAndIndexes__ und dann __dotnet ef database update__ aus.

## Offene Aufgaben — konkrete Umsetzungsschritte (Kurzliste)
- CAL-003: Algorithmus designen + Unit‑Tests
  - Ziel: für jedes Kalender‑Event frühere Vorbereitungsschritte zurückrechnen (Berücksichtigung `DurationMinutes`, `RequiresOvernightRest`, Puffer).
  - Deliverable: `ICalendarPlannerService` mit `GetPreparationScheduleAsync(userId, DateTime day, ct)`.
- PLAN-001 / PLAN-002: Arbeitsplan & Optimierer
  - MVP: Arbeitsplan‑Entity + API zum Erstellen/Abrufen.
  - Optimierungsphase: heuristische Scheduler (Greedy / Constraint Solver) als BackgroundJob; Metriken zur Evaluierung.
- SHOP-001 / SHOP-002: Einkaufsliste — Verifikation & Tests
  - Implementierung abgeschlossen; noch ausstehend: Unit‑ und E2E‑Tests für schnelle Eingabe‑Fokus, Merge‑Workflow beim Wechsel des Modus und Recipe→List Integration.
  - UI: Testfälle für Simple/Advanced Mode, Gruppen‑CRUD, Item‑CRUD und Export.
- FR-020 / FR-021 (Export):
  - Implementiere Streaming ZIP‑Erzeugung (`IExportService`) und Queue für Admin‑Exporte (BackgroundService).
  - Audit‑Log und PII‑Filter (NFR‑011) beachten.
  - API: Export‑Jobs mit Status (queued/running/done/error), Download‑SAS/TempLink.
- AUTH-005: Autorisierung für Export/Import
  - Policy `IsAdmin` für Admin Exporte / All‑Data Import; Policy `IsOwnerOrAdmin` für Benutzer‑Export.
- NFR-010..NFR-013: Architektur‑Ergänzungen
  - Exporte: Streaming + Pagination für große Datenmengen; Load‑Test schreiben / Profiling (siehe Hinweise).
  - Import: `dryRun` Report, Quotas, fail‑safe.
  - Kompatibilität: `formatVersion` im Export‑Manifest, Import tolerant gegenüber unbekannten Feldern.
- Tests / CI:
  - Integrationstest für Import Session Flow.
  - E2E Szenarios für UI‑Komponenten (Playwright).
  - CI: Migrationen in Pipeline laufen lassen; DB‑Update optional in Deploy‑Stage.

## Prioritäten (Empfohlen)
- Kurzfristig (High): NFR‑011 (Datenschutz bei Export), AUTH‑005 (Autorisierung), FR‑020 (Benutzer‑Export minimal).
- Mittelfristig (Medium): NFR‑010 (Performance Exporte), FR‑021 (Admin‑Export + Queue), SHOP‑001/002 Tests & Verifikation.
- Langfristig (Low): PLAN‑001/002 (Advanced Scheduling), CAL‑003 Feinschliff.

## Deploy / Migration Reminder
- Neue Migration für `AiRequestLog` ist erforderlich. Lokales Vorgehen:
  - __dotnet ef migrations add yyyyMMddHHmm_AddAiRequestLogTypeAndIndexes__
  - __dotnet ef database update__
- CI/CD: Migration optional in Deploy‑Job; bei Shared DB Abstimmung vor dem Ausrollen.

## Hinweise für Entwickler
- Singletons (z. B. `ImportOrchestrator`) dürfen keine Scoped Services direkt injizieren — Factory / `IServiceScopeFactory` wird bereits verwendet.
- Vermeide Blocking‑Calls in BackgroundServices; alle IO‑Methoden `async`/`await`.
- Sensible Informationen (Export‑Keys, PII) nicht in Logs.
- ShoppingList‑Hinweis: `Components/Pages/ShoppingList.razor` ist UI‑zentrisch; Business‑Logik (z. B. Zusammenführen, Mapping von Rezept‑Zutaten) gehört in `IShoppingListService`/`ShoppingListsController`. Tests für `ShoppingListsController` und `IShoppingListService` empfohlen.

