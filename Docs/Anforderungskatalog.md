# Anforderungskatalog – Kochrezepte-Verwaltungssystem (Blazor Server, .NET 9, SQLite)

| Kennung   | Status        | Anforderung                                                                  | Umsetzungsbeschreibung |
|-----------|---------------|------------------------------------------------------------------------------|------------------------|
| FR-001    | ✅ Erledigt   | Datenpersistenz mit SQLite                                                   | `RezepteDbContext` mit SQLite, Schema via EF-Migrationen beim Start (wenn vorhanden). |
| FR-002    | ✅ Erledigt   | Passwort-Hashing                                                             | PBKDF2 (`PasswordHasher`) eingesetzt. |
| FR-003    | ✅ Erledigt   | Cookie-Authentifizierung für Website                                         | Cookie-Auth in `Program.cs` konfiguriert (`rezepte.auth`), `UseAuthentication/UseAuthorization`. |
| FR-004    | ✅ Erledigt   | JWT-Bearer-Authentifizierung für API                                         | `JwtBearer` konfiguriert; `ITokenService` erzeugt HS256-Token; Key auf 256 Bit normalisiert. |
| FR-005    | ✅ Erledigt   | Automatische Authorization-Header bei API-Requests                           | `ApiAuthHandler` als `DelegatingHandler`, hängt `Authorization: Bearer <token>` anhand aktuellem Benutzer an. |
| FR-006    | ✅ Erledigt   | Login-Endpunkt                                                               | `POST /api/session/login` (Form-POST, Anti-Forgery ignoriert), setzt Cookie und erstellt JWT; Redirect via `LocalRedirect`. |
| FR-007    | ✅ Erledigt   | Logout-Endpunkt                                                              | `POST /api/session/logout` (Form-POST), löscht Cookie, Redirect via `LocalRedirect`. |
| FR-008    | ✅ Erledigt   | Login-Seite                                                                  | `Components/Pages/Login.razor` als `application/x-www-form-urlencoded`-Formular (Browser-POST, ReturnUrl-Support). |
| FR-009    | ✅ Erledigt   | Registrierung-Endpunkt                                                       | `POST /api/auth/register` akzeptiert Form- oder JSON-Requests; bei Form-POST Redirect zu `/login`. |
| FR-010    | ✅ Erledigt   | Registrierungsseite                                                          | `Components/Pages/Register.razor` als Browser-Form; E-Mail optional; Redirect nach Erfolg zu `/login`. |
| FR-011    | ✅ Erledigt   | Redirect-Logik global                                                        | Middleware `RedirectToRegisterMiddleware`: bei 0 Nutzern Redirect zu `/register`, sonst bei anonym `/login`; statische/Framework-Pfade ausgenommen; direkter Zugriff auf `/register` nur ohne vorhandene Nutzer. |
| FR-012    | ✅ Erledigt   | Navbar abhängig vom Anmeldestatus                                            | `AuthorizeView` in `MainLayout.razor`: Gäste sehen „Anmelden“, angemeldete Nutzer Begrüßung + „Abmelden“. |
| FR-013    | ✅ Erledigt   | Kein direkter Link zur Registrierung bei bestehenden Benutzern               | Link entfernt; Middleware blockiert Zugriff auf `/register`, wenn Nutzer existieren. |
| FR-014    | ✅ Erledigt   | Logging/Diagnostik im Development                                            | `UseDeveloperExceptionPage`, Blazor `DetailedErrors`, angehobene LogLevel in `appsettings.Development.json`. |
| FR-015    | ✅ Erledigt   | DB-Migrationen                                                               | Automatische Migration bei Programmstart (falls Migrationen vorhanden). |
| FR-016    | ✅ Erledigt   | Erste Registrierung als Admin markieren                                      | `IUserService.RegisterAsync` setzt `IsAdmin = true` für den ersten registrierten Benutzer (`Entities.User.IsAdmin`). |
| FR-017    | ✅ Erledigt   | Admin-Setup-Seite                                                            | Benutzerverwaltung als Einstellung "Benutzer" (`Components/Settings/UserAdmin.razor`), nur für Admins sichtbar; API: `GET/POST/PUT/DELETE /api/admin/users` (Bearer + Rolle Admin). |
| FR-018    | ✅ Erledigt   | Form-Handling                                                                | Login/Registrierung senden `x-www-form-urlencoded`; Controller erkennen Form/JSON und liefern bei Form-POST `LocalRedirect`. |
| FR-019    | ✅ Erledigt   | Internationalisierung (Deutsch)                                              | UI-Strings Deutsch; Erweiterbarkeit vorbereitet. |
| NFR-001   | ✅ Erledigt   | Sicherheit Cookie-Einstellungen                                              | Cookie `HttpOnly`, `SameSite=Lax`, `SecurePolicy=SameAsRequest`; HTTPS empfohlen. |
| NFR-002   | ✅ Erledigt   | JWT-Schlüsselstärke                                                          | Secret wird via SHA-256 auf 256 Bit gebracht (HS256-Anforderung). |
| AUTH-001  | ✅ Erledigt   | Registrierung nur möglich, wenn keine Benutzer existieren                    | Zugriff auf `/register` wird durch `RedirectToRegisterMiddleware` nur erlaubt, wenn 0 Nutzer existieren; weitere Konten werden über Admin-API erstellt. |
| AUTH-002  | ✅ Erledigt   | Erster Benutzer wird Administrator                                           | `IUserService.RegisterAsync` setzt `IsAdmin = true` beim ersten Benutzer. |
| AUTH-003  | ✅ Erledigt   | Administrator kann weitere Benutzer anlegen                                  | Admin-Panel `UserAdmin.razor` und `AdminUsersController` mit `GET/POST/PUT/DELETE /api/admin/users` (JWT + Rolle Admin). |
| AUTH-004  | ✅ Erledigt   | Login/Logout für Benutzer                                                    | `POST /api/session/login` und `POST /api/session/logout`; Website-Cookie + JWT für API (`TokenService`, `ApiAuthHandler`). |
| DB-001    | ✅ Erledigt   | Verwendung von SQLite als Datenbank                                          | EF Core mit SQLite Provider, `RezepteDbContext`, Migrationen/EnsureCreated bei Start. |
| BOOK-001  | 🕓 Offen      | Benutzer kann beliebig viele Kochbücher erstellen                            | Kochbuch-Entity mit Benutzerreferenz. |
| BOOK-002  | 🕓 Offen      | Rezepte können mehreren Kochbüchern zugeordnet werden                        | Many-to-Many Beziehung zwischen Rezept und Kochbuch. |
| RECIPE-001| 🕓 Offen      | Rezept hat Titel                                                             | Property `Title` in der Rezept-Entity. |
| RECIPE-002| 🕓 Offen      | Rezept hat beliebig viele Bilder                                             | Bilder als separate Entity mit Foreign Key zum Rezept. |
| RECIPE-003| 🕓 Offen      | Rezept hat beliebig viele Zubereitungsschritte                               | Schritte als Collection in der Rezept-Entity. |
| STEP-001  | 🕓 Offen      | Schritt hat optionalen Titel                                                 | Property `Title` (nullable) in Schritt-Entity. |
| STEP-002  | 🕓 Offen      | Schritt hat Beschreibung                                                     | Property `Description` in Schritt-Entity. |
| STEP-003  | 🕓 Offen      | Schritt hat Zutatenliste                                                     | Zutaten als Collection in Schritt-Entity. |
| STEP-004  | 🕓 Offen      | Schritt hat Zubereitungsdauer                                                | Property `DurationMinutes` in Schritt-Entity. |
| STEP-005  | 🕓 Offen      | Schritt kann Ruhezeit über Nacht enthalten                                   | Boolean-Flag `RequiresOvernightRest` in Schritt-Entity. |
| CAL-001   | 🕓 Offen      | Jeder Benutzer hat einen Kalender                                            | Kalender-View mit Benutzerbindung. |
| CAL-002   | 🕓 Offen      | Rezepte können im Kalender eingeplant werden                                 | Rezept-Zuordnung zu Datum mit Vorbereitungslogik. |
| CAL-003   | 🕓 Offen      | Vorbereitungen an Vortagen werden automatisch erkannt                         | Algorithmus zur Rückrechnung basierend auf Dauer und Ruhezeit. |
| PLAN-001  | 🕓 Offen      | Arbeitsplan kombiniert mehrere Rezepte                                       | Arbeitsplan-Entity mit Rezeptreferenzen. |
| PLAN-002  | 🕓 Offen      | Schritte werden zeitlich optimiert (z. B. Dessert vor Hauptgericht)          | Sortierlogik nach Zubereitungszeit und Rezepttyp. |
| SHOP-001  | 🕓 Offen      | Zutaten aus Arbeitsplan können in Einkaufsliste übernommen werden            | Zutatenextraktion aus Arbeitsplan. |
| SHOP-002  | 🕓 Offen      | Zutaten können als erledigt abgehakt werden                                  | Checkbox-Status pro Zutat in der Einkaufsliste. |
| KI-001    | 📌 Geplant    | Rezepterfassung per KI aus Fotos/Webseiten (zukünftig)                       | Platzhalter für KI-Modul, z. B. ML.NET oder Azure Cognitive Services. |

Hinweis: Für FR-015 (Migrationen) bitte per CLI ausführen: `dotnet ef migrations add InitialCreate` und `dotnet ef database update` (vorher Anwendung stoppen). Für FR-017 sind Datenmodell- und UI-Erweiterungen umgesetzt; die Seite ist als Einstellungsmodul integriert.

## Statuslegende

| Symbol            | Statusbezeichnung       | Bedeutung                                                                 |
| ----------------- |-------------------------|---------------------------------------------------------------------------|
| 🕓 Offen          | Offen                  | Die Anforderung wurde noch nicht begonnen                                 |
| 🚧 In Arbeit      | In Arbeit              | Die Umsetzung der Anforderung ist im Gange                                |
| ✅ Erledigt       | ✅ Erledigt               | Die Anforderung wurde vollständig umgesetzt und getestet                  |
| 🔍 Review         | Review                 | Die Umsetzung wird aktuell überprüft oder getestet                        |
| 🛠️ Überarbeiten   | Überarbeiten           | Die Umsetzung muss überarbeitet oder korrigiert werden                    |
| ⏸️ Zurückgestellt | Zurückgestellt         | Die Umsetzung wurde pausiert oder ist aktuell nicht priorisiert           |
| ❌ Verworfen      | Verworfen              | Die Anforderung wurde gestrichen und wird nicht umgesetzt                 |
| 📌 Geplant        | Geplant                | Die Anforderung ist für eine zukünftige Version vorgesehen                |
| ⚠️ Blockiert      | Blockiert              | Die Umsetzung ist aktuell nicht möglich (z. B. technische Abhängigkeiten) |
