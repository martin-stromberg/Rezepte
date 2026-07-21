# Datenmodelle & Konfigurationsklassen

## `AIRecipe`

Verwendet in: `Rezepte.Web/Services/Import/GeminiClient.cs`, `Rezepte.Tests/TestHelpers/TestGeminiClient.cs`

**Beschreibung:** Repräsentiert ein aus OCR oder HTML extrahiertes Rezept von der Gemini API.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Title` | `string?` | Titel des Rezepts |
| `Instructions` | `string?` | Zubereitungsschritte als Fließtext |
| `Ingredients` | `List<string>?` | Liste der Zutaten, jede als einzelner String |
| `Portions` | `int` | Anzahl der Portionen |
| `PreparationTimeInMinutes` | `int` | Vorbereitungszeit in Minuten |
| `CookingTimeInMinutes` | `int` | Kochzeit in Minuten |
| `ImageUri` | `string?` | URL zum Rezeptbild (falls von Gemini extrahiert) |
| `ImageData` | `byte[]?` | Binärdaten des Rezeptbildes (wird heruntergeladen, wenn ImageUri vorhanden) |

**Verwendung:**
- Rückgabewert von `IGeminiClient.ExtractRecipeAsync()` und `ExtractRecipeFromUrlAsync()`
- Wird in `BaseAIImportHandler` in `ImportedRecipe` konvertiert

---

## `AIOptions`

Datei: Registriert in `ServiceCollectionExtensions.cs` (Zeile 29)

```csharp
services.Configure<AIOptions>(configuration.GetSection("AI"));
```

**Beschreibung:** Konfigurationsoptionen für KI-Features. Verwendet in `BaseAIImportHandler`.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Simulate` | `bool` | Aktiviert Simulationsmodus für KI-Anfragen (keine echten API-Aufrufe) |

**Quelle:** `appsettings.json` Sektion `"AI"` (nicht vorhanden, wird mit Defaults geladen)

**Verwendung:**
- `BaseAIImportHandler.IsSimulationModeActive` (Zeile 48)
- Bestimmt ob echte oder simulierte Rezepte zurückgegeben werden

---

## `ServiceAccount JSON Struktur` (google.application-credentials.json)

Verwendet von: `GoogleCredentialsProvider` (als Struct `AccountFile`, Zeilen 12-24)

```csharp
private struct AccountFile
{
    public string project_id { get; set; }
    public string private_key_id { get; set; }
    public string private_key { get; set; }
    public string client_email { get; set; }
    public string client_id { get; set; }
    public string auth_uri { get; set; }
    public string token_uri { get; set; }
    public string auth_provider_x509_cert_url { get; set; }
    public string client_x509_cert_url { get; set; }
    public string universe_domain { get; set; }
}
```

**Zweck:** Struktur zur JSON-Deserialisierung der Google Service Account Datei

**Status:** Nicht verwendet im analysierten Code (struct ist definiert, wird aber nicht aktiv genutzt)

---

## `API Key JSON Struktur` (google.gemini.api-key.json)

Verwendet von: `GoogleCredentialsProvider` (als Struct `ApiKeyFile`, Zeilen 25-29)

```csharp
private struct ApiKeyFile
{
    public string type { get; set; }
    public string api_key { get; set; }
}
```

**Zweck:** Struktur zur JSON-Deserialisierung der Gemini API Key Datei

**Verwendung:** `GetGeminiApiKey()` (Zeilen 56-58)
- Prüft ob `type == "api_key"`
- Gibt `api_key` Wert zurück

**Dateiformat-Beispiel:**
```json
{
  "type": "api_key",
  "api_key": "AIza..."
}
```

---

## Umgebungsvariablen

| Variable | Gesetzt durch | Verwendung | Zweck |
|----------|----------------|-----------|-------|
| `GOOGLE_APPLICATION_CREDENTIALS` | `GoogleCredentialsProvider.GetServiceAccountFilePath()` (Zeile 38) | Wird von Google Cloud Bibliotheken gelesen | Pfad zur Service Account JSON Datei |

**Setzungslogik:**
- Wird nur gesetzt wenn `google.application-credentials.json` existiert
- Wird auf den vollständigen Pfad zur Datei gesetzt (Kombination aus `AppContext.BaseDirectory` + Dateiname)
