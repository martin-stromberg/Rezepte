# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### Rezepte.Web/Services/GoogleCredentialsProvider.cs (GoogleCredentialsProvider)

- **Fehlende Kapselung / Code Smell (versteckter Seiteneffekt in Query-Methode)** — `GetServiceAccountFilePath()` (Zeilen 18-32) verletzt das Command-Query-Separation-Prinzip: Die als reiner Getter benannte Methode schreibt in Zeile 27 die prozessweite Umgebungsvariable `GOOGLE_APPLICATION_CREDENTIALS`. Da `ServiceAccountFileExists()` (Zeile 34-40) diese Methode aufruft, mutiert bereits eine reine Existenzprüfung globalen Prozesszustand. Dieser Seiteneffekt ist aus dem Methodennamen nicht ersichtlich und erzwingt in den Tests eine serialisierte Ausführung (`GoogleCredentialsEnvironmentCollection`) samt Zurücksetzen der Variablen.

  Empfehlung: Das Setzen der Umgebungsvariable aus der Query-Methode entfernen und einmalig an einer klar benannten Stelle (z. B. beim Anwendungsstart bzw. in einer expliziten `EnsureEnvironmentConfigured()`-Methode) durchführen. Die Getter-Methoden sollen nur lesen, nicht mutieren.

- **Kopplung / Erweiterbarkeit (IOptionsMonitor-Hot-Reload wird durch Caching in die Umgebungsvariable ausgehebelt)** — In `GetServiceAccountFilePath()` wird der aus `_options.CurrentValue.ServiceAccountFilePath` gelesene Wert in Zeile 27 in die Umgebungsvariable geschrieben. Beim nächsten Aufruf gewinnt der Environment-Zweig (Zeilen 20-22) und liefert diesen zwischengespeicherten Wert zurück. Dadurch werden spätere Konfigurationsänderungen, die `IOptionsMonitor` gerade ermöglichen soll, dauerhaft ignoriert. Die Wahl von `IOptionsMonitor` (statt `IOptions`) und dieses Caching-Verhalten widersprechen sich.

  Empfehlung: Entweder auf das Schreiben in die Umgebungsvariable verzichten (dann bleibt der Options-Wert stets live), oder — falls die Umgebungsvariable für Drittbibliotheken zwingend gesetzt sein muss — bewusst auf `IOptions<GoogleCredentialsOptions>` wechseln und das Verhalten dokumentieren, damit die Erwartung an Live-Updates nicht erweckt wird.

### Rezepte.Tests/Services/GoogleCredentialsProviderTests.cs und Rezepte.Tests/Controllers/SettingsCredentialAvailabilityTests.cs

- **Doppelter Code (identische Test-Infrastruktur über zwei Dateien)** — Die verschachtelte Hilfsklasse `EnvironmentVariableScope` ist in beiden Testdateien nahezu identisch dupliziert (GoogleCredentialsProviderTests.cs Zeilen 109-124, SettingsCredentialAvailabilityTests.cs Zeilen 100-115), ebenso die beiden Konstanten `ServiceAccountEnvironmentVariable` und `GeminiApiKeyEnvironmentVariable` (jeweils Zeilen 14-15 bzw. 18-19). Es existiert bereits ein `Rezepte.Tests/TestHelpers`-Namespace, in dem gemeinsame Test-Infrastruktur liegt (`GoogleCredentialsEnvironmentCollection`).

  Empfehlung: `EnvironmentVariableScope` (inkl. der beiden Umgebungsvariablen-Konstanten) in eine wiederverwendbare Klasse unter `Rezepte.Tests/TestHelpers` extrahieren und aus beiden Testklassen referenzieren.

## Geprüfte Dateien

- `Rezepte.Web/Services/GoogleCredentialsProvider.cs`
- `Rezepte.Web/Extensions/ServiceCollectionExtensions.cs`
- `Rezepte.Web/appsettings.json`
- `Rezepte.Web/Rezepte.Web.csproj`
- `Rezepte.Web/Configuration/GoogleCredentialsOptions.cs`
- `Rezepte.Tests/Services/GoogleCredentialsProviderTests.cs`
- `Rezepte.Tests/Controllers/SettingsCredentialAvailabilityTests.cs`
- `Rezepte.Tests/Deployment/CsprojCredentialCopyTests.cs`
- `Rezepte.Tests/TestHelpers/GoogleCredentialsEnvironmentCollection.cs`
