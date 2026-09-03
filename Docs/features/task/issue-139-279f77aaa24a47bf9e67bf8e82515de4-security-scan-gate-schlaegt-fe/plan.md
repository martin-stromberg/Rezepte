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
  aktualisiert. Massgeblich ist nicht eine vorab angenommene SQLitePCLRaw-
  Zielversion, sondern die durch `dotnet restore` tatsaechlich aufgeloeste
  Version. Diese muss projektbezogen dokumentiert und gegen den
  Vulnerability-Scan geprueft werden.
- In `Rezepte.Tests/Rezepte.Tests.csproj` wird `bunit` von `1.38.5` auf
  `1.40.0` aktualisiert.
- `AngleSharp` wird in `Rezepte.Tests/Rezepte.Tests.csproj` direkt auf
  `1.7.3` gepinnt. Begruendung: Die lokal wiederhergestellten Metadaten von
  `bunit.web` `1.40.0` referenzieren fuer den relevanten Testpfad weiterhin
  `AngleSharp` `1.2.0`; der direkte Pin hebt die Testauflösung auf die sichere
  Version, ohne produktiven Code zu veraendern.
- Falls eine festgelegte Zielversion nicht aufloesbar oder weiterhin
  verwundbar ist, wird nicht ungeprueft fortgefahren. Stattdessen wird die
  naechste kompatible, nicht verwundbare Version derselben direkten
  Paketfamilie gewaehlt und die abweichende Aufloesung im Testergebnis
  dokumentiert.
- Generierte Dateien wie `obj/project.assets.json` werden ausschliesslich
  durch Restore erzeugt und nicht manuell bearbeitet oder committed.

Nach dem Restore muss `dotnet list Rezepte.sln package --include-transitive`
die tatsaechliche Aufloesung fuer Web-, Unit- und Browsertestprojekt
ausweisen. Die aktuell belegte sichere Matrix lautet:

| Projekt | Transitives Paket | Aufgeloeste sichere Version |
|---|---|---:|
| `Rezepte.Web` | `SQLitePCLRaw.lib.e_sqlite3` | `2.1.12` |
| `Rezepte.Tests` | `SQLitePCLRaw.lib.e_sqlite3` | `2.1.12` |
| `Rezepte.Tests.Browser` | `SQLitePCLRaw.lib.e_sqlite3` | `2.1.12` |
| `Rezepte.Tests` | `AngleSharp` | `1.7.3` |

Die Kontrolle gilt als fehlgeschlagen, wenn eines der betroffenen Projekte
weiterhin `SQLitePCLRaw.lib.e_sqlite3` `2.1.11` oder `AngleSharp` `1.2.0`
aufloest oder mehrere Versionen einschliesslich einer verwundbaren Version im
Abhaengigkeitsgraph verbleiben. Eine von frueheren Planannahmen abweichende,
aber nicht verwundbare Aufloesung ist zulaessig, wenn Version,
Aufloesungspfad und Vulnerability-Scan im Testergebnis festgehalten werden.

## Plattformentscheidung

Die native SQLite-Abhaengigkeit wird verbindlich auf Windows und Linux
validiert:

- Auf dem Windows-Entwicklungsrechner werden Restore, Release-Build,
  vollstaendige Tests, Publish und der vorhandene Playwright-Browserlauf
  ausgefuehrt. Der Publish-Pfad ist
  `Rezepte.Web/bin/Release/net10.0/publish`. Der Browserlauf muss diesen
  publizierten Prozess erreichen; die dabei verwendete temporaere SQLite-Datei
  wird im Testergebnis als Nachweis fuer das Laden der Windows-Native-Binaries
  dokumentiert.
- Der unveraenderte PR-CI-Lauf auf `ubuntu-latest` ist der verbindliche
  Linux-Nachweis. Seine Jobs `static checks` und `build & test` muessen gruen
  sein; Publish, Anwendungsstart und Playwright-Lauf weisen dabei die
  Linux-Native-Binaries nach.
- Ein neuer Betriebssystem-Matrix-Job wird nicht eingefuehrt. Windows ist der
  lokale Regressionsnachweis, Linux bleibt die fuer den Gate massgebliche
  CI-Plattform. Ohne erfolgreichen Nachweis auf beiden Plattformen ist die
  Paketaktualisierung nicht abgenommen.
- Ist beim lokalen Lifecycle-Lauf noch kein PR-CI-Ergebnis verfuegbar, bleibt
  die Linux-Abnahme explizit offen. Sie darf erst nach einem realen
  PR-CI-Lauf mit den gruenen Jobs `static checks` und `build & test` als
  erledigt dokumentiert werden.

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
   Testergebnis festhalten. Bei jeder von frueheren Erwartungen abweichenden
   sicheren Aufloesung sind Version, Aufloesungspfad und Begruendung zu
   dokumentieren.
4. Den vollstaendigen CI-Pfad des Jobs `static checks` in dessen Reihenfolge
   nachvollziehen:
   - `dotnet format Rezepte.sln --verify-no-changes --no-restore --severity error`
   - `dotnet list Rezepte.sln package --vulnerable --include-transitive`
   - `dotnet build Rezepte.sln --configuration Release --no-restore -p:TreatWarningsAsErrors=true`
5. Die drei CI-nahen Release-Builds aus `build & test` ausfuehren:
   - `dotnet build Rezepte.Web/Rezepte.Web.csproj --configuration Release --no-restore`
   - `dotnet build Rezepte.Tests/Rezepte.Tests.csproj --configuration Release --no-restore`
   - `dotnet build Rezepte.Tests.Browser/Rezepte.Tests.Browser.csproj --configuration Release --no-restore`
6. `Rezepte.Web` im Release-Modus mit `--no-restore` nach
   `Rezepte.Web/bin/Release/net10.0/publish` publizieren, den fuer den Build
   erzeugten Playwright-Chromium-Browser installieren und die vollstaendige
   Suite mit dem CI-Kommando
   `dotnet test Rezepte.sln --configuration Release --no-build --collect:"XPlat Code Coverage" --logger "trx;LogFileName=test-results.trx" --logger "console;verbosity=normal"`
   ausfuehren. Dabei die Ergebnisse der Export-, Restore-, Systembackup-,
   Komponenten-/Render- und Browsertests explizit auf Regressionen auswerten.
   Der Browserlauf muss die publizierte Anwendung mit einer temporaeren
   SQLite-Datei erfolgreich starten; Publish-Pfad, Prozessstart und SQLite-
   Datei sind im Testergebnis festzuhalten.
7. Nach Build, Publish und Tests den blockierenden Scan erneut mit
   `dotnet list Rezepte.sln package --vulnerable --include-transitive`
   ausfuehren. Der Scan muss fuer die gesamte Solution ohne gemeldete
   verwundbare Pakete enden.
8. Die Coverage-Erfassung wie im CI-Job auswerten:
   - `dotnet tool install -g dotnet-reportgenerator-globaltool --version 5.5.11`
   - `reportgenerator "-reports:**/TestResults/**/coverage.cobertura.xml" "-targetdir:coverage-report" "-reporttypes:TextSummary"`
   - die `Line coverage` aus `coverage-report/Summary.txt` gegen die
     70-%-Schwelle pruefen und im Testergebnis dokumentieren.
9. Den Rezepte-spezifischen Contract-Export wie im CI-Job ausfuehren. Wenn
   `contract-baselines/import-contract` existiert, muss
   `Microsoft.DotNet.ApiCompat.Tool` installiert und
   `scripts/Export-ImportContract.ps1` mit `-ApiCompatBaselineDirectory` und
   `-ApiCompatToolPath` aufgerufen werden. Ergebnis, Baseline-Version und
   ApiCompat-Befund sind im Testergebnis zu dokumentieren.
10. Nach Bereitstellung des Branches die unveraenderten PR-CI-Jobs
   `static checks` und `build & test` auf `ubuntu-latest` abwarten und ihre
   erfolgreiche Ausfuehrung dokumentieren. Konkret muessen im Job
   `static checks` die Steps `Restore`, `Format check`, `Security scan` und
   `Static analysis` gruen sein. Im Job `build & test` muessen mindestens
   `Restore`, die drei Build-Steps, `Install Playwright browsers`,
   `Publish application for browser tests`, `Test`,
   `Generate coverage report`, `Enforce coverage threshold` und
   `Export import plugin contract` gruen sein. Damit werden der vollstaendige
   Format-/Security-/Static-Analysis-Gate, die Coverage-Schwelle sowie der
   Linux-Publish-, SQLite-Start- und Playwright-Pfad bestaetigt. Liegt dieser
   CI-Lauf noch nicht vor oder ist er fehlgeschlagen, bleibt die Plattform-
   Abnahme offen und darf nicht als erledigt behauptet werden.

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
  tatsaechlich wiederhergestellte, nicht verwundbare Versionsmatrix in allen
  betroffenen Projekten und enthaelt keine alte verwundbare Parallelversion.
- `dotnet format Rezepte.sln --verify-no-changes --no-restore --severity error`
  ist erfolgreich.
- Der Release-Build der gesamten Solution mit
  `TreatWarningsAsErrors=true` sowie die drei projektweisen CI-Builds sind
  erfolgreich.
- Das CI-Testkommando mit Coverage-Collection ist vollstaendig erfolgreich;
  insbesondere bestehen Export-, Restore-, Systembackup-, Komponenten-/Render-
  und Browsertests.
- Der ReportGenerator-Summary weist mindestens 70 % Line Coverage aus.
- `scripts/Export-ImportContract.ps1` laeuft mit ApiCompat gegen die vorhandene
  Baseline erfolgreich durch.
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
