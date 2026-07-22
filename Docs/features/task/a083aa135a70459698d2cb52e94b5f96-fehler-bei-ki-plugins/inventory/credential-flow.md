# Detail: Credential-Aufloesung und KI-Aktivierung

## GoogleCredentialsProvider

`Rezepte.Web/Services/GoogleCredentialsProvider.cs` loest Credentials in dieser Reihenfolge auf:

1. Umgebungsvariable
2. Optionswert aus `GoogleCredentials`
3. leerer String

Fuer Service-Accounts wird `GOOGLE_APPLICATION_CREDENTIALS` verwendet. Fuer Gemini wird `GOOGLE_GEMINI_API_KEY` verwendet.

Damit entspricht der Provider grundsaetzlich der Anforderung, dass systemd-Environment-Variablen verwendet werden koennen.

## GeminiClient

`Rezepte.Web/Services/Import/GeminiClient.cs` initialisiert die HTTP-Authentifizierung so:

1. Wenn `GOOGLE_GEMINI_API_KEY` bzw. der konfigurierte API-Key vorhanden ist, wird der Header `x-goog-api-key` gesetzt.
2. Sonst wird der Service-Account-Pfad gelesen.
3. Existiert die Datei, wird `GoogleCredential.FromFile(path)` mit Scope `https://www.googleapis.com/auth/generative-language` erzeugt.
4. Daraus wird ein Bearer-Token fuer den Request gesetzt.
5. Wenn nichts davon moeglich ist, wird `InvalidOperationException("No valid Gemini authentication configured.")` geworfen.

Der Client bevorzugt also korrekt den API-Key vor dem Service-Account.

## Kritische Aktivierungslogik

`Rezepte.Web/Services/Import/BaseAIImportHandler.cs` prueft in `IsActiveAsync()`:

- `geminiClient.HasServiceAccount()`
- globale KI-Aktivierung
- benutzerspezifische KI-Aktivierung

Dadurch wird ein KI-Handler deaktiviert, wenn kein Service-Account vorhanden ist. Ein vorhandener Gemini-API-Key reicht in dieser Basisklasse nicht aus.

`Rezepte.Import.Plugins.AIUrl/AIUrlImportHandler.cs` hat eine Sonderlogik:

- Wenn `HasApiKey()` true ist, wird `base.IsActiveAsync()` nicht benoetigt.
- Danach werden globale/userbezogene KI- und Gemini-Settings geprueft.

AI-URL kann daher mit API-Key-only funktionieren.

`Rezepte.Import.Plugins.AIFoto/AIFotoImportHandler.cs` ruft dagegen zuerst `base.IsActiveAsync()` auf. Dadurch ist Fotoimport nur aktiv, wenn ein Service-Account-Dateipfad existiert. Danach prueft der Handler zusaetzlich Google-Vision- und Gemini-Settings.

## Google Vision

Fotoimport verwendet `Google.Cloud.Vision.V1.ImageAnnotatorClient.Create()` direkt. Das nutzt die Standard-Google-Application-Credentials der Google-Clientbibliothek. Fuer diesen Pfad ist `GOOGLE_APPLICATION_CREDENTIALS` plausibel erforderlich.

Der Code prueft aktuell aber nur `File.Exists(path)`. Nicht lesbare Dateien, ungueltiges JSON, falsche Berechtigungen oder fehlende Google-Berechtigungen fallen erst beim Vision-Aufruf auf.

## Bewertung

Die reine Credential-Aufloesung ist groesstenteils vorhanden. Das Hauptproblem liegt in der Vermischung von "Gemini verfuegbar" und "Service-Account verfuegbar" in der KI-Basisklasse. Fuer URL-Importe wurde das bereits lokal korrigiert, fuer die gemeinsame Basisklasse und Fotoimporte nicht.
