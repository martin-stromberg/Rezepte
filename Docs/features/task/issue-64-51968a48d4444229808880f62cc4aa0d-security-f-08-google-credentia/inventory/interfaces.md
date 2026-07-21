# Interfaces

## `IGoogleCredentialsProvider`

Datei: `Rezepte.Web/Services/IGoogleCredentialsProvider.cs`

**Beschreibung:** Provider für den Pfad zur Google Service Account JSON Datei. Setzt die Umgebungsvariable `GOOGLE_APPLICATION_CREDENTIALS` wenn die Datei vorhanden ist.

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetServiceAccountFilePath()` | — | `string` | Liefert den vollständigen Pfad zur Service-Account-Datei (auch wenn sie nicht existiert) |
| `ServiceAccountFileExists()` | — | `bool` | Prüft, ob die Service-Account-Datei vorhanden ist |
| `GetGeminiApiKey()` | — | `string` | Liefert den API-Key für Gemini (leer, falls Datei nicht vorhanden oder ungültiges Format) |

**Implementierung:** `GoogleCredentialsProvider` (siehe services.md)

---

## `IGeminiClient`

Datei: `Rezepte.Web/Services/Import/IGeminiClient.cs`

**Beschreibung:** Client für die Interaktion mit Gemini API zur Rezeptextraktion aus OCR-Text oder HTML-Inhalten.

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `ExtractRecipeAsync()` | `string ocrText`, `CancellationToken ct` | `Task<AIRecipe[]>` | Extrahiert Rezeptinformationen aus OCR-Text mittels Gemini API |
| `ExtractRecipeFromUrlAsync()` | `string responseContent`, `CancellationToken ct` | `Task<AIRecipe[]>` | Extrahiert Rezeptinformationen aus HTML-Inhalt mittels Gemini API |
| `HasServiceAccount()` | — | `bool` | Prüft, ob Service Account vorhanden ist |
| `HasApiKey()` | — | `bool` | Prüft, ob Gemini API Key vorhanden ist |

**Implementierung:** `GeminiClient` (siehe services.md)

**Verwendet:** `IGoogleCredentialsProvider` zur Abfrage der verfügbaren Credentials
