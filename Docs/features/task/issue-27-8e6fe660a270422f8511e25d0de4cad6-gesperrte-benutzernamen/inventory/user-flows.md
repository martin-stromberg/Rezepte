# Benutzerfluesse und Einstiegspunkte

## Oeffentliche Registrierung

- `Rezepte.Web/Components/Pages/Register.razor` rendert ein Blazor-Formular mit `Action="api/auth/register"` und `Method="post"`.
- `Rezepte.Web/Controllers/AuthController.cs` liest bei `POST api/auth/register` entweder Formularfelder (`Username`, `Password`, `Email`) oder JSON (`RegisterRequest`).
- Der Controller prueft aktuell nur, ob Benutzername und Passwort nicht leer sind. Bei Formularfehlern wird pauschal nach `/register?error=1` umgeleitet; JSON-Clients erhalten die Service-Fehlermeldung.
- Die eigentliche Anlage erfolgt ueber `IUserService.RegisterAsync(username, password, ct)`.

Relevanz: Die zentrale serverseitige Validierung muss in `RegisterAsync` greifen. Fuer API-Clients koennen genaue deutsche Meldungen direkt aus dem Service kommen. Fuer Formularregistrierung gibt es aktuell keine differenzierte Anzeige der Service-Fehler.

## Admin-Benutzeranlage

- `Rezepte.Web/ViewModels/UserAdminViewModel.cs` prueft in `CreateAsync` lokal nur Pflichtfelder, trimmt Benutzername/E-Mail und sendet JSON an `api/admin/users`.
- `Rezepte.Web/Controllers/AdminUsersController.cs` prueft beim `POST` derzeit Benutzername Mindestlaenge 3, E-Mail und Passwort.
- Danach ruft der Controller `IUserService.RegisterAsync(dto.Username, dto.Password, ct)` auf.
- Falls `IsAdmin` gesetzt ist, wird anschliessend `UpdateUserAsync` fuer das Admin-Flag aufgerufen.

Relevanz: Admin-Erstellung erreicht bereits `RegisterAsync`, wird aber vorher vom Controller mit unvollstaendiger Logik abgefangen. Diese Vorpruefung darf neue Regeln nicht umgehen oder mit abweichenden Fehlermeldungen verdecken.

## Admin-Benutzerbearbeitung

- `UserAdminViewModel.SaveAsync` sendet den bearbeiteten `UserRow` per `PUT api/admin/users/{id}`.
- `AdminUsersController.Update` reicht direkt an `IUserService.UpdateUserAsync(id, dto.Username, dto.Email, dto.IsAdmin, ct)` weiter.
- `UserService.UpdateUserAsync` prueft aktuell nur Pflichtfeld/Mindestlaenge, Eindeutigkeit und E-Mail.

Relevanz: Dies ist ein direkter Einstiegspunkt fuer geaenderte Benutzernamen. Die zentrale Validierung muss hier ebenfalls greifen.

## Profilbearbeitung durch Benutzer

- `UserProfileViewModel.SaveProfileAsync` trimmt `Profile.Username` und sendet `UpdateProfileRequest` an `PUT api/users/me`.
- `UsersController.UpdateMe` prueft `ModelState` und ruft `IUserService.UpdateProfileAsync`.
- `UserService.UpdateProfileAsync` prueft aktuell Pflichtfeld/Mindestlaenge, Eindeutigkeit und E-Mail.

Relevanz: Obwohl die Anforderung Admin-Bearbeitung explizit nennt, ist auch die vorhandene Benutzer-Selbstbearbeitung ein Weg zur Benutzernamensaenderung und muss dieselben Regeln verwenden.

## Login und Authentifizierung

- `UserService.LoginAsync` sucht Benutzer per exaktem `Username` und prueft das Passwort.
- `FindByUsernameAsync` sucht ebenfalls exakt.

Relevanz: Die neue Validierung sollte bestehende Login-Semantik nicht unbeabsichtigt aendern. Falls Normalisierung oder Case-insensitive Eindeutigkeit geplant wird, muss dies bewusst entschieden werden.

