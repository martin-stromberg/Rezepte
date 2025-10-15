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
| BOOK-001  | ✅ Erledigt   | Benutzer kann beliebig viele Kochbücher erstellen                            | Kochbuch-Entity mit Benutzerreferenz; UI/Endpoints zum Erstellen vorhanden (`Cookbooks.razor`, `POST /api/cookbooks`). |
| BOOK-002  | ✅ Erledigt   | Rezepte können mehreren Kochbüchern zugeordnet werden                        | Many-to-Many Beziehung zwischen Rezept und Kochbuch; UI: Mehrfachzuweisung (Multi-Assign Overlay) und API‑Endpunkte vorhanden. |
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
| FR-020    | 🕓 Offen      | Benutzer-Export: Anwender kann eigene Rezeptesammlung exportieren            | API: `GET /api/exports/me?format=zip|json` startet Export-Job; synchron für kleine Exporte, asynchron (Job + Download-Link) bei großen Exporten. Format: ZIP mit `recipes.json` (JSON-Array mit Rezepten, Schritten, Zutaten, Metadaten) und `images/` (Binärdateien, Dateinamen referenziert in JSON). Backend: `IExportService.ExportUserAsync(userId, format, ct)`; Auth: nur eigener Nutzer. |
| FR-021    | 🕓 Offen      | Administrator-Export: Admin kann vollständigen Datenexport durchführen       | API: `POST /api/admin/exports` (Admin-Role) startet asynchronen Export-Job für alle Daten; Ergebnis ZIP wie FR-020, zusätzlich `metadata.json` (Export-Zeit, Version, counts). Export wird als Background-Job ausgeführt, Download per zeitlich begrenztem Link. Service: `IExportService.ExportAllAsync(...)`. Audit-Log-Eintrag bei Start/Abschluss. |
| FR-022    | 🕓 Offen      | Importfunktion: Upload von Exportformat zur Erstellung neuer Rezepte        | API: `POST /api/imports` akzeptiert Export-ZIP (Format wie FR-020/FR-021). Import-Optionen: `dryRun=true`, `conflict=skip|replace|duplicate`. Service: `IImportService.ImportAsync(stream, options, userId, ct)` validiert Manifest, prüft Größe/Typ, entpackt, speichert Rezepte und Bilder, erzeugt Ids neu bei `duplicate`. Transactional pro Rezept; Fehlerprotokoll im Result. Nur authentifizierte Nutzer; Admin kann `importAs=userId`. |
| AUTH-005  | 🕓 Offen      | Autorisierung für Export/Import                                             | Benutzer-Export: eigener Nutzer. Admin-Export/All-Data-Import: nur `IsAdmin==true`. Download-Links nur mit kurzlebigen Tokens. |
| NFR-010   | 🕓 Offen      | Performance / Skalierung für Exporte                                         | Große Exporte asynchron; Streaming/Chunked ZIP-Erzeugung; max. synchronous export size z.B. 20 MB. Rate-Limit/Queue für Admin-Exporte. |
| NFR-011   | 🕓 Offen      | Sicherheit / Datenschutz beim Export                                         | PII minimieren; Exporte verschlüsseln optional (passwortgeschützt ZIP) für Admin-Export; Audit-Log aller Export/Import Aktionen. |
| NFR-012   | 🕓 Offen      | Import-Validierung & Safety                                                   | `dryRun` gibt Schema- und Konflikt-Report; Imports prüfen ContentType und Bildgrößen; Standard-Limits (z.B. 100 MB pro Upload) und Quotas pro Benutzer. |
| NFR-013   | 🕓 Offen      | Kompatibilität & Upgrade-Sicherheit                                          | Export enthält `formatVersion` in Manifest; Import ignoriert unbekannte Felder; Migrationspfade für ältere Export-Versionen dokumentieren. |

Hinweis zur Implementierung
- Datenmodell: Export liefert nur DTOs (keine EF-Entitäten) — mapping in Service-Schicht. Bilder als separate Dateien, JSON referenziert Dateinamen statt Base64 (bessere Performance).  
- Monitoring: `ILogger`-Einträge bei Start/Ende, Fehler-Details im Import-Result; Export/Import-Jobs persistent (z. B. DB-Tabelle `Jobs`) für Nachverfolgbarkeit.  
- CLI / Administration: Möglichkeit, Exporte im Background-Service zu planen oder per CLI zu triggern (optional).
