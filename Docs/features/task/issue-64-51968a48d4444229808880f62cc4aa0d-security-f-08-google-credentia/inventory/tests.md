# Tests & Test-Hilfsmittel

## Test-Implementierungen

### `TestGeminiClient`

Datei: `Rezepte.Tests/TestHelpers/TestGeminiClient.cs`

**Beschreibung:** Mock/Test-Implementation von `IGeminiClient` für Unit-Tests. Simuliert API-Responses ohne echte Netzwerk-Aufrufe.

**Methoden:**

| Methode | Rückgabewert | Verhalten |
|---------|--------------|----------|
| `ExtractRecipeAsync(string, CancellationToken)` | `Task<AIRecipe[]>` | Gibt simuliertes Rezept mit festem Titel zurück, enthält erster 20 Zeichen des OCR-Textes in Instructions |
| `ExtractRecipeFromUrlAsync(string, CancellationToken)` | `Task<AIRecipe[]>` | Gibt simuliertes URL-Rezept zurück, Länge des Response-Contents in Instructions |
| `HasApiKey()` | `bool` | Gibt immer `true` zurück |
| `HasServiceAccount()` | `bool` | Gibt immer `true` zurück |

**Test-Daten:**
- OCR-Rezept:
  - Title: "Simuliertes Rezept"
  - Instructions: Simulierte Antwort mit OCR-Text-Snippet
  - Ingredients: `["1 Zutat"]`
  - Portions: 1
  - PreparationTimeInMinutes: 10

- URL-Rezept:
  - Title: "Simuliertes URL-Rezept"
  - Instructions: Simulierte Antwort mit Länge des Response-Contents
  - Ingredients: `["1 Zutat"]`
  - Portions: 2
  - PreparationTimeInMinutes: 5

**Verwendung:** Wird in Tests eingebunden, um echte Gemini-API-Aufrufe zu vermeiden

---

## Dependency Injection Setup

### `ServiceCollectionExtensions`

Datei: `Rezepte.Web/Extensions/ServiceCollectionExtensions.cs`

**Credential-bezogene Registrierungen:**

```csharp
// Zeile 110
services.AddSingleton<IGoogleCredentialsProvider, GoogleCredentialsProvider>();

// Zeile 161
services.AddScoped<IGeminiClient, GeminiClient>();
```

**Registrierungen für HTTP-Clients:**

```csharp
// Zeile 116-118
services.AddHttpClient<ApiClient>()
    .AddHttpMessageHandler<ApiAuthHandler>()
    .AddHttpMessageHandler<AntiForgeryHandler>();
```

**Konfiguration (Zeilen 28-30):**
```csharp
services.Configure<ImageOptions>(configuration.GetSection("Images"));
services.Configure<AIOptions>(configuration.GetSection("AI"));
services.Configure<PluginUpdateOptions>(configuration.GetSection("PluginUpdates"));
```

**Hinweis:** Es gibt keine explizite Konfiguration für Google-Credentials aus `appsettings.json`. Die Credentials werden direkt aus Dateien (`google.application-credentials.json`, `google.gemini.api-key.json`) geladen.

---

## Konfigurationsquellen

### `appsettings.json`

Datei: `Rezepte.Web/appsettings.json`

**Bestandteile:**
- Logging-Level
- AllowedHosts
- JWT-Einstellungen (Key, Issuer, Audience, LifetimeMinutes)
- Images-Einstellungen (MaxSizeBytes, CacheMaxAgeSeconds, AllowedContentTypes)
- PluginUpdates-Einstellungen (GitHubApiBaseUrl, TimeoutSeconds, UserAgent)

**Credential-Konfiguration:** KEINE vorhanden

### `appsettings.Development.json`

Datei: `Rezepte.Web/appsettings.Development.json`

**Bestandteile:**
- Logging-Level (nur Development-spezifisch)

**Credential-Konfiguration:** KEINE vorhanden

---

## Ersichtliche Test-Szenarien & Abdeckung

### Implizit getestete Funktionalität

- Credential-Verfügbarkeit wird geprüft in `BaseAIImportHandler.IsActiveAsync()` – Tests würden hier wahrscheinlich `TestGeminiClient.HasServiceAccount()` / `HasApiKey()` verwenden
- `SettingsController.GetMySettings()` prüft `ServiceAccountFileExists()` und `GetGeminiApiKey()` – wird implizit getestet wenn Settings-Endpoints aufgerufen werden
- `GeminiClient.InitHttpClientAsync()` wird bei jedem ExtractRecipeAsync-Aufruf aufgerufen

### Mögliche Test-Lücken

- Keine expliziten Unit-Tests für `GoogleCredentialsProvider` gefunden (der Credentials eigentlich lädt)
- Keine Tests für `GoogleQuotaClient`
- Keine Tests für die Datei-lesende Logik in `GoogleCredentialsProvider.GetServiceAccountFilePath()` und `GetGeminiApiKey()`
- Keine Prüfung der Umgebungsvariablen-Setzung in `GetServiceAccountFilePath()`
