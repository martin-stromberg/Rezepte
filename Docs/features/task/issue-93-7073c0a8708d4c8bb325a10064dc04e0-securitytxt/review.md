# Plan-Review

## Ergebnis

**Status:** Vollständig umgesetzt

## Umgesetzte Planelemente

### Neue Klassen und Objekte

- [x] `SecurityTxtSettings` (Record / DTO) — angelegt in `Rezepte.Web/Dtos/SecurityTxtSettings.cs`
- [x] Feld `bool Enabled` in `SecurityTxtSettings` — vorhanden
- [x] Feld `string? Contact` in `SecurityTxtSettings` — vorhanden
- [x] Feld `DateTimeOffset? Expires` in `SecurityTxtSettings` — vorhanden
- [x] Feld `string? Encryption` in `SecurityTxtSettings` — vorhanden
- [x] Feld `string? Acknowledgments` in `SecurityTxtSettings` — vorhanden
- [x] Feld `string? PreferredLanguages` in `SecurityTxtSettings` — vorhanden
- [x] Feld `string? Canonical` in `SecurityTxtSettings` — vorhanden
- [x] Feld `string? Policy` in `SecurityTxtSettings` — vorhanden
- [x] Feld `string? Hiring` in `SecurityTxtSettings` — vorhanden
- [x] `ISecurityTxtRenderer` (Interface) — angelegt in `Rezepte.Web/Services/ISecurityTxtRenderer.cs`
- [x] Methode `RenderPlainText(SecurityTxtSettings)` in `ISecurityTxtRenderer` — vorhanden
- [x] Methode `RenderMarkdown(SecurityTxtSettings)` in `ISecurityTxtRenderer` — vorhanden
- [x] Methode `RenderHtml(SecurityTxtSettings)` in `ISecurityTxtRenderer` — vorhanden
- [x] `SecurityTxtRenderer` (Klasse) — angelegt in `Rezepte.Web/Services/SecurityTxtRenderer.cs`
- [x] Methode `RenderPlainText` in `SecurityTxtRenderer` — vorhanden; Mehrfachwerte per `\n` splitten, jede Zeile als eigene `Key:`-Direktive
- [x] Methode `RenderMarkdown` in `SecurityTxtRenderer` — vorhanden; Abschnitte als `## Key`
- [x] Methode `RenderHtml` in `SecurityTxtRenderer` — vorhanden; Abschnitte als `<h2>Key</h2><p>Value</p>`
- [x] `SecurityTxtRenderer` in DI registriert — `ServiceCollectionExtensions.cs`: `services.AddScoped<ISecurityTxtRenderer, SecurityTxtRenderer>()`
- [x] `SecurityTxtController` (Controller ohne `[Authorize]`) — angelegt in `Rezepte.Web/Controllers/SecurityTxtController.cs`
- [x] Methode `GetSecurityTxt` (`GET /security.txt` + `GET /.well-known/security.txt`) in `SecurityTxtController` — vorhanden; 404 bei `Enabled = false`, `text/plain; charset=utf-8`
- [x] Methode `GetSecurityMd` (`GET /.well-known/security.md`) in `SecurityTxtController` — vorhanden; `text/markdown; charset=utf-8`
- [x] Methode `GetSecurityHtml` (`GET /.well-known/security.html`) in `SecurityTxtController` — vorhanden; `text/html; charset=utf-8`
- [x] `SecurityTxtSettings.razor` (Blazor-Komponente) — angelegt in `Rezepte.Web/Components/Settings/SecurityTxtSettings.razor`; alle neun Felder als Eingabefelder, Laden bei Init, Speichern via PUT

### Änderungen an bestehenden Klassen

- [x] Methode `GetSecurityTxtSettingsAsync(CancellationToken)` in `ISettingsService` — vorhanden
- [x] Methode `SetSecurityTxtSettingsAsync(SecurityTxtSettings, CancellationToken)` in `ISettingsService` — vorhanden
- [x] Implementierung `GetSecurityTxtSettingsAsync` in `SettingsService` — vorhanden
- [x] Implementierung `SetSecurityTxtSettingsAsync` in `SettingsService` — vorhanden
- [x] Schlüsselkonstanten in `SettingsService` (`SecurityTxtEnabledKey`, `SecurityTxtContactKey`, `SecurityTxtExpiresKey`, `SecurityTxtEncryptionKey`, `SecurityTxtAcknowledgmentsKey`, `SecurityTxtPreferredLanguagesKey`, `SecurityTxtCanonicalKey`, `SecurityTxtPolicyKey`, `SecurityTxtHiringKey`) — alle neun vorhanden
- [x] Methode `GetGlobalSecurityTxt` (`GET global/securitytxt`, `[Authorize(Roles = "Admin")]`) in `SettingsController` — vorhanden
- [x] Methode `SetGlobalSecurityTxt` (`PUT global/securitytxt`, `[Authorize(Roles = "Admin")]`) in `SettingsController` — vorhanden; Pflichtfeldvalidierung für `Contact` und `Expires` bei `Enabled = true` mit HTTP 400; `Expires` muss in der Zukunft liegen
- [x] `SettingsViewModel` Konstruktor — neues `Item` für `SecurityTxtSettings.razor` mit `isAdmin`-Sichtbarkeit vorhanden
- [x] `RedirectToRegisterMiddleware.IsExcluded` — alle vier Pfade (`/security.txt`, `/.well-known/security.txt`, `/.well-known/security.md`, `/.well-known/security.html`) als Ausnahmen eingetragen

### Tests

- [x] `SecurityTxtRendererTests` — angelegt in `Rezepte.Tests/Services/SecurityTxtRendererTests.cs`
- [x] `RenderPlainText_ShouldReturnRfc9116Format_WhenAllFieldsSet` — vorhanden
- [x] `RenderPlainText_ShouldRepeatDirective_ForMultilineContact` — vorhanden
- [x] `RenderPlainText_ShouldOmitEmptyFields` — vorhanden
- [x] `RenderMarkdown_ShouldReturnSectionHeaders` — vorhanden
- [x] `RenderHtml_ShouldReturnH2AndParagraph` — vorhanden
- [x] `SecurityTxtControllerTests` — angelegt in `Rezepte.Tests/Controllers/SecurityTxtControllerTests.cs`
- [x] `GetSecurityTxt_ReturnsOk_WhenEnabled` — vorhanden
- [x] `GetSecurityTxt_ReturnsNotFound_WhenDisabled` — vorhanden
- [x] `GetWellKnownSecurityTxt_ReturnsOk_WhenEnabled` — vorhanden
- [x] `GetSecurityMd_ReturnsOk_WithMarkdownContentType` — vorhanden
- [x] `GetSecurityHtml_ReturnsOk_WithHtmlContentType` — vorhanden
- [x] `GetSecurityTxt_RequiresNoAuthentication` — vorhanden
- [x] `SettingsServiceTests` (Erweiterung) — `GetSecurityTxtSettingsAsync_ShouldReturnDefaults_WhenNoSettingsExist` und `SetSecurityTxtSettingsAsync_ShouldPersistAllFields` in `Rezepte.Tests/Services/SettingsServiceTests.cs` vorhanden; zusätzlich: `SetSecurityTxtSettingsAsync_ShouldOverwriteExistingValues`, `SetSecurityTxtSettingsAsync_ShouldClearNullableFields_WhenPassedNull`, `GetSecurityTxtSettingsAsync_ShouldReturnNullExpires_WhenValueIsInvalidDateString`
- [x] Validierungstests `SettingsControllerSecurityTxtValidationTests` — angelegt in `Rezepte.Tests/Controllers/SettingsControllerSecurityTxtValidationTests.cs`; deckt HTTP-400-Fälle für fehlende/ungültige `Contact`- und `Expires`-Werte ab
- [x] E2E-Szenario: `GET /security.txt` HTTP 200 ohne Auth (aktiviert) — `SecurityTxtBrowserTests.GetSecurityTxt_ReturnsOk_WithoutAuthentication_WhenEnabled`
- [x] E2E-Szenario: `GET /security.txt` HTTP 404 (deaktiviert) — `SecurityTxtBrowserTests.GetSecurityTxt_ReturnsNotFound_WhenDisabled`
- [x] E2E-Szenario: Admin konfiguriert security.txt im UI — `SecurityTxtBrowserTests.Admin_CanConfigureSecurityTxtViaUi_AndContentAppearsInPublicEndpoint`
- [x] E2E-Szenario: Normalbenutzer sieht keinen security.txt-Menüpunkt — `SecurityTxtBrowserTests.RegularUser_DoesNotSeeSecurityTxtMenuItemInSettings`

## Offene Aufgaben

Keine.

## Hinweise

- Die Tasks-Datei hatte die vier E2E-Testszenarien (Tasks 39–42) fälschlicherweise als `Offen` geführt. Alle vier sind vollständig in `Rezepte.Tests.Browser/SecurityTxtBrowserTests.cs` implementiert und wurden auf `Erledigt` gesetzt.
- Tasks 19–21 (Validierungstests im `SettingsController`) waren in der Tasks-Datei mit „Kein direkter Test" annotiert. Die vollständige Testklasse `SettingsControllerSecurityTxtValidationTests` mit acht Testmethoden war bereits vorhanden; Testnachweis wurde nachgetragen.
- Die Implementierung enthält über den Plan hinaus drei zusätzliche `SettingsServiceTests`-Methoden (`ShouldOverwriteExistingValues`, `ShouldClearNullableFields_WhenPassedNull`, `ShouldReturnNullExpires_WhenValueIsInvalidDateString`) sowie das Page-Object `SecurityTxtPageObject` — beides ohne Planabweichung, sondern als qualitätssichernde Ergänzung.
