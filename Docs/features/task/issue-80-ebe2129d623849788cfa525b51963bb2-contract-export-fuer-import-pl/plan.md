# Umsetzungsplan: Contract-Export fuer Import-Plugins

## Zielbild

Das Hauptrepository liefert einen versionierten, credential-frei abrufbaren
Contract-Export als deterministisches ZIP. Der Export enthaelt eine
whitelist-basierte Quellstruktur, ein selbstbeschreibendes Manifest sowie zwei
ApiCompat-Baseline-Assemblies. Das Plugin-Repository kann daraus spaeter
manuell einen Workspace aktualisieren; ein normaler Plugin-Build bleibt ohne
automatischen Download.

## Leitentscheidungen und Randbedingungen

- `Rezepte.Import.Abstractions/` bleibt die bestehende oeffentliche
  Vertragsschicht und wird vollstaendig exportiert.
- Das Inventory bestaetigt, dass `Rezepte.Import.PluginSdk` fehlt. Es wird als
  neues, im Hauptrepository versioniertes SDK-Projekt angelegt und in die
  Vertragsgrenze aufgenommen. Die SDK-Quellen werden auf die im privaten
  Plugin-Repository verwendeten, hostunabhaengigen Parser-/URL-Hilfen
  begrenzt; Host- und KI-Abhaengigkeiten gehoeren nicht in den Export.
- Das Inventory bestaetigt ebenfalls, dass `Directory.Build.props` fehlt. Es
  wird als minimale, vertragsbezogene gemeinsame Builddatei neu angelegt
  (Target Framework, Nullable/ImplicitUsings und zentrale Contract-Version),
  ohne private Pfade, Secrets oder Hostkonfiguration.
- Die Contract-Version wird unabhaengig von den Pluginversionen zentral
  gepflegt und initial auf `0.2.0` gesetzt, sofern keine bereits freigegebene
  Version im Zielrepository entgegensteht.
- Der Export umfasst ausschliesslich `contract-export.json`,
  `Directory.Build.props`, `Rezepte.Import.Abstractions/**`,
  `Rezepte.Import.PluginSdk/**` sowie die daraus gebauten Baselines. Alle
  anderen Projektverzeichnisse, insbesondere `Rezepte.Web` und die
  produktiven KI-/Backup-Plugins, bleiben ausgeschlossen.

## Umsetzungsschritte

1. **Vertragsprojekte vervollstaendigen**
   - `Directory.Build.props` mit zentraler Contract-Version und gemeinsamen,
     isoliert baubaren SDK-Einstellungen anlegen.
   - `Rezepte.Import.PluginSdk/` mit Projektdatei, Referenz auf
     `Rezepte.Import.Abstractions` und den hostunabhaengigen SDK-Quellen
     anlegen.
   - Beide Projekte in `Rezepte.sln` aufnehmen und Projektverweise so
     ausrichten, dass externe Plugins gegen den exportierten Workspace bauen
     koennen.
   - Keine Referenzen auf `Rezepte.Web`, `bin/`, `obj/`, externe Checkoutpfade
     oder geheime Konfiguration in die Vertragsprojekte uebernehmen.

2. **Exportskript mit harter Whitelist erstellen**
   - Ein PowerShell-Skript unter `scripts/` bereitstellen, mit Parametern fuer
     Ausgabeordner, `contractVersion` und optionalem `sourceCommit`.
   - Die erwarteten Pfade exakt pruefen: `Directory.Build.props`, beide
     Projektverzeichnisse und deren Projektdateien. Fehlende Pfade, leere
     Gruppen sowie unerwartete Vertragsdateien muessen den Lauf mit Fehler
     beenden.
   - Vor dem Packen alle Pfade normalisieren und sicherstellen, dass sie
     relativ sind, keine Laufwerksbuchstaben oder `..`-Segmente enthalten und
     nicht aus `bin/` oder `obj/` stammen.
   - Die Vertragsdateien in stabiler, sortierter Reihenfolge in einen
     isolierten Staging-Ordner kopieren. Keine Dateien ausserhalb der
     Whitelist uebernehmen.

3. **Manifest und deterministisches ZIP erzeugen**
   - `contract-export.json` mit festem `exportFormat`, SemVer-
     `contractVersion`, unveraenderlichem Git-`sourceCommit` und der
     vollstaendigen, sortierten Liste aus Pfad und SHA-256 erzeugen.
   - Das Manifest selbst nicht in `files` aufnehmen.
   - Das ZIP mit stabilen Pfaden, stabiler Reihenfolge und normalisierten
     Metadaten/Rechten erstellen, damit gleiche Quellen und gleicher Commit
     reproduzierbare Bytes ergeben.
   - Nach dem Packen den SHA-256 des vollstaendigen ZIPs berechnen und als
     Skriptoutput sowie als maschinenlesbare Metadaten ausgeben.

4. **ApiCompat-Baselines bauen und zuordnen**
   - Beide Vertragsprojekte aus dem Staging- bzw. Exportworkspace mit fixer
     Konfiguration bauen.
   - `Rezepte.Import.Abstractions.dll` und
     `Rezepte.Import.PluginSdk.dll` in ein eindeutig benanntes Baseline-
     Verzeichnis des Artefakts legen.
   - Baseline-Pfade und ihre Zuordnung zu `contractVersion` und `sourceCommit`
     in Exportmetadaten dokumentieren. Liegen die Assemblies im selben ZIP,
     werden sie wie alle exportierten Vertragsdateien ebenfalls mit Pfad und
     SHA-256 in `files` aufgenommen; nur `contract-export.json` bleibt dort
     ausgeschlossen.
   - Einen isolierten Build des exportierten Workspaces aus einem temporären
     Verzeichnis ohne Zugriff auf den Hauptcheckout verifizieren.

5. **Validierungs- und Regressionstests ergänzen**
   - Tests fuer Manifestfelder, SemVer, vollstaendige Dateiliste,
     Dateihashes, Ausschluss des Manifests aus `files`, relative Pfade und
     den ZIP-Gesamthash vorsehen.
   - Negativtests fuer fehlende `PluginSdk`-/`Directory.Build.props`-Pfade,
     unerwartete Dateien, `bin`/`obj`, absolute Pfade und `..`-Segmente
     vorsehen.
   - Einen Reproduzierbarkeitstest mit identischem Commit und eine
     Workspace-Isolationspruefung mit `dotnet build --no-restore` bzw. einem
     kontrollierten Restore aus dem Exportverzeichnis aufnehmen.
   - Die gebauten Baselines mit `Microsoft.DotNet.ApiCompat.Tool` gegen eine
     gespeicherte Referenz pruefbar machen. Der erste Export etabliert die
     Baseline; nachfolgende Vertragsaenderungen muessen die Contract-Version
     gemaess SemVer anheben.

6. **CI-/Release-Verarbeitung integrieren**
   - Im PR-Workflow den Exportprozess und alle Exporttests ausfuehren, damit
     fehlende oder unerwartete Vertragsdateien frueh fehlschlagen.
   - Im Release-Workflow nach Build und Tests das Contract-ZIP samt Baselines
     erzeugen, den ZIP-SHA-256 ausgeben und als separates Release-Artefakt
     hochladen.
   - Fuer Releases eine credential-frei abrufbare Asset-URL sowie
     `contractVersion`, `sourceCommit` und ZIP-SHA-256 in den
     Release-Metadaten/Release-Notizen veroeffentlichen. Das bestehende
     Web-Publish-ZIP bleibt ein separates Artefakt.

7. **Dokumentation fuer den manuellen Plugin-Import aktualisieren**
   - `Docs/help/import-plugins.md` um Exportstruktur, Artefaktmetadaten,
     Hashbindung, Baseline-Verwendung und den manuellen
     `Update-ContractExport.ps1`-Aufruf im Plugin-Repository ergaenzen.
   - Explizit dokumentieren, dass normale Plugin-Builds keinen neuen
     Vertragsstand herunterladen und dass der ZIP-SHA-256 den konkreten
     Exportstand unveraenderlich identifiziert.

## Abnahme- und Verifikationsstrategie

Die Abnahme erfolgt in dieser Reihenfolge:

1. Exportskript auf einem sauberen Checkout ausfuehren und erzeugte Pfade,
   Manifest und ZIP-SHA-256 pruefen.
2. ZIP in ein Verzeichnis ausserhalb des Checkouts entpacken und beide
   Vertragsprojekte isoliert bauen.
3. Manifestdateiliste gegen den ZIP-Inhalt und jeden SHA-256 vergleichen;
   insbesondere `contract-export.json`, absolute Pfade, `..`, Secrets,
   Hostdateien sowie `bin/`/`obj/` pruefen.
4. Beide DLL-Baselines laden und ihre Zuordnung zu Version und Commit
   verifizieren.
5. Den Export zweimal aus demselben Commit erzeugen und identische
   Datei-/ZIP-Hashes erwarten.
6. Einen absichtlich unvollstaendigen oder verunreinigten Vertragsstand
   exportieren und den erwarteten Fail-fast-Abbruch pruefen.
7. Release-Workflow mit separatem Contract-Artefakt und dokumentierter
   Download-URL/Pruefsumme ausfuehren.

## Abgrenzung

Der Import-Workflow und die eigentliche `Update-ContractExport.ps1`-Datei im
privaten Plugin-Repository werden nicht in diesem Checkout implementiert.
Das Hauptrepository liefert dafuer das formatierte Artefakt, die Baselines und
alle benoetigten Metadaten. Die bestehende Laufzeit-Pluginaktualisierung des
Hosts bleibt unveraendert.

## Offene Punkte

Keine. Die fehlenden Artefakte werden im Hauptrepository als neue
vertragsfuehrende Quellen angelegt; konkrete private Plugin-Repository-
Importdetails sind fuer die Planung des Exporters nicht erforderlich.
