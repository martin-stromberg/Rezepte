# Fachliche Zusammenfassung

Die bisher verwendete Version `msTools.Updater` `0.7.0-rc.11` soll durch die bereitgestellte Version `0.10.0` ersetzt werden. Der Versionswechsel betrifft alle Projekte, die den Updater als NuGet-Paket referenzieren. Das Paket `msTools.Updater.0.10.0.nupkg` liegt bereits in der lokalen Paketquelle `lib/nuget`.

## Betroffene Klassen und Komponenten

- `Rezepte.Web/Rezepte.Web.csproj`: `PackageReference` auf `msTools.Updater` aktualisieren
- `Rezepte.Tests/Rezepte.Tests.csproj`: `PackageReference` auf `msTools.Updater` aktualisieren
- `Rezepte.Updater.TestHost/Rezepte.Updater.TestHost.csproj`: `PackageReference` auf `msTools.Updater` aktualisieren
- `lib/nuget/msTools.Updater.0.10.0.nupkg`: bereitgestelltes Updater-Paket als neue lokale Paketversion
- Bestehende Updater-Tests und Build-Konfigurationen: auf Kompatibilität mit der neuen Paketversion prüfen

## Implementierungsansatz

Die drei bestehenden `PackageReference`-Einträge für `msTools.Updater` werden von `0.7.0-rc.11` auf `0.10.0` geändert. Anschließend werden die Abhängigkeiten aus der in `NuGet.config` konfigurierten lokalen Quelle `lib/nuget` wiederhergestellt sowie Build und vorhandene Tests ausgeführt. Änderungen an Anwendungsklassen, Schnittstellen oder Benutzeroberflächen sind aus der Anforderung nicht ableitbar.

## Konfiguration

Es ist keine neue Anwendungskonfiguration erforderlich. Die bestehende lokale Paketquelle `lib/nuget` wird weiterverwendet.

## Offene Fragen

Keine offenen Fragen.
