# Logik

## `PasswordHasher`
Datei: `Rezepte.Web/Security/PasswordHasher.cs`

Statische Klasse, `namespace Rezepte.Web.Security`.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `Hash(string password, int iterations = 100_000)` | public static | PBKDF2-HMAC-SHA256; Salt = 16 Bytes via `RandomNumberGenerator.GetBytes(16)`; Hash = 32 Bytes; Rückgabe `"{iterations}.{saltHex}.{hashHex}"` |
| `Verify(string password, string hashString)` | public static | Splittet auf `.`; erwartet 3 Teile; `int.Parse` der Iterationszahl (ungeprüft, wirft bei ungültigem Wert); `Convert.FromHexString` für Salt/Hash (wirft bei ungültigem Hex); PBKDF2 mit gespeicherter Iterationszahl; Vergleich via `CryptographicOperations.FixedTimeEquals` |

Aufgerufen von: `UserService.RegisterAsync`, `UserService.LoginAsync`, `UserService.ChangePasswordAsync`.

Schwachstellen im Ist-Zustand:
- Keine Min-/Max-Grenzen für `iterations` beim Verifizieren → DoS über manipulierte Hash-Strings möglich (z. B. `int.MaxValue` Iterationen).
- Zu schwache Altparameter werden akzeptiert und nie aktualisiert.
- Malformed Hash-Strings führen zu Exceptions statt `false`.

## `UserService`
Datei: `Rezepte.Web/Services/UserService.cs`

Primärer Konstruktor: `UserService(RezepteDbContext db, IUsernameValidator usernameValidator)`, Basisklasse `BaseService`, implementiert `IUserService`.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `RegisterAsync(username, password, ct)` | public | Validierung via `IUsernameValidator`, Eindeutigkeitsprüfung, `PasswordHasher.Hash(password)`, erste:r User:in wird Admin |
| `LoginAsync(username, password, ct)` | public | Lädt `Entities.User` per Username, `PasswordHasher.Verify` → `User`-Projection oder `null`; **kein Rehash** |
| `FindByUsernameAsync` / `GetByIdAsync` / `HasAnyUsersAsync` | public | Read-only Zugriffe (`AsNoTracking`) |
| `UpdateProfileAsync(id, username, email, ct)` | public | Profil-Update mit Validierung |
| `ChangePasswordAsync(id, currentPassword, newPassword, ct)` | public | `Verify` des alten Passworts, Mindestlänge 6 für neues Passwort, `Hash(newPassword)` + `SaveChangesAsync` |
| `GetAllAsync` / `UpdateUserAsync` / `DeleteAsync` | public | Admin-Funktionen |

## `IUserService`
Datei: `Rezepte.Web/Services/UserService.cs` (gleiche Datei)

Contract wie oben; `LoginAsync` liefert `Task<User?>` — kein Hinweis auf Rehash im Contract erforderlich.

## `Entities.User`
Datei: `Rezepte.Web/Data` bzw. `Entities` (EF-Core-Entität)

| Eigenschaft | Zweck |
|-------------|-------|
| `Id` | Primärschlüssel (string) |
| `Username` | eindeutig |
| `Email` | optional |
| `PasswordHash` | gespeicherter Hash-String im Format `iterations.salt.hash` |
| `IsAdmin` | Admin-Flag |
| `RegistrationTime` | Registrierungszeitpunkt |

Die Entität ist beim Login tracked (`FirstOrDefaultAsync` ohne `AsNoTracking`), ein gesetzter `PasswordHash` kann also direkt per `SaveChangesAsync` persistiert werden — Voraussetzung für Rehash-on-Login ohne weitere Änderungen.
