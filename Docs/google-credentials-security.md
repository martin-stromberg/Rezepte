# Sichere Verwaltung von Google-Credentials

Dieses Dokument beschreibt, wie Google-Credentials (Service Account und Gemini API-Key) in dieser Anwendung sicher verwaltet werden.

## Sicherheitsrichtlinie

Google-Credentials werden **nicht** als Datei im Projekt abgelegt und **nicht** in Build-Ausgaben kopiert. Sie werden zur Laufzeit über Umgebungsvariablen oder Konfiguration bereitgestellt.

- **Dateien im Projekt:** `google.application-credentials.json` und `google.gemini.api-key.json` werden nicht eingecheckt (siehe `.gitignore`)
- **Build-Ausgaben:** `dotnet build` und `dotnet publish` enthalten diese Dateien nicht
- **Laufzeit-Zugriff:** `GoogleCredentialsProvider` liest die Credentials über Umgebungsvariablen oder Konfiguration

## Für Entwickler

→ [Entwickler-Leitfaden: Google-Credentials](development-guide.md)

Beschreibt:
- Lokale Einrichtung über Umgebungsvariablen
- Verwendung von .NET User Secrets
- Vorrangregel (Umgebungsvariable vor Konfiguration)

## Für Administratoren / Deployment

→ [Deployment-Leitfaden: Google-Credentials](deployment-guide.md)

Beschreibt:
- Bereitstellung über Secret Store (Kubernetes Secrets, Vault, Cloud-Provider)
- Unterstützte Gemini-Authentifizierungswege (API-Key, Service Account)
- systemd-Integration für manuelle Linux-Deployment

## Technische Details

### Konfiguration

Die Anwendung erwartet die folgende Konfigurationsstruktur in `appsettings.json`:

```json
{
  "GoogleCredentials": {
    "ServiceAccountFilePath": "",
    "GeminiApiKey": ""
  }
}
```

Diese Werte sind nur Fallback und sollten nicht mit echten Secrets gefüllt werden.

### Umgebungsvariablen (Vorrang)

| Variable | Zweck |
|----------|-------|
| `GOOGLE_APPLICATION_CREDENTIALS` | Pfad zu einer Service-Account-JSON-Datei (außerhalb des Repositories) |
| `GOOGLE_GEMINI_API_KEY` | Gemini API-Key |

### Implementation

- **Provider-Klasse:** `GoogleCredentialsProvider` (Namespace: `Rezepte.Web.Services`)
- **Options-Klasse:** `GoogleCredentialsOptions` (Namespace: `Rezepte.Web.Configuration`)
- **Interface:** `IGoogleCredentialsProvider`

Der Provider wird als Singleton registriert und von Diensten wie `GeminiClient` und `SettingsController` verwendet.

### Tests

Folgende Testklassen prüfen die sichere Verwaltung:

- `GoogleCredentialsProviderTests` — Unit-Tests für Auflösungslogik
- `SettingsCredentialAvailabilityTests` — Integrationstests für API-Verhalten
- `CsprojCredentialCopyTests` — Regressionsschutz gegen unsichere MSBuild-Konfiguration

## Git-Historie

Die Dateien `google.application-credentials.json` und `google.gemini.api-key.json` wurden niemals in das Repository eingecheckt (geprüft mit `git log --all --full-history`). Wenn Sie ein lokales Backup haben, das diese Dateien enthält: Bitte sicher verwahren und niemals hochladen.

## Migrationsleitfaden

Falls Sie ein älteres Setup haben, das die Credential-Dateien im Projekt abgelegt hatte:

1. Löschen Sie die lokalen Dateien `Rezepte.Web/google.application-credentials.json` und `Rezepte.Web/google.gemini.api-key.json`
2. Setzen Sie die Umgebungsvariablen `GOOGLE_APPLICATION_CREDENTIALS` und/oder `GOOGLE_GEMINI_API_KEY` (siehe [Entwickler-Leitfaden](development-guide.md))
3. Prüfen Sie mit `dotnet run` oder `dotnet build`, dass die Anwendung startet und die Credentials erkannt werden (im Settings-Endpunkt `/api/settings` nachprüfen oder Admin-UI öffnen)
