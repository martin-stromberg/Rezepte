## `ISettingsService`
Datei: `src/Rezepte.Web/Services/ISettingsService.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetUserAiEnabledAsync` | `string userId, CancellationToken ct` | `Task<bool>` | Liest benutzerspezifischen KI-Aktivierungsstatus |
| `SetUserAiEnabledAsync` | `string userId, bool enabled, CancellationToken ct` | `Task` | Schreibt benutzerspezifischen KI-Aktivierungsstatus |
| `GetGlobalAiEnabledAsync` | `CancellationToken ct` | `Task<bool>` | Liest globalen KI-Aktivierungsstatus |
| `SetGlobalAiEnabledAsync` | `bool enabled, CancellationToken ct` | `Task` | Schreibt globalen KI-Aktivierungsstatus |
| `GetUserGoogleVisionEnabledAsync` | `string userId, CancellationToken ct` | `Task<bool>` | Benutzerspezifischer Google-Vision-Toggle |
| `SetUserGoogleVisionEnabledAsync` | `string userId, bool enabled, CancellationToken ct` | `Task` | — |
| `GetUserGeminiEnabledAsync` | `string userId, CancellationToken ct` | `Task<bool>` | Benutzerspezifischer Gemini-Toggle |
| `SetUserGeminiEnabledAsync` | `string userId, bool enabled, CancellationToken ct` | `Task` | — |
| `GetGlobalGoogleVisionEnabledAsync` | `CancellationToken ct` | `Task<bool>` | Globaler Google-Vision-Toggle |
| `SetGlobalGoogleVisionEnabledAsync` | `bool enabled, CancellationToken ct` | `Task` | — |
| `GetGlobalGeminiEnabledAsync` | `CancellationToken ct` | `Task<bool>` | Globaler Gemini-Toggle |
| `SetGlobalGeminiEnabledAsync` | `bool enabled, CancellationToken ct` | `Task` | — |
| `GetUserRequireAiConfirmationAsync` | `string userId, CancellationToken ct` | `Task<bool>` | Benutzerseitige Bestätigungspflicht für KI-Aktionen |
| `SetUserRequireAiConfirmationAsync` | `string userId, bool required, CancellationToken ct` | `Task` | — |
| `GetUserShoppingListEditModeAsync` | `string userId, CancellationToken ct` | `Task<bool>` | Einkaufslisten-Darstellungsmodus |
| `SetUserShoppingListEditModeAsync` | `string userId, bool editMode, CancellationToken ct` | `Task` | — |
| `GetGlobalMaxRequestsPerHourAsync` | `CancellationToken ct` | `Task<int?>` | Globales stündliches Request-Limit (null = unbegrenzt) |
| `SetGlobalMaxRequestsPerHourAsync` | `int? value, CancellationToken ct` | `Task` | — |
| `GetGlobalMaxRequestsPerDayAsync` | `CancellationToken ct` | `Task<int?>` | Globales tägliches Request-Limit (null = unbegrenzt) |
| `SetGlobalMaxRequestsPerDayAsync` | `int? value, CancellationToken ct` | `Task` | — |
| `GetGlobalDisableOnLimitReachedAsync` | `CancellationToken ct` | `Task<bool>` | KI bei Limit-Erreichen deaktivieren |
| `SetGlobalDisableOnLimitReachedAsync` | `bool disable, CancellationToken ct` | `Task` | — |

**Fehlend für security.txt:** `GetSecurityTxtSettingsAsync` und `SetSecurityTxtSettingsAsync` (mit DTO `SecurityTxtSettings`).
