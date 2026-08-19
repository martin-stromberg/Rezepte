# Deployment-Leitfaden: Google-Credentials

Diese Anleitung beschreibt, wie Google-Credentials fÃ¼r den Produktionsbetrieb von `Rezepte.Web` bereitgestellt werden.

## Grundsatz

Google-Credentials werden **nicht** als Datei im Deployment-Artefakt mitgeliefert. `Rezepte.Web.csproj` kopiert keine Credential-Dateien mehr in die Build- oder Publish-Ausgabe. Stattdessen liest `GoogleCredentialsProvider` die Credentials zur Laufzeit aus Umgebungsvariablen (bevorzugt) oder aus der Konfigurationssektion `GoogleCredentials` (Fallback).

Secrets mÃ¼ssen Ã¼ber den Secret Store bzw. die Deployment-Konfiguration des Zielsystems eingespeist werden, zum Beispiel:

- Kubernetes Secrets (als Umgebungsvariablen in den Pod gemountet)
- HashiCorp Vault (mit Injektion als Umgebungsvariable)
- Cloud-Provider-Secret-Manager (AWS Secrets Manager, Azure Key Vault, Google Secret Manager)
- Umgebungsvariablen der systemd-Unit bei manuellem Linux-Deployment (siehe `Docs/install.md`)

## Unterstuetzte Gemini-Authentifizierungswege

Beide Wege werden von `GoogleCredentialsProvider` und `GeminiClient` unterstuetzt und kÃ¶nnen in Production frei gewÃ¤hlt werden:

### Option 1: API-Key

```bash
export GOOGLE_GEMINI_API_KEY=AIza...
```

`GeminiClient` setzt bei vorhandenem Key den HTTP-Header `x-goog-api-key`.

### Option 2: Service Account

```bash
export GOOGLE_APPLICATION_CREDENTIALS=/etc/rezepte/secrets/google.application-credentials.json
```

Der Pfad muss auf eine Service-Account-JSON-Datei zeigen, die vom Deployment-System bereitgestellt wird (z. B. per Secret-Mount), aber ausserhalb des Anwendungsverzeichnisses liegt. `GeminiClient` erzeugt daraus ein Bearer-Token fÃ¼r die Gemini API.

**Vorrang:** Ist ein API-Key vorhanden, hat er Vorrang vor dem Service Account. Der Service Account wird nur verwendet, wenn kein API-Key konfiguriert ist.

## systemd-Beispiel

Ergaenzend zur Service-Definition in `Docs/install.md` kÃ¶nnen die Umgebungsvariablen in der systemd-Unit gesetzt werden:

```ini
[Service]
Environment=GOOGLE_GEMINI_API_KEY=AIza...
Environment=GOOGLE_APPLICATION_CREDENTIALS=/etc/rezepte/secrets/google.application-credentials.json
```

FÃ¼r URL-basierte KI-Importe reicht ein gÃ¼ltiger `GOOGLE_GEMINI_API_KEY`, sofern KI und Gemini in der Anwendung fÃ¼r den Benutzer aktiviert sind. Fotoimporte benÃ¶tigen zusÃ¤tzlich eine lesbare Service-Account-Datei fÃ¼r Google Vision. Setzen Sie deshalb fÃ¼r produktive Fotoimporte beide Variablen, wenn Gemini per API-Key betrieben wird.

Alternativ Ã¼ber eine separate, nicht eingecheckte EnvironmentFile:

```ini
[Service]
EnvironmentFile=/etc/rezepte/rezepte.env
```

Die Datei kann zum Beispiel enthalten:

```bash
GOOGLE_GEMINI_API_KEY=...
GOOGLE_APPLICATION_CREDENTIALS=/etc/rezepte/secrets/google.application-credentials.json
```

Der Pfad aus `GOOGLE_APPLICATION_CREDENTIALS` muss fÃ¼r den systemd-User lesbar sein, zum Beispiel fÃ¼r `User=www-data` aus `Docs/install.md`.

## Verhalten ohne Credentials

Sind weder Umgebungsvariable noch Konfiguration gesetzt, meldet der Settings-Endpunkt `GoogleServiceAccountFileAvailable = false` und `GeminiApiKeyAvailable = false`. Der KI-Import bleibt inaktiv, bis Credentials bereitgestellt werden. Es werden keine Fehler durch fehlende Dateien geworfen.

## Diagnose

Beim Start und bei der Nutzung der KI-Plugins protokolliert die Anwendung secret-freie Diagnosen:

- ob Gemini Ã¼ber API-Key oder Service Account initialisiert wird
- ob `GOOGLE_APPLICATION_CREDENTIALS` oder der Options-Fallback fÃ¼r den Service-Account-Pfad verwendet wird
- ob die Service-Account-Datei existiert
- warum ein KI-Handler inaktiv bleibt, zum Beispiel durch deaktivierte KI-/Gemini-/Vision-Schalter oder fehlende Credentials

API-Key-Werte und Token werden nicht geloggt. Bei falschem oder nicht lesbarem Service-Account-Pfad erscheinen Pfad, Quelle und Exception-Informationen in den Logs, damit Berechtigungs- und Konfigurationsfehler auf dem Server erkennbar sind.
