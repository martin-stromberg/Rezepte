# Code-Review

## Status

Keine Befunde

## Pruefumfang

- Ausgefuehrt als Hauptagent, da in dieser Umgebung keine Unteragenten-Delegation verfuegbar war.
- Geprueft wurden die aktuellen Arbeitsbaum-Aenderungen gegen `HEAD`.
- Relevante funktionale Aenderungen:
  - `Rezepte.Web/Rezepte.Web.csproj:13` aktualisiert `Microsoft.EntityFrameworkCore.Design` auf `10.0.11`.
  - `Rezepte.Web/Rezepte.Web.csproj:17` aktualisiert `Microsoft.EntityFrameworkCore.Sqlite` auf `10.0.11`.
  - `Rezepte.Tests/Rezepte.Tests.csproj:17` aktualisiert `Microsoft.EntityFrameworkCore.InMemory` auf `10.0.11`.
  - `Rezepte.Tests/Rezepte.Tests.csproj:18` aktualisiert `Microsoft.Extensions.Caching.Memory` auf `10.0.11`.
  - `Rezepte.Tests/Rezepte.Tests.csproj:21` aktualisiert `bunit` auf `1.40.0`.
  - `Rezepte.Tests/Rezepte.Tests.csproj:22` pinnt `AngleSharp` auf `1.7.3`.

## Befunde

Keine.

## Hinweise

- Der direkte `AngleSharp`-Pin ist nachvollziehbar: Die lokal wiederhergestellten Metadaten von `bunit.web` `1.40.0` referenzieren fuer `net9.0` weiterhin `AngleSharp` `1.2.0`; ohne direkten Pin wuerde die bekannte verwundbare Version voraussichtlich im Testprojekt verbleiben.
- Die tatsaechliche Restore-Aufloesung fuer `SQLitePCLRaw.lib.e_sqlite3` ist `2.1.12` in `Rezepte.Web`, `Rezepte.Tests` und `Rezepte.Tests.Browser`. Der lokale Vulnerability-Scan meldet damit keine anfaelligen Pakete. Diese Abweichung ist im aktualisierten Plan und Testergebnis dokumentiert und bleibt kein Code-Review-Befund, solange der Security-Scan gruen bleibt.
- `Rezepte.Web/Extensions/LoggingExtensions.cs` erscheint im Arbeitsbaum als geaendert, hat aber keinen inhaltlichen Diff; `git ls-files --eol` zeigt gemischte Arbeitsbaum-Zeilenenden. Vor einem Commit sollte entschieden werden, ob diese reine Zeilenendungsmarkierung mit aufgenommen oder separat normalisiert wird.

## Verifikation

- `dotnet list Rezepte.sln package --include-transitive`: `AngleSharp` `1.7.3` in `Rezepte.Tests`; `SQLitePCLRaw.lib.e_sqlite3` `2.1.12` in Web-, Unit- und Browsertestpfad.
- `dotnet list Rezepte.sln package --vulnerable --include-transitive`: keine anfaelligen Pakete in den Projekten der Solution.
- `dotnet build Rezepte.sln --configuration Release --no-restore -p:TreatWarningsAsErrors=true`: erfolgreich, 0 Warnungen, 0 Fehler.
- Vollstaendige Testausfuehrung, Publish-Start und PR-CI-Nachweis wurden in diesem Schritt nicht ausgefuehrt; sie gehoeren zum separaten Lifecycle-Schritt `/run-tests`.
