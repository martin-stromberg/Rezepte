# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### Rezepte.Tests.Browser/SecurityTxtBrowserTests.cs (SecurityTxtBrowserTests)

- **Testqualität** — `GetSecurityTxt_ReturnsNotFound_WhenDisabled` trifft keine eigene Arrange-Aussage zum Deaktivierungszustand und verlässt sich auf globalen Zustand aus anderen Tests. Da andere Tests in derselben Klasse `security.txt` aktivieren, entsteht Reihenfolge-/Seiteneffekt-Risiko.

  Empfehlung: In jedem Test den benötigten Zustand explizit herstellen (z. B. vorab über API/UI deaktivieren) oder pro Test eine isolierte App-/Datenbasis verwenden.

### Rezepte.Tests.Controllers/SecurityTxtControllerTests.cs (SecurityTxtControllerTests)

- **Testqualität** — `GetSecurityTxt_RequiresNoAuthentication` ruft die Controller-Methode direkt auf und prüft nur, dass kein `UnauthorizedResult`/`ForbidResult` zurückkommt. Das testet die ASP.NET-Authentifizierungspipeline nicht und kann damit fälschlich Sicherheit suggerieren.

  Empfehlung: Als Integrations-/WebApplicationFactory-Test unauthentifiziert gegen `/security.txt` aufrufen und den HTTP-Status (nicht 401/403) prüfen.

### Rezepte.Web/Services/SettingsService.cs (SettingsService)

- **Kopplung und Erweiterbarkeit** — Der Konstruktor erzeugt bei fehlender DI-Abhängigkeit selbst `new SecurityTxtSettingsService(db)`. Damit ist die Implementierung fest verdrahtet und schlechter austauschbar/testbar.

  Empfehlung: `ISecurityTxtSettingsService` als verpflichtende Konstruktorabhängigkeit injizieren und die manuelle Instanziierung entfernen.

## Geprüfte Dateien

- `Rezepte.Tests.Browser/Infrastructure/SecurityTxtPageObject.cs`
- `Rezepte.Tests.Browser/SecurityTxtBrowserTests.cs`
- `Rezepte.Tests/Controllers/SecurityTxtControllerTests.cs`
- `Rezepte.Tests/Controllers/SettingsControllerSecurityTxtValidationTests.cs`
- `Rezepte.Tests/Services/SecurityTxtRendererTests.cs`
- `Rezepte.Tests/Services/SettingsServiceTests.cs`
- `Rezepte.Web/Components/Pages/Settings.razor`
- `Rezepte.Web/Components/Settings/SecurityTxtSettings.razor`
- `Rezepte.Web/Controllers/SecurityTxtController.cs`
- `Rezepte.Web/Controllers/SettingsController.cs`
- `Rezepte.Web/Dtos/SecurityTxtSettings.cs`
- `Rezepte.Web/Extensions/ServiceCollectionExtensions.cs`
- `Rezepte.Web/Middleware/RedirectToRegisterMiddleware.cs`
- `Rezepte.Web/Services/ISecurityTxtRenderer.cs`
- `Rezepte.Web/Services/ISettingsService.cs`
- `Rezepte.Web/Services/SecurityTxtRenderer.cs`
- `Rezepte.Web/Services/SettingsService.cs`
- `Rezepte.Web/ViewModels/SettingsViewModel.cs`
