# Betriebsdokumentation mit .NET-10-Bezügen

Bestandsaufnahme der aktuellen Dokumentation (ohne historische `Docs/features/`- und `Docs/projects/`-Aufzeichnungen) mit Verweisen auf `net10.0`, `10.0.x`, `10.0.15`, .NET 10 bzw. dazugehörige Paket-/Befehlsnamen.

## `Docs/install.md`

| Zeilen | Inhalt |
|--------|--------|
| 10 | „die Anwendung zielt auf `net10.0`"; Debian-12-Hinweis auf .NET-10-Microsoft-Feed (`packages.microsoft.com/config/debian/12`) |
| 27 | framework-abhängiger Publish: „müssen die passenden .NET-10-Shared-Frameworks … installiert sein"; Ubuntu-24.04-Feed-Hinweis für .NET 10 |
| 31 | `sudo apt install aspnetcore-runtime-10.0` |
| 34 | SDK-Metapaket `dotnet10` |
| 42, 47 | Shared-Framework-Prüfung auf „passende .NET-10-Version" bzw. „fehlende .NET-10-Runtime" |
| 53, 55 | „Maschine mit .NET-10-SDK", „.NET-10-Shared-Frameworks" |
| 58 | `dotnet publish Rezepte.Web -c Release -f net10.0 -r linux-x64 --self-contained false` |
| 64 | `dotnet publish Rezepte.Web -c Release -f net10.0 -r linux-x64 --self-contained true` |

## `Docs/dependencies.md`

| Zeile | Inhalt |
|-------|--------|
| 13 | `Microsoft.EntityFrameworkCore.Sqlite` in `Rezepte.Web`: `10.0.15` |
| 29 | SQLite-Providerwechsel „nicht Teil des .NET-10-Upgrades" |
| 30 | „`Microsoft.*`-Direktreferenzen werden auf einem einheitlichen Stand innerhalb der .NET-10-Linie gehalten (aktuell `10.0.15`)" |
| 31 | Hinweis auf verschobene `AngleSharp`-/`bunit`-2.x-Migration (Issue #228) und `QuestPDF` ≥ 2026.9.x (Issue #229) — Kontext der Folge-Issues |

## `Docs/help/github-actions.md`

| Zeile | Inhalt |
|-------|--------|
| 19 | „fuehrt der Workflow auf `ubuntu-latest` mit .NET `10.0.x` diese Prüfungen aus" |
| 89 | „publisht `Rezepte.Web` framework-abhängig für `net10.0` und `linux-x64` und erstellt daraus `release.zip`" — beschreibt zudem einen veralteten Release-Ablauf (framework-abhängig + `release.zip`), während die aktuelle `build-and-package`-Action self-contained `release-linux-x64.zip`/`release-win-x64.zip` erzeugt |
| 98 | „bleibt der Testaufruf mit dem .NET-10-Testrunner kompatibel" |

## `Docs/screenshot-generation.md`

| Zeile | Inhalt |
|-------|--------|
| 7 | Voraussetzung „[.NET SDK 10](https://dotnet.microsoft.com)" |

## `Docs/Anforderungskatalog.md`

| Zeile | Inhalt |
|-------|--------|
| 1 | Titel: „Kochrezepte-Verwaltungssystem (Blazor Server, .NET 10, SQLite)" |

## Dokumente ohne .NET-10-Bezug

Grep-verifiziert ohne Treffer auf `net10.0`, `10.0.x`, `10.0.15`, `.NET 10`, `dotnet10`, `aspnetcore-runtime-10`, `net-10`, `SDK 10`, `.NET-10`: `Docs/deployment-guide.md`, `Docs/development-guide.md`, `Docs/testing.md`, `Docs/configuration.md`, `Docs/features.md`, `Docs/github-pages.md`, `Docs/google-credentials-security.md`, `Docs/index.md`, `Docs/project-structure.md`, `Docs/security.md`, `Docs/RELEASE_NOTES.md` sowie die übrigen `Docs/help/*` (einzige Fundstelle dort: `github-actions.md`, siehe oben).
