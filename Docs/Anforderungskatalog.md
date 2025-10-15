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
| BOOK-002  | ✅ Erledigt   | Rezepte können mehreren Kochbüchern zugeordnet werden                        | Many‑to‑Many Beziehung zwischen Rezept und Kochbuch; UI: Mehrfachzuweisung (Multi‑Assign Overlay) und API‑Endpunkte vorhanden. |
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
| CAL-003   | 🕓 Offen      | Vorbereitungen an Vortagen werden automatisch erkannt                         | Algorithmus zur Rückrechnung basierend auf Dauer und Ruhezeit. |
| PLAN-001  | 🕓 Offen      | Arbeitsplan kombiniert mehrere Rezepte                                       | Arbeitsplan‑Entity mit Rezeptreferenzen. |
| PLAN-002  | 🕓 Offen      | Schritte werden zeitlich optimiert (z. B. Dessert vor Hauptgericht)          | Sortierlogik nach Zubereitungszeit und Rezepttyp. |
| SHOP-001  | 🕓 Offen      | Zutaten aus Arbeitsplan können in Einkaufsliste übernommen werden            | Zutatenextraktion aus Arbeitsplan. |
| SHOP-002  | 🕓 Offen      | Zutaten können als erledigt abgehakt werden                                  | Checkbox‑Status pro Zutat in der Einkaufsliste. |
| KI-001    | 📌 Geplant    | Rezepterfassung per KI aus Fotos/Webseiten (zukünftig)                       | Platzhalter für KI‑Modul, z. B. ML.NET oder Azure Cognitive Services. |
| FR-020    | 🕓 Offen      | Benutzer‑Export: Anwender kann eigene Rezeptesammlung exportieren            | API: `GET /api/exports/me?format=zip|json` startet Export‑Job; Backend: `IExportService.ExportUserAsync(userId, format, ct)`. |
| FR-021    | 🕓 Offen      | Administrator‑Export: Admin kann vollständigen Datenexport durchführen       | API: `POST /api/admin/exports` (Admin‑Role) startet asynchronen Export‑Job. |
| FR-022    | 🕓 Offen      | Importfunktion: Upload von Exportformat zur Erstellung neuer Rezepte        | API: `POST /api/imports` akzeptiert Export‑ZIP (Format wie FR‑020/FR‑021). |
| AUTH-005  | 🕓 Offen      | Autorisierung für Export/Import                                             | Benutzer‑Export: eigener Nutzer. Admin‑Export/All‑Data‑Import: nur `IsAdmin==true`. |
| NFR-010   | 🕓 Offen      | Performance / Skalierung für Exporte                                         | Große Exporte asynchron; Streaming/Chunked ZIP‑Erzeugung; Rate‑Limit/Queue für Admin‑Exporte. |
| NFR-011   | 🕓 Offen      | Sicherheit / Datenschutz beim Export                                         | PII minimieren; Optionale Verschlüsselung für Admin‑Export; Audit‑Log aller Aktionen. |
| NFR-012   | 🕓 Offen      | Import‑Validierung & Safety                                                   | `dryRun` gibt Schema‑ und Konflikt‑Report; Upload‑Limits und Quotas. |
| NFR-013   | 🕓 Offen      | Kompatibilität & Upgrade‑Sicherheit                                          | Export enthält `formatVersion` im Manifest; Import ignoriert unbekannte Felder. |

Hinweis zur Implementierung
- `Recipe`‑Bilder: entity `RecipeImage`, DbContext‑Mapping und Controller/Service Endpunkte (Upload/Download/Delete) sind implementiert. Upload enthält Content‑Type‑Whitelist, konfigurierbare Max‑Size (`ImageOptions`) und ETag/Cache‑Header beim Download.
- `OrderIndex` für `Cookbook` wurde ergänzt; EF‑Migration erforderlich (`dotnet ef migrations add yyyyMMddHHmm_CookbookOrderIndex` + `dotnet ef database update`).
- Falls weitere Änderungen am Modell nötig sind, bitte Migrationen nach Anleitung erstellen und anwenden.
