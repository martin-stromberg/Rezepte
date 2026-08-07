## Testklassen

### `SettingsServiceTests`
Datei: `Rezepte.Tests/Services/SettingsServiceTests.cs`

- `GetUserAiEnabledAsync_ShouldReturnTrueByDefault_WhenNoSettingExists` — Prüft Default-Rückgabe `true` bei fehlendem Eintrag
- `SetUserAiEnabledAsync_ShouldPersistValue_AndBeReadable` — Persistiert `false`, liest zurück, aktualisiert auf `true`

*(Weitere Testmethoden in der Datei vorhanden, nicht vollständig aufgelistet — für security.txt nicht direkt relevant)*

### `SettingsCredentialAvailabilityTests`
Datei: `Rezepte.Tests/Controllers/SettingsCredentialAvailabilityTests.cs`

- `GetMySettings_ReportsCredentialsAvailable_WhenProvidedViaEnvironmentVariable` — Prüft, dass `GoogleServiceAccountFileAvailable` und `GeminiApiKeyAvailable` korrekt gesetzt sind

---

## Fehlende Tests

Für die security.txt-Anforderung existieren **keine** bestehenden Testklassen:

- `SecurityTxtRendererTests` — noch nicht vorhanden
- `SecurityTxtControllerTests` — noch nicht vorhanden
