# Bestandsaufnahme

## Zusammenfassung

Die Anforderung betrifft zwei transitive NuGet-Verwundbarkeiten. Der reproduzierte Scan auf `Rezepte.sln` meldet:

- `SQLitePCLRaw.lib.e_sqlite3` `2.1.11`, Severity High, GHSA-2m69-gcr7-jv3q
- `AngleSharp` `1.2.0`, Severity Moderate, GHSA-pgww-w46g-26qg

Die betroffenen Pakete werden nicht direkt referenziert. Die Paketauflosung stammt aus den direkten Referenzen in `Rezepte.Web/Rezepte.Web.csproj` (EF Core SQLite) und `Rezepte.Tests/Rezepte.Tests.csproj` (bUnit). Die Browsertests erben die SQLite-Abhangigkeit uber die Projektreferenz auf `Rezepte.Web`.

## Betroffene Bereiche

| Bereich | Bestand | Relevanz |
|---|---|---|
| Anwendung | `Rezepte.Web/Rezepte.Web.csproj:17` referenziert `Microsoft.EntityFrameworkCore.Sqlite` `10.0.9` | Erzeugt die SQLitePCLRaw-Abhangigkeiten zur Laufzeit |
| Unit-/Komponententests | `Rezepte.Tests/Rezepte.Tests.csproj:21` referenziert `bunit` `1.38.5` | Erzeugt die AngleSharp-Abhangigkeit; Testprojekt erbt ausserdem SQLite uber `Rezepte.Web` |
| Browsertests | `Rezepte.Tests.Browser/Rezepte.Tests.Browser.csproj:23` referenziert `Rezepte.Web` | Verwendet die aktualisierte Anwendung und deren SQLite-Abhangigkeiten |
| Datenzugriff | `Rezepte.Web/Extensions/ServiceCollectionExtensions.cs:54-56` | `UseSqlite` ist der zentrale produktive SQLite-Anschluss |
| Export-/Restore-Tests | `Rezepte.Tests/Services/ExportService*Tests.cs` | Nutzen `Microsoft.Data.Sqlite` mit In-Memory-Datenbanken und validieren Export, Restore und Systembackup |
| Browser-Testfixture | `Rezepte.Tests.Browser/Infrastructure/RezepteAppFixture.cs:10,100-155` | Startet die Webanwendung gegen eine temporaere SQLite-Datenbank |
| CI Security Gate | `.github/actions/security-scan/action.yml:25-40` | Fuehrt den blockierenden Vulnerability-Scan aus; Logik bleibt unveraendert |
| PR-/Staging-CI | `.github/workflows/pr-staging-ci.yml:80-100`, `.github/workflows/staging-ci.yml:80-92` | Restore, Security Gate und statische Analyse |
| Unabhangiger Scan | `.github/workflows/security-scan.yml:20-40` | Woechentlicher und manueller Scan derselben Solution |

## Empfohlene Umsetzung

1. Zuerst die direkte Paketversion `Microsoft.EntityFrameworkCore.Sqlite` beziehungsweise die daraus aufgeloste SQLitePCLRaw-Version auf eine Version mit behobener Sicherheitsmeldung anheben. Der aktuelle Outdated-Check meldet als neueste verfuegbare SQLitePCLRaw-Komponenten `3.0.5` und `SQLitePCLRaw.lib.e_sqlite3` `3.53.3`; die konkrete kompatible Kombination ist im Plan festzulegen.
2. `bunit` auf eine Version aktualisieren, die `AngleSharp` nicht mehr in `1.2.0` auflost, oder AngleSharp kontrolliert direkt pinnen, falls dies mit der Paketauflosung kompatibel ist.
3. `obj/project.assets.json` nicht manuell bearbeiten. Nach der Paketanderung Restore und vollstandigen Build ausfuehren.
4. Export-/Restore-Tests, die gesamte Solution-Testausfuehrung, Browser-Testbuild/-lauf und den Vulnerability-Scan erneut ausfuehren.
5. CI-Workflows und `.github/actions/security-scan/action.yml` nicht funktional anpassen; sie sind Nachweis- und Integrationsflaeche.

## Nicht betroffen

Es gibt keine zentrale Paketverwaltungsdatei und keine `packages.lock.json`. `Directory.Build.props` setzt nur Target Framework, Nullable/Implicit Usings und die Import-Contract-Version. Die Solution enthalt neben Web-, Unit- und Browsertests mehrere Import- und Testfixture-Projekte; der aktuelle Scan meldet dort keine verwundbaren Pakete.

## Detaildokumente

- [Paket- und Abhangigkeitskarte](inventory/package-dependency-map.md)
- [CI- und Validierungsflaeche](inventory/ci-validation-surface.md)
- [Laufzeit- und Testauswirkungen](inventory/runtime-test-impact.md)

## Offene Punkte fuer den Plan

- Welche konkrete `Microsoft.EntityFrameworkCore.Sqlite`-Version soll verwendet werden, damit die aufgeloeste SQLitePCLRaw-Familie kompatibel und nicht verwundbar ist?
- Soll `bunit` aktualisiert oder `AngleSharp` als direkte Testreferenz gepinnt werden?
- Sind Linux- und Windows-Runtime-Varianten der SQLite-Native-Binaries durch den Upgrade betroffen und muessen explizit im Build-/Browser-Test geprueft werden?
