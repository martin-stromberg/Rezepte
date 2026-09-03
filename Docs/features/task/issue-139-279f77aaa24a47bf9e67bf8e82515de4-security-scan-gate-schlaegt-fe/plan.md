# Umsetzungsplan

## Ziel

Die beiden im blockierenden CI-Security-Scan gemeldeten transitiven Pakete
`SQLitePCLRaw.lib.e_sqlite3` und `AngleSharp` werden durch Updates ihrer
direkten Ursprungspakete auf aktuelle, kompatible und nicht verwundbare
Versionen angehoben. Der bestehende Security-Gate und seine CI-Workflows
bleiben funktional unveraendert.

## Festgelegte Paketstrategie

- In `Rezepte.Web/Rezepte.Web.csproj` wird
  `Microsoft.EntityFrameworkCore.Sqlite` von `10.0.9` auf `10.0.11`
  aktualisiert. Erwartet wird damit `SQLitePCLRaw` `3.0.5` und
  `SQLitePCLRaw.lib.e_sqlite3` `3.53.3`.
- In `Rezepte.Tests/Rezepte.Tests.csproj` wird `bunit` von `1.38.5` auf
  `1.40.0` aktualisiert. Erwartet wird damit `AngleSharp` `1.7.3`.
- `AngleSharp` wird nicht direkt gepinnt, solange das bUnit-Update die
  erwartete sichere transitive Version aufloest. Eine direkte Referenz ist
  nur als dokumentierter Fallback zulaessig, falls Restore oder Tests eine
  nicht anderweitig loesbare Abhaengigkeitsinkompatibilitaet zeigen.
- Falls eine festgelegte Zielversion nicht aufloesbar oder weiterhin
  verwundbar ist, wird nicht ungeprueft fortgefahren. Stattdessen wird die
  naechste kompatible, nicht verwundbare Version derselben direkten
  Paketfamilie gewaehlt und die abweichende Aufloesung im Testergebnis
  dokumentiert.
- Generierte Dateien wie `obj/project.assets.json` werden ausschliesslich
  durch Restore erzeugt und nicht manuell bearbeitet oder committed.

Nach dem Restore muss `dotnet list Rezepte.sln package --include-transitive`
mindestens folgende Aufloesung ausweisen:

| Projekt | Transitives Paket | Erwartete Version |
|---|---|---:|
| `Rezepte.Web` | `SQLitePCLRaw.lib.e_sqlite3` | `3.53.3` |
| `Rezepte.Tests` | `SQLitePCLRaw.lib.e_sqlite3` | `3.53.3` |
| `Rezepte.Tests.Browser` | `SQLitePCLRaw.lib.e_sqlite3` | `3.53.3` |
| `Rezepte.Tests` | `AngleSharp` | `1.7.3` |

Die Kontrolle gilt als fehlgeschlagen, wenn eines der betroffenen Projekte
weiterhin `SQLitePCLRaw.lib.e_sqlite3` `2.1.11` oder `AngleSharp` `1.2.0`
aufloest, die erwartete Zielversion fehlt oder mehrere Versionen einschliesslich
einer verwundbaren Version im Abhaengigkeitsgraph verbleiben.

## Plattformentscheidung

Die native SQLite-Abhaengigkeit wird verbindlich auf Windows und Linux
validiert:

- Auf dem Windows-Entwicklungsrechner werden Restore, Release-Build,
  vollstaendige Tests, Publish und der vorhandene Playwright-Browserlauf
  ausgefuehrt. Publish und Start gegen die temporaere SQLite-Datei weisen die
  Windows-Native-Binaries nach.
- Der unveraenderte PR-CI-Lauf auf `ubuntu-latest` ist der verbindliche
  Linux-Nachweis. Seine Jobs `static checks` und `build & test` muessen gruen
  sein; Publish, Anwendungsstart und Playwright-Lauf weisen dabei die
  Linux-Native-Binaries nach.
- Ein neuer Betriebssystem-Matrix-Job wird nicht eingefuehrt. Windows ist der
  lokale Regressionsnachweis, Linux bleibt die fuer den Gate massgebliche
  CI-Plattform. Ohne erfolgreichen Nachweis auf beiden Plattformen ist die
  Paketaktualisierung nicht abgenommen.

## Umsetzungsschritte

1. Den Ausgangszustand vor der Aenderung mit
   `dotnet list Rezepte.sln package --vulnerable --include-transitive`
   erfassen und die beiden bekannten Befunde samt betroffenen Projekten
   bestaetigen.
2. Ausschliesslich die direkten Paketversionen in
   `Rezepte.Web/Rezepte.Web.csproj` und
   `Rezepte.Tests/Rezepte.Tests.csproj` gemaess der festgelegten
   Paketstrategie aktualisieren. CI-Dateien bleiben unveraendert.
3. Mit `dotnet restore Rezepte.sln` eine frische Paketaufloesung erzeugen.
   Anschliessend mit
   `dotnet list Rezepte.sln package --include-transitive` die oben definierte
   Versionsmatrix fuer Web-, Unit- und Browsertestprojekt pruefen und im
   Testergebnis festhalten.
4. Den vollstaendigen CI-Pfad des Jobs `static checks` in dessen Reihenfolge
   nachvollziehen:
   - `dotnet format Rezepte.sln --verify-no-changes --no-restore --severity error`
   - `dotnet list Rezepte.sln package --vulnerable --include-transitive`
   - `dotnet build Rezepte.sln --configuration Release --no-restore -p:TreatWarningsAsErrors=true`
5. Die drei CI-nahen Release-Builds aus `build & test` ausfuehren:
   - `dotnet build Rezepte.Web/Rezepte.Web.csproj --configuration Release --no-restore`
   - `dotnet build Rezepte.Tests/Rezepte.Tests.csproj --configuration Release --no-restore`
   - `dotnet build Rezepte.Tests.Browser/Rezepte.Tests.Browser.csproj --configuration Release --no-restore`
6. `Rezepte.Web` im Release-Modus mit `--no-restore` publizieren, den fuer den
   Build erzeugten Playwright-Chromium-Browser installieren und die
   vollstaendige Suite mit
   `dotnet test Rezepte.sln --configuration Release --no-build` ausfuehren.
   Dabei die Ergebnisse der Export-, Restore-, Systembackup-, Komponenten-/
   Render- und Browsertests explizit auf Regressionen auswerten. Der
   Browserlauf muss die publizierte Anwendung mit einer temporaeren
   SQLite-Datei erfolgreich starten.
7. Nach Build, Publish und Tests den blockierenden Scan erneut mit
   `dotnet list Rezepte.sln package --vulnerable --include-transitive`
   ausfuehren. Der Scan muss fuer die gesamte Solution ohne gemeldete
   verwundbare Pakete enden.
8. Nach Bereitstellung des Branches die unveraenderten PR-CI-Jobs
   `static checks` und `build & test` auf `ubuntu-latest` abwarten und ihre
   erfolgreiche Ausfuehrung dokumentieren. Damit werden der vollstaendige
   Format-/Security-/Static-Analysis-Gate, die Coverage-Schwelle sowie der
   Linux-Publish-, SQLite-Start- und Playwright-Pfad bestaetigt.

## Betroffene Dateien

Vorgesehene produktive Aenderungen:

- `Rezepte.Web/Rezepte.Web.csproj`
- `Rezepte.Tests/Rezepte.Tests.csproj`

Ausdruecklich nicht zu aendern:

- `.github/actions/security-scan/action.yml`
- `.github/workflows/pr-staging-ci.yml`
- `.github/workflows/staging-ci.yml`
- `.github/workflows/security-scan.yml`
- `obj/project.assets.json` und andere generierte Buildartefakte

Weitere Projekt- oder Quelldateien duerfen nur angepasst werden, wenn eine
konkret reproduzierte API- oder Kompatibilitaetsaenderung des Paketupdates dies
erfordert. Solche Folgeaenderungen muessen eng begrenzt und durch einen
gezielten Regressionstest abgesichert werden.

## Test- und Abnahmekriterien

- Der transitive Paketnachweis mit `--include-transitive` bestaetigt die
  festgelegte Versionsmatrix in allen betroffenen Projekten und enthaelt keine
  alte verwundbare Parallelversion.
- `dotnet format Rezepte.sln --verify-no-changes --no-restore --severity error`
  ist erfolgreich.
- Der Release-Build der gesamten Solution mit
  `TreatWarningsAsErrors=true` sowie die drei projektweisen CI-Builds sind
  erfolgreich.
- `dotnet test Rezepte.sln --configuration Release --no-build` ist vollstaendig
  erfolgreich; insbesondere bestehen Export-, Restore-, Systembackup-,
  Komponenten-/Render- und Browsertests.
- Der abschliessende Vulnerability-Scan meldet weder
  `SQLitePCLRaw.lib.e_sqlite3` noch `AngleSharp` und insgesamt keine
  verbleibende Verwundbarkeit.
- Publish, Anwendungsstart mit temporaerer SQLite-Datei und Playwright-Lauf
  bestehen auf Windows lokal und auf Linux im unveraenderten
  `ubuntu-latest`-PR-CI-Lauf.
- Die PR-CI-Jobs `static checks` und `build & test` sind gruen; damit bleibt
  auch die vorhandene Coverage-Schwelle erfuellt.
- An Security-Scan-Action und CI-Workflows gibt es keine funktionale Aenderung.

## E2E-Abdeckung

Es ist kein neuer fachlicher UI-E2E-Test erforderlich, da weder UI,
Navigation, Sichtbarkeit, Berechtigungen noch Benutzerinteraktionen geaendert
werden. Die vorhandene Playwright-Suite bleibt jedoch ein verpflichtender
technischer E2E-Regressionsnachweis: Sie publiziert und startet die Anwendung
mit einer temporaeren SQLite-Datei und validiert damit die native
SQLite-Laufzeit in einem realen Anwendungsprozess.

## Risiken und Gegenmassnahmen

- Das SQLitePCLRaw-Upgrade kann Auswahl und Laden nativer Runtime-Binaries
  beeinflussen. Dem begegnen die verpflichtenden Publish-, Start- und
  Browsernachweise auf Windows und Linux sowie die SQLite-basierten Export-,
  Restore- und Systembackup-Tests.
- Unterschiedliche transitive Versionen zwischen Web-, Unit- und
  Browsertestprojekt koennen einen Teilpfad weiterhin verwundbar lassen. Die
  explizite projektbezogene Ausgabe von `--include-transitive` verhindert eine
  Abnahme anhand nur direkter Paketlisten.
- Das bUnit-Upgrade kann Komponenten- oder Renderverhalten im Teststack
  veraendern. Die vollstaendige Testsuite ist deshalb verbindlich; produktive
  AngleSharp-Verwendungen bestehen laut Bestandsaufnahme nicht.
- Ein gruenes lokales Scanergebnis allein bildet den CI-Gate nicht vollstaendig
  ab. Format-Check, statischer Build mit Warnungen als Fehler und die beiden
  realen PR-CI-Jobs sind deshalb eigenstaendige Abnahmekriterien.

## Offene Punkte

Keine. Paketstrategie, transitiver Versionsnachweis, vollstaendiger
`static checks`-Pfad und Plattformabdeckung sind festgelegt.
