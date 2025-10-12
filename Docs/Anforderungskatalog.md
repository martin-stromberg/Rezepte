# Anforderungskatalog – Benutzerkontoverwaltung (Blazor Server, .NET 9, SQLite)

| Kennung | Status     | Anforderung                                                                  | Umsetzungsbeschreibung |
|---------|------------|------------------------------------------------------------------------------|------------------------|
| FR-001  | Erledigt   | Datenpersistenz mit SQLite                                                   | `RezepteDbContext` mit SQLite, Schema via EF-Migrationen beim Start (wenn vorhanden). |
| FR-002  | Erledigt   | Passwort-Hashing                                                             | PBKDF2 (`PasswordHasher`) eingesetzt. |
| FR-003  | Erledigt   | Cookie-Authentifizierung für Website                                         | Cookie-Auth in `Program.cs` konfiguriert (`rezepte.auth`), `UseAuthentication/UseAuthorization`. |
| FR-004  | Erledigt   | JWT-Bearer-Authentifizierung für API                                         | `JwtBearer` konfiguriert; `ITokenService` erzeugt HS256-Token; Key auf 256 Bit normalisiert. |
| FR-005  | Erledigt   | Automatische Authorization-Header bei API-Requests                           | `ApiAuthHandler` als `DelegatingHandler`, hängt `Authorization: Bearer <token>` anhand aktuellem Benutzer an. |
| FR-006  | Erledigt   | Login-Endpunkt                                                               | `POST /api/session/login` (Form-POST, Anti-Forgery ignoriert), setzt Cookie und erstellt JWT; Redirect via `LocalRedirect`. |
| FR-007  | Erledigt   | Logout-Endpunkt                                                              | `POST /api/session/logout` (Form-POST), löscht Cookie, Redirect via `LocalRedirect`. |
| FR-008  | Erledigt   | Login-Seite                                                                  | `Components/Pages/Login.razor` als `application/x-www-form-urlencoded`-Formular (Browser-POST, ReturnUrl-Support). |
| FR-009  | Erledigt   | Registrierung-Endpunkt                                                       | `POST /api/auth/register` akzeptiert Form- oder JSON-Requests; bei Form-POST Redirect zu `/login`. |
| FR-010  | Erledigt   | Registrierungsseite                                                          | `Components/Pages/Register.razor` als Browser-Form; E-Mail optional; Redirect nach Erfolg zu `/login`. |
| FR-011  | Erledigt   | Redirect-Logik global                                                        | Middleware `RedirectToRegisterMiddleware`: bei 0 Nutzern Redirect zu `/register`, sonst bei anonym `/login`; statische/Framework-Pfade ausgenommen; direkter Zugriff auf `/register` nur ohne vorhandene Nutzer. |
| FR-012  | Erledigt   | Navbar abhängig vom Anmeldestatus                                            | `AuthorizeView` in `MainLayout.razor`: Gäste sehen „Anmelden“, angemeldete Nutzer Begrüßung + „Abmelden“. |
| FR-013  | Erledigt   | Kein direkter Link zur Registrierung bei bestehenden Benutzern               | Link entfernt; Middleware blockiert Zugriff auf `/register`, wenn Nutzer existieren. |
| FR-014  | Erledigt   | Logging/Diagnostik im Development                                            | `UseDeveloperExceptionPage`, Blazor `DetailedErrors`, angehobene LogLevel in `appsettings.Development.json`. |
| FR-015  | Erledigt   | DB-Migrationen                                                               | Automatische Migration bei Programmstart (falls Migrationen vorhanden). |
| FR-016  | Erledigt   | Erste Registrierung als Admin markieren                                      | `IUserService.RegisterAsync` setzt `IsAdmin = true` für den ersten registrierten Benutzer (`Entities.User.IsAdmin`). |
| FR-017  | Erledigt   | Admin-Setup-Seite                                                            | Benutzerverwaltung als Einstellung "Benutzer" (`Components/Settings/UserAdmin.razor`), nur für Admins sichtbar; API: `GET/POST/PUT/DELETE /api/admin/users` (Bearer + Rolle Admin). |
| FR-018  | Erledigt   | Form-Handling                                                                | Login/Registrierung senden `x-www-form-urlencoded`; Controller erkennen Form/JSON und liefern bei Form-POST `LocalRedirect`. |
| FR-019  | Erledigt   | Internationalisierung (Deutsch)                                              | UI-Strings Deutsch; Erweiterbarkeit vorbereitet. |
| NFR-001 | Erledigt   | Sicherheit Cookie-Einstellungen                                              | Cookie `HttpOnly`, `SameSite=Lax`, `SecurePolicy=SameAsRequest`; HTTPS empfohlen. |
| NFR-002 | Erledigt   | JWT-Schlüsselstärke                                                          | Secret wird via SHA-256 auf 256 Bit gebracht (HS256-Anforderung). |

Hinweis: Für FR-015 (Migrationen) bitte per CLI ausführen: `dotnet ef migrations add InitialCreate` und `dotnet ef database update` (vorher Anwendung stoppen). Für FR-017 sind Datenmodell- und UI-Erweiterungen umgesetzt; die Seite ist als Einstellungsmodul integriert.
