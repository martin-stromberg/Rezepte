# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### GoogleCredentialsProvider.cs (GoogleCredentialsProvider)

- **Einheitlichkeit / Doppelter Code** — Die beiden parallel aufgebauten Auflösungsmethoden behandeln den Konfigurations-Fallback uneinheitlich. `GetServiceAccountFilePath()` (Zeilen 24–28) prüft den konfigurierten Wert mit `!string.IsNullOrWhiteSpace(configuredPath)` und liefert bei reinem Whitespace `string.Empty`. `GetGeminiApiKey()` (Zeile 45) gibt dagegen `_options.CurrentValue.GeminiApiKey ?? string.Empty` ohne Whitespace-Prüfung zurück, sodass ein rein aus Leerzeichen bestehender konfigurierter Key unverändert durchgereicht wird. Die Auswirkung ist aktuell gering, weil alle Konsumenten (`SettingsController`, `GeminiClient`) selbst mit `IsNullOrWhiteSpace` prüfen, aber das Verhalten der beiden Getter divergiert ohne fachlichen Grund.

  Empfehlung: `GetGeminiApiKey()` analog zu `GetServiceAccountFilePath()` aufbauen, z. B.
  `var configuredKey = _options.CurrentValue.GeminiApiKey;`
  `if (!string.IsNullOrWhiteSpace(configuredKey)) return configuredKey;`
  `return string.Empty;`
  Damit verhalten sich beide Auflösungspfade identisch (Env → Options → leer, jeweils mit Whitespace als „nicht gesetzt").

## Geprüfte Dateien

- `Rezepte.Web/Services/GoogleCredentialsProvider.cs`
- `Rezepte.Web/Services/IGoogleCredentialsProvider.cs`
- `Rezepte.Web/Configuration/GoogleCredentialsOptions.cs`
- `Rezepte.Web/Extensions/ServiceCollectionExtensions.cs`
- `Rezepte.Web/Rezepte.Web.csproj`
- `Rezepte.Web/appsettings.json`
- `Rezepte.Tests/Services/GoogleCredentialsProviderTests.cs`
- `Rezepte.Tests/Controllers/SettingsCredentialAvailabilityTests.cs`
- `Rezepte.Tests/Deployment/CsprojCredentialCopyTests.cs`
- `Rezepte.Tests/TestHelpers/EnvironmentVariableScope.cs`
- `Rezepte.Tests/TestHelpers/GoogleCredentialsEnvironmentCollection.cs`
- `.gitignore`
