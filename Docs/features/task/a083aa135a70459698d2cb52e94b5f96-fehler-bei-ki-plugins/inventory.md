# Bestandsaufnahme: Fehler bei KI-Plugins

## Zusammenfassung

Die Anwendung enthaelt bereits eine zentrale Credential-Aufloesung ueber `GoogleCredentialsProvider`. Dort haben `GOOGLE_APPLICATION_CREDENTIALS` und `GOOGLE_GEMINI_API_KEY` Vorrang vor der Konfigurationssektion `GoogleCredentials`.

Der wahrscheinlich wichtigste fachliche Befund liegt in der Aktivierungslogik der KI-Import-Handler: `BaseAIImportHandler.IsActiveAsync()` verlangt zwingend einen vorhandenen Service-Account. Das passt fuer Fotoimporte mit Google Vision, blockiert aber Szenarien, in denen Gemini nur ueber `GOOGLE_GEMINI_API_KEY` betrieben werden soll. `AIUrlImportHandler` umgeht das teilweise, `AIFotoImportHandler` nicht.

Das Logging ist an mehreren Stellen vorhanden, aber bei nicht aktivem Handler entsteht fuer Nutzer und Betrieb oft nur "No suitable import plugin found". Dadurch bleiben Konfigurationsprobleme bei Credentials oder Plugin-Aktivierung schwer nachvollziehbar.

## Detaildokumente

- [Architektur und Importfluss](inventory/architecture.md)
- [Credential-Aufloesung und KI-Aktivierung](inventory/credential-flow.md)
- [Logging und Fehlerbehandlung](inventory/logging-and-errors.md)
- [Tests und Absicherungsbedarf](inventory/tests-and-risks.md)

## Betroffene Bereiche

| Bereich | Relevante Dateien | Befund |
| --- | --- | --- |
| Credential-Provider | `Rezepte.Web/Services/GoogleCredentialsProvider.cs`, `Rezepte.Web/Configuration/GoogleCredentialsOptions.cs` | Env-Variablen werden korrekt bevorzugt, aber es gibt keine diagnostische Ausgabe ueber Quelle/Fehlerzustand. |
| Gemini-Client | `Rezepte.Web/Services/Import/GeminiClient.cs` | API-Key wird vor Service-Account verwendet. Fehler bei fehlender/ungueltiger Authentifizierung werden nur generisch geworfen. |
| KI-Basis-Handler | `Rezepte.Web/Services/Import/BaseAIImportHandler.cs` | Basisklasse verlangt Service-Account und deaktiviert damit AI-Handler vor dem eigentlichen Gemini-Aufruf. |
| AI-Foto-Plugin | `Rezepte.Import.Plugins.AIFoto/AIFotoImportHandler.cs` | Benoetigt fachlich Google Vision und Gemini; prueft ueber Basisklasse implizit nur Service-Account, nicht explizit beide benoetigten Credentials. |
| AI-URL-Plugin | `Rezepte.Import.Plugins.AIUrl/AIUrlImportHandler.cs` | Laesst API-Key-Only fuer Gemini zu, prueft danach Settings. |
| Plugin-Orchestrierung | `Rezepte.Web/Services/Import/ImportOrchestrator.cs`, `Rezepte.Web/Services/Import/ImportService.cs`, `Rezepte.Web/Services/Import/Plugins/PluginManager.cs` | Exceptions werden geloggt, aber Handler, die wegen `CanHandleAsync == false` ausscheiden, liefern keine Ursache. |
| Tests | `Rezepte.Tests/Services/GoogleCredentialsProviderTests.cs`, `Rezepte.Tests/Services/Import/*` | Provider-Verhalten ist getestet; Aktivierungslogik und Diagnose fuer KI-Handler sind nicht erkennbar abgedeckt. |

## Wahrscheinliche technische Ursache

1. Die Service-Umgebung setzt `GOOGLE_GEMINI_API_KEY` und `GOOGLE_APPLICATION_CREDENTIALS`.
2. `GoogleCredentialsProvider` kann diese Werte grundsaetzlich lesen.
3. Beim Import wird zuerst `CanHandleAsync()` der Plugin-Handler aufgerufen.
4. In der KI-Basisklasse fuehrt `!geminiClient.HasServiceAccount()` sofort zu `false`.
5. Wenn Service-Account-Datei fehlt, nicht lesbar ist oder der Prozess keinen Zugriff hat, wird der Handler deaktiviert, ohne dass ein klarer Credential-Fehler geloggt wird.
6. Fuer Gemini-API-Key-only-Szenarien ist dieses Verhalten fachlich falsch, weil `GeminiClient` selbst API-Key-Authentifizierung unterstuetzt.

## Umsetzungshinweise fuer den Plan

- Die Aktivierungspruefung sollte zwischen Gemini-Verfuegbarkeit und Google-Vision-Verfuegbarkeit unterscheiden.
- Fuer Gemini genuegt `HasApiKey()` oder ein verwendbarer Service-Account.
- Fuer Fotoimport muss Google Vision weiterhin einen vorhandenen und lesbaren Service-Account voraussetzen.
- Fehlende, nicht existierende oder nicht lesbare Credential-Dateien sollten mit Pfad-Kontext geloggt werden, ohne Secret-Inhalte auszugeben.
- `CanHandleAsync()` sollte bei deaktivierter KI nicht nur still `false` liefern, sondern intern diagnostisch protokollieren, warum ein Handler nicht aktiv ist.
- Tests sollten API-Key-only, Service-Account-only, fehlende Datei und nicht lesbare/ungueltige Datei getrennt abdecken.

## Offene Punkte aus der Anforderung

- Die konkrete produktive Fehlermeldung ist nicht bekannt.
- Fuer Fotoimporte ist Google Vision die erkennbare Google-Application-Funktion.
- Die alte dateibasierte Gemini-Key-Quelle im Programmverzeichnis ist im aktuellen Code nicht mehr als Pflichtpfad vorhanden; Fallback erfolgt ueber `GoogleCredentials:GeminiApiKey`.
