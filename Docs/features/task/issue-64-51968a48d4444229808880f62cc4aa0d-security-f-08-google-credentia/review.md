# Plan-Review

## Ergebnis

**Status:** Vollständig umgesetzt

## Umgesetzte Planelemente

### Neue Klassen

- [x] `GoogleCredentialsOptions` (Options-Klasse) — angelegt in `Rezepte.Web/Configuration/GoogleCredentialsOptions.cs`, Namespace `Rezepte.Web.Configuration`
- [x] `GoogleCredentialsProviderTests` (Testklasse) — angelegt in `Rezepte.Tests/Services/GoogleCredentialsProviderTests.cs`
- [x] `SettingsCredentialAvailabilityTests` (Testklasse) — angelegt in `Rezepte.Tests/Controllers/SettingsCredentialAvailabilityTests.cs`
- [x] `CsprojCredentialCopyTests` (Testklasse) — angelegt in `Rezepte.Tests/Deployment/CsprojCredentialCopyTests.cs`

### Felder `GoogleCredentialsOptions`

- [x] Feld `ServiceAccountFilePath` (`string?`, Default `""`) in `GoogleCredentialsOptions` — vorhanden
- [x] Feld `GeminiApiKey` (`string?`, Default `""`) in `GoogleCredentialsOptions` — vorhanden

### `GoogleCredentialsProvider` (Umbau)

- [x] Neue Abhängigkeit `IOptionsMonitor<GoogleCredentialsOptions>` im Konstruktor — vorhanden
- [x] `GetServiceAccountFilePath()` — liest `GOOGLE_APPLICATION_CREDENTIALS`, sonst `GoogleCredentialsOptions.ServiceAccountFilePath`; setzt Umgebungsvariable bei Options-Auflösung; liefert `string.Empty` wenn nichts konfiguriert — vorhanden
- [x] `ServiceAccountFileExists()` — prüft `File.Exists()` auf aufgelöstem Pfad, `false` bei leerem Pfad — vorhanden
- [x] `GetGeminiApiKey()` — liest `GOOGLE_GEMINI_API_KEY`, sonst `GoogleCredentialsOptions.GeminiApiKey` — vorhanden
- [x] Entfernte Konstanten `ServiceAccountFileName`, `GeminiApiKeyFileName`, `accountfile_type_service_account`, `apikeyfile_type_api_key` — nicht mehr im Code
- [x] Entfernte Structs `AccountFile` und `ApiKeyFile` — nicht mehr im Code
- [x] Kein Lesen mehr aus `AppContext.BaseDirectory` — bestätigt
- [x] Interface `IGoogleCredentialsProvider` unverändert (Signaturen `GetServiceAccountFilePath`, `ServiceAccountFileExists`, `GetGeminiApiKey`) — bestätigt

### `ServiceCollectionExtensions` (DI)

- [x] `services.Configure<GoogleCredentialsOptions>(configuration.GetSection("GoogleCredentials"))` — vorhanden (Zeile 31)
- [x] `AddSingleton<IGoogleCredentialsProvider, GoogleCredentialsProvider>()` bleibt bestehen — vorhanden (Zeile 111)

### `Rezepte.Web.csproj`

- [x] Unsichere `<ItemGroup>` mit `google.application-credentials.json` / `google.gemini.api-key.json` und `CopyToOutputDirectory=Always` — vollständig entfernt (im Projekt nicht mehr vorhanden)

### `.gitignore`

- [x] Eintrag `Rezepte.Web/google.application-credentials.json` — vorhanden (Zeile 407)
- [x] Eintrag `Rezepte.Web/google.gemini.api-key.json` — vorhanden (Zeile 408)

### `appsettings.json`

- [x] Leere Sektion `GoogleCredentials` mit `ServiceAccountFilePath` und `GeminiApiKey` als Schema-Dokumentation — vorhanden (Zeilen 25–28)

### Dokumentation

- [x] `Docs/development-guide.md` (neu) — lokale Einrichtung über Umgebungsvariablen/User Secrets, Vorrangregel, Begründung, Code-Audit-Abschnitt — vorhanden
- [x] `Docs/deployment-guide.md` (neu) — Secret-Store-/Umgebungsvariablen-Setup, beide Gemini-Auth-Wege (API-Key + Service Account) inkl. Vorrang-Hinweis — vorhanden
- [x] README-Abschnitt „Voraussetzungen" korrigiert — verweist auf Umgebungsvariablen und die neuen Guides (Zeilen 82–88)
- [x] Code-Audit dokumentiert (Provider lädt keine festen Pfade, `GoogleQuotaClient` erhält Pfad vom Aufrufer) — im development-guide.md, Abschnitt „Code-Audit"

### Tests

- [x] `GetServiceAccountFilePath_ReturnsPath_FromEnvironmentVariable` (`GoogleCredentialsProviderTests`) — vorhanden, grün
- [x] `GetServiceAccountFilePath_ReturnsPath_FromOptions_WhenEnvNotSet` — vorhanden, grün
- [x] `GetServiceAccountFilePath_ReturnsEmpty_WhenNothingConfigured` — vorhanden, grün
- [x] `ServiceAccountFileExists_ReturnsFalse_WhenPathMissing` — vorhanden, grün
- [x] `GetGeminiApiKey_ReturnsKey_FromEnvironmentVariable` — vorhanden, grün
- [x] `GetGeminiApiKey_ReturnsKey_FromOptions_WhenEnvNotSet` — vorhanden, grün
- [x] `GetGeminiApiKey_ReturnsEmpty_WhenNothingConfigured` — vorhanden, grün
- [x] `Csproj_DoesNotCopyCredentialFiles_ToOutput` (`CsprojCredentialCopyTests`) — vorhanden, grün
- [x] E2E `GetMySettings_ReportsCredentialsAvailable_WhenProvidedViaEnvironmentVariable` (`SettingsCredentialAvailabilityTests`) — vorhanden, grün
- [x] E2E `GetMySettings_ReportsCredentialsUnavailable_WhenNothingConfigured` (`SettingsCredentialAvailabilityTests`) — vorhanden, grün
- [x] Test-Isolation via `GoogleCredentialsEnvironmentCollection` (xUnit-Collection für serialisierten Umgebungsvariablen-Zugriff) — vorhanden

Testlauf (Filter auf die drei neuen Testklassen): **10 erfolgreich, 0 Fehler**.

## Offene Aufgaben

Keine.

## Hinweise

- Für diesen Branch existiert keine `...-tasks.md`-Datei; Schritt 3 des Reviews (Tasks-Datei aktualisieren) entfällt daher mangels Datei. Alle Planelemente sind über dieses Review nachgewiesen.
- Die beiden neuen Dokumente liegen unter `Docs/` (Großschreibung). Der Plan referenziert sie als `docs/...`; auf dem case-insensitiven Windows-Dateisystem ist das identisch, die README verweist konsistent auf `Docs/development-guide.md` und `Docs/deployment-guide.md`.
- Die bestehende `TestGeminiClient`-Testinfrastruktur wurde plangemäß nicht angepasst und referenziert keine entfernten Datei-Konstanten (kein Kompilierfehler beim Testbuild).
- Nicht Teil dieser Anforderung (bewusste Scope-Begrenzung laut Plan): Entfernung des ungenutzten Pakets `Google.Cloud.Vision.V1` und Änderungen an `GoogleQuotaClient`.
- Verifikationsschritt 10 (Prüfung, dass `dotnet publish` keine `google.*.json` erzeugt) ist durch den Projektdatei-Regressionstest `CsprojCredentialCopyTests` abgesichert; die Credential-Dateien sind zudem nicht im Arbeitsbaum vorhanden.
