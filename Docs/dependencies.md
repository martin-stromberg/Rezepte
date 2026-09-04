# Abhängigkeits- und Sicherheitsstrategie

Stand: 2026-09-03

## Behobene transitive Sicherheitsbefunde

Die Anwendung verwendet SQLite über `Microsoft.EntityFrameworkCore.Sqlite`. Das
Security-Scan-Gate hatte `SQLitePCLRaw.lib.e_sqlite3` `2.1.11` mit hoher
Schwere sowie `AngleSharp` `1.2.0` in den Testprojekten gemeldet.

Die direkten Ursprungspakete wurden aktualisiert:

- `Microsoft.EntityFrameworkCore.Sqlite` in `Rezepte.Web`: `10.0.11`
- `bunit` in `Rezepte.Tests`: `1.40.0`
- direkter `AngleSharp`-Pin in `Rezepte.Tests`: `1.7.3`

Die aktuelle Restore-Aufloesung verwendet `SQLitePCLRaw.lib.e_sqlite3`
`2.1.12` in Web-, Unit- und Browsertestprojekt sowie `AngleSharp` `1.7.3` im
Testprojekt. Der abschliessende Aufruf
`dotnet list Rezepte.sln package --vulnerable --include-transitive` meldet
keine verbleibenden verwundbaren Pakete.

## Paketstrategie

- Es wird keine direkte `PackageReference` auf `SQLitePCLRaw.lib.e_sqlite3` ergänzt. Die sichere transitive Version wird über das kompatible EF-Core-/SQLite-Update bezogen.
- Direkte Paketupdates sind projektbezogen zu prüfen. Nach jedem Update sind Restore, Build, Tests und der Vulnerability-Scan für die gesamte Solution auszuführen.
- `NU1903` wird nicht per Projektdatei unterdrückt. Der Security-Scan bleibt ein blockierender CI-Schritt.
- Die Nutzung von SQLite bleibt bestehen; ein Datenbank- oder Providerwechsel waere eine fachliche Architekturentscheidung und ist nicht Teil des .NET-10-Upgrades.

## Risikoakzeptanz

Für die beiden im Security-Scan gemeldeten Pakete besteht nach der
Aktualisierung keine dokumentierte Risikoakzeptanz mehr. Vor produktiven
Releases muss der Vulnerability-Check weiterhin erneut ausgeführt werden.
