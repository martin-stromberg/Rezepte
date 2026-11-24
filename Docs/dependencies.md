# Abhängigkeits- und Sicherheitsstrategie

Stand: 2026-11-24

## Behobene transitive Sicherheitsbefunde

Die Anwendung verwendet SQLite über `Microsoft.EntityFrameworkCore.Sqlite`. Das
Security-Scan-Gate hatte `SQLitePCLRaw.lib.e_sqlite3` `2.1.11` mit hoher
Schwere sowie `AngleSharp` `1.2.0` in den Testprojekten gemeldet.

Die direkten Ursprungspakete wurden aktualisiert:

- `Microsoft.EntityFrameworkCore.Sqlite` in `Rezepte.Web`: `11.0.0`
- `bunit` in `Rezepte.Tests`: `2.15.2`
- direkter `AngleSharp`-Pin in `Rezepte.Tests`: `1.10.5`

Die aktuelle Restore-Aufloesung verwendet `SQLitePCLRaw.lib.e_sqlite3`
`2.1.13` in Web-, Unit- und Browsertestprojekt sowie `AngleSharp` `1.10.5` im
Testprojekt. Der abschliessende Aufruf
`dotnet list Rezepte.sln package --vulnerable --include-transitive` meldet
keine verbleibenden verwundbaren Pakete.

## Paketstrategie

- Es wird keine direkte `PackageReference` auf `SQLitePCLRaw.lib.e_sqlite3` ergänzt. Die sichere transitive Version wird über das kompatible EF-Core-/SQLite-Update bezogen.
- Direkte Paketupdates sind projektbezogen zu prüfen. Nach jedem Update sind Restore, Build, Tests und der Vulnerability-Scan für die gesamte Solution auszuführen.
- `NU1903` wird nicht per Projektdatei unterdrückt. Der Security-Scan bleibt ein blockierender CI-Schritt.
- Neben dem blockierenden Security-Gate und dem wöchentlichen `security-scan.yml`-Lauf erzeugt der Workflow `.github/workflows/review-nuget-packages.yml` wöchentlich ein NuGet-Prüf-Issue (Updates, Schwachstellen, Pflegezustand, Breaking Changes) mit `@softwarewerkstatt`-Prüfauftrag; Details stehen in [help/github-actions.md](help/github-actions.md).
- Die Nutzung von SQLite bleibt bestehen; ein Datenbank- oder Providerwechsel waere eine fachliche Architekturentscheidung und ist nicht Teil des .NET-11-Upgrades.
- `Microsoft.*`-Direktreferenzen werden auf einem einheitlichen Stand innerhalb der .NET-11-Linie gehalten (aktuell `11.0.0`); der wöchentliche Review-Lauf gleicht Drift zwischen den Projekten aus.
- `Serilog.AspNetCore` wurde major-parallel zur .NET-Linie auf `11.0.0` angehoben; die Sinks (`Serilog.Sinks.Console` `6.1.1`, `Serilog.Sinks.File` `7.0.0`) bleiben unverändert.

## Test-Stack (xunit.v3/bunit, Issue #228)

Der Test-Stack wurde im Zuge der .NET-11-Migration umgestellt:

- `xunit` `2.9.3` → `xunit.v3` `4.0.3` in `Rezepte.Tests` und `Rezepte.Tests.Browser`; `xunit.runner.visualstudio` `4.0.1` und `Microsoft.NET.Test.Sdk` `18.12.0` bleiben unverändert — `dotnet test` läuft weiterhin über den VSTest-Pfad (bewusst kein `test.runner`-Eintrag in `global.json`, damit die CI-Filter-, TRX- und Coverage-Aufrufe unverändert bleiben).
- `Xunit.SkippableFact` wurde entfernt; Laufzeit-Skips nutzen das in `xunit.v3` eingebaute `Assert.SkipUnless`.
- `bunit` `1.40.0` → `2.15.2`; `AngleSharp` bleibt als direkter Pin auf `1.10.5` — dieselbe Minor-Linie wie die von `bunit` 2.x deklarierte `AngleSharp`-Untergrenze (`>= 1.10.1`). Ein Sprung auf `1.11.x` ist bewusst unterblieben, um Binärkompatibilität zur bunit-Build-Basis sicherzustellen.
- Übrige Test-Pakete unverändert: `coverlet.collector` `10.1.1`, `Microsoft.Playwright` `1.65.1`, `Moq` `4.21.0`, `FluentAssertions` `8.11.0`.

## QuestPDF-Font-Strategie (Issue #229)

`QuestPDF` wurde auf `2026.9.2` angehoben; die Font-Auflösung ist deterministisch konfiguriert (`Rezepte.Web/Configuration/QuestPdfConfiguration.cs`, aufgerufen aus `Program.cs` vor `builder.Build()`):

- `Settings.License = LicenseType.Community`, `UseSystemFonts = false`, `ThrowOnMissingFontFamilies = true`, `ThrowOnMissingTextGlyphs = false`.
- Basisschrift ist das im QuestPDF-Paket gebündelte Lato (`QuestPDF.Fonts.Lato.br`), Emoji-Fallback ist die deployte monochrome Noto-Emoji-Schrift (`Rezepte.Web/Assets/Fonts/NotoEmoji-Regular.ttf`, OFL-Lizenz — `OFL.txt` liegt bei; kopiert nach `Fonts/` im Build- und Publish-Output). `DefaultTextStyle` setzt `FontFamily("Lato", "Noto Emoji")`.
- Fehlende Font-Familien sind Konfigurationsfehler und werfen (Fail-fast); fehlende Einzelglyphen aus Nutzerdaten rendern als Tofu-Platzhalter statt das PDF komplett ausfallen zu lassen.

## msTools.Updater

Das lokale Paket `lib/nuget/msTools.Updater.0.10.4.nupkg` enthält ausschließlich ein `lib/net10.0`-Asset. NuGet löst es als nächstniedrigeres kompatibles `netcoreapp`-Asset NuGet-konform für `net11.0` auf (verifiziert über Restore/Build). Bezug in `Rezepte.Web`, `Rezepte.Tests` und `Rezepte.Updater.TestHost`. Bei künftigen Inkompatibilitäten ist ein `net11.0`-Asset vom Paket-Eigentümer anzufordern.

## Risikoakzeptanz

Für die beiden im Security-Scan gemeldeten Pakete besteht nach der
Aktualisierung keine dokumentierte Risikoakzeptanz mehr. Vor produktiven
Releases muss der Vulnerability-Check weiterhin erneut ausgeführt werden.
