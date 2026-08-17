# Tests

## Bestehende Testklassen

### `Rezepte.Tests.Controllers.AuthControllerTests`
- Datei: `Rezepte.Tests/Controllers/AuthControllerTests.cs`

Testet Endpunkte des `AuthController` (Registrierung). Keine direkte Berührung des `ApiAuthHandler` oder des `ApiClient`.

### `Rezepte.Tests.Controllers.SecurityTxtControllerTests_Authentication`
- Datei: `Rezepte.Tests/Controllers/SecurityTxtControllerTests_Authentication.cs`

Testet Authentifizierungsaspekte des `SecurityTxtController`.

## Hilfsmethoden

Keine spezialisierten Hilfsmethoden für `ApiClient` oder `ApiAuthHandler` identifiziert.

## Hinweis

`Rezepte.Web.ViewModels.UserProfileViewModel` besitzt keine Unit-Tests. Die bestehenden Tests decken den Auth-Flow im Circuit/Prerender nicht ab.
