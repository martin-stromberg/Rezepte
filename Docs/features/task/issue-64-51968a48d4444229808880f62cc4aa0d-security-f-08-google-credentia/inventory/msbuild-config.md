# MSBuild-Konfiguration

## Rezepte.Web.csproj

Datei: `Rezepte.Web/Rezepte.Web.csproj`

### Problematische ItemGroup (Zeilen 24-31)

```xml
<ItemGroup>
  <Content Update="google.application-credentials.json">
    <CopyToOutputDirectory>Always</CopyToOutputDirectory>
  </Content>
  <Content Update="google.gemini.api-key.json">
    <CopyToOutputDirectory>Always</CopyToOutputDirectory>
  </Content>
</ItemGroup>
```

**Status:** SICHERHEITSKRITISCH – Diese Konfiguration würde Credential-Dateien automatisch in alle Build-Ausgaben kopieren, sollten die Dateien im Projektverzeichnis vorhanden sein.

**Probleme:**
- `CopyToOutputDirectory=Always` bedeutet, dass die Dateien in `bin/Debug/` und `bin/Release/` kopiert werden
- `CopyToPublishDirectory` ist nicht explizit gesetzt, würde aber auch bei Publish mitgegeben
- Sensible Credentials könnten so in Build-Artefakte gelangen

**Aktuelle Situation:**
- Die Dateien `google.application-credentials.json` und `google.gemini.api-key.json` sind **nicht** im Arbeitsbaum vorhanden
- Diese ItemGroup ist daher derzeit "tote" Konfiguration
- Jedoch stellt sie ein Sicherheitsrisiko dar, falls Dateien zukünftig hinzugefügt werden

### Weitere Content-Konfigurationen

Die Datei `test.recipe-import.json` wird mit `<CopyToPublishDirectory>Never</CopyToPublishDirectory>` konfiguriert (Zeilen 34-38).

Im Debug-Modus wird `test.recipe-import.json` mit `<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>` konfiguriert (Zeilen 41-45).

### Package-References

- `Google.Cloud.Vision.V1` Version 3.8.0 – ist registriert, wird aber nicht direkt im analysierten Code verwendet

## Dateistatus

- `google.application-credentials.json` – Nicht im Arbeitsbaum, nicht in .gitignore explizit erwähnt
- `google.gemini.api-key.json` – Nicht im Arbeitsbaum, nicht in .gitignore explizit erwähnt

## Git-Historie

```bash
git log --all --full-history -- "Rezepte.Web/google.application-credentials.json"
git log --all --full-history -- "Rezepte.Web/google.gemini.api-key.json"
```

**Ergebnis:** Keine Commits gefunden. Die Dateien wurden nie in die Git-Historie eingecheckt.
