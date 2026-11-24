# Bestandsaufnahme: [Review] NuGet-Paketprüfung – 2026-11-23 (Issue #227)

Bestandsaufnahme aller NuGet-Paketreferenzen der `Rezepte.sln` (10 Projekte),
der Paketquellen (`NuGet.config`, lokaler Feed `lib/nuget`), der Paketstrategie
(`docs/dependencies.md`) und der prüfrelevanten CI-Workflows — bezogen auf die
turnusmäßige Paketprüfung inklusive `Microsoft.*`-Versionsdrift-Vereinheitlichung.
Es handelt sich um einen Wartungslauf ohne Datenmodell-/Logik-Änderung; die
Inventarisierung fokussiert daher auf Paketstände, Feeds und Test-Baseline.

## Zusammenfassung

- **37 direkte `<PackageReference>`-Einträge** auf 28 Pakete in 6 von 10 Projekten
  (`Rezepte.Web` 9, `Rezepte.Tests` 12, `Rezepte.Tests.Browser` 7,
  `Rezepte.Import.Plugins.AIFoto` 5, `Rezepte.Import.Plugins.AIUrl` 2,
  `Rezepte.Updater.TestHost` 2); 4 Projekte ohne Paketreferenzen.
- **`Microsoft.*`-Versionsdrift bestätigt**: drei Stände der .NET-10-Linie —
  `10.0.0` (`Microsoft.Extensions.Hosting`), `10.0.9` (ASP.NET/Extensions in Web,
  Tests und Plugins), `10.0.11` (EF Core + `Caching.Memory`). Keine
  `System.*`-Direktreferenzen.
- **Vulnerability-Befund: keiner.** `dotnet list Rezepte.sln package --vulnerable
  --include-transitive` (identisch zum blockierenden CI-Gate) meldet für alle
  10 Projekte keine verwundbaren Pakete — konsistent mit `docs/dependencies.md`.
- **Deprecation-Befund:** `xunit` 2.9.3 wird in beiden Testprojekten als
  `Legacy`/`deprecated` gemeldet (Alternative `xunit.v3`).
- **Outdated-Snapshot erfasst** (Stichtag 2026-11-24): u. a. `Microsoft.NET.Test.Sdk`
  18.7.0→18.12.0, `Moq`→4.21.0, `FluentAssertions`→8.11.0, `coverlet.collector`→10.1.1,
  `Microsoft.Playwright`→1.65.1, `Xunit.SkippableFact`→1.5.85, `QuestPDF`→2026.9.2;
  Major-Sprünge `bunit`→2.x, `xunit.runner.visualstudio`→4.x und sämtliche
  `Microsoft.*`→11.0.0 (außerhalb der erlaubten .NET-10-Linie). Die CLI listet nur
  die absolute neueste Version; die höchste `10.0.x` ist separat zu ermitteln.
- **`msTools.Updater` 0.10.4** liegt nur im lokalen Feed `lib/nuget`
  (`msTools.Updater.0.10.4.nupkg`) und ist über nuget.org nicht bewertbar.
- **Build-Reihenfolgen-Falle verifiziert**: `dotnet test` auf sauberem Stand
  scheitert, weil `AIFoto`/`AIUrl` `Rezepte.Web.dll` per `HintPath` referenzieren;
  CI-Reihenfolge (erst `Rezepte.Web` bauen) ist verbindlich.
- **Test-Ausgangszustand: vollständig grün.** `Rezepte.Tests` 774/774 bestanden,
  `Rezepte.Tests.Browser` 35/35 bestanden (ungefilterte lokale Läufe, inkl.
  `OsInterface`-Teilmenge); kein nachgewiesener Testfehlschlag. Details und
  Nachweise in [inventory/tests.md](inventory/tests.md).

## Details

- [Paketinventar: Referenzen, Feeds, Drift, Befund-Snapshot](inventory/packages.md)
- [CI-/Workflow-Inventar: Review-Workflow, Security-Gates, Test-Lanes](inventory/ci.md)
- [Tests und Test-Ausgangszustand](inventory/tests.md)
