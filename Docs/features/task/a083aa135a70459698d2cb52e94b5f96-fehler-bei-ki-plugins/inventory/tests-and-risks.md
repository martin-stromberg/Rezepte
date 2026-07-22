# Detail: Tests und Absicherungsbedarf

## Vorhandene Tests

`Rezepte.Tests/Services/GoogleCredentialsProviderTests.cs` deckt ab:

- Service-Account-Pfad aus Environment
- Service-Account-Pfad aus Options
- kein Mutieren der Environment-Variable
- OptionsMonitor-Aenderungen ohne Caching
- leere und Whitespace-Werte
- Existenzpruefung fuer Service-Account-Datei
- Gemini-API-Key aus Environment
- Gemini-API-Key aus Options
- leere und Whitespace-Werte

`Rezepte.Tests/Services/Import/ImportServicePluginTests.cs` deckt generisches Plugin-Routing ab, aber nicht die KI-spezifische Aktivierung.

Weitere Tests im Importbereich pruefen Plugin-Manager, Package-Installer, Plugin-Settings, Orchestrator und produktive Plugin-Parser. Aus der Bestandsaufnahme ergibt sich kein direkter Test fuer die Kombination aus `BaseAIImportHandler`, `AIUrlImportHandler`, `AIFotoImportHandler`, `IGeminiClient.HasApiKey()` und `IGeminiClient.HasServiceAccount()`.

## Fehlende Testfaelle

Empfohlene gezielte Tests:

- `AIUrlImportHandler.CanHandleAsync()` ist aktiv, wenn nur Gemini-API-Key vorhanden ist und globale/User-Settings aktiv sind.
- `AIUrlImportHandler.CanHandleAsync()` ist inaktiv, wenn weder API-Key noch Service-Account vorhanden sind.
- `AIFotoImportHandler.CanHandleAsync()` ist aktiv, wenn Service-Account-Datei vorhanden ist und alle KI-/Vision-/Gemini-Settings aktiv sind.
- `AIFotoImportHandler.CanHandleAsync()` loggt nachvollziehbar, wenn Service-Account fehlt oder nicht existiert.
- Gemeinsame KI-Aktivierungslogik prueft Gemini-Verfuegbarkeit nicht pauschal ueber Service-Account.
- `GeminiClient` verwendet API-Key vor Service-Account.
- `GeminiClient` erzeugt bei fehlender Authentifizierung eine klare, secret-freie Fehlermeldung.
- Ungueltiger oder nicht lesbarer Service-Account wird mit Exception-Typ und Pfad-Kontext geloggt.

## Risiken bei der Umsetzung

- Fotoimport benoetigt Google Vision und damit weiterhin Google-Application-Credentials. Eine pauschale API-Key-only-Freigabe fuer `AIFotoImportHandler` waere falsch.
- URL-Import benoetigt nur Gemini. Dort sollte API-Key-only ausdruecklich erlaubt bleiben.
- Logging darf keine Secrets ausgeben. Besonders `GOOGLE_GEMINI_API_KEY` darf nie als Wert geloggt werden.
- `IGeminiClient` ist aktuell relativ schmal. Wenn Diagnostik in diese Schnittstelle wandert, muessen Tests und Plugin-Projekte angepasst werden.
- `GeminiClient` cached Header im HttpClient. Wenn Options zur Laufzeit wechseln, werden bereits gesetzte Header nicht automatisch aktualisiert.
- `ImportOrchestrator` laeuft im Hintergrund. Fehler muessen sowohl im Serverlog als auch in Sessionstatus/Result sinnvoll sichtbar bleiben.

## Verifikationsstrategie

- Unit-Tests fuer Credential-Provider erweitern, falls neue Diagnosemethoden entstehen.
- Unit-Tests fuer KI-Handler-Aktivierung mit gemocktem `IGeminiClient`, `ISettingsService`, `IAiUsageService` und Logger ergaenzen.
- Unit-Tests fuer `GeminiClient` mit Fake-`HttpMessageHandler` oder abstrahiertem HttpClientFactory-Verhalten ergaenzen, damit keine echten Google-Requests laufen.
- Bestehende `dotnet test`-Suite ausfuehren.
- Optional einen lokalen Smoke-Test fuer Plugin-Discovery nach Build ausfuehren, weil die produktiven KI-Plugins ueber den `plugins`-Output-Ordner geladen werden.
