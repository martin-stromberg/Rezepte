# Umsetzungsplan: Security F-08 – Google-Credential-Dateien nicht in Build-Ausgaben kopieren

## Übersicht

Die unsichere MSBuild-Konfiguration in `Rezepte.Web.csproj`, die `google.application-credentials.json` und `google.gemini.api-key.json` mit `CopyToOutputDirectory=Always` in jede Build-Ausgabe kopieren würde, wird entfernt. Da die bestehende Credential-Ladelogik (`GoogleCredentialsProvider`) diese Dateien aus dem Build-Ausgabeverzeichnis (`AppContext.BaseDirectory`) liest, wird die Credential-Beschaffung auf Umgebungsvariablen / Konfiguration (Options-Pattern) umgestellt. Zusätzlich werden `.gitignore`, README und eine neue Sicherheits-/Deployment-Dokumentation angepasst, und es werden Tests für die neue Credential-Auflösung ergänzt.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| MSBuild-ItemGroup (Zeilen 24–31) | **Option A: ItemGroup komplett entfernen** | Die Credentials werden nach der Umstellung nicht mehr aus dem Projektverzeichnis in die Build-Ausgabe kopiert, sondern per Umgebungsvariable/Konfiguration bezogen. Eine `Never`-Konfiguration wäre nur eine tote Absicherung für Dateien, die es im Projekt nicht mehr geben soll. Regressionsschutz erfolgt über `.gitignore` und einen Projektdatei-Test. |
| Credential-Beschaffung `GoogleCredentialsProvider` | **Options-Pattern (`IOptionsMonitor<GoogleCredentialsOptions>`) + Standard-Umgebungsvariablen (`GOOGLE_APPLICATION_CREDENTIALS`, `GOOGLE_GEMINI_API_KEY`)** als Gateway zu externen Secrets | Entfernt das Lesen von Dateien aus `AppContext.BaseDirectory`. Umgebungsvariablen decken lokale Entwicklung, CI und Secret-Store-Injektion (Production) einheitlich ab; `GOOGLE_APPLICATION_CREDENTIALS` ist der von den Google-Bibliotheken erwartete Standard. |
| Vorrangregel Umgebungsvariable vs. Konfiguration (**entschieden**) | **Umgebungsvariable hat Vorrang, `GoogleCredentials:*`-Konfiguration ist Fallback** | Production injiziert Secrets typischerweise per Umgebungsvariable (Secret Store/Deployment); Konfiguration bzw. .NET User Secrets dienen als lokaler/Test-Default. Diese Reihenfolge deckt beide Ebenen konfliktfrei ab. |
| Gemini-Authentifizierungsweg (**entschieden**) | **Sowohl API-Key als auch Service Account bleiben unterstützt** (bestehende `GeminiClient`-Logik: Key vor Service Account) | Beide Wege werden weiterhin über den `GoogleCredentialsProvider` aufgelöst; keine Codeänderung an der Auswahllogik nötig. Die Deployment-Doku beschreibt beide Optionen, damit Production frei zwischen API-Key und Service Account wählen kann. |
| Beibehaltung von `IGoogleCredentialsProvider` (Signaturen) | **Interface unverändert lassen**; nur die Implementierung ändern | `GeminiClient` und `SettingsController` konsumieren `GetServiceAccountFilePath()`, `ServiceAccountFileExists()`, `GetGeminiApiKey()` unverändert. Ein signaturneutraler Umbau hält den Änderungsradius minimal. |
| Konfigurationsklasse `GoogleCredentialsOptions` | **Value Object / Options-Klasse**, gebunden an Sektion `GoogleCredentials` | Folgt dem bestehenden Muster (`AIOptions`, `ImageOptions`, `PluginUpdateOptions`), die alle über `services.Configure<T>(configuration.GetSection(...))` registriert sind. |

## Programmabläufe

### Credential-Auflösung Service Account (Laufzeit)

1. Ein Konsument (`GeminiClient.InitHttpClientAsync()` oder `SettingsController.GetMySettings()`) ruft `IGoogleCredentialsProvider.GetServiceAccountFilePath()` bzw. `ServiceAccountFileExists()` auf.
2. `GoogleCredentialsProvider` liest zuerst die Umgebungsvariable `GOOGLE_APPLICATION_CREDENTIALS`; ist sie leer, wird `GoogleCredentialsOptions.ServiceAccountFilePath` aus der Konfiguration verwendet.
3. Ist ein Pfad aufgelöst und die Umgebungsvariable war nicht gesetzt, setzt der Provider `GOOGLE_APPLICATION_CREDENTIALS` auf diesen Pfad (damit die Google-Bibliotheken denselben Wert sehen).
4. `GetServiceAccountFilePath()` gibt den Pfad zurück (oder leeren String, wenn nichts konfiguriert ist). `ServiceAccountFileExists()` prüft zusätzlich `File.Exists()`.
5. Der Konsument verwendet den Pfad wie bisher (`GoogleCredential.FromFile(path)`); es wird **keine** Datei mehr aus `AppContext.BaseDirectory` gelesen.

Beteiligte Klassen/Komponenten: `GoogleCredentialsProvider`, `GoogleCredentialsOptions`, `IGoogleCredentialsProvider`, `GeminiClient`, `SettingsController`

### Credential-Auflösung Gemini API-Key (Laufzeit)

1. Ein Konsument ruft `IGoogleCredentialsProvider.GetGeminiApiKey()` auf.
2. `GoogleCredentialsProvider` liest die Umgebungsvariable `GOOGLE_GEMINI_API_KEY`; ist sie leer, wird `GoogleCredentialsOptions.GeminiApiKey` aus der Konfiguration verwendet.
3. Der aufgelöste Wert wird zurückgegeben (oder leerer String). Es wird **keine** `google.gemini.api-key.json` mehr gelesen.
4. `GeminiClient.InitHttpClientAsync()` setzt bei nicht-leerem Key den Header `x-goog-api-key`; `SettingsController` meldet `GeminiApiKeyAvailable = !string.IsNullOrWhiteSpace(key)`.

Beteiligte Klassen/Komponenten: `GoogleCredentialsProvider`, `GoogleCredentialsOptions`, `GeminiClient`, `SettingsController`

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `GoogleCredentialsOptions` | Konfigurations-/Options-Klasse (Value Object) | Bindet Sektion `GoogleCredentials`; Eigenschaften `ServiceAccountFilePath` und `GeminiApiKey` zur Bereitstellung der Credentials über Konfiguration/Umgebungsvariablen |
| `GoogleCredentialsProviderTests` | Testklasse | Unit-Tests für die neue Auflösungslogik in `GoogleCredentialsProvider` |
| `SettingsCredentialAvailabilityTests` | Integrations-/E2E-Testklasse | Prüft, dass der Settings-Endpunkt die Credential-Verfügbarkeit anhand von Konfiguration/Umgebungsvariablen korrekt meldet |
| `CsprojCredentialCopyTests` | Testklasse | Regressionsschutz: prüft, dass `Rezepte.Web.csproj` keine `CopyToOutputDirectory=Always`-Konfiguration für die Credential-Dateien enthält |

## Änderungen an bestehenden Klassen

### `GoogleCredentialsProvider` (Service / Gateway)

- **Neue Abhängigkeit (Konstruktor):** `IOptionsMonitor<GoogleCredentialsOptions>` — Zugriff auf konfigurierte Credential-Werte (bisher hatte die Klasse keinen Konstruktor / keine DI).
- **Geänderte Methoden:**
  - `GetServiceAccountFilePath()` — liest den Pfad nicht mehr aus `AppContext.BaseDirectory + ServiceAccountFileName`, sondern aus Umgebungsvariable `GOOGLE_APPLICATION_CREDENTIALS` bzw. `GoogleCredentialsOptions.ServiceAccountFilePath`; setzt die Umgebungsvariable, falls nur aus Optionen aufgelöst.
  - `ServiceAccountFileExists()` — prüft `File.Exists()` auf dem neu aufgelösten Pfad; liefert `false`, wenn kein Pfad konfiguriert ist.
  - `GetGeminiApiKey()` — liest den Key nicht mehr aus `google.gemini.api-key.json`, sondern aus Umgebungsvariable `GOOGLE_GEMINI_API_KEY` bzw. `GoogleCredentialsOptions.GeminiApiKey`.
- **Entfernte Elemente:** Konstanten `ServiceAccountFileName`, `GeminiApiKeyFileName`, `accountfile_type_service_account`, `apikeyfile_type_api_key`; private Structs `AccountFile` und `ApiKeyFile` (werden nach dem Umbau nicht mehr benötigt).

### `ServiceCollectionExtensions` (DI-Registrierung)

- **Neue Registrierung:** `services.Configure<GoogleCredentialsOptions>(configuration.GetSection("GoogleCredentials"));` (analog zu `AIOptions` etc.).
- **Geänderte Registrierung:** `AddSingleton<IGoogleCredentialsProvider, GoogleCredentialsProvider>()` bleibt bestehen; die Singleton-Registrierung ist mit `IOptionsMonitor` kompatibel.

### `Rezepte.Web.csproj` (Projektdatei)

- **Entfernte Konfiguration:** Die `<ItemGroup>` (Zeilen 24–31) mit `<Content Update="google.application-credentials.json">` / `<Content Update="google.gemini.api-key.json">` inkl. `CopyToOutputDirectory=Always` wird vollständig gelöscht.

### `.gitignore`

- **Neue Einträge:** `Rezepte.Web/google.application-credentials.json` und `Rezepte.Web/google.gemini.api-key.json` (Regressionsschutz gegen versehentliches Einchecken lokal abgelegter Credential-Dateien).

### `README.md`

- **Geänderter Abschnitt „Voraussetzungen" (Zeilen 82–90):** Die Aussage, dass die Google-Dateien in das Build-Ausgabeverzeichnis kopiert werden, wird korrigiert. Stattdessen Verweis auf Umgebungsvariablen `GOOGLE_APPLICATION_CREDENTIALS` / `GOOGLE_GEMINI_API_KEY` und auf die neue Dokumentation.

## Datenbankmigrationen

Keine.

## Validierungsregeln

Keine. (Fehlende Credentials führen zum bestehenden Verhalten: `ServiceAccountFileExists()`/`HasApiKey()` liefern `false`; `GeminiClient.InitHttpClientAsync()` wirft weiterhin `InvalidOperationException("No valid Gemini authentication configured.")`, wenn weder Key noch Service Account vorhanden sind.)

## Konfigurationsänderungen

| Eintrag | Typ | Standardwert | Zweck |
|---------|-----|--------------|-------|
| `GoogleCredentials:ServiceAccountFilePath` | `string?` | `""` (leer) | Pfad zur Service-Account-JSON-Datei außerhalb des Repositories; alternativ über Umgebungsvariable `GOOGLE_APPLICATION_CREDENTIALS` |
| `GoogleCredentials:GeminiApiKey` | `string?` | `""` (leer) | Gemini API-Key; alternativ über Umgebungsvariable `GOOGLE_GEMINI_API_KEY` |

- In `appsettings.json` wird die Sektion `GoogleCredentials` mit leeren Werten als Schema-Dokumentation aufgenommen (keine echten Secrets im Repo).
- `appsettings.Development.json` erhält keine Secrets; lokale Werte werden über Umgebungsvariablen oder .NET User Secrets bereitgestellt (in der Doku beschrieben).

## Seiteneffekte und Risiken

- **KI-Import (`BaseAIImportHandler.IsActiveAsync()`):** Ruft `geminiClient.HasServiceAccount()` auf. Nach der Umstellung ist der KI-Import nur noch aktiv, wenn Credentials per Umgebungsvariable/Konfiguration bereitstehen. Entwicklungsumgebungen, die sich bisher auf die in die Build-Ausgabe kopierte Datei verlassen haben, müssen die Umgebungsvariablen setzen (in der Doku beschrieben).
- **`SettingsController.GetMySettings()`:** Meldet `GoogleServiceAccountFileAvailable` / `GeminiApiKeyAvailable` künftig auf Basis von Umgebungsvariable/Konfiguration statt Datei-Existenz im Ausgabeverzeichnis. Kein API-Vertragsbruch (Felder unverändert).
- **`GoogleQuotaClient`:** Wird im Code aktuell nirgends instanziiert (toter Pfad) und nimmt einen Pfad direkt entgegen. Kein akutes Risiko; wird im Code-Audit als „erhält Pfad vom Aufrufer, kein Hardcoding" dokumentiert. Keine Änderung erforderlich.
- **CI/Release-Workflow (`.github/workflows/release.yml`):** Führt `dotnet publish` aus und zippt die Ausgabe als Release-Artefakt. Ohne die kopierten Credential-Dateien enthält das Artefakt keine Secrets mehr. Es werden aktuell keine echten Google-Credentials in CI verwendet (Prüfung durchgeführt).
- **`Google.Cloud.Vision.V1` (registriert, aber ungenutzt):** Wird im Rahmen dieser Sicherheitsanforderung bewusst **nicht** entfernt (Scope-Begrenzung — Entscheidung getroffen). Das Paket hat keinen Bezug zum Credential-Copy-Problem; eine Entfernung wird als separates Cleanup vermerkt und außerhalb dieser Aufgabe verfolgt.

## Umsetzungsreihenfolge

1. **`GoogleCredentialsOptions` anlegen**
   - Voraussetzungen: Keine (Options-Pattern via `Microsoft.Extensions.Options` bereits im Projekt genutzt).
   - Beschreibung: Neue Options-Klasse mit `ServiceAccountFilePath` (`string?`) und `GeminiApiKey` (`string?`) im passenden Namespace (`Rezepte.Web.Services` bzw. Konfigurations-Namespace) erstellen.

2. **DI-Registrierung ergänzen**
   - Voraussetzungen: `GoogleCredentialsOptions` (Schritt 1).
   - Beschreibung: In `ServiceCollectionExtensions` `services.Configure<GoogleCredentialsOptions>(configuration.GetSection("GoogleCredentials"))` hinzufügen.

3. **`GoogleCredentialsProvider` umbauen**
   - Voraussetzungen: `GoogleCredentialsOptions` (Schritt 1), DI-Registrierung (Schritt 2).
   - Beschreibung: Konstruktor mit `IOptionsMonitor<GoogleCredentialsOptions>` einführen; die drei Methoden auf Umgebungsvariablen/Optionen umstellen; Datei-Konstanten und Structs entfernen. Interface-Signaturen bleiben unverändert.

4. **`Rezepte.Web.csproj` bereinigen**
   - Voraussetzungen: Keine.
   - Beschreibung: Die unsichere `<ItemGroup>` (Zeilen 24–31) entfernen.

5. **`.gitignore` ergänzen**
   - Voraussetzungen: Keine.
   - Beschreibung: Einträge für `Rezepte.Web/google.application-credentials.json` und `Rezepte.Web/google.gemini.api-key.json` hinzufügen.

6. **`appsettings.json` ergänzen**
   - Voraussetzungen: `GoogleCredentialsOptions` (Schritt 1).
   - Beschreibung: Leere Sektion `GoogleCredentials` mit `ServiceAccountFilePath` und `GeminiApiKey` als Schema-Dokumentation aufnehmen (keine Secrets).

7. **Dokumentation erstellen/aktualisieren**
   - Voraussetzungen: Umgestellte Beschaffung (Schritte 1–3) steht fachlich fest.
   - Beschreibung: `docs/development-guide.md` (neu) mit lokaler Einrichtung über Umgebungsvariablen/User Secrets, der Vorrangregel (Umgebungsvariable vor `GoogleCredentials:*`-Konfiguration) und Begründung, warum Credentials nicht ins Repo gehören; `docs/deployment-guide.md` (neu) mit Secret-Store-/Umgebungsvariablen-Setup für Production. Die Deployment-Doku beschreibt **beide** unterstützten Gemini-Authentifizierungswege — API-Key (`GOOGLE_GEMINI_API_KEY`) und Service Account (`GOOGLE_APPLICATION_CREDENTIALS`) — inkl. Hinweis, dass der API-Key Vorrang vor dem Service Account hat. README-Abschnitt „Voraussetzungen" korrigieren.

8. **Code-Audit dokumentieren**
   - Voraussetzungen: Schritt 3 abgeschlossen.
   - Beschreibung: Bestätigen und (in der Doku) festhalten, dass kein Code mehr Credential-Dateien aus festen Pfaden lädt und dass die Google-Bibliotheken `GOOGLE_APPLICATION_CREDENTIALS` verwenden. `GoogleQuotaClient` als „Pfad vom Aufrufer" vermerken.

9. **Tests ergänzen**
   - Voraussetzungen: Schritte 1–4 abgeschlossen; Testprojekt `Rezepte.Tests` vorhanden.
   - Beschreibung: `GoogleCredentialsProviderTests`, `SettingsCredentialAvailabilityTests` und `CsprojCredentialCopyTests` anlegen (siehe Abschnitt Tests).

10. **Verifikation Build/Publish & Git-Historie**
    - Voraussetzungen: Schritte 3–4 abgeschlossen.
    - Beschreibung: `dotnet build` und `dotnet publish` ausführen und prüfen, dass keine `google.*.json` in `bin/`/`publish/` liegt; `git log --all --full-history` für beide Dateien erneut bestätigen (laut Bestandsaufnahme sauber); Release-Workflow gegenprüfen.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `GetServiceAccountFilePath_ReturnsPath_FromEnvironmentVariable` | `GoogleCredentialsProviderTests` | `GOOGLE_APPLICATION_CREDENTIALS` gesetzt → Pfad wird zurückgegeben |
| `GetServiceAccountFilePath_ReturnsPath_FromOptions_WhenEnvNotSet` | `GoogleCredentialsProviderTests` | Nur Options gesetzt → Pfad wird zurückgegeben und Umgebungsvariable gesetzt |
| `GetServiceAccountFilePath_ReturnsEmpty_WhenNothingConfigured` | `GoogleCredentialsProviderTests` | Keine Konfiguration → leerer String |
| `ServiceAccountFileExists_ReturnsFalse_WhenPathMissing` | `GoogleCredentialsProviderTests` | Nicht existierender/leerer Pfad → `false` |
| `GetGeminiApiKey_ReturnsKey_FromEnvironmentVariable` | `GoogleCredentialsProviderTests` | `GOOGLE_GEMINI_API_KEY` gesetzt → Key wird zurückgegeben |
| `GetGeminiApiKey_ReturnsKey_FromOptions_WhenEnvNotSet` | `GoogleCredentialsProviderTests` | Nur Options gesetzt → Key wird zurückgegeben |
| `GetGeminiApiKey_ReturnsEmpty_WhenNothingConfigured` | `GoogleCredentialsProviderTests` | Keine Konfiguration → leerer String |
| `Csproj_DoesNotCopyCredentialFiles_ToOutput` | `CsprojCredentialCopyTests` | `Rezepte.Web.csproj` enthält keine `CopyToOutputDirectory>Always`-Konfiguration für die Credential-Dateien |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `TestGeminiClient` (`Rezepte.Tests/TestHelpers`) | Keine Änderung erforderlich (liefert weiterhin `true` für `HasApiKey`/`HasServiceAccount`); nur zu prüfen, dass keine Testinfrastruktur die entfernten Datei-Konstanten referenziert. |

Falls beim Umbau keine weiteren bestehenden Tests brechen: Keine.

### E2E-Tests (Pflicht)

| Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium |
|----------|------------------------|-------------------------------|
| Settings-Endpunkt meldet `GeminiApiKeyAvailable = true`, wenn der Key über Umgebungsvariable/Konfiguration bereitgestellt wird | `SettingsCredentialAvailabilityTests` | Kriterium 1 & 5 (Secrets-Bezug über sichere Mechanismen; kein Datei-Hardcoding) |
| Settings-Endpunkt meldet `GeminiApiKeyAvailable = false` / `GoogleServiceAccountFileAvailable = false`, wenn nichts konfiguriert ist | `SettingsCredentialAvailabilityTests` | Kriterium 5 (Verhalten ohne Credentials) |
| Build/Publish-Ausgabe enthält keine `google.*.json` (als Projektdatei-Regressionstest) | `CsprojCredentialCopyTests` | Kriterium 2 (keine Credential-Dateien in Artefakten) |

Welche bestehenden E2E-Tests müssen angepasst werden? Keine.

## Offene Punkte

Keine.
