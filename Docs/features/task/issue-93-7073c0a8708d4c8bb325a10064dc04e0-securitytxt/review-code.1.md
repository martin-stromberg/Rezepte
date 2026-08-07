# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### SettingsService.cs (SettingsService)

- **Toter Code** — Die private Methode `WriteNullableString` (ab Zeile ~260) ist im gesamten Branch definiert, wird aber nirgends aufgerufen. Für das Schreiben optionaler `AppSetting`-Einträge wird stattdessen die lokale `Upsert`-Hilfsfunktion in `SetSecurityTxtSettingsAsync` verwendet.

  Empfehlung: `WriteNullableString` entfernen, da sie keinen Aufrufer hat und nur toten Code darstellt.

- **Doppelter Code (Struktur)** — Die `GetSecurityTxtSettingsAsync`-Methode und `SetSecurityTxtSettingsAsync` definieren beide das identische Array der neun `SecurityTxt.*`-Schlüssel inline:
  ```csharp
  var keys = new[] { SecurityTxtEnabledKey, SecurityTxtContactKey, ... };
  ```
  Die Array-Definition ist in beiden Methoden wortgleich wiederholt.

  Empfehlung: Das Schlüssel-Array in ein privates `static readonly`-Feld `SecurityTxtKeys` auslagern und in beiden Methoden referenzieren.

- **Primitive Obsession / Long Parameter List (SecurityTxtSettings-Konstruktion)** — Die Methode `GetSecurityTxtSettingsAsync` instanziiert den `SecurityTxtSettings`-Record mit neun Positional-Parametern in einem einzigen Konstruktoraufruf. Da es sich um einen positional Record handelt, ist die Reihenfolge der Argumente fehleranfällig (kein Compiler-Schutz bei zwei aufeinanderfolgenden `string?`-Werten mit ähnlichem Namen).

  Empfehlung: Wenn der Record-Typ geändert werden kann, auf Named-Properties umstellen oder die Instanziierung mit benannten Argumenten schreiben (`Contact: Get(SecurityTxtContactKey), ...`), damit die Zuordnung klar und typsicher ist.

### SettingsController.cs (SettingsController)

- **Fehlende Eingabevalidierung** — Die Endpunkte `SetGlobalMaxRequestsPerHour` (`PUT global/ai/maxrequestsperhour`) und `SetGlobalMaxRequestsPerDay` (`PUT global/ai/maxrequestsperday`) akzeptieren `int? value` ohne Validierung auf negative Werte. Ein Wert wie `-5` würde als gültiges Limit gespeichert.

  Empfehlung: Guard-Bedingung ergänzen: `if (value.HasValue && value.Value <= 0) return BadRequest("Wert muss größer als 0 sein.");`

### SettingsViewModel.cs (SettingsViewModel)

- **Synchroner Block auf asynchroner Methode** — Im Konstruktor wird `authenticationStateProvider.GetAuthenticationStateAsync().GetAwaiter().GetResult()` synchron blockierend aufgerufen. Der Kommentar beschreibt dies als „bewusst", jedoch ist dies in Blazor Server potenziell ein Deadlock-Risiko, wenn der Synchronisierungskontext belegt ist.

  Empfehlung: Falls die Architektur es erlaubt, das ViewModel in eine asynchrone Factory-Methode oder `OnInitializedAsync` im zugehörigen Razor-Component verlagern, um den blocking call zu vermeiden.

### SettingsServiceTests.cs (SettingsServiceTests)

- **Toter Code / Nicht getestete öffentliche Methoden** — Die neuen Methoden `GetUserGoogleVisionEnabledAsync`, `SetUserGoogleVisionEnabledAsync`, `GetUserGeminiEnabledAsync`, `SetUserGeminiEnabledAsync`, `GetUserRequireAiConfirmationAsync`, `SetUserRequireAiConfirmationAsync`, `GetGlobalGoogleVisionEnabledAsync`, `SetGlobalGoogleVisionEnabledAsync`, `GetGlobalGeminiEnabledAsync`, `SetGlobalGeminiEnabledAsync`, `GetGlobalMaxRequestsPerHourAsync`, `SetGlobalMaxRequestsPerHourAsync`, `GetGlobalMaxRequestsPerDayAsync`, `SetGlobalMaxRequestsPerDayAsync`, `GetGlobalDisableOnLimitReachedAsync` und `SetGlobalDisableOnLimitReachedAsync` haben keine Testabdeckung im Branch. Nur AI- und Security-txt-Methoden sowie ShoppingList-EditMode sind getestet.

  Empfehlung: Zumindest je einen Happy-Path-Test für die globalen Toggle-Methoden (`GlobalGoogleVision`, `GlobalGemini`) hinzufügen, da diese dem gleichen Muster wie die bereits getesteten `GlobalAi`-Methoden folgen und in `GetMySettings` zusammen abgefragt werden.

## Geprüfte Dateien

- `Rezepte.Web/Services/ISettingsService.cs`
- `Rezepte.Web/Services/SettingsService.cs`
- `Rezepte.Web/Controllers/SettingsController.cs`
- `Rezepte.Web/ViewModels/SettingsViewModel.cs`
- `Rezepte.Web/Middleware/RedirectToRegisterMiddleware.cs`
- `Rezepte.Web/Extensions/ServiceCollectionExtensions.cs`
- `Rezepte.Tests/Services/SettingsServiceTests.cs`
