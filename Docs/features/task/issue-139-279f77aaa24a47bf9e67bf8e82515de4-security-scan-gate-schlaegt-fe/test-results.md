# Testergebnisse

Datum: 2026-09-03
Branch: `task/issue-139-279f77aaa24a47bf9e67bf8e82515de4-security-scan-gate-schlaegt-fe`

## Zusammenfassung

**Status:** Lokale Nachweise erfolgreich; externer Linux-PR-CI-Nachweis offen.

Die im Plan geforderte technische Playwright-Abdeckung wurde erfolgreich
ausgefuehrt. Die publizierte `Rezepte.Web`-Anwendung wurde von den Browser-
Tests erreicht und mit temporaerer SQLite-Datei betrieben. Es gibt keinen
neuen fachlichen UI-E2E-Fluss.

## Ausgefuehrte Tests

| Testschritt | Ergebnis | Details |
|---|---|---|
| `dotnet restore Rezepte.sln` | Bestanden | Restore erfolgreich; alle Projekte waren aktuell. |
| `dotnet list Rezepte.sln package --include-transitive` | Bestanden | `SQLitePCLRaw.lib.e_sqlite3` `2.1.12` in Web, Tests und Browser; `AngleSharp` `1.7.3` in Tests. |
| `dotnet list Rezepte.sln package --vulnerable --include-transitive` | Bestanden | Fuer alle Solution-Projekte wurden keine anfaelligen Pakete gemeldet. |
| `dotnet format Rezepte.sln --verify-no-changes --no-restore --severity error` | Bestanden | Exitcode 0; Workspace-Warnungen zu Browserreferenzen wurden gemeldet. |
| `dotnet build Rezepte.sln --configuration Release --no-restore -p:TreatWarningsAsErrors=true` | Bestanden | 0 Warnungen, 0 Fehler. |
| Release-Build Web, Tests und Browser | Bestanden | Alle drei projektweisen Builds erfolgreich, jeweils 0 Warnungen, 0 Fehler. |
| `dotnet publish Rezepte.Web/Rezepte.Web.csproj --configuration Release --no-restore` | Bestanden | Publish nach `Rezepte.Web/bin/Release/net10.0/publish`. |
| Vollstaendige Solution-Test-Suite mit Coverage | Bestanden | `Rezepte.Tests`: 531/531; `Rezepte.Tests.Browser`: 13/13. |
| Playwright-E2E-Suite | Bestanden | Alle 13 vorhandenen Browser-Szenarien bestanden; publizierter Prozess und temporaere SQLite-Datei wurden verwendet. |
| Playwright-Browserinstallation | Bestanden | `Rezepte.Tests.Browser/bin/Release/net10.0/playwright.ps1 install chromium` wurde am 2026-09-03 mit Prozess-/Netzwerkfreigabe erneut ausgefuehrt; Exitcode 0. |
| Coverage-Auswertung | Bestanden | ReportGenerator-Summary: `71.1 %` Line Coverage bei Schwelle `70 %`. |
| `scripts/Export-ImportContract.ps1` mit ApiCompat | Bestanden | `Microsoft.DotNet.ApiCompat.Tool` `10.0.400` wurde in ein Temp-Verzeichnis installiert; Script lief mit temporaerem Output, Baseline `0.3.0`, keine Breaking Changes. |
| Abschliessender Vulnerability-Scan | Bestanden | Erneuter Scan ohne gemeldete verwundbare Pakete. |

## Paketaufloesung

| Projekt | Paket | Aufloesung |
|---|---|---:|
| `Rezepte.Web` | `SQLitePCLRaw.lib.e_sqlite3` | `2.1.12` |
| `Rezepte.Tests` | `SQLitePCLRaw.lib.e_sqlite3` | `2.1.12` |
| `Rezepte.Tests.Browser` | `SQLitePCLRaw.lib.e_sqlite3` | `2.1.12` |
| `Rezepte.Tests` | `AngleSharp` | `1.7.3` |

## Fehlgeschlagene Tests

- [ ] Linux-PR-CI-Nachweis — `gh pr checks task/issue-139-279f77aaa24a47bf9e67bf8e82515de4-security-scan-gate-schlaegt-fe --repo martin-stromberg/Rezepte --watch=false` ergab `no pull requests found for branch ...`. Daher existiert kein externer Linux-PR-CI-Lauf fuer `static checks` und `build & test`; die Linux-Abnahme bleibt offen.

## E2E-Abdeckung

Laut Plan ist kein neuer fachlicher UI-E2E-Test erforderlich. Die vorhandene
technische Playwright-Suite deckt den verpflichtenden SQLite-Regressionsnachweis
ab. Die 13 bestandenen Szenarien umfassen SecurityTxt (Endpoint, Konfiguration,
Sichtbarkeit) und LoadingBar (Sichtbarkeit, Navigation, Formulare, Deaktivierung,
Timeout und Farbkonfiguration).

## Eingeschraenkte Nachweise

- Der PR-CI-Linux-Nachweis kann erst nach Erstellung eines Pull Requests mit
  erfolgreichem `ubuntu-latest`-Lauf geschlossen werden.
