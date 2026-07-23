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

## Erkennung und Reihenfolge

Beim Programmstart sucht die Anwendung im Programmverzeichnis unter `plugins` nach Plugin-DLLs. Unterstuetzt werden DLLs direkt im Ordner `plugins` sowie DLLs in direkten Unterordnern von `plugins`.

Beim Build und Publish der Web-Anwendung werden die drei produktiven Plugins des Hauptrepositorys automatisch gebaut und nach `plugins/<Projektname>/` in das Ausgabe- bzw. Publish-Verzeichnis kopiert. Die klassischen Webseitenquellen liegen in einem separaten privaten Plugin-Repository. Wenn dessen Artefakte unter `external/rezepte-import-plugins-private/artifacts/plugins` vorhanden sind, uebernimmt der Host-Build diese ebenfalls in das jeweilige `plugins`-Verzeichnis.

Neu erkannte Plugins werden automatisch in der Datenbank gespeichert. Bei der allerersten Erfassung bestimmt die Standard-Prioritaet der Plugins die Startreihenfolge; die KI-Plugins haben dabei eine niedrigere Prioritaet als Plugins mit fester Quellenstruktur. Sobald eine Reihenfolge existiert, werden spaeter erkannte Plugins hinten angehaengt. Bereits konfigurierte Plugins behalten ihre Reihenfolge. Wenn ein zuvor bekanntes Plugin beim Start nicht mehr gefunden wird, bleibt es in der Verwaltung sichtbar und erhaelt den Status `Missing`.

Fehlerhafte oder inkompatible Plugin-DLLs werden nicht fuer Imports genutzt. Sie werden mit Fehlerstatus und Fehlermeldung in der Pluginliste angezeigt.

## Importablauf

Fuer jeden Import werden frische Handlerinstanzen aus den aktivierten Plugins erzeugt. Die Anwendung prueft die Plugins der Reihe nach:

1. Das Plugin erhaelt die Importdatei oder URL und prueft, ob es die Quelle verarbeiten kann.
2. Das erste passende Plugin verarbeitet den Import.
3. Weitere Plugins werden danach nicht mehr probiert.
4. Wenn kein aktiviertes Plugin passt, endet der Import mit der Meldung `No suitable import plugin found for this file or URL.`

Interaktive Importpfade, zum Beispiel KI-Importe mit Bestaetigungsdialog, laufen weiterhin ueber den bestehenden Importdialog. Session-basierte Importablaeufe sind an den authentifizierten Benutzer gebunden, der den Import gestartet hat. Status, Bestaetigung, Abbruch und Zwischenauswahl koennen nur fuer eigene Sessions abgerufen oder gesteuert werden; fremde oder ungueltige Session-IDs liefern keine Sessiondetails.

## Chefkoch-Rezeptsammlungen

Das Chefkoch-Plugin kann neben einzelnen Rezeptseiten auch Chefkoch-Rezeptsammlungen importieren. Eine Sammlungs-URL fuehrt nicht sofort zum Import aller enthaltenen Rezepte. Stattdessen liest die Anwendung zuerst nur die Informationen aus der Sammlungsseite und zeigt eine Zwischenauswahl im Importdialog.

In dieser Zwischenauswahl sehen Sie die gefundenen Rezepte der Sammlung mit bereinigten Rezeptnamen ohne Bewertungsreste. Waehlen Sie die Rezepte aus, die importiert werden sollen, und legen Sie fuer jedes ausgewaehlte Rezept das Zielkochbuch fest. Fuer groessere Sammlungen koennen alle gefundenen Rezepte gesammelt aus- oder abgewaehlt werden. Ein Zielkochbuch kann ausserdem fuer alle aktuell ausgewaehlten Rezepte uebernommen werden. Nicht ausgewaehlte Rezepte werden nicht abgerufen und nicht importiert.

Erst nach dem Absenden der Auswahl ruft die Anwendung die ausgewaehlten Rezeptseiten ab. Die Auswahl und die Zielkochbuecher sind danach gesperrt. Waehrend des Imports zeigt der Dialog den Fortschritt pro Rezept an. Erfolgreiche Rezepte werden mit einem Erfolgssymbol markiert. Falls ein Rezept nicht importiert werden kann, wird es mit einem Warnsymbol angezeigt; die konkrete Fehlermeldung ist dort einsehbar.

Der Dialog kann waehrend des laufenden Imports geschlossen werden. Das Schliessen blendet nur die Fortschrittsanzeige aus und bricht den Import nicht ab.

Diese Sammlungsfunktion gilt derzeit fuer Chefkoch. Andere Import-Plugins verarbeiten weiterhin einzelne Rezeptquellen.

## Aktueller Umsetzungsstand

Der erreichte Stand ist ein Plugin-Framework mit gemeinsamer Vertragsschicht `Rezepte.Import.Abstractions`, dem isoliert baubaren SDK-Projekt `Rezepte.Import.PluginSdk`, persistierter Plugin-Konfiguration, Start-Erkennung externer Plugin-DLLs, Admin-UI, Plugin-basierter Auswahl im Datei- und URL-Import sowie Chefkoch-Unterstuetzung fuer Rezeptsammlungen mit Zwischenauswahl.

Backup bleibt als produktives Pluginprojekt im Hauptrepository:

- `Rezepte.Import.Plugins.Backup`
- `Rezepte.Import.Plugins.AIFoto`
- `Rezepte.Import.Plugins.AIUrl`

Die klassischen Webseitenquellen liegen im separaten privaten Plugin-Repository `rezepte-import-plugins-private`:

- `Rezepte.Import.Plugins.Chefkoch`
- `Rezepte.Import.Plugins.SecondSource`
- `Rezepte.Import.Plugins.ThirdSource`
- `Rezepte.Import.Plugins.FourthSource`
- `Rezepte.Import.Plugins.FifthSource`
- `Rezepte.Import.Plugins.SixthSource`

Diese Plugins referenzieren die gemeinsame Vertragsschicht und liefern neutrale Rezeptdaten zurueck. Gemeinsame Parser- und URL-Hilfen fuer die Webseitenquellen werden im Hauptrepository im SDK-Projekt `Rezepte.Import.PluginSdk` gefuehrt und als Teil des Contract-Exports bereitgestellt. Der Host persistiert aus den neutralen Importdaten Rezepte, Zutaten, Schritte, Bilder und Kochbuchzuordnungen.

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

Das private Plugin-Repository enthaelt ausserdem ein rudimentaeres Console-Testprogramm `Rezepte.Import.PluginRunner`. Damit kann ein Plugin per ID oder Nummer ausgewaehlt und gegen eine Datei oder URL ausgefuehrt werden. Bei nicht passenden Eingaben meldet der Runner, dass das ausgewaehlte Plugin die Quelle nicht verarbeiten kann; bei Erfolg gibt er die gelesenen Rezeptdaten aus.

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

Die produktiven Pluginparser sind mit dedizierten Fixture-Tests abgedeckt. Im Hauptrepository werden Backup-, KI-Foto- und KI-URL-Plugins gebaut und entdeckt. Im privaten Plugin-Repository pruefen eigene Tests repraesentative HTML-/JSON-Strukturen fuer Chefkoch, SecondSource, ThirdSource, FourthSource, FifthSource und SixthSource ueber den oeffentlichen Importvertrag.

Zusaetzliche Host-Integrationstests koennen ueber `REZEPTE_EXTERNAL_PLUGINS_PATH` auf einen Checkout des privaten Plugin-Repositories zeigen. Ist kein separates Repository konfiguriert, wird standardmaessig `external/rezepte-import-plugins-private` verwendet. Die Tests publizieren externe Plugin-Artefakte in einen temporaeren Host-Plugin-Ordner und pruefen, dass die Plugins ohne benachbarte `Rezepte.Import.Abstractions.dll` geladen werden.
