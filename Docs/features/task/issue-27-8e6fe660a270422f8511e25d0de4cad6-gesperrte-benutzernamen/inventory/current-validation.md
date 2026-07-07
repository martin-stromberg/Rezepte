# Aktueller Validierungsstand

## Service-Layer

`Rezepte.Web/Services/UserService.cs` ist die wichtigste zentrale Stelle.

- `RegisterAsync` prueft aktuell nur, ob ein Benutzer mit exakt gleichem `Username` existiert. Es gibt keine Leerwert-, Mindestlaengen-, Maximal-, Zeichen- oder Sperrlistenpruefung in dieser Methode.
- `UpdateProfileAsync` prueft `string.IsNullOrWhiteSpace(username) || username.Length < 3`.
- `UpdateUserAsync` prueft ebenfalls `string.IsNullOrWhiteSpace(username) || username.Length < 3`.
- Eindeutigkeitspruefungen sind vorhanden:
  - Registrierung: `AnyAsync(u => u.Username == username)`
  - Profil: bei geaendertem Namen `AnyAsync(u => u.Username == username)`
  - Admin-Update: `AnyAsync(u => u.Username == username && u.Id != id)`

Bewertung: Der Service ist fachlich der richtige Ort, aber die Regeln sind unvollstaendig und teilweise inkonsistent. Besonders `RegisterAsync` laesst aktuell z. B. zu kurze oder leerraumhaltige Namen durch, sofern der Controller dies nicht vorher verhindert.

## Controller und DTOs

- `RegisterRequest` in `Rezepte.Web/Contracts/AuthDtos.cs` nutzt `[Required, MinLength(3)]` fuer `Username`; `AuthController.Register` wertet `ModelState` beim JSON-Lesen aber nicht explizit aus.
- `UpdateProfileRequest` in `Rezepte.Web/Contracts/UserDtos.cs` nutzt `[Required, MinLength(3)]`; `UsersController.UpdateMe` prueft `ModelState`.
- `AdminUsersController.Create` hat eigene Inline-Regeln fuer Benutzername, E-Mail und Passwort.
- `AdminUsersController.Update` delegiert ohne eigene Username-Pruefung an den Service.

Bewertung: DataAnnotations sind hilfreich fuer ModelState, ersetzen aber die geforderte zentrale serverseitige Validierung nicht. Die Admin-Create-Inline-Pruefung ist eine Duplikationsstelle.

## ViewModels und UI

- `UserAdminViewModel.NewUserModel.Username` und `UserRow.Username` verwenden `[Required, MinLength(3)]`.
- `UserAdminViewModel.CreateAsync` prueft Pflichtfelder lokal und zeigt bei API-Fehlern nur `Anlegen fehlgeschlagen.` an, ohne die API-Fehlermeldung auszulesen.
- `UserProfileViewModel.ProfileModel.Username` verwendet `[Required, MinLength(3)]`; API-Fehler werden aus `message` gelesen und angezeigt.
- `Register.razor` hat kein DataAnnotations-Modell fuer Username-Regeln; das Formular postet direkt an den Auth-Endpunkt.

Bewertung: Die UI ist nicht der Sicherheitsanker. Fuer bessere Akzeptanzkriterien sollten API-Fehlermeldungen aber moeglichst nicht durch pauschale UI-Fehler verdeckt werden.

## Fehlermeldungen

Der Bestand enthaelt gemischte deutsche und englische Meldungen, z. B. `Username already taken.`, `User not found.`, `Der Benutzername muss mindestens 3 Zeichen haben.`. In mehreren Dateien sind Umlaute in der gelesenen Darstellung fehlerhaft (`ung�ltig`, `L�schen`).

Bewertung: Die neue Validierung sollte deutschsprachige Meldungen zentral liefern. Bestehende Encoding-Probleme sollten nicht durch weitere falsch codierte Texte verstaerkt werden; am sichersten sind entweder korrekt gespeicherte UTF-8-Dateien oder ASCII-Umschreibungen, falls der Bestand uneinheitlich ist.

