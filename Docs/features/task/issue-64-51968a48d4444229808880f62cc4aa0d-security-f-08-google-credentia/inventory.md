# Bestandsaufnahme: Security F-08 - Google-Credential-Dateien nicht in Build-Ausgaben kopieren

Diese Bestandsaufnahme analysiert den Projektcode bezogen auf die Sicherheitsanforderung zur Entfernung von Google-Credentials aus der MSBuild-Konfiguration. Sie dokumentiert die aktuelle Credential-Verwaltung im Projekt `Rezepte.Web`.

## Zusammenfassung

### Kritische Befunde

1. **MSBuild-Konfigurationsproblem (SICHERHEITSKRITISCH)**
   - `Rezepte.Web.csproj` enthält ItemGroup mit `CopyToOutputDirectory=Always` für zwei Google-Credential-Dateien (Zeilen 24-31)
   - Dateien sind derzeit **nicht im Arbeitsbaum vorhanden**, Konfiguration ist "tote" Konfiguration
   - Aber: Falls Dateien zukünftig hinzugefügt werden, würden sie in alle Build-Ausgaben kopiert (erhebliches Sicherheitsrisiko)

2. **Credential-Verwaltung (SICHERHEITSKRITISCH)**
   - Credentials werden derzeit aus Dateien im Arbeitsverzeichnis geladen (`AppContext.BaseDirectory`)
   - `GoogleCredentialsProvider` setzt Umgebungsvariable `GOOGLE_APPLICATION_CREDENTIALS` wenn Datei existiert
   - Keine Nutzung von .gitignore zur expliziten Ausschließung der Credential-Dateien
   - Keine Konfiguration über `appsettings.json` oder Umgebungsvariablen

3. **Git-Historie (POSITIV)**
   - Credential-Dateien wurden **nie in Git eingecheckt**
   - `git log --all --full-history` zeigt keine Commits für diese Dateien
   - Keine Bereinigungs-Maßnahmen erforderlich

4. **Verwendung in Code**
   - `IGoogleCredentialsProvider` wird von mehreren Klassen verwendet: `GeminiClient`, `BaseAIImportHandler`, `SettingsController`
   - `GoogleQuotaClient` nimmt Service-Account-Pfad direkt als Parameter an
   - Google Cloud Vision API (Paket `Google.Cloud.Vision.V1` v3.8.0) ist registriert, wird aber nicht direkt im Code verwendet

5. **Test-Abdeckung (MANGELHAFT)**
   - Test-Mock `TestGeminiClient` vorhanden, aber keine expliziten Unit-Tests für Credential-Loading
   - `GoogleCredentialsProvider` ist nicht direkt getestet
   - `GoogleQuotaClient` ist nicht getestet

6. **Dokumentation (FEHLEND)**
   - Keine Dokumentation zur sicheren Credential-Verwaltung
   - `appsettings.json` und `appsettings.Development.json` enthalten keine Credential-Konfiguration

### Wo werden Credentials benötigt?

| Komponente | Pfad | Verwendung |
|-----------|------|-----------|
| GoogleCredentialsProvider | Services/GoogleCredentialsProvider.cs | Lädt google.application-credentials.json und google.gemini.api-key.json |
| GeminiClient | Services/Import/GeminiClient.cs | Rezeptextraktion via Gemini API |
| GoogleQuotaClient | Services/Import/GoogleQuotaClient.cs | Quota-Abfragen via Google Service Usage API |
| BaseAIImportHandler | Services/Import/BaseAIImportHandler.cs | Prüft Credential-Verfügbarkeit via GeminiClient |
| SettingsController | Controllers/SettingsController.cs | Gibt Credential-Verfügbarkeit in API-Response zurück |

### Dateien und Status

| Datei | Status | Git-Historie | Auswirkung |
|-------|--------|--------------|-----------|
| google.application-credentials.json | Nicht vorhanden | Nie eingecheckt | CopyToOutputDirectory=Always würde kopieren falls vorhanden |
| google.gemini.api-key.json | Nicht vorhanden | Nie eingecheckt | CopyToOutputDirectory=Always würde kopieren falls vorhanden |
| .gitignore | Existiert | — | Credential-Dateien sind nicht explizit in .gitignore |

## Details

- [MSBuild-Konfiguration](inventory/msbuild-config.md)
- [Interfaces](inventory/interfaces.md)
- [Services & Logik](inventory/services.md)
- [Datenmodelle & Konfiguration](inventory/models.md)
- [Tests & Test-Hilfsmittel](inventory/tests.md)

## Zusammenhang mit Anforderung

### Was die Anforderung verlangt

1. **MSBuild bereinigen:** Entfernen oder Ändern der `<ItemGroup>` mit `CopyToOutputDirectory=Always`
2. **Credential-Bereitstellung sichern:** Über Umgebungsvariablen statt Dateisystem
3. **Code-Audit:** Keine hardcodierten Datei-Pfade
4. **Git-Historie prüfen:** ✓ Bereits überprüft, keine Credential-Dateien gefunden
5. **Artefakt-Cleanup:** Build-Artefakte überprüfen
6. **Dokumentation:** Hinzufügen zur Credential-Verwaltung

### Was bereits existiert und sicher ist

- ✓ Credentials sind nicht im Arbeitsbaum
- ✓ Credentials wurden nie in Git eingecheckt
- ✓ `GoogleCredentialsProvider` versucht bereits, `GOOGLE_APPLICATION_CREDENTIALS` zu setzen
- ✓ Code lädt Credentials nicht hart aus bekannten Pfaden

### Was noch zu implementieren ist

- ✗ MSBuild ItemGroup mit `CopyToOutputDirectory=Always` entfernen
- ✗ Explizite Konfiguration in .gitignore für Credential-Dateien
- ✗ Dokumentation zur sicheren lokalen Entwicklung mit Umgebungsvariablen
- ✗ Evtl. Updates der Konfiguration für Production/Deployment
- ✗ Tests für `GoogleCredentialsProvider` und `GoogleQuotaClient`
