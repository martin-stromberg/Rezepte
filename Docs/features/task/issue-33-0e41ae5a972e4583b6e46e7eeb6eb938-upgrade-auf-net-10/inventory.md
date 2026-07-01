# Bestandsaufnahme: Upgrade auf .NET 10

Datum: 2026-07-01

## Kurzfazit

Das Repository enthaelt eine .NET-Solution mit zwei Projekten:

- `Rezepte.Web`: Blazor/Web-Anwendung, aktuell `net9.0`
- `Rezepte.Tests`: xUnit-Testprojekt, aktuell `net9.0`, mit Referenz auf `Rezepte.Web`

Eine lokale .NET-10-Toolchain ist vorhanden (`dotnet --info`: SDK `10.0.301`, Host `10.0.9`). Es gibt keine `global.json`, keine zentrale Paketverwaltung, keine `NuGet.config`, keine Lockfiles, keine Docker-Dateien und keine ausfuehrbaren CI-Workflowdateien. Alte .NET-9-Verweise stehen in Projektdateien sowie in `README.md`, `Docs/install.md` und `Docs/Anforderungskatalog.md`.

## Detaildokumente

- [Projekt- und Frameworkbestand](inventory/projects-and-frameworks.md)
- [NuGet-Abhaengigkeiten](inventory/dependencies.md)
- [Build- und Test-Baseline](inventory/build-and-tests.md)
- [Konfiguration, CI, Deployment und Dokumentation](inventory/config-ci-docs.md)

## Relevante Befunde fuer die Planung

| Bereich | Befund | Konsequenz fuer .NET-10-Upgrade |
|---|---|---|
| Target Frameworks | Beide `.csproj` verwenden `net9.0`. | Auf `net10.0` umstellen, sofern keine begruendete Ausnahme entsteht. |
| SDK | Lokal ist .NET SDK `10.0.301` installiert; `global.json` fehlt. | Build kann lokal bereits mit .NET 10 SDK laufen; Planung muss entscheiden, ob eine `global.json` noetig ist. |
| NuGet direkt | 15 direkte Paketverweise, davon 14 mit neueren Versionen laut `dotnet list package --outdated`; `Moq` wurde nicht als veraltet gemeldet. | Paket-Upgrade projektweise planen, besonders Microsoft-/EF-Core-Pakete auf `10.0.9`. |
| NuGet transitiv | `SQLitePCLRaw.lib.e_sqlite3` `2.1.10` ist transitiv in beiden Projekten vorhanden und wird als vulnerabel gemeldet. | Sicherheitsfix voraussichtlich ueber Aktualisierung der EF-Core/SQLite-Kette oder explizite Paketsteuerung pruefen. |
| Build | `dotnet build Rezepte.sln --no-restore -clp:Summary` erfolgreich, 0 Warnungen im inkrementellen Build. | Baseline ist baubar. Nach Upgrade Full Build/Clean Build einplanen. |
| Tests | `dotnet test Rezepte.sln --no-restore` erfolgreich: 33/33 Tests bestanden. Beim Testbuild wurden bestehende Warnungen ausgegeben. | Tests sind als Regression-Baseline nutzbar; Warnungen koennen sich mit .NET 10/Paketupdates veraendern. |
| CI | `.github/copilot-instructions.md` beschreibt erwartete CI-Schritte, aber es gibt keinen Workflow unter `.github/workflows`. | Kein CI-Workflow zu aktualisieren; Dokumentation/Anweisungen ggf. synchronisieren. |
| Deployment-Doku | `README.md` und `Docs/install.md` referenzieren `net9.0`. | Dokumentationsverweise nach erfolgreichem Upgrade auf `net10.0` anpassen. |
| Docker | Keine Docker- oder Compose-Dateien gefunden. | Kein Container-Basisimage zu migrieren. |

## Ausgefuehrte Inventarisierungsbefehle

- `rg --files`
- `rg -n "TargetFramework|TargetFrameworks|PackageReference|Sdk=|global.json|Microsoft.NET.Sdk|net[0-9]|LangVersion|Nullable|ImplicitUsings" ...`
- `dotnet --info`
- `dotnet list Rezepte.sln package --outdated`
- `dotnet list Rezepte.sln package --include-transitive`
- `dotnet list Rezepte.sln package --vulnerable --include-transitive`
- `dotnet build Rezepte.sln --no-restore -clp:Summary`
- `dotnet test Rezepte.sln --no-restore`

## Abgrenzung dieser Bestandsaufnahme

Es wurden keine Produktionsdateien geaendert. Die ermittelten neuesten Paketversionen stammen aus dem lokalen `dotnet list package --outdated`-Lauf gegen `https://api.nuget.org/v3/index.json` am 2026-07-01 und sollten bei der Umsetzung erneut verifiziert werden.
