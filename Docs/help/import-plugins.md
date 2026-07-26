# Import-Plugins

Die Anwendung verwendet fuer Rezeptimporte ein Plugin-Framework. Der Import auf der Startseite laeuft nicht mehr ueber eine feste Handlerliste, sondern fragt den `PluginManager` nach den aktivierten Import-Plugins in der gespeicherten Reihenfolge.

## Administration

Administratoren finden die Pluginverwaltung in den Einstellungen unter `Plugins`.

Die Liste zeigt jedes bekannte Plugin mit Name, Plugin-ID, Status, Assembly und Handler-Typ. Plugins koennen dort aktiviert oder deaktiviert werden. Die Reihenfolge wird ueber die Pfeile nach oben und unten geaendert und dauerhaft gespeichert.

### GitHub-Pluginquellen

Administratoren koennen unter `Plugins` globale GitHub-Repositorys als Pluginquellen verwalten. Beim Hinzufuegen werden die Repository-URL, die Sichtbarkeit (`Oeffentlich` oder `Privat`), die Aktivierung und eine ausdrueckliche Vertrauensbestaetigung erfasst. Die Vertrauensbestaetigung ist beim Hinzufuegen erforderlich. Bestehende Quellen koennen bearbeitet, aktiviert, deaktiviert oder geloescht werden.

Fuer private Repositorys kann ein Personal Access Token (PAT) hinterlegt oder ueber das Feld `PAT aktualisieren` erneuert werden. Der Token wird serverseitig geschuetzt verwaltet und weder in der Pluginliste angezeigt noch an den Browser uebertragen. Bei oeffentlichen Quellen ist kein Token erforderlich.

Aktivierte und bestaetigte Quellen werden einmalig beim Start der Anwendung geprueft. Es gibt derzeit keinen regelmaessigen Hintergrundlauf und keinen manuellen Update-Button; Aenderungen an Quellen oder Tokens werden beim naechsten Anwendungsstart wirksam. Aus dem neuesten veroeffentlichten GitHub-Release wird ein ZIP-Asset geladen. Der Assetname kann variieren, sofern es sich um ein geeignetes ZIP-Asset handelt. GitHub-Rate-Limits werden erkannt und als eigener Status gespeichert; ein `Retry-After`-Hinweis wird kontrolliert beruecksichtigt.

Das Paket wird vor der Installation in einem temporaeren Verzeichnis geprueft. Nur erkannte Plugin-Unterverzeichnisse und zulaessige Inhalte werden uebernommen. Austausch und produktiver Reload laufen koordiniert: neue Imports werden waehrenddessen gesperrt, laufende Handler werden nicht parallel zum Dateiaustausch unterbrochen, und nachfolgende Imports sehen erst den neu initialisierten Pluginbestand. Bei Kopier-, Installations- oder Reloadfehlern wird der bisherige Pluginbestand wiederhergestellt. Der Status der Quelle zeigt den letzten erfolgreichen Release, den letzten Fehler und den Zeitpunkt der letzten Pruefung; Reloadstatus, Reloadzeitpunkt und Reloadfehler bleiben in der Releasehistorie nachvollziehbar.

Beim Import werden nur aktivierte Plugins mit Status `Loaded` beruecksichtigt. Deaktivierte, fehlende oder fehlerhafte Plugins werden nicht angesprochen.

## Nutzbarkeit und Konfigurationsvalidierung

Die Verwaltungsoberflaeche prueft die Nutzbarkeit von Plugins zur Laufzeit, wenn die Pluginliste geladen wird. Dies ermoeglicht es Administratoren, sofort zu sehen, wenn ein Plugin nicht einsatzfaehig ist, und konkrete Fehlerursachen sowie Loesungsvorschlaege zu erhalten.

Fuer jedes geladene Plugin wird eine Nutzbarkeitsprüfung durchgefuehrt. Plugins ohne Konfigurations-Voraussetzungen (wie das Backup-Plugin) gelten per Default als nutzbar. KI-Plugins (AIUrl und AIFoto) prüfen ihre globalen Voraussetzungen:

### AIUrl-Prüfung

Das Plugin `AIUrl` prueft folgende Bedingungen:

- Globale KI ist aktiviert
- Gemini-Authentifizierung ist vorhanden (API-Key oder Service Account)
- Globales Gemini ist aktiviert

Ist eine Bedingung nicht erfuellt, zeigt das Plugin eine Fehlermeldung mit einem Loesungshinweis an, zum Beispiel: `Enable the global Gemini switch in the AI settings.`

### AIFoto-Prüfung

Das Plugin `AIFoto` prueft zusaetzlich zu den AIUrl-Bedingungen noch folgende Punkte:

- Vision-Service-Account-Datei existiert und ist lesbar
- Globales Google Vision ist aktiviert

### Fehleranzeige in der Admin-UI

In der Pluginliste wird ein nicht nutzbares Plugin mit einem `nicht nutzbar`-Badge gekennzeichnet. Darunter werden die Fehlerursachen aufgelistet:

```
[Plugin-Name]
[Status-Badge] [nicht nutzbar]
Global AI is disabled.
Enable the global AI switch in the AI settings.
Gemini authentication is missing.
Configure a Gemini API key or a Google service account.
```

Jede Fehlerursache besteht aus einer Meldung (in rot) und einem optionalen Loesungshinweis (in Grau). Der Administrator kann diese Hinweise nutzen, um die Konfiguration zu korrigieren.

### Technische Details

Die Nutzbarkeitsprüfung laeuft ohne Netzwerk-Zugriffe ab — sie validiert nur die lokale Konfiguration (Datenbank-Einstellungen, Credentials-Dateien, In-Memory-Schlüssel). Dies vermeidet Blockierungen der Admin-UI durch langsame externe Services. Jedes Mal, wenn die Pluginliste geladen wird, werden die Prüfungen neu durchgefuehrt, um stets aktuelle Ergebnisse zu liefern.

## Erkennung und Reihenfolge

Beim Programmstart sucht die Anwendung im Programmverzeichnis unter `plugins` nach Plugin-DLLs. Unterstuetzt werden DLLs direkt im Ordner `plugins` sowie DLLs in direkten Unterordnern von `plugins`.

Beim Build und Publish der Web-Anwendung werden die drei produktiven Plugins des Hauptrepositorys automatisch gebaut und nach `plugins/<Projektname>/` in das Ausgabe- bzw. Publish-Verzeichnis kopiert.

Neu erkannte Plugins werden automatisch in der Datenbank gespeichert. Bei der allerersten Erfassung bestimmt die Standard-Prioritaet der Plugins die Startreihenfolge; die KI-Plugins haben dabei eine niedrigere Prioritaet als Plugins mit fester Quellenstruktur. Sobald eine Reihenfolge existiert, werden spaeter erkannte Plugins hinten angehaengt. Bereits konfigurierte Plugins behalten ihre Reihenfolge. Wenn ein zuvor bekanntes Plugin beim Start nicht mehr gefunden wird, bleibt es in der Verwaltung sichtbar und erhaelt den Status `Missing`.

Fehlerhafte oder inkompatible Plugin-DLLs werden nicht fuer Imports genutzt. Sie werden mit Fehlerstatus und Fehlermeldung in der Pluginliste angezeigt.

## Importablauf

Fuer jeden Import werden frische Handlerinstanzen aus den aktivierten Plugins erzeugt. Die Anwendung prueft die Plugins der Reihe nach:

1. Das Plugin erhaelt die Importdatei oder URL und prueft, ob es die Quelle verarbeiten kann.
2. Das erste passende Plugin verarbeitet den Import.
3. Weitere Plugins werden danach nicht mehr probiert.
4. Wenn kein aktiviertes Plugin passt, endet der Import mit der Meldung `No suitable import plugin found for this file or URL.`

Interaktive Importpfade, zum Beispiel KI-Importe mit Bestaetigungsdialog, laufen weiterhin ueber den bestehenden Importdialog. Session-basierte Importablaeufe sind an den authentifizierten Benutzer gebunden, der den Import gestartet hat. Status, Bestaetigung, Abbruch und Zwischenauswahl koennen nur fuer eigene Sessions abgerufen oder gesteuert werden; fremde oder ungueltige Session-IDs liefern keine Sessiondetails.

## Aktueller Umsetzungsstand

Der erreichte Stand ist ein Plugin-Framework mit gemeinsamer Vertragsschicht `Rezepte.Import.Abstractions`, dem isoliert baubaren SDK-Projekt `Rezepte.Import.PluginSdk`, persistierter Plugin-Konfiguration, Start-Erkennung externer Plugin-DLLs, Admin-UI sowie Plugin-basierter Auswahl im Datei- und URL-Import.

Backup bleibt als produktives Pluginprojekt im Hauptrepository:

- `Rezepte.Import.Plugins.Backup`
- `Rezepte.Import.Plugins.AIFoto`
- `Rezepte.Import.Plugins.AIUrl`

Externe Import-Plugins referenzieren die gemeinsame Vertragsschicht und liefern neutrale Rezeptdaten zurueck. Gemeinsame Parser- und URL-Hilfen fuer solche Plugins werden im Hauptrepository im SDK-Projekt `Rezepte.Import.PluginSdk` gefuehrt und als Teil des Contract-Exports bereitgestellt. Der Host persistiert aus den neutralen Importdaten Rezepte, Zutaten, Schritte, Bilder und Kochbuchzuordnungen.

## Contract-Export fuer externe Plugin-Repositories

Der Import-Plugin-Vertrag wird mit `scripts/Export-ImportContract.ps1` als reproduzierbares ZIP erzeugt. Der Export enthaelt ausschliesslich:

- `contract-export.json`
- `Directory.Build.props`
- alle freigegebenen Quellen aus `Rezepte.Import.Abstractions/`
- alle freigegebenen Quellen aus `Rezepte.Import.PluginSdk/`
- ApiCompat-Baselines unter `baselines/<contractVersion>/`

`bin/`, `obj/`, Host-Projektdateien, KI-/Backup-Pluginimplementierungen und Secret-Konfigurationen gehoeren nicht zum Export. Alle ZIP-Pfade sind repositoryrelativ, verwenden `/` als Trenner und duerfen keine absoluten Pfade oder `..`-Segmente enthalten.

Das Manifest `contract-export.json` beschreibt den Export mit `exportFormat`, `contractVersion`, `sourceCommit`, Baseline-Zuordnung und einer vollstaendigen Liste aller exportierten Dateien ausser dem Manifest selbst. Jeder Dateieintrag enthaelt den repositoryrelativen Pfad und den SHA-256-Hash der Datei. Der SHA-256 des vollstaendigen ZIPs steht ausserhalb des ZIPs in `contract-export.metadata.json` und wird vom Skript ausgegeben. Die lokale Metadatei enthaelt keinen absoluten Runner- oder Checkout-Pfad; sie nennt den Artefaktnamen, den ZIP-SHA-256, `contractVersion`, `sourceCommit` und die Dateihashes.

Lokaler Export:

```powershell
dotnet tool install Microsoft.DotNet.ApiCompat.Tool --tool-path ./.tools
./scripts/Export-ImportContract.ps1 -OutputDirectory artifacts/contract-export
```

Sobald gespeicherte Referenzen unter `contract-baselines/import-contract/` vorhanden sind, fuehrt der lokale Export einen harten ApiCompat-Vergleich aus. Dafuer muss `apicompat` installiert sein oder mit `-ApiCompatToolPath <pfad-zum-tool>` explizit uebergeben werden. Die GitHub-Workflows installieren das Tool automatisch, wenn gespeicherte Baselines vorhanden sind.

Fuer Release-Laeufe wird das Contract-ZIP als separates Artefakt neben dem Web-`release.zip` erzeugt. Wenn der Workflow einen GitHub Release erstellt, enthalten die Release-Notizen und die dort veroeffentlichte `contract-export.metadata.json` eine konkrete credential-frei abrufbare URL im Format `https://github.com/<owner>/<repo>/releases/download/<tag>/rezepte-import-contract-<contractVersion>.zip`. Der ZIP-SHA-256 identifiziert den konkreten Exportstand unveraenderlich; gleiche `contractVersion` mit anderem ZIP-SHA-256 ist ein anderer Exportstand und darf nicht stillschweigend ersetzt werden. Actions-Artefakte aus nicht taggenden CI-Laeufen sind davon getrennte CI-Artefakte und nicht der dokumentierte oeffentliche Plugin-Importpfad.

Das externe Plugin-Repository aktualisiert den Vertrag manuell ueber eine konkrete Artefakt-URL und den erwarteten ZIP-SHA-256:

```powershell
./scripts/Update-ContractExport.ps1 `
  -ArtifactUrl <oeffentliche-export-zip-url> `
  -ArtifactSha256 <64-stelliger-zip-sha256>
```

Ein normaler Plugin-Build laedt keinen neuen Vertragsstand herunter. Nach einem manuellen Update prueft das Plugin-Repository ZIP-SHA-256, Manifestfelder, Dateiliste, Datei-SHA-256 und den isolierten Build des importierten Workspaces. Die Baseline-DLLs `Rezepte.Import.Abstractions.dll` und `Rezepte.Import.PluginSdk.dll` sind demselben `contractVersion`-/`sourceCommit`-Stand zugeordnet und dienen als Referenz fuer `Microsoft.DotNet.ApiCompat.Tool`.

Historische ApiCompat-Vergleiche werden hart, sobald passende gespeicherte Referenzen im Hauptrepository abgelegt sind. Erwartet wird die Struktur `contract-baselines/import-contract/<semver>/Rezepte.Import.Abstractions.dll` und `contract-baselines/import-contract/<semver>/Rezepte.Import.PluginSdk.dll`. Das Exportskript kann mit `-ApiCompatBaselineVersion <semver>` auf eine konkrete gespeicherte Baseline festgelegt werden. Ohne diesen Parameter waehlt es automatisch die neueste gespeicherte SemVer-Baseline, deren Version nicht ueber der aktuellen Contract-Version liegt. Ohne passende gespeicherte Baseline wird der Vergleich im Skript sowie in den Workflows explizit als uebersprungen protokolliert.

### Oeffentliche API im Plugin-Vertrag aendern (Breaking-Change-Workflow)

`Rezepte.Import.Abstractions` und `Rezepte.Import.PluginSdk` sind der eingefrorene Vertrag fuer externe Plugin-Repositories. Jede sichtbare Aenderung an diesen beiden Projekten — neuer Typ, neues Interface-Member, geaenderte Signatur, auch wenn sie binaerkompatibel ist (z. B. eine Default Interface Method) — aendert die oeffentliche API-Flaeche. Sobald unter `contract-baselines/import-contract/<semver>/` eine Baseline mit derselben `ImportContractVersion` liegt, vergleicht der PR-Workflow im `--strict-mode` die aktuelle Assembly gegen genau diese eingefrorene Baseline. `--strict-mode` meldet dabei nicht nur entfernte oder geaenderte, sondern auch rein additive Aenderungen (`CP0001`/`CP0002`) als Abweichung. Ohne Versions-Bump schlaegt der PR-Check deshalb bei jeder API-Erweiterung fehl — das ist beabsichtigt und kein CI-Fehler.

Vorgehen bei einer beabsichtigten, additiven und binaerkompatiblen Aenderung (z. B. neue Default Interface Method, neuer Typ):

1. **Version anheben:** `ImportContractVersion` in `Directory.Build.props` erhoehen (Minor-Bump fuer additive/binaerkompatible Aenderungen, Major-Bump fuer entfernte oder inkompatibel geaenderte Typen/Member). Diese Version ist unabhaengig von den Git-Tag-basierten Anwendungs-Releases (siehe Abschnitt „Versionierung" in [GitHub Actions](github-actions.md)).
2. **Baseline lokal ohne historischen Vergleich erzeugen**, damit der Export nicht sofort gegen die alte Baseline scheitert:

   ```powershell
   ./scripts/Export-ImportContract.ps1 `
     -OutputDirectory artifacts/contract-export `
     -ApiCompatBaselineDirectory contract-baselines/does-not-exist
   ```

   Das Skript meldet „ApiCompat baseline directory not found; skipping historical API comparison" und baut die beiden Assemblies unter `artifacts/contract-export/_staging/export/baselines/<neue-version>/`.
3. **Neue Baseline committen:** Die beiden gebauten DLLs aus Schritt 2 nach `contract-baselines/import-contract/<neue-version>/` kopieren und zusammen mit der `Directory.Build.props`-Aenderung committen. Die aeltere Baseline bleibt unveraendert erhalten; sie dokumentiert weiterhin den zuvor gueltigen Vertragsstand.
4. **Lokal verifizieren, bevor der PR erstellt/aktualisiert wird:**

   ```powershell
   dotnet tool install Microsoft.DotNet.ApiCompat.Tool --tool-path ./.tools
   ./scripts/Export-ImportContract.ps1 `
     -OutputDirectory artifacts/contract-export `
     -ApiCompatBaselineDirectory contract-baselines/import-contract `
     -ApiCompatToolPath ./.tools/apicompat.exe
   ```

   Das Skript muss „APICompat wurde erfolgreich ausgefuehrt, ohne Breaking Changes zu finden." fuer beide Assemblies ausgeben. Wird stattdessen wieder die alte Baseline-Version verwendet, wurde `ImportContractVersion` nicht hoch genug angehoben oder die neue Baseline fehlt/liegt am falschen Pfad.
5. **`Rezepte.Tests/ContractExport/ContractExportScriptTests.cs` nachziehen.** Diese Tests laufen direkt gegen die echte `Directory.Build.props` der Repository-Wurzel und halten die zuvor gueltige Vertragsversion an mehreren Stellen woertlich fest. Nach einem Versions-Bump muessen angepasst werden:
   - die Konstante `ContractVersion` am Kopf der Klasse,
   - alle `"baselines/<alte-version>/..."`-Pfade und `"rezepte-import-contract-<alte-version>.zip"`-Dateinamen,
   - `new Version(<major>, <minor>, <patch>, 0)` in `ExportedBaselineAssembliesUseContractVersion`,
   - die erwartete Fehlermeldung „...ImportContractVersion (<alte-version>): ..." in `ExportFailsFastWhenParameterVersionDiffersFromDirectoryBuildProps`.

   Der Test `ExportUsesLatestStoredApiCompatBaselineBelowCurrentVersion` verwendet bewusst eigene, in sich geschlossene Baseline-Versionen (`0.1.0`, `0.2.0` als Beispiel fuer „aeltere, gespeicherte Baselines") in einem isolierten temporaeren Verzeichnis und ist von der echten `ImportContractVersion` unabhaengig — dieser Test muss bei einem Versions-Bump **nicht** angepasst werden.
6. **`dotnet test Rezepte.sln`** vor dem Push einmal vollstaendig laufen lassen, nicht nur die ContractExport-Tests.

Wenn eine automatisierte PR-Fehleranalyse bei diesem Fehlerbild einen `--generate-suppression-file`- oder `GenerateSuppressionFile`-Parameter fuer `scripts/Export-ImportContract.ps1` oder den Workflow vorschlaegt: Dieser Parameter existiert im Skript nicht und darf nicht ergaenzt werden. Es gibt keinen Suppression-Mechanismus fuer diesen Vertrag — jede akzeptierte API-Aenderung muss ueber einen Versions-Bump und eine neue, eingefrorene Baseline nachvollziehbar sein.

KI-Foto und KI-URL werden als eigene Plugins im Hauptrepository ausgeliefert. Ihre Handler verwenden weiterhin die vom Host bereitgestellten Services fuer AI-Konfiguration, Usage-Limits, Google Vision, Gemini, Cache und interaktive Bestaetigung. Die Plugins liefern ihre Ergebnisse als neutrale Import-DTOs und nutzen denselben zentralen Persistenzpfad wie externe Plugins.

## KI-Importe

Die KI-Plugins sind nur aktiv, wenn das jeweilige Plugin geladen und aktiviert ist, die globalen KI-Schalter aktiv sind und der angemeldete Benutzer KI verwenden darf. Zusaetzlich gelten dienstspezifische Voraussetzungen:

- `Rezepte.Import.Plugins.AIUrl` verarbeitet HTML-Quellen und benoetigt Gemini. Gemini kann entweder ueber `GOOGLE_GEMINI_API_KEY` oder ueber einen vorhandenen Google-Service-Account authentifiziert werden.
- `Rezepte.Import.Plugins.AIFoto` verarbeitet Bilddateien und benoetigt Google Vision sowie Gemini. Dafuer muss eine lesbare Service-Account-Datei ueber `GOOGLE_APPLICATION_CREDENTIALS` oder den Konfigurationsfallback `GoogleCredentials:ServiceAccountFilePath` verfuegbar sein. Fuer Gemini reicht zusaetzlich ein API-Key oder derselbe Service Account.

Wenn `GOOGLE_GEMINI_API_KEY` gesetzt ist, verwendet Gemini diesen API-Key bevorzugt. Der fruehere Konfigurationswert `GoogleCredentials:GeminiApiKey` bleibt als Fallback erhalten, ist aber nicht fuer produktive Secrets gedacht.

Wenn ein KI-Plugin nicht angeboten wird oder eine Quelle nicht verarbeitet, pruefen Sie diese Punkte in der Reihenfolge:

1. Das Plugin ist in der Pluginverwaltung aktiviert und hat den Status `Loaded`.
2. Die globalen KI-, Gemini- und bei Fotoimporten Google-Vision-Schalter sind aktiv.
3. Der angemeldete Benutzer darf KI, Gemini und bei Fotoimporten Google Vision verwenden.
4. Fuer URL-Importe ist Gemini ueber API-Key oder Service Account authentifiziert.
5. Fuer Fotoimporte existiert die Service-Account-Datei und ist fuer den Prozess lesbar.

Die Anwendung loggt Konfigurations- und Aktivierungsgruende mit Handlername und Benutzer-ID. Fehlende Gemini-Authentifizierung, deaktivierte KI-/Gemini-/Vision-Schalter und fehlende Vision-Credentials fuehren zu nachvollziehbaren Logeintraegen. Secret-Werte werden dabei nicht ausgegeben; der Gemini-API-Key wird nur als vorhanden oder nicht vorhanden und mit seiner Quelle protokolliert.

## Qualitaetssicherung

Die produktiven Pluginparser sind mit dedizierten Fixture-Tests abgedeckt. Im Hauptrepository werden Backup-, KI-Foto- und KI-URL-Plugins gebaut und entdeckt.
