# Tasks: Security F-08 – Google-Credential-Dateien nicht in Build-Ausgaben kopieren

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Konfiguration | `GoogleCredentialsOptions` Options-Klasse mit `ServiceAccountFilePath` (`string?`) und `GeminiApiKey` (`string?`) anlegen | Offen | — |
| 2 | Konfiguration | `services.Configure<GoogleCredentialsOptions>(configuration.GetSection("GoogleCredentials"))` in `ServiceCollectionExtensions` registrieren | Offen | — |
| 3 | Konfiguration | Leere Sektion `GoogleCredentials` (`ServiceAccountFilePath`, `GeminiApiKey`) als Schema-Dokumentation in `appsettings.json` aufnehmen | Offen | — |
| 4 | Logik | `GoogleCredentialsProvider` Konstruktor mit `IOptionsMonitor<GoogleCredentialsOptions>` einführen | Offen | — |
| 5 | Logik | `GetServiceAccountFilePath()` auf `GOOGLE_APPLICATION_CREDENTIALS` (Vorrang) mit Options-Fallback umstellen; Env-Variable setzen, falls nur aus Optionen aufgelöst | Offen | — |
| 6 | Logik | `ServiceAccountFileExists()` auf `File.Exists()` des neu aufgelösten Pfades umstellen | Offen | — |
| 7 | Logik | `GetGeminiApiKey()` auf `GOOGLE_GEMINI_API_KEY` (Vorrang) mit Options-Fallback umstellen; kein Lesen von `google.gemini.api-key.json` mehr | Offen | — |
| 8 | Logik | Datei-Konstanten (`ServiceAccountFileName`, `GeminiApiKeyFileName`, `accountfile_type_service_account`, `apikeyfile_type_api_key`) und Structs `AccountFile`/`ApiKeyFile` aus `GoogleCredentialsProvider` entfernen | Offen | — |
| 9 | Build | Unsichere `<ItemGroup>` mit `CopyToOutputDirectory=Always` für die Credential-Dateien aus `Rezepte.Web.csproj` vollständig entfernen | Offen | — |
| 10 | Build | `.gitignore` um `Rezepte.Web/google.application-credentials.json` und `Rezepte.Web/google.gemini.api-key.json` ergänzen | Offen | — |
| 11 | Dokumentation | `docs/development-guide.md` (neu): lokale Einrichtung über Umgebungsvariablen/User Secrets, Vorrangregel Env vor Konfiguration, Begründung „keine Credentials im Repo" | Offen | — |
| 12 | Dokumentation | `docs/deployment-guide.md` (neu): Secret-Store-/Env-Setup für Production, beide Gemini-Authentifizierungswege (API-Key und Service Account, Key vor Service Account) | Offen | — |
| 13 | Dokumentation | README-Abschnitt „Voraussetzungen" auf Umgebungsvariablen-Bezug korrigieren und auf neue Doku verweisen | Offen | — |
| 14 | Dokumentation | Code-Audit-Ergebnis festhalten: kein Credential-Datei-Loading aus festen Pfaden; Google-Bibliotheken nutzen `GOOGLE_APPLICATION_CREDENTIALS`; `GoogleQuotaClient` als „Pfad vom Aufrufer" vermerken; `Google.Cloud.Vision.V1`-Entfernung als separates Cleanup notieren | Offen | — |
| 15 | Tests | `GetServiceAccountFilePath_ReturnsPath_FromEnvironmentVariable` in `GoogleCredentialsProviderTests` | Offen | — |
| 16 | Tests | `GetServiceAccountFilePath_ReturnsPath_FromOptions_WhenEnvNotSet` in `GoogleCredentialsProviderTests` | Offen | — |
| 17 | Tests | `GetServiceAccountFilePath_ReturnsEmpty_WhenNothingConfigured` in `GoogleCredentialsProviderTests` | Offen | — |
| 18 | Tests | `ServiceAccountFileExists_ReturnsFalse_WhenPathMissing` in `GoogleCredentialsProviderTests` | Offen | — |
| 19 | Tests | `GetGeminiApiKey_ReturnsKey_FromEnvironmentVariable` in `GoogleCredentialsProviderTests` | Offen | — |
| 20 | Tests | `GetGeminiApiKey_ReturnsKey_FromOptions_WhenEnvNotSet` in `GoogleCredentialsProviderTests` | Offen | — |
| 21 | Tests | `GetGeminiApiKey_ReturnsEmpty_WhenNothingConfigured` in `GoogleCredentialsProviderTests` | Offen | — |
| 22 | Tests | `Csproj_DoesNotCopyCredentialFiles_ToOutput` in `CsprojCredentialCopyTests` (Regressionsschutz Projektdatei) | Offen | — |
| 23 | E2E-Tests | `SettingsCredentialAvailabilityTests`: `GeminiApiKeyAvailable = true`, wenn Key über Umgebungsvariable/Konfiguration bereitgestellt wird | Offen | — |
| 24 | E2E-Tests | `SettingsCredentialAvailabilityTests`: `GeminiApiKeyAvailable`/`GoogleServiceAccountFileAvailable = false`, wenn nichts konfiguriert ist | Offen | — |
| 25 | Verifikation | `dotnet build` und `dotnet publish` ausführen und prüfen, dass keine `google.*.json` in `bin/`/`publish/` liegt; Git-Historie und Release-Workflow gegenprüfen | Offen | — |
