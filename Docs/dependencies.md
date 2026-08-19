# Abhängigkeits- und Sicherheitsstrategie

Stand: 2026-07-01

## Transitive SQLitePCLRaw-Abhängigkeit

Die Anwendung verwendet SQLite über `Microsoft.EntityFrameworkCore.Sqlite`. Nach dem Upgrade auf .NET 10 und `Microsoft.EntityFrameworkCore.Sqlite` `10.0.9` wird transitiv `SQLitePCLRaw.lib.e_sqlite3` `2.1.11` aufgeloest.

`dotnet list Rezepte.sln package --vulnerable --include-transitive` meldet dafür weiterhin `NU1903` mit hoher Schwere und Advisory `GHSA-2m69-gcr7-jv3q` in `Rezepte.Web` und über die Projektreferenz auch in `Rezepte.Tests`.

## Paketstrategie

- Es wird keine direkte `PackageReference` auf `SQLitePCLRaw.lib.e_sqlite3` ergaenzt, solange NuGet.org keine hoehere stabile Version als `2.1.11` anbietet. Eine direkte Referenz auf dieselbe Version wuerde die Sicherheitswarnung nicht beheben und nur die transitive Abhängigkeit kuenstlich festschreiben.
- Die EF-Core-/SQLite-Pakete bleiben auf den neuesten kompatiblen Versionen. Sobald `Microsoft.EntityFrameworkCore.Sqlite`, `Microsoft.Data.Sqlite` oder die `SQLitePCLRaw`-Pakete eine Version bereitstellen, die die Advisory nicht mehr meldet, soll dieses Paketupdate bevorzugt eingespielt werden.
- `NU1903` wird nicht per Projektdatei unterdrückt. Restore, Build und Vulnerability-Checks sollen die Warnung weiter sichtbar machen, bis ein technischer Fix verfügbar ist.
- Die Nutzung von SQLite bleibt bestehen; ein Datenbank- oder Providerwechsel waere eine fachliche Architekturentscheidung und ist nicht Teil des .NET-10-Upgrades.

## Risikoakzeptanz

Das verbleibende Risiko wird für diesen Upgrade-Stand bewusst akzeptiert, weil aktuell kein fixbares stabiles NuGet-Paket verfügbar ist und Build, Tests sowie Publish erfolgreich bleiben. Vor produktiven Releases muss der Vulnerability-Check erneut ausgefuehrt und diese Entscheidung überprüft werden.
