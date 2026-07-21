# Security F-08: Google-Credential-Dateien nicht in Build-Ausgaben kopieren

**Aufgaben-ID:** 51968a48-d444-4229-8088-80f62cc4aa0d  
**Branch:** task/issue-64-51968a48d4444229808880f62cc4aa0d-security-f-08-google-credentia  
**Erstellt:** 2026-07-21

## Fachliche Zusammenfassung

Das Projekt `Rezepte.Web` kopiert die Dateien `google.application-credentials.json` und `google.gemini.api-key.json` derzeit mit `CopyToOutputDirectory=Always` in alle Build-Ausgaben. Dies stellt ein erhebliches Sicherheitsrisiko dar, da sensitive Credentials in Entwickler- und Deployment-Umgebungen, Build-Artefakten und möglicherweise öffentlich zugänglichen Repositories exponiert werden könnten. Die Anforderung verlangt, diese Dateien aus der automatischen Copy-Konfiguration zu entfernen und stattdessen über sichere Mechanismen (Environment-Variablen, Secret Store, sichere Deployment-Prozesse) bereitzustellen.

## Betroffene Klassen und Komponenten

### Build & Deployment
- **Rezepte.Web.csproj** – MSBuild-Projektdatei mit problematischer `Content`-Konfiguration (Zeilen 25–30)
  - `<Content Update="google.application-credentials.json">` mit `<CopyToOutputDirectory>Always</CopyToOutputDirectory>`
  - `<Content Update="google.gemini.api-key.json">` mit `<CopyToOutputDirectory>Always</CopyToOutputDirectory>`

### Konfiguration & Dokumentation
- **appsettings.json** / **appsettings.Development.json** – ggf. Anpassungen oder Dokumentation zur Credential-Verwaltung
- **Dokumentation** – Neue oder aktualisierte Dokumentation zum sicheren Umgang mit Google-Credentials

### Laufzeit (ggf. betroffene Klassen, die auf Google-Credentials zugreifen)
- Klassen, die `Google.Cloud.Vision.V1` (bereits in Abhängigkeiten enthalten) oder das Gemini-API verwenden – müssen überprüft werden, ob sie die Credentials über Umgebungsvariablen oder Code beziehen (nicht aus Dateisystem)

## Implementierungsansatz

### 1. MSBuild-Konfiguration bereinigen
- Entfernen oder Ändern der `<ItemGroup>` (Zeilen 24–31) in `Rezepte.Web.csproj`, die `google.application-credentials.json` und `google.gemini.api-key.json` mit `CopyToOutputDirectory=Always` konfiguriert.
- **Option A (bevorzugt):** Komplett entfernen, wenn die Dateien nicht mehr benötigt werden.
- **Option B:** Mit `<CopyToOutputDirectory>Never</CopyToOutputDirectory>` und `<CopyToPublishDirectory>Never</CopyToPublishDirectory>` ersetzen, falls die Dateien für lokale Tests oder Debug-Szenarien vorhanden sein können (aber nicht kopiert werden sollen).

### 2. Credential-Bereitstellung über Environment-Variablen
- **Entwicklung (lokal):** Google-Credentials über Umgebungsvariablen bereitstellen (z. B. `GOOGLE_APPLICATION_CREDENTIALS` auf den Pfad zur lokalen Datei, `GOOGLE_GEMINI_API_KEY` für API-Keys).
- **Testing:** Secrets über Test-Fixture oder Mock-Objekte bereitstellen (nicht über Dateisystem).
- **Production:** Secrets über den Secret Store / Deployment-Konfiguration (z. B. Kubernetes Secrets, HashiCorp Vault, Cloud-Provider-Secrets) einspeisen.

### 3. Code-Audit durchführen
- Überprüfen, welche Klassen Google-Credentials laden/verwenden.
- Sicherstellen, dass die Google Cloud-Bibliotheken standardmäßig über `GOOGLE_APPLICATION_CREDENTIALS` Umgebungsvariable die Credentials laden.
- Falls hardcodierte Pfade oder File-based Credential-Loading vorhanden sind, auf Environment-Variable oder Konfiguration umstellen.

### 4. Git-Historie prüfen
- Prüfen, ob die Dateien jemals in die Git-Historie eingecheckt wurden (`git log --all --full-history -- google.application-credentials.json` / `google.gemini.api-key.json`).
- Falls ja, entsprechende Bereinigungs-Maßnahmen einleiten (History-Rewrite, Credential-Rotation).

### 5. Artefakt-Retention & Cleanup
- Überprüfen, dass Build-Artefakte (auch alte) keine Credential-Dateien enthalten.
- CI/CD-Konfiguration überprüfen: Werden Secrets versehentlich in Logs oder Artefakten ausgegeben?

### 6. Dokumentation
- Hinzufügen/Aktualisierung einer Dokumentation (z. B. in `docs/development-guide.md` oder `docs/deployment-guide.md`):
  - Wie Google-Credentials lokal konfiguriert werden
  - Umgebungsvariablen für Entwicklung
  - Best Practices für Deployment & Production

## Konfiguration

### Ebenen der Credential-Verwaltung

1. **Lokale Entwicklung:**
   - Umgebungsvariablen in `.env` oder IDE-Einstellungen
   - Oder: Lokale Datei außerhalb des Repositories, auf die `GOOGLE_APPLICATION_CREDENTIALS` zeigt
   - **Nicht:** Dateien im Projekt mit `CopyToOutputDirectory`

2. **Testing / CI-Pipeline:**
   - Mock-Objekte / Test-Fixtures mit Dummy-Credentials
   - Oder: Begrenzte Test-Credentials über GitHub Secrets / CI-Environment-Variablen
   - **Nicht:** Echte Production-Credentials

3. **Production / Staging:**
   - Secret Store (z. B. Kubernetes Secrets, Vault, AWS Secrets Manager, Azure Key Vault)
   - Environment-Variablen zur Laufzeit injiziert vom Deployment-System
   - **Nicht:** Fest eingecheckte Dateien

### MSBuild-Konfiguration (nach der Änderung)

```xml
<!-- BEFORE (unsicher) -->
<ItemGroup>
  <Content Update="google.application-credentials.json">
    <CopyToOutputDirectory>Always</CopyToOutputDirectory>
  </Content>
  <Content Update="google.gemini.api-key.json">
    <CopyToOutputDirectory>Always</CopyToOutputDirectory>
  </Content>
</ItemGroup>

<!-- AFTER (sicher) – Option A: Komplett entfernen -->
<!-- (Diese ItemGroup kann komplett gelöscht werden) -->

<!-- AFTER (sicher) – Option B: Explizit "Never" setzen für bessere Lesbarkeit -->
<ItemGroup>
  <Content Update="google.application-credentials.json">
    <CopyToOutputDirectory>Never</CopyToOutputDirectory>
    <CopyToPublishDirectory>Never</CopyToPublishDirectory>
  </Content>
  <Content Update="google.gemini.api-key.json">
    <CopyToOutputDirectory>Never</CopyToOutputDirectory>
    <CopyToPublishDirectory>Never</CopyToPublishDirectory>
  </Content>
</ItemGroup>
```

## Akzeptanzkriterien

1. **Secrets-Bezug über sichere Mechanismen:**
   - `Rezepte.Web.csproj` konfiguriert `google.application-credentials.json` und `google.gemini.api-key.json` **nicht** mit `CopyToOutputDirectory=Always`
   - Dokumentation erklärt, wie Google-Credentials über Umgebungsvariablen oder Secret Store bereitgestellt werden

2. **Build-/Publish-Artefakte enthalten keine Credential-Dateien:**
   - `dotnet publish` erzeugt kein Verzeichnis mit `google.application-credentials.json` oder `google.gemini.api-key.json`
   - `dotnet build` erzeugt keine dieser Dateien im Output-Verzeichnis
   - Test: `ls -la bin/Debug/net10.0/ | grep -i google` sollte **nicht** auf Credential-Dateien finden

3. **Existenz, Git-Historie und Artefakt-Retention überprüft:**
   - `git log --all --full-history -- "Rezepte.Web/google.application-credentials.json"` überprüft
   - Falls Dateien in der Geschichte vorhanden waren: Bereinigungs-Maßnahmen durchgeführt (z. B. BFG Repo-Cleaner oder git-filter-branch)
   - Überprüfung: Sind alte Build-Artefakte noch vorhanden? Falls ja, cleanup
   - GitHub-Workflows überprüft: Werden Secrets oder sensitive Dateien in Logs/Artifacts ausgegeben?

4. **Dokumentation:**
   - `docs/development-guide.md` (neu oder aktualisiert) beschreibt:
     - Wie Google-Credentials lokal für Entwicklung eingerichtet werden
     - Welche Umgebungsvariablen verwendet werden
     - Warum Credentials nicht in den Repo gehören
   - ggf. `docs/deployment-guide.md` beschreibt Production-Setup mit Secret Store

5. **Code-Audit:**
   - Überprüfung, dass Code nicht versucht, Credential-Dateien hart aus dem FileSystem zu laden
   - Google Cloud-Bibliotheken verwenden standardmäßig `GOOGLE_APPLICATION_CREDENTIALS` – dies ist dokumentiert oder konfiguriert

## Offene Fragen / Annahmen

1. **Wie werden Google-Credentials derzeit in der lokalen Entwicklung verwendet?**
   - Annahme: Die Dateien `google.application-credentials.json` und `google.gemini.api-key.json` sind nicht im Arbeitsbaum vorhanden (Befund bestätigt dies), daher ist die `CopyToOutputDirectory=Always` Konfiguration potenziell "tote" Konfiguration – aber sicherheitskritisch, wenn die Dateien jemals hinzugefügt würden.
   - Klärung erforderlich: Sollen die Dateien für lokales Testen vorhanden sein (dann über `.gitignore` ausgeschlossen und per env-var geladen)?

2. **Welche Google-APIs werden verwendet?**
   - `Google.Cloud.Vision.V1` ist als Package-Reference vorhanden – wird die Gemini-API auch verwendet?
   - Klärung erforderlich: Welche Code-Stellen laden/verwenden diese Credentials?

3. **Secret Store in Production:**
   - Annahme: Es existiert ein Deployment-Prozess, der Secrets bereitstellt (z. B. via Container-Secrets, Kubernetes, Cloud-Provider).
   - Klärung erforderlich: Wie sieht der aktuelle Deployment-Setup aus?

4. **CI/CD-Konfiguration:**
   - Überprüfung erforderlich: Werden in GitHub Actions oder anderen CI-Systemen jemals echte Google-Credentials verwendet?
   - Falls ja: Diese aus der CI-Konfiguration in die Secret-Management-Lösung des CI-Systems verschieben.

5. **Backup & Artefakt-Cleanup:**
   - Annahme: Falls die Dateien jemals in die Build-Ausgaben kopiert wurden (durch die `Always`-Konfiguration), könnten sie noch in älteren Build-Artefakten vorhanden sein.
   - Maßnahme erforderlich: Überprüfung und ggf. Cleanup durchführen.

---

## Zusammenfassung der Änderungen

| Aspekt | Aktueller Zustand | Zielzustand |
|--------|-------------------|------------|
| **MSBuild Config** | `CopyToOutputDirectory=Always` | `Never` oder entfernt |
| **Build Output** | Credential-Dateien könnten kopiert werden | Niemals Credential-Dateien in Output |
| **Secrets-Bezug** | Dateibasiert (risikobehaftet) | Environment-Variablen / Secret Store |
| **Dokumentation** | Keine Anleitung vorhanden | Anleitung für sicheren Credential-Umgang |
| **Git-Historie** | Ggf. Dateien in History | Bereinigt (falls notwendig) |
