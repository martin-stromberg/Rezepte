# Build-, Release- und Exportinfrastruktur

## Pull-Request-Workflow

`.github/workflows/pr.yml` stellt .NET 10 bereit und fuehrt Restore, Release-
Build, Tests und `dotnet format --verify-no-changes` fuer `Rezepte.sln` aus.
Es gibt keinen Schritt, der Vertragsdateien sammelt, ein Manifest erzeugt,
ein Contract-ZIP baut oder dessen Inhalt validiert.

## Release-Workflow

`.github/workflows/release.yml` wird nach einem Merge in `main` ausgefuehrt.
Er bestimmt eine Git-Tag-Version, baut und testet die Anwendung, publisht
`Rezepte.Web` fuer `net10.0/linux-x64`, schreibt `release-metadata.json` und
packt das Publish-Verzeichnis als `artifacts/release.zip`.

Das Release-ZIP ist ein Laufzeit-/Webartefakt. Es ist weder auf die im Vertrag
genannten repositoryrelativen Quellpfade beschraenkt noch enthaelt es ein
`contract-export.json` oder ApiCompat-Baselines. Der Workflow gibt ausserhalb
des ZIPs keinen Contract-SHA-256 aus.

## Hostseitiger Plugin-Build

`Rezepte.Web/Rezepte.Web.csproj` baut die drei produktiven Plugins vor Build
und Publish und kopiert deren Ergebnisse nach `plugins/<Projektname>/`. Externe
Pluginartefakte werden optional aus
`external/rezepte-import-plugins-private/artifacts/plugins` uebernommen.

Diese Targets sind auf die Hostausgabe ausgerichtet. Sie definieren keinen
isolierten Contract-Workspace und keine Whitelist fuer exportierte Dateien.

## Fehlende Exportbausteine

- keine feste Contract-Version in MSBuild oder einer zentralen Konfiguration;
- kein Exportskript oder Buildtarget mit Pfad-Whitelist;
- keine Pruefung auf fehlende oder unerwartete Vertragsdateien;
- keine deterministische ZIP-Erstellung dokumentiert;
- keine Hashberechnung fuer einzelne Dateien oder das Gesamt-ZIP;
- kein Manifest mit `exportFormat`, `contractVersion`, `sourceCommit` und
  vollstaendiger Dateiliste;
- kein Bau und keine Ablage der zwei ApiCompat-Baseline-Assemblies;
- keine oeffentliche Artefakt-URL oder Release-Dokumentation fuer Plugin-
  Repository-Importe.

## Reproduzierbarkeit

Der Release-Workflow verwendet `zip -r` ohne dokumentierte Sortierung,
Zeitstempel-Normalisierung oder feste Dateirechte. Fuer den Contract-Export
braucht es deshalb eine gesonderte deterministische Packlogik, damit gleiche
Quellen und gleicher Commit denselben ZIP-Inhalt und nachvollziehbare Hashes
ergeben.

