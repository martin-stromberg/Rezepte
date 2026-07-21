# Services & Logik

## `GoogleCredentialsProvider`

Datei: `Rezepte.Web/Services/GoogleCredentialsProvider.cs`

**Beschreibung:** Implementierung von `IGoogleCredentialsProvider`. Lädt Google-Credentials aus Dateien im Arbeitsverzeichnis und setzt entsprechende Umgebungsvariablen.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `GetServiceAccountFilePath()` | public | Ermittelt den Pfad zur `google.application-credentials.json` Datei (kombiniert AppContext.BaseDirectory mit konstantem Dateinamen) |
| `ServiceAccountFileExists()` | public | Prüft mit `File.Exists()`, ob die Service-Account-Datei vorhanden ist |
| `GetGeminiApiKey()` | public | Liest `google.gemini.api-key.json`, deserialisiert JSON und extrahiert `api_key`-Feld |

**Konstanten:**
- `ServiceAccountFileName = "google.application-credentials.json"`
- `GeminiApiKeyFileName = "google.gemini.api-key.json"`

**Implementierungsdetails:**
- `GetServiceAccountFilePath()` ruft automatisch `Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", jsonPath)` auf, wenn die Datei existiert (Zeile 38)
- `GetGeminiApiKey()` liest die JSON-Datei und deserialisiert sie in ein `ApiKeyFile`-Struct (Zeilen 50-61)
- Es gibt zwei private Structs: `AccountFile` und `ApiKeyFile` zur JSON-Deserialisierung

**Abhängigkeiten:** Keine Dependency Injection, wird als Singleton registriert

---

## `GeminiClient`

Datei: `Rezepte.Web/Services/Import/GeminiClient.cs`

**Beschreibung:** HTTP-Client für die Google Gemini API. Extrahiert Rezepte aus OCR-Text oder HTML-Inhalten mittels generativer KI.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `GeminiClient(IHttpClientFactory, IGoogleCredentialsProvider, ILogger)` | public | Konstruktor, initialisiert HttpClient und Dependencies |
| `InitHttpClientAsync()` | private | Initialisiert HTTP-Client mit Authentifizierung (API-Key oder Bearer Token) |
| `ExtractRecipeAsync(string, CancellationToken)` | public | Sendet OCR-Text an Gemini API, parsing Rezeptinformationen |
| `ExtractRecipeFromUrlAsync(string, CancellationToken)` | public | Sendet HTML-Inhalt an Gemini API, parst Rezeptinformationen |
| `ParseRecipe(string)` | private | Parst die Gemini-Antwort und konvertiert in AIRecipe Objekt |
| `ParseMinutes(string)` | private | Parst Zeitangaben (Minuten/Stunden) aus Textform |
| `ParsePortion(string)` | private | Extrahiert Portionszahl aus Text |
| `ParseInformation(string, string)` | private | Extrahiert Abschnitte aus der Rezeptantwort |
| `ExtractInformation(string)` | private | Extrahiert alle Schlüssel-Wert-Paare aus Rezeptantwort |
| `HasServiceAccount()` | public | Delegiert an `IGoogleCredentialsProvider.ServiceAccountFileExists()` |
| `HasApiKey()` | public | Delegiert an `IGoogleCredentialsProvider.GetGeminiApiKey()` (prüft auf nicht-leeren String) |

**Abhängigkeiten:**
- `IHttpClientFactory` – für HTTP-Requests
- `IGoogleCredentialsProvider` – für Credential-Loading
- `ILogger<GeminiClient>` – für Logging

**Authentifizierung:**
1. Prüft zuerst auf Gemini API-Key (über `x-goog-api-key` Header)
2. Falls kein API-Key: Versucht Service Account zu laden und erzeugt Bearer Token
3. Wirft `InvalidOperationException` wenn keine Authentifizierung konfiguriert

**Besonderheiten:**
- Verwendet Thread-Lock (`_initLock`) um wiederholte Initialisierungen zu vermeiden
- Gemini Model: `gemini-2.5-flash-lite`
- API Endpoint: `https://generativelanguage.googleapis.com/v1beta/models/`

---

## `GoogleQuotaClient`

Datei: `Rezepte.Web/Services/Import/GoogleQuotaClient.cs`

**Beschreibung:** Client für die Google Service Usage API. Ruft Quota-Metriken für Google Cloud Services ab.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `GoogleQuotaClient(string)` | public | Konstruktor, nimmt Pfad zur Service Account JSON Datei |
| `GetQuotaAsync(string, string)` | public async | Ruft Quota-Informationen für einen Service ab |

**Abhängigkeiten:**
- Nimmt Service Account JSON Pfad direkt im Konstruktor an (nicht über IGoogleCredentialsProvider)
- Erstellt `GoogleCredential.FromFile()` bei jedem Aufruf

**Authentifizierung:**
- Nutzt Google Cloud Platform Service Account
- Scope: `https://www.googleapis.com/auth/cloud-platform`
- Wird mit Bearer Token (Access Token) authentifiziert

**API Endpoint:**
- `https://serviceusage.googleapis.com/v1beta1/projects/{projectId}/services/{serviceName}/consumerQuotaMetrics`

---

## `BaseAIImportHandler`

Datei: `Rezepte.Web/Services/Import/BaseAIImportHandler.cs`

**Beschreibung:** Abstrakte Basis-Klasse für KI-gestützte Rezept-Import-Handler. Prüft Credential-Verfügbarkeit und orchestriert Rezeptextraktion.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `IsActiveAsync()` | protected virtual | Prüft, ob KI-Import aktiviert ist (Credentials + Einstellungen) |
| `CanHandleAsync(Stream, string, CancellationToken)` | public | Prüft, ob Handler den Stream verarbeiten kann |
| `HandleAsync(Stream, string, string, string, string, CancellationToken)` | public | Führt Rezeptextraktion durch |
| `HandleInteractiveAsync(...)` | public | Interaktive Variante mit Benutzerbestätigung |

**Credential-Abhängigkeiten:**
- `IsActiveAsync()` ruft `geminiClient.HasServiceAccount()` auf, um zu prüfen ob Credentials vorhanden sind (Zeile 34)
- Falls keine Service Account: Handler wird als inaktiv markiert

**Abhängigkeiten:**
- `IOptionsMonitor<AIOptions>` – für Konfiguration
- `IAiUsageService` – für Nutzungs-Tracking
- `IRecipeService` – für Rezept-Verwaltung
- `IGeminiClient` – für Rezeptextraktion
- `ISettingsService` – für Einstellungen
- `ILogger` – für Logging

---

## `SettingsController`

Datei: `Rezepte.Web/Controllers/SettingsController.cs`

**Beschreibung:** API-Controller für Benutzer- und System-Einstellungen. Gibt Status der Google-Credentials zurück.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `GetMySettings(CancellationToken)` | public | Liefert alle Einstellungen für aktuellen Benutzer + Credential-Verfügbarkeit |

**Credential-Abfragen (Zeilen 42-43):**
```csharp
var serviceAccountAvailable = _googleCredentialsProvider.ServiceAccountFileExists();
var apiKeyAvailable = !string.IsNullOrWhiteSpace(_googleCredentialsProvider.GetGeminiApiKey());
```

**Response-Felder für Credentials:**
- `GoogleServiceAccountFileAvailable` (bool)
- `GeminiApiKeyAvailable` (bool)

**Abhängigkeiten:**
- `ISettingsService` – für Settings-Verwaltung
- `IGoogleCredentialsProvider` – zur Abfrage der Credential-Verfügbarkeit
