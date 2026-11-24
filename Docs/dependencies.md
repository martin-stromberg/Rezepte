# Abhängigkeits- und Sicherheitsstrategie

Stand: 2026-11-24

## Behobene transitive Sicherheitsbefunde

Die Anwendung verwendet SQLite über `Microsoft.EntityFrameworkCore.Sqlite`. Das
Security-Scan-Gate hatte `SQLitePCLRaw.lib.e_sqlite3` `2.1.11` mit hoher
Schwere sowie `AngleSharp` `1.2.0` in den Testprojekten gemeldet.

Die direkten Ursprungspakete wurden aktualisiert:

- `Microsoft.EntityFrameworkCore.Sqlite` in `Rezepte.Web`: `10.0.15`
- `bunit` in `Rezepte.Tests`: `1.40.0`
- direkter `AngleSharp`-Pin in `Rezepte.Tests`: `1.7.3`

Die aktuelle Restore-Aufloesung verwendet `SQLitePCLRaw.lib.e_sqlite3`
`2.1.13` in Web-, Unit- und Browsertestprojekt sowie `AngleSharp` `1.7.3` im
Testprojekt. Der abschliessende Aufruf
`dotnet list Rezepte.sln package --vulnerable --include-transitive` meldet
keine verbleibenden verwundbaren Pakete.

## Paketstrategie

- Es wird keine direkte `PackageReference` auf `SQLitePCLRaw.lib.e_sqlite3` ergänzt. Die sichere transitive Version wird über das kompatible EF-Core-/SQLite-Update bezogen.
- Direkte Paketupdates sind projektbezogen zu prüfen. Nach jedem Update sind Restore, Build, Tests und der Vulnerability-Scan für die gesamte Solution auszuführen.
- `NU1903` wird nicht per Projektdatei unterdrückt. Der Security-Scan bleibt ein blockierender CI-Schritt.
- Neben dem blockierenden Security-Gate und dem wöchentlichen `security-scan.yml`-Lauf erzeugt der Workflow `.github/workflows/review-nuget-packages.yml` wöchentlich ein NuGet-Prüf-Issue (Updates, Schwachstellen, Pflegezustand, Breaking Changes) mit `@softwarewerkstatt`-Prüfauftrag; Details stehen in [help/github-actions.md](help/github-actions.md).
- Die Nutzung von SQLite bleibt bestehen; ein Datenbank- oder Providerwechsel waere eine fachliche Architekturentscheidung und ist nicht Teil des .NET-10-Upgrades.
- `Microsoft.*`-Direktreferenzen werden auf einem einheitlichen Stand innerhalb der .NET-10-Linie gehalten (aktuell `10.0.15`); der wöchentliche Review-Lauf gleicht Drift zwischen den Projekten aus.
- `AngleSharp`-Updates sind an die `bunit`-Version gekoppelt: `AngleSharp` ≥ `1.10.x` ist binär inkompatibel zu `bunit` `1.40.0` (`TypeLoadException`) und kann daher erst zusammen mit der `bunit`-2.x-Migration angehoben werden (Folge-Issue „Test-Stack-Migration: `xunit.v3` + `bunit` 2.x"). Ebenso aufgeschoben ist `QuestPDF` ≥ 2026.9.x wegen Breaking Changes in den Font-Einstellungen (`UseSystemFonts`, `ThrowOnMissing*`) — Umsetzung über ein separates Folge-Issue.

## Risikoakzeptanz

Für die beiden im Security-Scan gemeldeten Pakete besteht nach der
Aktualisierung keine dokumentierte Risikoakzeptanz mehr. Vor produktiven
Releases muss der Vulnerability-Check weiterhin erneut ausgeführt werden.
