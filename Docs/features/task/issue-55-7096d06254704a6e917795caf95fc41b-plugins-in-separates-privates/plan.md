# Umsetzungsplan: Rezeptimport-Plugins auslagern

## Zielbild

Die produktiven Rezeptabruf-Plugins werden aus dem Hauptrepository geloest und in ein separates privates Plugin-Repository ueberfuehrt. Das Hauptrepository bleibt Host der Web-Anwendung und des stabilen Importvertrags. Das neue Repository enthaelt die ausgelagerten Plugin-Projekte, die zugehoerigen Parser-Tests, Beispiel-Eingaben und ein rudimentaeres Console-Testprogramm zur manuellen Ausfuehrung eines ausgewaehlten Plugins gegen URL oder Datei.

## Planungsentscheidungen

1. Das neue private Repository wird unter dem Arbeitstitel `rezepte-import-plugins-private` geplant. Der konkrete Remote-Name kann spaeter beim Anlegen des Repositories angepasst werden, ohne die technische Struktur zu aendern.
2. `Rezepte.Import.Abstractions` bleibt im Hauptrepository als Host-Vertrag bestehen. Das Plugin-Repository referenziert diesen Vertrag zunaechst als lokale Projektkopie/Subtree oder spaeter als internes NuGet-Paket. Die Implementierung muss die Referenzform dokumentieren, aber keine Host-Persistierung auslagern.
3. `UrlRecipeImportHandlerBase` und die damit verbundenen Parser-Hilfen werden als Plugin-SDK behandelt. Sie werden aus dem Abstraktionsprojekt herausgeloest und im Plugin-Repository in ein gemeinsames SDK-Projekt verschoben, damit das Host-Abstraktionsprojekt schlank bleibt.
4. Ausgelagert werden die Online-Quellplugins `Chefkoch`, `SecondSource`, `ThirdSource`, `FourthSource`, `FifthSource` und `SixthSource`. `Backup` bleibt im Hauptrepository, weil es fachlich ein Backup-Dateiimport und kein Abruf aus bekannten Online-Quellen ist.
5. Das rudimentaere Testprogramm liegt im neuen Plugin-Repository, weil es die Nutzbarkeit der ausgelagerten Plugins unabhaengig vom Web-Host nachweist.
6. Der manuelle Mindestnachweis erfolgt mit `chefkoch` und einer lokalen Beispiel-HTML-Datei. Live-URL-Abrufe bleiben moeglich, duerfen aber nicht der einzige Nachweis sein.

## Arbeitspakete

### 1. Zielstruktur fuer das Plugin-Repository anlegen

- Ein neues Verzeichnis fuer den spaeteren privaten Repository-Inhalt vorbereiten, z. B. `external/rezepte-import-plugins-private/` oder ein separates Nachbarverzeichnis ausserhalb des Hauptrepos.
- Eine eigene Solution fuer das Plugin-Repository anlegen, z. B. `Rezepte.Import.Plugins.sln`.
- Gemeinsame Build-Einstellungen fuer `net10.0`, Nullable und ImplicitUsings ueber `Directory.Build.props` definieren.
- Eine kurze `README.md` im Plugin-Repository erstellen mit Zweck, Build, Runner-Verwendung und Plugin-Ausgabeformat.

### 2. Vertrag und SDK trennen

- `Rezepte.Import.Abstractions` im Hauptrepository auf den reinen Host-Vertrag reduzieren:
  - `IImportPlugin`
  - `IImportHandler`
  - `ICollectionImportHandler`
  - Import-/Collection-Ergebnis- und Rezeptmodelle
- Parser- und URL-Hilfscode, insbesondere `UrlRecipeImportHandlerBase` und gemeinsam genutzte Parserfunktionen, in ein neues SDK-Projekt im Plugin-Repository verschieben, z. B. `Rezepte.Import.PluginSdk`.
- Alle ausgelagerten URL-Plugins auf das neue SDK-Projekt umstellen.
- Sicherstellen, dass das SDK-Projekt weiterhin nur vom Abstraktionsvertrag abhaengt und keine Referenz auf `Rezepte.Web` hat.

### 3. Produktive Online-Plugins auslagern

- Die folgenden Projekte in das Plugin-Repository uebernehmen:
  - `Rezepte.Import.Plugins.Chefkoch`
  - `Rezepte.Import.Plugins.SecondSource`
  - `Rezepte.Import.Plugins.ThirdSource`
  - `Rezepte.Import.Plugins.FourthSource`
  - `Rezepte.Import.Plugins.FifthSource`
  - `Rezepte.Import.Plugins.SixthSource`
- Projektverweise der Plugins von `..\Rezepte.Import.Abstractions\...` auf die im Plugin-Repository gewaehlte Vertragsreferenz umstellen.
- Namespace, Plugin-ID, DisplayName und HandlerType unveraendert lassen, damit bestehende Plugin-Einstellungen und Tests nicht unnoetig brechen.
- `Rezepte.Import.Plugins.Backup` im Hauptrepository belassen und nicht in den manuellen Online-Plugin-Nachweis aufnehmen.

### 4. Hauptrepository entkoppeln

- Die ausgelagerten Online-Plugin-Projekte aus `Rezepte.sln` entfernen.
- Direkte `ProjectReference`-Eintraege auf ausgelagerte Online-Plugins aus `Rezepte.Tests/Rezepte.Tests.csproj` entfernen.
- Host-seitige Tests behalten, die keine produktiven Plugin-Projekte direkt referenzieren:
  - Discovery mit `Rezepte.Tests.PluginFixture`
  - Reihenfolge und Fehlerpfade von `ImportService`
  - Plugin-Einstellungen und Inkompatibilitaetsfaelle
- Den Test `PluginManagerTests.InitializeAsync_ShouldDiscoverProductiveExternalImportPlugins` entweder entfernen oder auf gebaute externe Plugin-Artefakte aus einem Test-Plugin-Ausgabeordner umstellen. Das Hauptrepository darf dafuer keine direkten Projektverweise auf die ausgelagerten Plugins behalten.
- Bestehende Web-Laufzeitintegration in `PluginManager` unveraendert lassen, sofern externe DLLs weiterhin aus `plugins/` geladen werden koennen.

### 5. Parser-Tests in das Plugin-Repository verschieben

- `ProductiveImportPluginParserTests` in ein neues Testprojekt im Plugin-Repository verschieben, z. B. `Rezepte.Import.Plugins.Tests`.
- Tests fuer `Chefkoch`, `SecondSource`, `ThirdSource`, `FourthSource`, `FifthSource` und `SixthSource` dort gegen die ausgelagerten Projekte ausfuehren.
- Backup-spezifische Tests im Hauptrepository belassen oder in einen eigenen Host-Test verschieben, solange `Backup` dort bleibt.
- HTML-/JSON-Testdaten aus den bisherigen Inline-Tests nach Moeglichkeit als Dateien unter `tests/fixtures/` ablegen, damit sie vom Console-Runner ebenfalls genutzt werden koennen.

### 6. Rudimentaeres Console-Testprogramm erstellen

- Im Plugin-Repository eine Console-App anlegen, z. B. `Rezepte.Import.PluginRunner`.
- Der Runner listet alle lokal referenzierten `IImportPlugin`-Implementierungen mit Nummer, ID und DisplayName auf.
- Der Runner erlaubt Plugin-Auswahl per ID oder Nummer.
- Der Runner akzeptiert eine Eingabe als URL oder Dateipfad:
  - URL: HTML per `HttpClient` laden, Stream erzeugen, Original-URL als Dateiname/Quelle weitergeben.
  - Datei: Datei als Stream oeffnen, Dateiname an den Handler weitergeben.
- Der Runner ruft zuerst `CanHandleAsync` auf. Bei `false` gibt er klar aus, dass das ausgewaehlte Plugin die Eingabe nicht verarbeiten kann.
- Bei `true` ruft der Runner `HandleAsync` auf und gibt aus:
  - Erfolg/Fehler
  - Fehlertext
  - Anzahl importierter Rezepte
  - je Rezept mindestens Titel, Beschreibung/Quelle, Portionen, Zeiten, Zutaten und Schritte, soweit vorhanden
- Fuer Collection-Handler soll der Runner optional Collection-Preview-Daten anzeigen, wenn der ausgewaehlte Handler `ICollectionImportHandler` implementiert.
- Eine lokale Chefkoch-Beispiel-HTML-Datei wird als Fixture abgelegt und in der README als stabiler Demo-Befehl dokumentiert.

### 7. Build- und Ausgabeformat dokumentieren

- Im Plugin-Repository ein Publish-/Copy-Skript ergaenzen, das jedes Plugin in die vom Host erwartete Struktur ausgibt:
  - `plugins/Rezepte.Import.Plugins.Chefkoch/Rezepte.Import.Plugins.Chefkoch.dll`
  - analog fuer die weiteren Online-Plugins
- Die Ausgabe soll alle notwendigen Abhaengigkeiten enthalten, aber die Host-Abstraktionsassembly nicht als inkompatible Zweitversion erzwingen.
- Dokumentieren, dass der Host die eigene `Rezepte.Import.Abstractions`-Assembly bevorzugt und Plugin-/Host-Versionen deshalb kompatibel bleiben muessen.
- Optional eine Versionsnummer fuer das Abstraktionspaket einfuehren, sobald der Vertrag als NuGet-Paket konsumiert wird.

### 8. Host-Kompatibilitaet verifizieren

- Im Hauptrepository pruefen, dass `PluginManager` externe Plugin-DLLs weiterhin aus `plugins/` und Unterordnern laedt.
- Ein gebautes Chefkoch-Plugin aus dem neuen Repository testweise in den Host-`plugins/`-Ordner kopieren.
- Host-Test oder manuellen Start ausfuehren und pruefen, dass das Plugin als `Loaded` erkannt wird.
- Sicherstellen, dass bei nicht passenden Eingaben weiterhin `No suitable import plugin found for this file or URL.` oder eine gleichwertig nachvollziehbare Meldung erscheint.

## Betroffene Dateien und Projekte

### Hauptrepository

- `Rezepte.sln`
- `Rezepte.Import.Abstractions/`
- `Rezepte.Import.Plugins.Backup/`
- `Rezepte.Tests/Rezepte.Tests.csproj`
- `Rezepte.Tests/Services/Import/ProductiveImportPluginParserTests.cs`
- `Rezepte.Tests/Services/Import/PluginManagerTests.cs`
- `Rezepte.Web/Services/Import/Plugins/PluginManager.cs`
- `Rezepte.Web/Services/Import/ImportService.cs`

### Neues Plugin-Repository

- `Rezepte.Import.Plugins.sln`
- `Directory.Build.props`
- `Rezepte.Import.PluginSdk/`
- `Rezepte.Import.Plugins.Chefkoch/`
- `Rezepte.Import.Plugins.SecondSource/`
- `Rezepte.Import.Plugins.ThirdSource/`
- `Rezepte.Import.Plugins.FourthSource/`
- `Rezepte.Import.Plugins.FifthSource/`
- `Rezepte.Import.Plugins.SixthSource/`
- `Rezepte.Import.Plugins.Tests/`
- `Rezepte.Import.PluginRunner/`
- `tests/fixtures/`
- `README.md`

## Validierung

1. Im Hauptrepository:
   - `dotnet test Rezepte.sln`
   - Erwartung: Host-, Backup- und Plugin-Discovery-Tests laufen ohne direkte Referenzen auf ausgelagerte Online-Plugins.
2. Im Plugin-Repository:
   - `dotnet test Rezepte.Import.Plugins.sln`
   - Erwartung: Parser-Tests fuer alle ausgelagerten Online-Plugins laufen erfolgreich.
3. Manueller Runner mit lokaler Datei:
   - Beispiel: `dotnet run --project Rezepte.Import.PluginRunner -- --plugin chefkoch --file tests/fixtures/chefkoch-recipe.html`
   - Erwartung: Rezeptdaten werden lesbar ausgegeben.
4. Manueller Runner mit URL:
   - Beispiel: `dotnet run --project Rezepte.Import.PluginRunner -- --plugin chefkoch --url <bekannte-chefkoch-url>`
   - Erwartung: Bei erreichbarer und passender Seite werden Rezeptdaten ausgegeben; bei nicht passender Seite erscheint eine klare Nicht-verarbeitbar-Meldung.
5. Host-Integration:
   - Gebautes Plugin nach `plugins/Rezepte.Import.Plugins.Chefkoch/` kopieren.
   - Host starten oder passenden Integrationstest ausfuehren.
   - Erwartung: Plugin wird geladen und ist ueber die bestehende Plugin-Verwaltung sichtbar.

## Risiken und Gegenmassnahmen

| Risiko | Auswirkung | Gegenmassnahme |
|--------|------------|----------------|
| Abstraktionsversion zwischen Host und Plugin weicht ab | Plugin laedt, scheitert aber zur Laufzeit oder wird inkompatibel | Vertrag im Hauptrepo stabil halten, Versionierung dokumentieren, spaeter internes NuGet-Paket verwenden |
| Live-Webseiten aendern Markup oder blockieren Abrufe | Manueller URL-Nachweis wird instabil | Lokale Fixture-Dateien als stabilen Mindestnachweis verwenden |
| SDK-Code bleibt im Abstraktionsprojekt | Host-Vertrag bleibt unnoetig breit und koppelt Parserlogik an Host | `UrlRecipeImportHandlerBase` in `Rezepte.Import.PluginSdk` auslagern |
| Hauptrepo-Tests referenzieren ausgelagerte Plugins weiter direkt | Auslagerung ist nur formal, nicht technisch | Parser-Tests verschieben und direkte `ProjectReference`s entfernen |
| Backup-Plugin wird versehentlich als Online-Quelle behandelt | Fachliche Abgrenzung wird unscharf | Backup im Hauptrepo belassen und separat testen |

## Reihenfolge der Umsetzung

1. Neues Plugin-Repository lokal strukturieren.
2. `Rezepte.Import.PluginSdk` anlegen und URL-/Parser-Basisklassen dorthin verschieben.
3. Online-Plugin-Projekte in das neue Repository uebernehmen und Referenzen anpassen.
4. Parser-Tests ins Plugin-Repository verschieben und dort gruen bekommen.
5. Console-Runner implementieren und mit Chefkoch-Fixture demonstrieren.
6. Hauptrepository von ausgelagerten Online-Plugin-Projekten entkoppeln.
7. Host-Tests aktualisieren und `dotnet test Rezepte.sln` ausfuehren.
8. Plugin-Publish-Struktur erzeugen und Host-Kompatibilitaet mit mindestens Chefkoch pruefen.
9. README/Build-Dokumentation im Plugin-Repository vervollstaendigen.

## Akzeptanznachweis

- Die Online-Rezeptabruf-Plugins liegen in einer eigenstaendigen Repository-Struktur und sind nicht mehr Teil von `Rezepte.sln`.
- Das Hauptrepository baut und testet ohne direkte Referenzen auf ausgelagerte Online-Plugin-Projekte.
- Das neue Plugin-Repository baut und testet die ausgelagerten Plugins eigenstaendig.
- Der Console-Runner kann eine lokale Datei verarbeiten.
- Der Console-Runner kann eine URL verarbeiten.
- Der Console-Runner erlaubt die Auswahl eines Plugins vor der Ausfuehrung.
- Der Console-Runner meldet nachvollziehbar, wenn das gewaehlte Plugin die Eingabe nicht verarbeiten kann.
- Der Console-Runner zeigt bei erfolgreicher Verarbeitung Rezeptdaten an.
- Mindestens `chefkoch` ist mit lokaler Fixture erfolgreich demonstrierbar.
- Ein gebautes externes Plugin kann vom bestehenden Host aus dem `plugins/`-Ordner geladen werden.

## Offene Punkte

Keine. Die zuvor offenen Punkte sind fuer die Umsetzung wie folgt entschieden: Arbeitstitel `rezepte-import-plugins-private`, Testprogramm im neuen Plugin-Repository, Mindestnachweis mit `chefkoch` und lokaler Fixture, `Backup` bleibt im Hauptrepository.
