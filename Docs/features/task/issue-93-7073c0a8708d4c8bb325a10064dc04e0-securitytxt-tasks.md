# Tasks: security.txt (RFC 9116)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Datenmodell | `SecurityTxtSettings`-Record anlegen (Felder: `bool Enabled`, `string? Contact`, `DateTimeOffset? Expires`, `string? Encryption`, `string? Acknowledgments`, `string? PreferredLanguages`, `string? Canonical`, `string? Policy`, `string? Hiring`) | Erledigt | `SecurityTxtRendererTests.RenderPlainText_ShouldReturnRfc9116Format_WhenAllFieldsSet` |
| 2 | Logik | `ISettingsService` um `GetSecurityTxtSettingsAsync(CancellationToken ct)` erweitern | Erledigt | `SettingsServiceTests.GetSecurityTxtSettingsAsync_ShouldReturnDefaults_WhenNoSettingsExist` |
| 3 | Logik | `ISettingsService` um `SetSecurityTxtSettingsAsync(SecurityTxtSettings settings, CancellationToken ct)` erweitern | Erledigt | `SettingsServiceTests.SetSecurityTxtSettingsAsync_ShouldPersistAllFields` |
| 4 | Logik | `SettingsService` Schlüsselkonstanten `SecurityTxtEnabledKey`, `SecurityTxtContactKey`, `SecurityTxtExpiresKey`, `SecurityTxtEncryptionKey`, `SecurityTxtAcknowledgmentsKey`, `SecurityTxtPreferredLanguagesKey`, `SecurityTxtCanonicalKey`, `SecurityTxtPolicyKey`, `SecurityTxtHiringKey` hinzufügen | Erledigt | `SettingsServiceTests.SetSecurityTxtSettingsAsync_ShouldPersistAllFields` |
| 5 | Logik | `SettingsService.GetSecurityTxtSettingsAsync` implementieren | Erledigt | `SettingsServiceTests.GetSecurityTxtSettingsAsync_ShouldReturnDefaults_WhenNoSettingsExist` |
| 6 | Logik | `SettingsService.SetSecurityTxtSettingsAsync` implementieren (Mehrfachwerte als newline-separierter String) | Erledigt | `SettingsServiceTests.SetSecurityTxtSettingsAsync_ShouldPersistAllFields` |
| 7 | Logik | `ISecurityTxtRenderer`-Interface anlegen (`RenderPlainText`, `RenderMarkdown`, `RenderHtml`) | Erledigt | `SecurityTxtRendererTests.RenderPlainText_ShouldReturnRfc9116Format_WhenAllFieldsSet` |
| 8 | Logik | `SecurityTxtRenderer` implementieren: `RenderPlainText` nach RFC 9116 (Mehrfachwerte als separate `Key:`-Zeilen) | Erledigt | `SecurityTxtRendererTests.RenderPlainText_ShouldRepeatDirective_ForMultilineContact` |
| 9 | Logik | `SecurityTxtRenderer` implementieren: `RenderMarkdown` (Abschnitte als `## Key`) | Erledigt | `SecurityTxtRendererTests.RenderMarkdown_ShouldReturnSectionHeaders` |
| 10 | Logik | `SecurityTxtRenderer` implementieren: `RenderHtml` (`<h2>Key</h2><p>Value</p>`) | Erledigt | `SecurityTxtRendererTests.RenderHtml_ShouldReturnH2AndParagraph` |
| 11 | Logik | `ISecurityTxtRenderer` / `SecurityTxtRenderer` in DI registrieren | Erledigt | Kein direkter Test (`ServiceCollectionExtensions.cs`: `services.AddScoped<ISecurityTxtRenderer, SecurityTxtRenderer>()`) |
| 12 | Middleware | `RedirectToRegisterMiddleware.IsExcluded` um `/security.txt`, `/.well-known/security.txt`, `/.well-known/security.md`, `/.well-known/security.html` erweitern | Erledigt | `SecurityTxtControllerTests.GetSecurityTxt_RequiresNoAuthentication` |
| 13 | Konfiguration | Routing in `Program.cs` prüfen: `MapControllers` greift vor `UseStaticFiles` für `/security.txt` und `/.well-known/*` | Erledigt | Kein direkter Test (keine physischen Dateien in wwwroot für diese Pfade; StaticFiles reicht durch) |
| 14 | Controller | `SecurityTxtController` anlegen: `GET /security.txt` (kein `[Authorize]`, 404 wenn `Enabled = false`, `Content-Type: text/plain`) | Erledigt | `SecurityTxtControllerTests.GetSecurityTxt_ReturnsOk_WhenEnabled`, `SecurityTxtControllerTests.GetSecurityTxt_ReturnsNotFound_WhenDisabled` |
| 15 | Controller | `SecurityTxtController`: `GET /.well-known/security.txt` hinzufügen | Erledigt | `SecurityTxtControllerTests.GetWellKnownSecurityTxt_ReturnsOk_WhenEnabled` |
| 16 | Controller | `SecurityTxtController`: `GET /.well-known/security.md` hinzufügen (`Content-Type: text/markdown`) | Erledigt | `SecurityTxtControllerTests.GetSecurityMd_ReturnsOk_WithMarkdownContentType` |
| 17 | Controller | `SecurityTxtController`: `GET /.well-known/security.html` hinzufügen (`Content-Type: text/html`) | Erledigt | `SecurityTxtControllerTests.GetSecurityHtml_ReturnsOk_WithHtmlContentType` |
| 18 | Controller | `SettingsController.GetGlobalSecurityTxt` anlegen (`GET global/securitytxt`, `[Authorize(Roles = "Admin")]`) | Erledigt | Kein direkter Test |
| 19 | Controller | `SettingsController.SetGlobalSecurityTxt` anlegen (`PUT global/securitytxt`, `[Authorize(Roles = "Admin")]`, Pflichtfeldvalidierung `Contact`/`Expires` wenn `Enabled = true`) | Erledigt | `SettingsControllerSecurityTxtValidationTests.SetGlobalSecurityTxt_ReturnsNoContent_WhenEnabledAndAllRequiredFieldsValid` |
| 20 | Validierung | `SetGlobalSecurityTxt`: HTTP 400 zurückgeben, wenn `Enabled = true` und `Contact` fehlt | Erledigt | `SettingsControllerSecurityTxtValidationTests.SetGlobalSecurityTxt_ReturnsBadRequest_WhenEnabledAndContactMissing` |
| 21 | Validierung | `SetGlobalSecurityTxt`: HTTP 400 zurückgeben, wenn `Enabled = true` und `Expires` fehlt oder in der Vergangenheit liegt | Erledigt | `SettingsControllerSecurityTxtValidationTests.SetGlobalSecurityTxt_ReturnsBadRequest_WhenEnabledAndExpiresMissing` |
| 22 | UI | `SecurityTxtSettings.razor` unter `Components/Settings/` anlegen: Formular mit allen neun Feldern | Erledigt | Kein direkter Test |
| 23 | UI | `SecurityTxtSettings.razor`: Laden der aktuellen Einstellungen via `GET /api/settings/global/securitytxt` beim Init | Erledigt | Kein direkter Test |
| 24 | UI | `SecurityTxtSettings.razor`: Speichern via `PUT /api/settings/global/securitytxt` | Erledigt | Kein direkter Test |
| 25 | UI | `SettingsViewModel` Konstruktor: neues `Item` für `SecurityTxtSettings.razor` mit `isAdmin`-Sichtbarkeit eintragen | Erledigt | Kein direkter Test |
| 26 | Tests | `SecurityTxtRendererTests`: `RenderPlainText_ShouldReturnRfc9116Format_WhenAllFieldsSet` | Erledigt | `SecurityTxtRendererTests.RenderPlainText_ShouldReturnRfc9116Format_WhenAllFieldsSet` |
| 27 | Tests | `SecurityTxtRendererTests`: `RenderPlainText_ShouldRepeatDirective_ForMultilineContact` | Erledigt | `SecurityTxtRendererTests.RenderPlainText_ShouldRepeatDirective_ForMultilineContact` |
| 28 | Tests | `SecurityTxtRendererTests`: `RenderPlainText_ShouldOmitEmptyFields` | Erledigt | `SecurityTxtRendererTests.RenderPlainText_ShouldOmitEmptyFields` |
| 29 | Tests | `SecurityTxtRendererTests`: `RenderMarkdown_ShouldReturnSectionHeaders` | Erledigt | `SecurityTxtRendererTests.RenderMarkdown_ShouldReturnSectionHeaders` |
| 30 | Tests | `SecurityTxtRendererTests`: `RenderHtml_ShouldReturnH2AndParagraph` | Erledigt | `SecurityTxtRendererTests.RenderHtml_ShouldReturnH2AndParagraph` |
| 31 | Tests | `SettingsServiceTests`: `GetSecurityTxtSettingsAsync_ShouldReturnDefaults_WhenNoSettingsExist` | Erledigt | `SettingsServiceTests.GetSecurityTxtSettingsAsync_ShouldReturnDefaults_WhenNoSettingsExist` |
| 32 | Tests | `SettingsServiceTests`: `SetSecurityTxtSettingsAsync_ShouldPersistAllFields` | Erledigt | `SettingsServiceTests.SetSecurityTxtSettingsAsync_ShouldPersistAllFields` |
| 33 | Tests | `SecurityTxtControllerTests`: `GetSecurityTxt_ReturnsOk_WhenEnabled` (HTTP 200, `Content-Type: text/plain`) | Erledigt | `SecurityTxtControllerTests.GetSecurityTxt_ReturnsOk_WhenEnabled` |
| 34 | Tests | `SecurityTxtControllerTests`: `GetSecurityTxt_ReturnsNotFound_WhenDisabled` (HTTP 404) | Erledigt | `SecurityTxtControllerTests.GetSecurityTxt_ReturnsNotFound_WhenDisabled` |
| 35 | Tests | `SecurityTxtControllerTests`: `GetWellKnownSecurityTxt_ReturnsOk_WhenEnabled` | Erledigt | `SecurityTxtControllerTests.GetWellKnownSecurityTxt_ReturnsOk_WhenEnabled` |
| 36 | Tests | `SecurityTxtControllerTests`: `GetSecurityMd_ReturnsOk_WithMarkdownContentType` | Erledigt | `SecurityTxtControllerTests.GetSecurityMd_ReturnsOk_WithMarkdownContentType` |
| 37 | Tests | `SecurityTxtControllerTests`: `GetSecurityHtml_ReturnsOk_WithHtmlContentType` | Erledigt | `SecurityTxtControllerTests.GetSecurityHtml_ReturnsOk_WithHtmlContentType` |
| 38 | Tests | `SecurityTxtControllerTests`: `GetSecurityTxt_RequiresNoAuthentication` | Erledigt | `SecurityTxtControllerTests.GetSecurityTxt_RequiresNoAuthentication` |
| 39 | E2E-Tests | Szenario: `GET /security.txt` liefert HTTP 200 mit RFC-9116-Inhalt ohne Authentifizierung (Enabled = true) | Erledigt | `SecurityTxtBrowserTests.GetSecurityTxt_ReturnsOk_WithoutAuthentication_WhenEnabled` |
| 40 | E2E-Tests | Szenario: `GET /security.txt` liefert HTTP 404, wenn `Enabled = false` | Erledigt | `SecurityTxtBrowserTests.GetSecurityTxt_ReturnsNotFound_WhenDisabled` |
| 41 | E2E-Tests | Szenario: Admin konfiguriert security.txt im UI — Menüpunkt sichtbar, Speichern erfolgreich, Inhalt im öffentlichen Abruf korrekt | Erledigt | `SecurityTxtBrowserTests.Admin_CanConfigureSecurityTxtViaUi_AndContentAppearsInPublicEndpoint` |
| 42 | E2E-Tests | Szenario: Benutzer ohne Admin-Rolle sieht den security.txt-Menüpunkt in den Einstellungen nicht | Erledigt | `SecurityTxtBrowserTests.RegularUser_DoesNotSeeSecurityTxtMenuItemInSettings` |
