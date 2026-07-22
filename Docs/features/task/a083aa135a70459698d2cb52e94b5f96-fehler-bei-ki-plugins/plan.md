# Umsetzungsplan: Fehler bei KI-Plugins beheben

## Zielbild

Die KI-gestuetzte Rezepterfassung nutzt die per systemd gesetzten Zugangsdaten wieder korrekt:

- Gemini verwendet bevorzugt `GOOGLE_GEMINI_API_KEY`.
- Google Vision verwendet `GOOGLE_APPLICATION_CREDENTIALS`.
- URL-Importe bleiben mit Gemini-API-Key ohne Service-Account nutzbar.
- Fotoimporte verlangen weiterhin Google-Vision-Credentials und Gemini-Zugriff.
- Credential-, Initialisierungs- und Plugin-Aktivierungsfehler werden nachvollziehbar, aber ohne Secret-Werte geloggt.
- Bestehende lokale Konfigurationswege ueber `GoogleCredentials` bleiben als Fallback erhalten.

## Technischer Ansatz

Die aktuelle Credential-Aufloesung ist grundsaetzlich richtig. Der Fehler liegt voraussichtlich in der Aktivierungslogik: `BaseAIImportHandler.IsActiveAsync()` verlangt pauschal `geminiClient.HasServiceAccount()`. Dadurch wird KI deaktiviert, obwohl `GeminiClient` selbst API-Key-Authentifizierung unterstuetzt. Die Umsetzung trennt deshalb allgemeine KI-Aktivierung, Gemini-Verfuegbarkeit und Google-Vision-Verfuegbarkeit.

## Arbeitspakete

### 1. KI-Basisaktivierung entkoppeln

Betroffene Dateien:

- `Rezepte.Web/Services/Import/BaseAIImportHandler.cs`
- `Rezepte.Web/Services/Import/IGeminiClient.cs`
- `Rezepte.Web/Services/Import/GeminiClient.cs`

Umsetzung:

- `BaseAIImportHandler.IsActiveAsync()` darf keinen Service-Account mehr als allgemeine KI-Voraussetzung erzwingen.
- Die Basisklasse prueft nur noch die globalen und benutzerbezogenen KI-Schalter:
  - `GetGlobalAiEnabledAsync()`
  - `GetUserAiEnabledAsync(UserId)`
- Eine geschuetzte Hilfsmethode fuer Gemini-Verfuegbarkeit einfuehren, z. B. `HasGeminiAuthentication()`, die `HasApiKey()` oder `HasServiceAccount()` akzeptiert.
- Bei deaktivierter KI oder fehlender Gemini-Authentifizierung mit Handlername, UserId und Grund loggen.
- Keine Secret-Werte loggen.

### 2. AI-URL-Plugin auf Gemini-Anforderung vereinfachen

Betroffene Datei:

- `Rezepte.Import.Plugins.AIUrl/AIUrlImportHandler.cs`

Umsetzung:

- `IsActiveAsync()` auf die neue Basislogik umstellen:
  - allgemeine KI-Aktivierung ueber `base.IsActiveAsync()`
  - Gemini-Authentifizierung ueber API-Key oder Service-Account
  - globale und benutzerbezogene Gemini-Schalter
- Die aktuelle Sonderlogik `HasApiKey() || base.IsActiveAsync()` entfernen, weil die Basisklasse danach nicht mehr Service-Account-basiert blockiert.
- HTML-Erkennung und Importverhalten unveraendert lassen.

### 3. AI-Foto-Plugin explizit auf Vision und Gemini pruefen

Betroffene Datei:

- `Rezepte.Import.Plugins.AIFoto/AIFotoImportHandler.cs`

Umsetzung:

- `IsActiveAsync()` prueft explizit:
  - allgemeine KI-Aktivierung
  - Service-Account-Datei fuer Google Vision vorhanden
  - Gemini-Authentifizierung vorhanden, also API-Key oder Service-Account
  - globale und benutzerbezogene Google-Vision-Schalter
  - globale und benutzerbezogene Gemini-Schalter
- Fotoimport darf nicht durch API-Key-only aktiviert werden, wenn kein Service-Account fuer Vision vorhanden ist.
- Fehlende Vision-Credentials mit Pfad-Kontext loggen, sofern ein Pfad konfiguriert ist.

### 4. Credential-Diagnose verbessern

Betroffene Dateien:

- `Rezepte.Web/Services/GoogleCredentialsProvider.cs`
- `Rezepte.Web/Services/IGoogleCredentialsProvider.cs`
- `Rezepte.Web/Services/Import/GeminiClient.cs`

Umsetzung:

- Die Schnittstelle um diagnostische, secret-freie Informationen erweitern oder kleine Hilfsmethoden ergaenzen:
  - ob `GOOGLE_APPLICATION_CREDENTIALS` gesetzt ist
  - ob `GoogleCredentials:ServiceAccountFilePath` als Fallback verwendet wird
  - ob `GOOGLE_GEMINI_API_KEY` gesetzt ist
  - ob `GoogleCredentials:GeminiApiKey` als Fallback verwendet wird
  - Service-Account-Pfad und Existenzstatus
- Gemini-API-Key nur als "vorhanden/nicht vorhanden" und Quelle loggen, niemals als Wert.
- `GeminiClient.InitHttpClientAsync()` bei fehlender Authentifizierung mit konkreter Meldung abbrechen:
  - kein API-Key vorhanden
  - kein Service-Account-Pfad konfiguriert
  - Service-Account-Datei nicht gefunden
- Fehler beim Laden der Service-Account-Datei (`GoogleCredential.FromFile`) mit Exception-Typ, Message und Pfad loggen und danach eine klare `InvalidOperationException` werfen.
- Header-Initialisierung unveraendert API-Key vor Service-Account priorisieren.

### 5. Handler-Diagnose fuer stille `false`-Rueckgaben ergaenzen

Betroffene Dateien:

- `Rezepte.Web/Services/Import/BaseAIImportHandler.cs`
- `Rezepte.Import.Plugins.AIUrl/AIUrlImportHandler.cs`
- `Rezepte.Import.Plugins.AIFoto/AIFotoImportHandler.cs`

Umsetzung:

- Alle Konfigurations- und Aktivierungsgruende, die zu `false` in `IsActiveAsync()` fuehren, mindestens als `Information` oder `Warning` loggen.
- Inhaltliche Nicht-Zustaendigkeit wie falsche Dateiendung oder nicht erkanntes HTML weiterhin nicht als Fehler behandeln.
- Bei Exceptions in Stream-/HTML-Erkennung weiterhin `false` liefern, aber eine Debug- oder Warning-Meldung mit Handlername und Dateiname schreiben.

### 6. Tests ergaenzen

Neue oder erweiterte Tests:

- `Rezepte.Tests/Services/Import/AIImportHandlerActivationTests.cs`
- `Rezepte.Tests/Services/GoogleCredentialsProviderTests.cs`
- optional `Rezepte.Tests/Services/Import/GeminiClientTests.cs`

Testfaelle:

- AI-URL ist aktiv, wenn nur Gemini-API-Key vorhanden ist und AI-/Gemini-Settings aktiv sind.
- AI-URL ist inaktiv, wenn weder API-Key noch Service-Account vorhanden sind.
- AI-Foto ist aktiv, wenn Service-Account-Datei vorhanden ist, Gemini authentifiziert ist und alle AI-/Vision-/Gemini-Settings aktiv sind.
- AI-Foto ist inaktiv, wenn nur Gemini-API-Key vorhanden ist, aber Vision-Service-Account fehlt.
- AI-Foto ist inaktiv, wenn Service-Account-Pfad gesetzt ist, die Datei aber nicht existiert.
- Gemeinsame Basislogik blockiert nicht mehr pauschal ohne Service-Account.
- `GeminiClient` verwendet API-Key vor Service-Account.
- `GeminiClient` wirft bei fehlender Authentifizierung eine klare, secret-freie Fehlermeldung.
- Provider-Diagnose unterscheidet Environment-Quelle und Options-Fallback, ohne Werte des API-Keys offenzulegen.

Mocks und Hilfen:

- Bestehende Testkonventionen mit xUnit, FluentAssertions und Moq verwenden.
- `IGeminiClient`, `ISettingsService`, `IAiUsageService`, `IRecipeService` und Logger mocken.
- Fuer Dateiexistenz temporare Dateien verwenden; Environment-Variablen ueber `EnvironmentVariableScope` isolieren.

### 7. Verifikation

Auszufuehren:

```powershell
dotnet test
```

Optional, falls die Testlaufzeit oder Plugin-Kopierlogik auffaellig ist:

```powershell
dotnet build
```

Manuelle Smoke-Pruefung auf dem Linux-Server nach Deployment:

- systemd-Service enthaelt `GOOGLE_GEMINI_API_KEY`.
- systemd-Service enthaelt `GOOGLE_APPLICATION_CREDENTIALS=/etc/rezepte/secrets/google.application-credentials.json`.
- Die Credentials-Datei existiert und ist fuer den Service-User lesbar.
- URL-Rezeptimport erzeugt Gemini-Logeintrag mit API-Key-Quelle, aber ohne Key-Wert.
- Foto-Rezeptimport erzeugt Vision- und Gemini-Nutzungslogs.
- Bei absichtlich falschem Credential-Pfad erscheint ein klarer Logeintrag mit Pfad und Ursache.

## Risiken und Gegenmassnahmen

- Risiko: Fotoimport wird versehentlich API-Key-only aktiviert.
  Gegenmassnahme: Fotoimport prueft Vision-Service-Account getrennt und wird gezielt getestet.

- Risiko: Secrets landen in Logs.
  Gegenmassnahme: API-Key nur als vorhanden/nicht vorhanden und Quelle protokollieren; Tests oder Code-Review auf Logparameter fokussieren.

- Risiko: Interface-Aenderungen brechen Plugin-Projekte.
  Gegenmassnahme: `IGeminiClient` moeglichst klein halten und betroffene Plugin-Projekte mitbauen.

- Risiko: Google-Credential-Datei existiert, ist aber ungueltig oder nicht lesbar.
  Gegenmassnahme: Existenzpruefung fuer Aktivierung beibehalten, Initialisierungsfehler in `GeminiClient` klar loggen; Vision-Initialisierungsfehler im Fotoimport nicht verschlucken.

## Nicht-Ziele

- Keine Umstellung auf neue Google- oder Gemini-SDKs.
- Keine echte Google-Netzwerkkommunikation in Unit-Tests.
- Keine Ausgabe von Secret-Inhalten oder teilmaskierten API-Keys.
- Keine Entfernung des bestehenden `GoogleCredentials`-Konfigurationsfallbacks.

## Offene Punkte

Keine offenen Punkte fuer die Umsetzung. Die produktive Fehlermeldung ist unbekannt, blockiert die Umsetzung aber nicht, weil Ursache und Diagnosepfade im Code nachvollziehbar eingegrenzt sind.
