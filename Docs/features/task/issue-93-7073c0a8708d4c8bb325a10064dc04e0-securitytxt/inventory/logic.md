## `SettingsService`
Datei: `src/Rezepte.Web/Services/SettingsService.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `GetUserAiEnabledAsync` | `public` | Liest `UserSetting.AiEnabled`; Default `true` |
| `SetUserAiEnabledAsync` | `public` | Schreibt `UserSetting.AiEnabled`; legt Eintrag an falls nicht vorhanden |
| `GetGlobalAiEnabledAsync` | `public` | Liest `AppSetting["AiEnabled"]`; Default `true` |
| `SetGlobalAiEnabledAsync` | `public` | Schreibt `AppSetting["AiEnabled"]` |
| `GetUserGoogleVisionEnabledAsync` | `public` | Liest `UserSetting.GoogleVisionEnabled`; Default `true` |
| `SetUserGoogleVisionEnabledAsync` | `public` | Schreibt `UserSetting.GoogleVisionEnabled` |
| `GetUserGeminiEnabledAsync` | `public` | Liest `UserSetting.GeminiEnabled`; Default `true` |
| `SetUserGeminiEnabledAsync` | `public` | Schreibt `UserSetting.GeminiEnabled` |
| `GetUserRequireAiConfirmationAsync` | `public` | Liest `UserSetting.RequireAiConfirmation`; Default `false` |
| `SetUserRequireAiConfirmationAsync` | `public` | Schreibt `UserSetting.RequireAiConfirmation` |
| `GetUserShoppingListEditModeAsync` | `public` | Liest `AppSetting["ShoppingListEditMode:{userId}"]`; Default `false` |
| `SetUserShoppingListEditModeAsync` | `public` | Schreibt `AppSetting["ShoppingListEditMode:{userId}"]` |
| `GetGlobalGoogleVisionEnabledAsync` | `public` | Liest `AppSetting["GlobalGoogleVisionEnabled"]`; Default `true` |
| `SetGlobalGoogleVisionEnabledAsync` | `public` | Schreibt `AppSetting["GlobalGoogleVisionEnabled"]` |
| `GetGlobalGeminiEnabledAsync` | `public` | Liest `AppSetting["GlobalGeminiEnabled"]`; Default `true` |
| `SetGlobalGeminiEnabledAsync` | `public` | Schreibt `AppSetting["GlobalGeminiEnabled"]` |
| `GetGlobalMaxRequestsPerHourAsync` | `public` | Liest `AppSetting["GlobalMaxRequestsPerHour"]`; Default `null` |
| `SetGlobalMaxRequestsPerHourAsync` | `public` | Schreibt oder löscht `AppSetting["GlobalMaxRequestsPerHour"]` |
| `GetGlobalMaxRequestsPerDayAsync` | `public` | Liest `AppSetting["GlobalMaxRequestsPerDay"]`; Default `null` |
| `SetGlobalMaxRequestsPerDayAsync` | `public` | Schreibt oder löscht `AppSetting["GlobalMaxRequestsPerDay"]` |
| `GetGlobalDisableOnLimitReachedAsync` | `public` | Liest `AppSetting["GlobalDisableOnLimitReached"]`; Default `false` |
| `SetGlobalDisableOnLimitReachedAsync` | `public` | Schreibt `AppSetting["GlobalDisableOnLimitReached"]` |
| `ShoppingListEditModeKey` | `private static` | Hilfsmethode: bildet den AppSetting-Schlüssel `"ShoppingListEditMode:{userId}"` |

**Konstante Schlüssel:**

| Konstante | Wert |
|-----------|------|
| `AiKey` | `"AiEnabled"` |
| `GoogleVisionKey` | `"GlobalGoogleVisionEnabled"` |
| `GeminiKey` | `"GlobalGeminiEnabled"` |
| `GlobalMaxPerHourKey` | `"GlobalMaxRequestsPerHour"` |
| `GlobalMaxPerDayKey` | `"GlobalMaxRequestsPerDay"` |
| `GlobalDisableOnLimitKey` | `"GlobalDisableOnLimitReached"` |
| `ShoppingListEditModePrefix` | `"ShoppingListEditMode:"` |

---

## `SettingsController`
Datei: `src/Rezepte.Web/Controllers/SettingsController.cs`

Basis-Route: `api/settings` · Authentifizierung: `[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]`

| Methode | HTTP | Route | Auth | Kurzbeschreibung |
|---------|------|-------|------|------------------|
| `GetMySettings` | GET | `me` | Authentifiziert | Gibt alle Benutzer- und globalen KI-Einstellungen zurück (anonymes Objekt) |
| `SetMyAi` | PUT | `me/ai` | Authentifiziert | Setzt `UserAiEnabled` |
| `SetMyGoogleVision` | PUT | `me/ai/googlevision` | Authentifiziert | Setzt `UserGoogleVisionEnabled` |
| `SetMyGemini` | PUT | `me/ai/gemini` | Authentifiziert | Setzt `UserGeminiEnabled` |
| `SetMyAiConfirm` | PUT | `me/ai/confirm` | Authentifiziert | Setzt `UserRequireAiConfirmation` |
| `GetGlobal` | GET | `global` | Admin | Gibt alle globalen KI-Einstellungen zurück |
| `SetGlobalAi` | PUT | `global/ai` | Admin | Setzt `GlobalAiEnabled` |
| `SetGlobalGoogleVision` | PUT | `global/ai/googlevision` | Admin | Setzt `GlobalGoogleVisionEnabled` |
| `SetGlobalGemini` | PUT | `global/ai/gemini` | Admin | Setzt `GlobalGeminiEnabled` |
| `SetGlobalMaxRequestsPerHour` | PUT | `global/ai/maxrequestsperhour` | Admin | Setzt `GlobalMaxRequestsPerHour` |
| `SetGlobalMaxRequestsPerDay` | PUT | `global/ai/maxrequestsperday` | Admin | Setzt `GlobalMaxRequestsPerDay` |
| `SetGlobalDisableOnLimit` | PUT | `global/ai/disableonlimit` | Admin | Setzt `GlobalDisableOnLimitReached` |

**Fehlend für security.txt:**
- `GET api/settings/global/securitytxt`
- `PUT api/settings/global/securitytxt`

---

## `RedirectToRegisterMiddleware`
Datei: `src/Rezepte.Web/Middleware/RedirectToRegisterMiddleware.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `InvokeAsync` | `public` | Prüft Pfad auf Ausnahmen; leitet ggf. auf `/register` oder `/login` um |
| `RedirectToRegistration` | `private static` | Leitet auf `/register` um, wenn noch kein Benutzer vorhanden |
| `RedirectToLogin` | `private static` | Leitet auf `/login` um, wenn nicht authentifiziert |
| `IsExcluded` | `private static` | Gibt `true` zurück für `/login`, `/api/*`, `/_blazor/*`, `/_framework/*`, `/_content/*`, `/_vs/*` und statische Dateiendungen |

**Relevanz für security.txt:** Die Pfade `/security.txt`, `/.well-known/security.txt`, `/.well-known/security.md` und `/.well-known/security.html` sind in `IsExcluded` **nicht** enthalten. Ohne Ergänzung werden nicht-authentifizierte Anfragen auf `/login` umgeleitet.
