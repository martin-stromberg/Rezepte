# Contract-Export fuer Import-Plugins

## Metadaten

| Feld | Wert |
|------|------|
| Aufgaben-ID | `ebe2129d-6238-4978-8cfa-525b51963bb2` |
| Branch | `task/issue-80-ebe2129d623849788cfa525b51963bb2-contract-export-fuer-import-pl` |
| Erstellt | `2026-07-23` |

## Ziel

Das Hauptprojekt stellt den fuer Import-Plugins relevanten Vertrag als eigenstaendiges, versioniertes Exportartefakt bereit. Das Plugin-Repository kann dieses Artefakt manuell importieren, validieren und gegen unbeabsichtigte Drift pruefen, ohne auf den Quellcode oder Checkout des Hauptprojekts zugreifen zu muessen.

Das Hauptprojekt muss nicht oeffentlich sein. Oeffentlich beziehungsweise ohne Authentifizierung erreichbar muss nur das erzeugte Exportartefakt sein.

## Fachlicher Vertrag

Der Plugin-Vertrag umfasst die Quell- und Builddateien, gegen die externe Import-Plugins entwickelt und gebaut werden muessen. Das Hauptprojekt ist die fuehrende Quelle. Das Plugin-Repository uebernimmt ausschliesslich explizit freigegebene Exportstaende.

Ein normaler Build im Plugin-Repository darf keinen neuen Vertragsstand automatisch herunterladen. Vertragsupdates erfolgen ausschliesslich manuell ueber eine konkrete Artefakt-URL und den erwarteten SHA-256-Hash des vollstaendigen ZIP-Archivs.

## Exportartefakt

### Mindestumfang

Das ZIP muss mindestens folgende repositoryrelativen Inhalte enthalten:

- `contract-export.json` im ZIP-Wurzelverzeichnis
- `Directory.Build.props`
- alle Dateien unter `Rezepte.Import.Abstractions/`
- alle Dateien unter `Rezepte.Import.PluginSdk/`

Die exportierten Projekte muessen ausserhalb des Hauptprojekt-Checkouts isoliert baubar sein. Zusaetzlich benoetigte Builddateien sind mit zu exportieren oder die Projektdateien sind so anzupassen, dass sie ohne private Host-Dateien funktionieren.

### Ausschlussumfang

Das ZIP darf folgende Inhalte nicht enthalten:

- Zugangsdaten oder andere Geheimnisse
- private Host-Projektdateien ausserhalb des Plugin-Vertrags
- Host-spezifische Implementierungen
- Buildartefakte aus `bin/` oder `obj/`

### ZIP-Struktur und Pfade

Die Dateien muessen mit repositoryrelativen Pfaden abgelegt werden, zum Beispiel:

```text
contract-export.json
Directory.Build.props
Rezepte.Import.Abstractions/Rezepte.Import.Abstractions.csproj
Rezepte.Import.Abstractions/...
Rezepte.Import.PluginSdk/Rezepte.Import.PluginSdk.csproj
Rezepte.Import.PluginSdk/...
```

Jeder ZIP-Pfad muss relativ sein und darf weder Laufwerksbuchstaben, absolute Pfade noch `..`-Segmente enthalten.

## Manifest

Das ZIP muss im Wurzelverzeichnis `contract-export.json` enthalten. Die Datei muss mindestens folgende Struktur aufweisen:

```json
{
  "exportFormat": "rezepte-import-contract-v1",
  "contractVersion": "0.2.0",
  "sourceCommit": "0123456789abcdef0123456789abcdef01234567",
  "files": [
    {
      "path": "Directory.Build.props",
      "sha256": "..."
    }
  ]
}
```

| Feld | Anforderung |
|------|-------------|
| `exportFormat` | Festes Formatkennzeichen `rezepte-import-contract-v1`. |
| `contractVersion` | SemVer-Version des Plugin-Vertrags. |
| `sourceCommit` | Unveraenderlicher Commit-Hash oder vergleichbarer Build-Identifier des Hauptprojekts. |
| `files` | Vollstaendige Liste aller exportierten Vertragsdateien ausser `contract-export.json`, jeweils mit Pfad und SHA-256-Hash. |

Die Manifestdatei wird mitgeliefert, darf aber nicht in `files` aufgefuehrt werden. Der SHA-256-Hash des vollstaendigen ZIPs wird ausserhalb des ZIPs veroeffentlicht oder beim Import angegeben.

## Versionierung

`contractVersion` verwendet SemVer:

- Patch: kompatible Korrektur ohne oeffentliche API- oder ABI-Aenderung
- Minor: additive, rueckwaertskompatible Erweiterung der Plugin-API
- Major: inkompatible API- oder ABI-Aenderung

Ein Artefakt mit gleicher `contractVersion`, aber anderem ZIP-SHA-256, ist ein anderer Exportstand und darf nicht stillschweigend ersetzt werden. Der ZIP-SHA-256 ist der unveraenderliche technische Identifier des konkreten Exports.

## ApiCompat-Baselines

Zusaetzlich zum Quell-Export muessen Referenzassemblies fuer ApiCompat bereitgestellt werden:

- `Rezepte.Import.Abstractions.dll`
- `Rezepte.Import.PluginSdk.dll`

Die Assemblies muessen aus dem freigegebenen Host-Vertragsstand gebaut sein und als Baseline fuer `Microsoft.DotNet.ApiCompat.Tool` im Plugin-Repository dienen. Sie koennen in einem separaten Baseline-Verzeichnis desselben ZIPs oder als separates versioniertes Baseline-Artefakt bereitgestellt werden. Quellexport und Baseline muessen eindeutig demselben `contractVersion`- und `sourceCommit`-Stand zugeordnet sein.

## Exportprozess

Das Hauptprojekt muss einen reproduzierbaren Build- oder Release-Schritt fuer folgende Ablaufschritte bereitstellen:

1. Den festgelegten Vertragsumfang sammeln.
2. Die Datei-Hashes aller exportierten Dateien berechnen.
3. `contract-export.json` erzeugen.
4. Das ZIP mit stabiler Struktur erstellen.
5. Den SHA-256-Hash des vollstaendigen ZIPs ausgeben.
6. Die ApiCompat-Baseline-Assemblies bereitstellen.
7. Artefakt-URL, ZIP-SHA-256, `contractVersion` und `sourceCommit` fuer das Plugin-Repository dokumentieren.

Der Exportprozess muss bei fehlenden oder unerwarteten Vertragsdateien fehlschlagen.

## Importvertrag fuer das Plugin-Repository

Das Plugin-Repository muss den Export spaeter manuell ueber folgenden Aufruf aktualisieren koennen:

```powershell
./scripts/Update-ContractExport.ps1 `
  -ArtifactUrl <oeffentliche-export-zip-url> `
  -ArtifactSha256 <64-stelliger-zip-sha256>
```

Nach dem Aufruf prueft das Plugin-Repository mindestens:

- den SHA-256-Hash des ZIPs
- das Manifest
- die vollstaendige Dateiliste
- die Datei-Hashes
- den importierten Workspace-Stand

Nach Bereitstellung der Host-Baselines muss die CI-Pruefung auf harte ApiCompat-Validierung umgestellt werden koennen.

## Abnahmekriterien

- Ein oeffentlich erreichbares oder credential-frei abrufbares ZIP-Artefakt wird erzeugt.
- Das ZIP enthaelt `contract-export.json` und alle Vertragsdateien mit gueltigen relativen Pfaden.
- `contract-export.json` enthaelt das feste Exportformat, eine SemVer-`contractVersion`, einen unveraenderlichen `sourceCommit` sowie SHA-256-Hashes fuer alle exportierten Vertragsdateien.
- `contract-export.json` selbst ist nicht in `files` enthalten.
- Der SHA-256-Hash des vollstaendigen ZIPs wird veroeffentlicht oder eindeutig ausgegeben.
- Die exportierten Vertragsprojekte sind ausserhalb des Hauptprojekt-Checkouts baubar.
- Referenzassemblies fuer `Rezepte.Import.Abstractions` und `Rezepte.Import.PluginSdk` werden fuer ApiCompat bereitgestellt.
- Quellexport und Baseline sind demselben Vertragsstand eindeutig zugeordnet.
- Der Exportprozess ist reproduzierbar und in CI- oder Release-Pipelines ausfuehrbar.
- Der Prozess bricht bei fehlenden oder unerwarteten Vertragsdateien ab.
- Ein normaler Plugin-Repository-Build laedt keinen neuen Vertragsstand automatisch herunter.
