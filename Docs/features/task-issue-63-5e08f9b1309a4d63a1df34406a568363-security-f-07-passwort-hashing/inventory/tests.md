# Tests

## Testklassen

### `UserServiceTests`
Datei: `Rezepte.Tests/Services/UserServiceTests.cs` (xUnit + FluentAssertions, EF Core InMemory)

- `RegisterAsync_ShouldCreateFirstUserAsAdmin_WhenNoUsersExist` — Registrierung + Admin-Flag
- `RegisterAsync_ShouldFail_WhenUsernameAlreadyExists` — Eindeutigkeit
- `LoginAsync_ShouldReturnUser_WhenPasswordValid` — Happy Path Login
- `LoginAsync_ShouldReturnNull_WhenPasswordInvalid` — falsches Passwort
- `UpdateProfileAsync_ShouldUpdateUsernameAndEmail_WhenValid` — Profil-Update
- `RegisterAsync_ShouldFail_WhenUsernameIsReserved` — reservierter Name
- `UpdateProfileAsync_ShouldFail_WhenUsernameIsInvalid` — Validierung
- `UpdateUserAsync_ShouldFail_WhenUsernameIsInvalid` — Admin-Update-Validierung
- `ChangePasswordAsync_ShouldChange_WhenCurrentPasswordMatches` — Passwortwechsel inkl. Login mit neuem/altem Passwort
- `UpdateUserAsync_ShouldSetAdminFlag` — Admin-Flag
- `DeleteAsync_ShouldRemoveUser` — Löschen

**Keine Tests für `PasswordHasher` vorhanden** — kein `Rezepte.Tests/Security/`-Verzeichnis.

## Hilfsmethoden

### `UserServiceTests`
- `CreateDb()` — `RezepteDbContext` mit `UseInMemoryDatabase(Guid.NewGuid())`
- `CreateSut(db)` — `new UserService(db, new UsernameValidator())`
