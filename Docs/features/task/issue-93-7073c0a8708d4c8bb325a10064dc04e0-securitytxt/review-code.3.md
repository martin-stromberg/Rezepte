# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### Rezepte.Web/Services/SettingsService.cs (SettingsService)

- **Struktur und Verantwortlichkeiten (God-Klasse)** — `SettingsService` bündelt mehrere fachlich getrennte Bereiche (AI-Flags pro User/global, Shopping-List-Modus, Limits, security.txt-Management) in einer Klasse. Die Klasse wächst weiter und wird schwerer test- und wartbar.

  Empfehlung: `SettingsService` in fachlich getrennte Services aufteilen (z. B. `AiSettingsService`, `SecurityTxtSettingsService`, `UserPreferenceService`) und über schlanke Interfaces registrieren.

### Rezepte.Web/Components/Settings/SecurityTxtSettings.razor (SecurityTxtSettings)

- **Fehlerbehandlung** — In `LoadAsync` und `SaveAsync` wird jeweils `catch (Exception ex)` verwendet (Zeilen 151, 179). Das ist zu breit und behandelt technische Fehler nicht gezielt.

  Empfehlung: Erwartbare Fehlerfälle spezifisch abfangen (z. B. HTTP-/Timeout-Fehler), unerwartete Exceptions loggen und dem Benutzer eine generische Fehlermeldung ohne Low-Level-Details anzeigen.

### Rezepte.Web/ViewModels/SettingsViewModel.cs (SettingsViewModel)

- **Toter Code** — `using System.Runtime.InteropServices;` (Zeile 3) wird nicht verwendet.

  Empfehlung: Unbenutztes `using` entfernen.

- **Toter Code** — Die Property `public bool Visible { get; }` (Zeile 21) wird in den geprüften Änderungen nicht verwendet und nie gesetzt.

  Empfehlung: Property entfernen oder sauber in die View-Logik integrieren, falls sie fachlich benötigt wird.

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
