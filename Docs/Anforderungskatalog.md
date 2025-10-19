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
| CAL-001   | 🕓 Offen      | Jeder Benutzer hat einen Kalender                                            | Kalender‑View mit Benutzerbindung. |
| CAL-002   | 🕓 Offen      | Rezepte können im Kalender eingeplant werden                                 | Rezept‑Zuordnung zu Datum mit Vorbereitungslogik. |
| CAL-003   | 🕓 Offen      | Vorbereitungen an Vortagen werden automatisch erkannt                        | Algorithmus zur Rückrechnung basierend auf Dauer und Ruhezeit. |
| PLAN-001  | 🕓 Offen      | Arbeitsplan kombiniert mehrere Rezepte                                       | Arbeitsplan‑Entity mit Rezeptreferenzen. |
| PLAN-002  | 🕓 Offen      | Schritte werden zeitlich optimiert (z. B. Dessert vor Hauptgericht)           | Sortierlogik nach Zubereitungszeit und Rezepttyp. |
| SHOP-001  | 🕓 Offen      | Zutaten aus Arbeitsplan können in Einkaufsliste übernommen werden            | Zutatenextraktion aus Arbeitsplan. |
| SHOP-002  | 🕓 Offen      | Zutaten können als erledigt abgehakt werden                                  | Checkbox‑Status pro Zutat in der Einkaufsliste. |
| KI-001    | ✅ Erledigt  | Rezepterfassung per KI aus Fotos/Webseiten (langfristig)                      | Infrastruktur: Gemini/Vision‑Clients vorhanden; Einstellungen & Laufzeit‑Guards implementiert (siehe KI-002/KI-003). Extraktion/Parser/Prompts in Weiterentwicklung. |
| KI-002    | ✅ Erledigt   | Benutzerbezogene KI‑Einstellung (User kann KI für eigenes Konto aktivieren)  | `UserSetting` Entity, `SettingsService`, API `GET /api/settings/me` und `PUT /api/settings/me/ai` sowie Blazor‑Component `AiSettings.razor` implementiert. |
| KI-003    | ✅ Erledigt   | Globale KI‑Deaktivierung durch Admin                                         | `AppSetting` Entity, `SettingsService`, Admin‑API `GET /api/settings/global` und `PUT /api/settings/global/ai`; UI: Admin sieht globalen Switch in `AiSettings`. |
| IMG-001   | ✅ Erledigt   | Bild‑Zuschneiden vor Upload (Clientseitig)                                   | `ImageCropper` Component (Cropper.js + JS Interop) + direct fetch upload (`imageCropper.uploadCroppedBlob`) implementiert; verhindert große SignalR/Base64‑Transfers. |
| IMG-002   | ✅ Erledigt   | iOS Safari kompatibler File‑Trigger                                          | Label‑trigger für verstecktes `InputFile` (1×1 px, nicht display:none) zur zuverlässigen Öffnung des Dateidialogs. |
| SYS-001   | ✅ Erledigt   | AI‑Aufrufe runtime‑guard                                                     | AI‑Handler (z. B. `BaseAIImportHandler`, `AIFotoImportHandler`) prüfen vor externen API‑Aufrufen zuerst `SettingsService.GetGlobalAiEnabledAsync` und `GetUserAiEnabledAsync(userId)`. |
| DB-002    | ✅ Erledigt   | Settings‑Tabellen und Mapping                                                | `UserSetting` und `AppSetting` in `RezepteDbContext` registriert; Modell‑Konfiguration und Indexe hinzugefügt; Migration erstellt/applied empfohlen. |
| INF-001   | ✅ Erledigt   | Tokenbereitstellung für Browser‑Uploads                                      | `ITokenService` + `ApiAuthHandler` sorgen für gültige JWT; ImageCropper liest serverseitigen Token bei Blazor Server und übergibt an Fetch. |
| FR-020    | 🕓 Offen      | Benutzer‑Export: Anwender kann eigene Rezeptesammlung exportieren            | API: `GET /api/exports/me?format=zip|json` startet Export‑Job; Backend: `IExportService.ExportUserAsync(userId, format, ct)`. |
| FR-021    | 🕓 Offen      | Administrator‑Export: Admin kann vollständigen Datenexport durchführen       | API: `POST /api/admin/exports` (Admin‑Role) startet asynchronen Export‑Job. |
| FR-022    | ✅ Erledigt   | Importfunktion: Upload von Exportformat zur Erstellung neuer Rezepte         | API: session‑basierte Import‑Flows implementiert. Endpunkte: URL‑Start `POST /api/cookbooks/import-session/start`, File‑Start `POST /api/cookbooks/import-session/start-file` (je mit/ohne `cookbookId` Varianten). `ImportOrchestrator` verwaltet Import‑Sessions (waiting/confirm/result). Client (Blazor) pollt Status, zeigt interaktive Bestätigungs‑Overlay; Fehler werden freundlich (lokalisiert, gekürzt) angezeigt. |
| FR-023    | ✅ Erledigt   | Suche nach Rezepten                                                          | UI & API zum Finden von Rezepten anhand mehrerer Kriterien. (siehe Detailbeschreibung weiter unten) |
| AUTH-005  | 🕓 Offen      | Autorisierung für Export/Import                                              | Benutzer‑Export: eigener Nutzer. Admin‑Export/All‑Data‑Import: nur `IsAdmin==true`. |
| NFR-010   | 🕓 Offen      | Performance / Skalierung für Exporte                                         | Große Exporte asynchron; Streaming/Chunked ZIP‑Erzeugung; Rate‑Limit/Queue für Admin‑Exporte. |
| NFR-011   | 🕓 Offen      | Sicherheit / Datenschutz beim Export                                         | PII minimieren; Optionale Verschlüsselung für Admin‑Export; Audit‑Log aller Aktionen. |
| NFR-012   | 🕓 Offen      | Import‑Validierung & Safety                                                  | `dryRun` gibt Schema‑ und Konflikt‑Report; Upload‑Limits und Quotas. |
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
- Fehler‑UX:
  - Technische Exceptions (z. B. Google/Gemini errors) werden serverseitig durch `ImportExceptionHelper.BeautifyExceptionMessage` aufbereitet; volle Details bleiben in Logs, Benutzer sieht kurze, lokalisierte Meldung.
- Tests / Wartung:
  - Bitte bestehende Import‑Tests anpassen: Orchestrator (singleton) erfordert Scoping in Tests; Handler‑Mocks bleiben Scoped.
  - Empfohlen: Integrationstest für Session‑Flow (start → poll → confirm → result).
- Security:
  - Endpoints weiterhin autorisiert (Bearer/Cookie). Externe HTTP‑Calls setzen browser‑like Header (UserAgent, Accept, Referrer) um 403‑Risiken zu reduzieren.
- Migration / Ops:
  - Keine DB‑Schema‑Änderung nötig für Import‑Sessions (in‑memory). Falls persistent Sessions benötigt werden, planen Sie Migration und Storage.

## FR-023: Suche nach Rezepten (Detailbeschreibung)
- Ziel: Anwender sollen Rezepte schnell und zuverlässig finden können.
- Kernfunktionen:
  - Volltext-/Freitextsuche über `Title`, `Ingredients` (Zutaten), `Steps` (Schritte) und `Tags`.
  - Filter: `cookbookId`, `tag`, `maxPreparationMinutes`, `difficulty` (falls vorhanden).
  - Pagination + Sortierung (relevance, newest, title).
  - Autocomplete/Suggest Endpoint (optional) für schnelle Vorschläge im Suchfeld.
  - Debounce + CancellationToken auf Client‑Seite, serverseitige Timeouts.
- API‑Contract (Vorschlag):
  - `GET /api/recipes/search?q={q}&ingredients={ing}&tags={tags}&cookbookId={id}&page={page}&pageSize={pageSize}&sort={sort}`
  - `GET /api/recipes/suggest?q={q}&limit=10`
  - Antwort: `RecipeSearchResultDto[]` mit `Id, Title, Snippet, PrimaryImageUrl, Cookbooks[], Tags[]`
- UI:
  - Search‑Box in Navbar (globaler Shortcut `"/"`), eigene Seite `Components/Pages/RecipeSearch.razor` für erweiterte Filter.
  - Ergebnisse als Liste/Karten mit Paginierung; Highlighting des Snippets.
  - Accessibility: ARIA attributes, keyboard navigation, screenreader‑friendly labels.
- Implementierungsempfehlungen:
  - SQLite: FTS5 nutzen (Virtual Table + Migrations) für gute Volltext‑Performance und Relevanz. Alternativ per EF.Functions.Like bei kleinen DBs.
  - Bei FTS5: virtuelle Tabelle für `Recipe` (Title, IngredientsText, StepsText) + Triggers/Sync mit Haupttabelle in Migrations.
  - EF Core: Projektion auf DTOs, `AsNoTracking()`, `Take(pageSize)`, `Skip((page-1)*pageSize)`.
  - Caching: MemoryCache für häufige, unveränderte Suchanfragen; Cache‑Keys inkl. Query‑Parameter.
  - Sicherheitsaspekte: Keine sensiblen Daten in Snippets; Logging der Query‑Hashes statt Roh‑Queries bei Bedarf.
  - Tests: Unit‑Tests für Filter/Sort‑Logik (InMemory/SQLite InMemory), Integrationstest gegen SQLite‑DB mit FTS.
- Migration:
  - Falls FTS5 eingesetzt wird: `dotnet ef migrations add 20251017_AddRecipeSearchFts`; `dotnet ef database update`.
- Performance / NFRs:
  - Max `pageSize` begrenzen (z. B. 50).
  - Indexe auf Filterfeldern (`CookbookId`, `Tags`) sicherstellen.
  - Asynchrone API‑Methods mit CancellationToken.
- Monitoring & Telemetrie:
  - Track query latencies (Histogram), slow‑query logging > 500ms.
- Sichtbarkeit & Priorisierung:
  - MVP: Freitext über Title + Ingredients + Tags, Pagination, einfache Sortierung.
  - Erweiterungen: Fuzzy/typo‑tolerant Search, Synonyme, personalisierte Ranking.

## Hinweise zur Umsetzung der jüngsten Änderungen
- `ImportOrchestrator` als Singleton; Handlers scoped — ServiceRegistration in `ServiceCollectionExtensions` angepasst.
- `StartImportSessionFromStreamAsync` (private Controller‑Methode) reduziert Duplikation für URL/File Start.
- Client: `CreateRecipeDialog` UI‑Änderungen (overlay confirm, input focus, improved messages).
- Import‑Fehler werden lokalisiert/gekürzt durch `ImportExceptionHelper`; Logs enthalten volle Details.
- Testempfehlung: Integrationstest des interaktiven Imports (Session lifecycle).
