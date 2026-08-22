# Entwickler-Leitfaden: Google-Credentials

Diese Anleitung beschreibt, wie Google-Credentials (Service Account und Gemini API-Key) für die lokale Entwicklung bereitgestellt werden.

## Warum die Credentials nicht im Repository liegen

`Rezepte.Web` liest Google-Credentials nicht mehr aus Dateien im Projekt- oder Build-Ausgabeverzeichnis. Frühere Versionen kopierten `google.application-credentials.json` und `google.gemini.api-key.json` per `CopyToOutputDirectory=Always` in jede Build-Ausgabe. Das haette sensible Secrets in Entwickler- und Deployment-Umgebungen sowie in Build-Artefakten exponiert. Stattdessen werden die Credentials zur Laufzeit über Umgebungsvariablen oder Konfiguration (Options-Pattern) bezogen; es gibt keine Datei mehr, die versehentlich eingecheckt werden koennte.

## Vorrangregel

`GoogleCredentialsProvider` loest Credentials in dieser Reihenfolge auf:

1. Umgebungsvariable (`GOOGLE_APPLICATION_CREDENTIALS` bzw. `GOOGLE_GEMINI_API_KEY`)
2. Konfiguration `GoogleCredentials:ServiceAccountFilePath` bzw. `GoogleCredentials:GeminiApiKey` (Fallback)

Die Umgebungsvariable hat Vorrang, weil Production Secrets typischerweise per Umgebungsvariable aus einem Secret Store injiziert. Konfiguration bzw. .NET User Secrets dienen als lokaler/Test-Default.

## Lokale Einrichtung über Umgebungsvariablen

Service Account (Pfad zu einer lokalen JSON-Datei ausserhalb des Repositories):

```powershell
$env:GOOGLE_APPLICATION_CREDENTIALS = "C:\secrets\google.application-credentials.json"
```

```bash
export GOOGLE_APPLICATION_CREDENTIALS=/home/user/secrets/google.application-credentials.json
```

Gemini API-Key:

```powershell
$env:GOOGLE_GEMINI_API_KEY = "AIza..."
```

```bash
export GOOGLE_GEMINI_API_KEY=AIza...
```

## Lokale Einrichtung über .NET User Secrets

Alternativ können die Werte über die Konfigurationssektion `GoogleCredentials` gesetzt werden, zum Beispiel per .NET User Secrets:

```powershell
dotnet user-secrets set "GoogleCredentials:ServiceAccountFilePath" "C:\secrets\google.application-credentials.json" --project Rezepte.Web
dotnet user-secrets set "GoogleCredentials:GeminiApiKey" "AIza..." --project Rezepte.Web
```

Diese Werte werden nur verwendet, wenn die entsprechende Umgebungsvariable nicht gesetzt ist.

## Code-Audit

Kein Code laedt Credential-Dateien mehr aus einem festen Pfad im Dateisystem. `GoogleCredentialsProvider` liest ausschliesslich Umgebungsvariablen bzw. Konfiguration; die Google-Bibliotheken (`GoogleCredential.FromFile`) verwenden dabei intern die von den .NET-Google-Client-Bibliotheken erwartete Standard-Umgebungsvariable `GOOGLE_APPLICATION_CREDENTIALS`. `GoogleQuotaClient` erhält den Service-Account-Pfad als Konstruktorparameter vom Aufrufer und laedt ihn nicht selbst aus einem festen Pfad; er wird aktuell im Code nirgends instanziiert.

## Testing / CI

Unit- und Integrationstests verwenden Test-Fixtures bzw. Mock-Objekte (z. B. `TestGeminiClient`) und keine echten Credential-Dateien. In der CI-Pipeline werden aktuell keine echten Google-Credentials verwendet.

## Pre-Commit-Hook

Ein Git-Pre-Commit-Hook liegt unter `.githooks/pre-commit`. Er prüft vor jedem Commit, ob die Solution formatiert ist (`dotnet format Rezepte.sln --verify-no-changes --no-restore`). Ein Commit wird abgelehnt, wenn die Formatierung nicht passt. Fuehrt einmalig aus:

```powershell
git config core.hooksPath .githooks
```

Wenn der Hook abbricht, repariert die Formatierung mit:

```powershell
dotnet format Rezepte.sln
```
