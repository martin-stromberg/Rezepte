# Plan-Review

## Ergebnis

**Status:** Vollständig umgesetzt

## Umgesetzte Planelemente

### Neue Klassen

- [x] `SecurityTxtSettings` (Record / DTO) — angelegt unter `Rezepte.Web/Dtos/SecurityTxtSettings.cs` mit allen neun Feldern (`Enabled`, `Contact`, `Expires`, `Encryption`, `Acknowledgments`, `PreferredLanguages`, `Canonical`, `Policy`, `Hiring`)
- [x] `ISecurityTxtRenderer` (Interface) — angelegt unter `Rezepte.Web/Services/ISecurityTxtRenderer.cs` mit `RenderPlainText`, `RenderMarkdown`, `RenderHtml`
- [x] `SecurityTxtRenderer` (Klasse) — angelegt unter `Rezepte.Web/Services/SecurityTxtRenderer.cs`; implementiert alle drei Render-Methoden; in DI registriert (`AddScoped<ISecurityTxtRenderer, SecurityTxtRenderer>` in `ServiceCollectionExtensions.cs`)
- [x] `SecurityTxtController` (Controller ohne `[Authorize]`) — angelegt unter `Rezepte.Web/Controllers/SecurityTxtController.cs`; vier Endpunkte (`GET /security.txt`, `GET /.well-known/security.txt`, `GET /.well-known/security.md`, `GET /.well-known/security.html`); 404 bei `Enabled = false`; korrekte `Content-Type`-Header
- [x] `SecurityTxtSettings.razor` (Blazor-Komponente) — angelegt unter `Rezepte.Web/Components/Settings/SecurityTxtSettings.razor`; alle neun Felder als Eingabefelder; lädt per `GET api/settings/global/securitytxt`, speichert per `PUT`
- [x] `SecurityTxtRendererTests` (Testklasse) — angelegt unter `Rezepte.Tests/Services/SecurityTxtRendererTests.cs`
- [x] `SecurityTxtControllerTests` (Testklasse) — angelegt unter `Rezepte.Tests/Controllers/SecurityTxtControllerTests.cs`

### Änderungen an bestehenden Klassen

- [x] `ISettingsService` — um `GetSecurityTxtSettingsAsync` und `SetSecurityTxtSettingsAsync` erweitert (`Rezepte.Web/Services/ISettingsService.cs`, Zeilen 45–46)
- [x] `SettingsService` — alle neun Schlüsselkonstanten (`SecurityTxtEnabledKey`, `SecurityTxtContactKey`, `SecurityTxtExpiresKey`, `SecurityTxtEncryptionKey`, `SecurityTxtAcknowledgmentsKey`, `SecurityTxtPreferredLanguagesKey`, `SecurityTxtCanonicalKey`, `SecurityTxtPolicyKey`, `SecurityTxtHiringKey`) sowie Implementierungen beider Methoden vorhanden (`SettingsService.cs`, Zeilen 41–49, 183–251)
- [x] `SettingsController` — `GetGlobalSecurityTxt` (`GET global/securitytxt`, `[Authorize(Roles = "Admin")]`) und `SetGlobalSecurityTxt` (`PUT global/securitytxt`, `[Authorize(Roles = "Admin")]`) mit Pflichtfeldvalidierung für `Contact` und `Expires`; HTTP 204 bei Erfolg
- [x] `SettingsViewModel` — neues `Item("security.txt", "🔒", isAdmin, typeof(SecurityTxtSettings))` im Konstruktor eingetragen
- [x] `RedirectToRegisterMiddleware.IsExcluded` — alle vier Pfade (`/security.txt`, `/.well-known/security.txt`, `/.well-known/security.md`, `/.well-known/security.html`) als Ausnahmen eingetragen (Zeilen 75–78)

### Routing / Konfiguration

- [x] `Program.cs` — `app.UseStaticFiles()` (Zeile 37) liegt vor `app.MapControllers()` (Zeile 57); da keine physischen Dateien unter `wwwroot/.well-known/` vorhanden sind, werden die Anfragen korrekt an den Controller weitergegeben

### Neue Tests

- [x] `RenderPlainText_ShouldReturnRfc9116Format_WhenAllFieldsSet` — `SecurityTxtRendererTests`
- [x] `RenderPlainText_ShouldRepeatDirective_ForMultilineContact` — `SecurityTxtRendererTests`
- [x] `RenderMarkdown_ShouldReturnSectionHeaders` — `SecurityTxtRendererTests`
- [x] `RenderHtml_ShouldReturnH2AndParagraph` — `SecurityTxtRendererTests`
- [x] `RenderPlainText_ShouldOmitEmptyFields` — `SecurityTxtRendererTests`
- [x] `GetSecurityTxt_ReturnsOk_WhenEnabled` — `SecurityTxtControllerTests`
- [x] `GetSecurityTxt_ReturnsNotFound_WhenDisabled` — `SecurityTxtControllerTests`
- [x] `GetWellKnownSecurityTxt_ReturnsOk_WhenEnabled` — `SecurityTxtControllerTests`
- [x] `GetSecurityMd_ReturnsOk_WithMarkdownContentType` — `SecurityTxtControllerTests`
- [x] `GetSecurityHtml_ReturnsOk_WithHtmlContentType` — `SecurityTxtControllerTests`
- [x] `GetSecurityTxt_RequiresNoAuthentication` — `SecurityTxtControllerTests`
- [x] `GetSecurityTxtSettingsAsync_ShouldReturnDefaults_WhenNoSettingsExist` — `SettingsServiceTests` (Erweiterung)
- [x] `SetSecurityTxtSettingsAsync_ShouldPersistAllFields` — `SettingsServiceTests` (Erweiterung)

### E2E-Tests (Pflicht)

- [x] `GetSecurityTxt_ReturnsOk_WithoutAuthentication_WhenEnabled` — `Rezepte.Tests.Browser/SecurityTxtBrowserTests.cs`
- [x] `GetSecurityTxt_ReturnsNotFound_WhenDisabled` — `Rezepte.Tests.Browser/SecurityTxtBrowserTests.cs`
- [x] `Admin_CanConfigureSecurityTxtViaUi_AndContentAppearsInPublicEndpoint` — `Rezepte.Tests.Browser/SecurityTxtBrowserTests.cs`
- [x] `RegularUser_DoesNotSeeSecurityTxtMenuItemInSettings` — `Rezepte.Tests.Browser/SecurityTxtBrowserTests.cs`
- [x] `SecurityTxtPageObject` (Infrastruktur) — `Rezepte.Tests.Browser/Infrastructure/SecurityTxtPageObject.cs`

## Offene Aufgaben

Keine.

## Hinweise

- **Zusätzliche Testabdeckung:** Die `SettingsServiceTests` enthalten drei über den Plan hinausgehende Testmethoden: `SetSecurityTxtSettingsAsync_ShouldOverwriteExistingValues`, `SetSecurityTxtSettingsAsync_ShouldClearNullableFields_WhenPassedNull` und `GetSecurityTxtSettingsAsync_ShouldReturnNullExpires_WhenValueIsInvalidDateString`. Ebenso wurde `SettingsControllerSecurityTxtValidationTests` als eigene Klasse mit acht Validierungstests ergänzt (nicht im Plan vorgesehen, aber inhaltlich korrekt).
- **`Expires`-Zukunftsprüfung:** Der `PUT`-Endpunkt validiert zusätzlich, ob `Expires` in der Zukunft liegt (`Expires <= DateTimeOffset.UtcNow` → HTTP 400). Diese Erweiterung gegenüber dem Plan entspricht RFC 9116.
- **Routing `Program.cs`:** Die im Plan als Risiko genannte Reihenfolge (`UseStaticFiles` vor `MapControllers`) ist implementiert wie im Bestand vorgefunden. Da keine physischen `/.well-known/`-Dateien im wwwroot existieren, ist kein unmittelbares Routing-Problem zu erwarten. Sollte zukünftig ein `wwwroot/.well-known/`-Verzeichnis angelegt werden, wäre die Reihenfolge erneut zu prüfen.
