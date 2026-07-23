# Bestandsaufnahme - Contract-Export fuer Import-Plugins

Erstellt am: 2026-07-23  
Branch: `task/issue-80-ebe2129d623849788cfa525b51963bb2-contract-export-fuer-import-pl`

## Zusammenfassung

Der Checkout enthaelt eine gemeinsame Vertragsschicht unter
`Rezepte.Import.Abstractions`. Sie wird von den drei im Hauptrepository
liegenden Plugins, den Tests und der Web-Anwendung als Projektverweis genutzt.
Ein eigenstaendiges `Rezepte.Import.PluginSdk` ist im Checkout nicht vorhanden.

Der aktuelle Build erzeugt kein isoliertes Contract-Exportartefakt. Die
Release-Pipeline erstellt ausschliesslich ein Web-Publish-ZIP. Es gibt weder
ein Exportskript noch ein Manifestformat, Dateihashes, einen veroeffentlichten
ZIP-SHA-256-Hash oder ApiCompat-Baseline-Assemblies fuer den Plugin-Vertrag.

Damit ist die Anforderung noch nicht durch bestehende Infrastruktur abgedeckt;
die Bestandsaufnahme dient als Grundlage fuer die Planung der neuen Export- und
Validierungsgrenzen.

## Relevante Detaildokumente

- [Repository-Struktur und Vertragsdateien](inventory/repository-structure.md)
- [Vertragssurface und Abhaengigkeiten](inventory/contract-surface.md)
- [Build-, Release- und Exportinfrastruktur](inventory/build-release.md)
- [Plugin-Host, Import und bestehende Paketsicherheit](inventory/plugin-host.md)
- [Tests, Dokumentation und offene Risiken](inventory/verification-risks.md)

## Bestehende Artefakte und Luecken

| Bereich | Bestand | Relevanz fuer die Anforderung |
|---|---|---|
| Vertragsprojekt | `Rezepte.Import.Abstractions/Rezepte.Import.Abstractions.csproj` und die zugehoerigen C#-Dateien | Muss in den Quell-Export aufgenommen und ausserhalb des Checkouts baubar sein. |
| Plugin-SDK | Nicht vorhanden | Der im privaten Plugin-Repository dokumentierte SDK-Vertrag kann aus diesem Checkout nicht exportiert werden. Eigentum und Quelle muessen in der Planung geklaert werden. |
| Gemeinsame Builddatei | Keine `Directory.Build.props` gefunden | Der geforderte Mindestumfang kann aktuell nicht unveraendert gesammelt werden. |
| Exportprozess | Kein `scripts`-Exportskript und kein Workflow-Schritt fuer Contract-Exports | Neuer reproduzierbarer Prozess mit Fail-fast-Pruefungen erforderlich. |
| Manifest | Kein `contract-export.json` im Repository | Muss beim Export erzeugt und aus dem Datei-Hash-Verzeichnis ausgeschlossen werden. |
| Baselines | Keine ApiCompat-Referenzassemblies gefunden | Muss als Teil des Exports oder als zugehoeriges Baseline-Artefakt gebaut werden. |
| Release | `.github/workflows/release.yml` publisht nur `Rezepte.Web` | Exportartefakt und ZIP-Hash muessen separat veroeffentlicht und dokumentiert werden. |
| Import-Update | Host kann GitHub-Plugin-ZIPs beim Start laden | Das ist kein Contract-Import und ersetzt nicht den manuellen, hashgebundenen Plugin-Repository-Import. |

## Betroffene Systemgrenzen

1. Hauptrepository: fuehrende Quelle fuer die freigegebenen Vertragsdateien.
2. Export-Build: sammelt, validiert, hasht und paketiert den Vertrag.
3. Veroeffentlichung: stellt ZIP, ZIP-SHA-256, Version, Commit und URL ohne
   Credentials bereit.
4. Plugin-Repository: importiert spaeter manuell anhand URL und erwarteter
   ZIP-Pruefsumme und fuehrt Manifest-/ApiCompat-Pruefungen aus.

Die ersten drei Grenzen sind im Checkout teilweise vorhanden; die vierte ist
nur als Zielvertrag beschrieben und liegt nicht als Quellcode vor.

