# Umsetzungsplan: Upgrade auf .NET 10

Datum: 2026-07-01

## Zielbild

Die Solution `Rezepte.sln` wird von `net9.0` auf `net10.0` migriert. Beide vorhandenen Projekte bleiben in ihrer bestehenden Struktur erhalten:

- `Rezepte.Web/Rezepte.Web.csproj` als Blazor/Web-Projekt mit `Microsoft.NET.Sdk.Web`
- `Rezepte.Tests/Rezepte.Tests.csproj` als xUnit-Testprojekt mit Referenz auf `Rezepte.Web`

Alle direkten NuGet-Paketversionen werden auf den neuesten laut Inventarisierung kompatiblen Stand angehoben, sofern Restore, Build, Tests und betroffene Codepfade damit funktionieren. Bestehende Projektkonventionen bleiben erhalten: Paketversionen werden weiter direkt in den `.csproj`-Dateien gepflegt; es wird keine zentrale Paketverwaltung und keine `global.json` eingefuehrt, solange sich waehrend der Umsetzung kein konkreter technischer Bedarf ergibt.

## Betroffene Dateien

Voraussichtlich zu aendern:

- `Rezepte.Web/Rezepte.Web.csproj`
- `Rezepte.Tests/Rezepte.Tests.csproj`
- `README.md`
- `Docs/install.md`
- `Docs/Anforderungskatalog.md`

Nur bei technisch notwendiger Anpassung zu aendern:

- Code unter `Rezepte.Web/Services/`, `Rezepte.Web/Data/`, `Rezepte.Web/Controllers/`, `Rezepte.Web/Extensions/` und `Rezepte.Web/Migrations/`
- Tests unter `Rezepte.Tests/`
- `.github/copilot-instructions.md`, falls sich die dokumentierten Qualitaetsschritte durch das Upgrade konkret aendern

Nicht geplant:

- Einfuehrung von `Directory.Packages.props`, `Directory.Build.props`, `NuGet.config`, Lockfiles oder Docker-/CI-Artefakten
- Fachliche Funktionsaenderungen, UI-Umbauten oder bewusste Aenderungen oeffentlicher Schnittstellen

## Umsetzungsschritte

1. Arbeitsstand und Paketlage erneut pruefen
   - Vor Aenderungen `git status --short` pruefen und vorhandene fremde Aenderungen nicht zuruecksetzen.
   - `dotnet --info` pruefen und sicherstellen, dass ein .NET-10-SDK verfuegbar ist.
   - `dotnet list Rezepte.sln package --outdated` erneut ausfuehren, damit Paketversionen nicht ausschliesslich auf der Inventarisierung beruhen.
   - `dotnet list Rezepte.sln package --vulnerable --include-transitive` als Sicherheitsbaseline erneut ausfuehren.

2. Target Frameworks auf .NET 10 umstellen
   - In `Rezepte.Web/Rezepte.Web.csproj` `<TargetFramework>net9.0</TargetFramework>` auf `net10.0` aendern.
   - In `Rezepte.Tests/Rezepte.Tests.csproj` `<TargetFramework>net9.0</TargetFramework>` auf `net10.0` aendern.
   - Bestehende Properties wie `Nullable`, `ImplicitUsings`, `IsPackable`, `ProjectReference` und Content-Regeln unveraendert lassen.

3. Produktivpakete aktualisieren
   - `Google.Cloud.Vision.V1` von `3.7.0` auf die neueste kompatible Version aktualisieren, laut Inventarisierung `3.8.0`.
   - `Microsoft.AspNetCore.Authentication.JwtBearer` auf die .NET-10-Version aktualisieren, laut Inventarisierung `10.0.9`.
   - `Microsoft.EntityFrameworkCore.Design` und `Microsoft.EntityFrameworkCore.Sqlite` gemeinsam auf `10.0.9` aktualisieren.
   - `QuestPDF` auf die neueste kompatible Version aktualisieren, laut Inventarisierung `2026.6.1`.
   - `Serilog.AspNetCore`, `Serilog.Sinks.Console` und `Serilog.Sinks.File` auf die neuesten kompatiblen Versionen aktualisieren, laut Inventarisierung `10.0.0`, `6.1.1` und `7.0.0`.
   - `PrivateAssets` und `IncludeAssets` bei `Microsoft.EntityFrameworkCore.Design` erhalten.

4. Testpakete aktualisieren
   - `Microsoft.NET.Test.Sdk` auf die neueste kompatible Version aktualisieren, laut Inventarisierung `18.7.0`.
   - `xunit` auf `2.9.3` aktualisieren.
   - `xunit.runner.visualstudio` auf die neueste kompatible Version aktualisieren, laut Inventarisierung `3.1.5`; `PrivateAssets` und `IncludeAssets` erhalten.
   - `FluentAssertions` auf die neueste kompatible Version aktualisieren, laut Inventarisierung `8.10.0`.
   - `Microsoft.EntityFrameworkCore.InMemory` und `Microsoft.Extensions.Caching.Memory` auf `10.0.9` aktualisieren.
   - `coverlet.collector` auf die neueste kompatible Version aktualisieren, laut Inventarisierung `10.0.1`; `PrivateAssets` und `IncludeAssets` erhalten.
   - `Moq` bei `4.20.72` belassen, sofern der erneute Outdated-Check weiterhin keine neuere stabile Version meldet.

5. Restore und Paketfolgen pruefen
   - `dotnet restore Rezepte.sln` ausfuehren.
   - Bei Restore-Konflikten zuerst Paketkombinationen konsistent machen, besonders im Microsoft-/EF-Core-/Test-SDK-Bereich.
   - Danach `dotnet list Rezepte.sln package --include-transitive` und `dotnet list Rezepte.sln package --vulnerable --include-transitive` pruefen.
   - Sicherstellen, dass die bisherige transitive Schwachstelle in `SQLitePCLRaw.lib.e_sqlite3` nicht mehr gemeldet wird.
   - Falls die Schwachstelle trotz EF-Core-Upgrade bestehen bleibt, eine explizite Paketstrategie fuer die SQLitePCLRaw-Kette ergaenzen oder die verbleibende Begruendung dokumentieren.

6. Build- und API-Anpassungen vornehmen
   - `dotnet build Rezepte.sln -clp:Summary` ausfuehren.
   - Upgradebedingte Compilerfehler beheben.
   - Bei API-Aenderungen gezielt die betroffenen Stellen anpassen:
     - EF Core Nutzung in `Rezepte.Web/Data`, Services, Controllern, Migrations und Tests
     - JWT-Konfiguration und Authentifizierung in Controller-/Service-Registrierungscode
     - Serilog-Konfiguration in `Rezepte.Web/Extensions/LoggingExtensions.cs`
     - QuestPDF-Nutzung in `Rezepte.Web/Services/PdfGenerator.cs`
     - Google-Vision-Nutzung in `Rezepte.Web/Services/Import/AIFotoImportHandler.cs`
     - FluentAssertions-/xUnit-/Moq-Nutzung in `Rezepte.Tests`
   - Bereits vor dem Upgrade vorhandene Warnungen nicht pauschal als Scope-Erweiterung behandeln; neue upgradebedingte Warnungen oder Fehler beheben, sofern sie nicht technisch begruendet offen bleiben muessen.

7. Tests und Publish pruefen
   - `dotnet test Rezepte.sln --no-restore` ausfuehren und alle 33 bestehenden Tests als Mindestbaseline erwarten.
   - Bei Testfehlern zuerst zwischen echter Regression und geaendertem Paket-/Framework-Verhalten unterscheiden.
   - Einen Release-Build pruefen: `dotnet build Rezepte.sln -c Release -clp:Summary`.
   - Einen frameworkabhaengigen Publish fuer die Web-Anwendung pruefen: `dotnet publish Rezepte.Web -c Release -f net10.0 -r linux-x64 --self-contained false`.
   - Nach dem Publish stichprobenartig pruefen, dass `test.recipe-import.json` weiterhin nicht im Publish-Output landet und die bestehenden Content-Regeln nicht versehentlich veraendert wurden.

8. Dokumentation synchronisieren
   - `README.md` von `.NET 9` auf `.NET 10` aktualisieren.
   - Publish-Beispiel in `README.md` von `-f net9.0` auf `-f net10.0` aktualisieren.
   - `Docs/install.md` von `Framework net9.0` auf `Framework net10.0` aktualisieren.
   - Titel in `Docs/Anforderungskatalog.md` von `.NET 9` auf `.NET 10` aktualisieren.
   - Keine CI-Workflowdateien anlegen, da laut Inventarisierung keine ausfuehrbaren CI-Workflows vorhanden sind.

9. Abschlusspruefung
   - `rg -n "net9\.0|\.NET 9|.NET 9" README.md Docs Rezepte.Web Rezepte.Tests .github -S` ausfuehren.
   - Treffer in Lifecycle-/Inventory-Artefakten ignorieren; verbleibende Produktiv-, Test-, Doku- oder Konfigurationsverweise auf .NET 9 muessen entweder entfernt oder begruendet dokumentiert werden.
   - `dotnet list Rezepte.sln package --outdated` erneut ausfuehren und verbleibende nicht aktualisierte Pakete mit technischer Begruendung dokumentieren.
   - `dotnet list Rezepte.sln package --vulnerable --include-transitive` erneut ausfuehren und sicherstellen, dass keine bekannte upgradebezogene Vulnerability verbleibt oder eine Begruendung vorhanden ist.

## Validierung

Pflichtpruefungen nach der Umsetzung:

```powershell
dotnet restore Rezepte.sln
dotnet build Rezepte.sln -clp:Summary
dotnet test Rezepte.sln --no-restore
dotnet build Rezepte.sln -c Release -clp:Summary
dotnet publish Rezepte.Web -c Release -f net10.0 -r linux-x64 --self-contained false
dotnet list Rezepte.sln package --outdated
dotnet list Rezepte.sln package --vulnerable --include-transitive
rg -n "net9\.0|\.NET 9|.NET 9" README.md Docs Rezepte.Web Rezepte.Tests .github -S
```

Optionale Zusatzpruefung, wenn sie im lokalen Stand ohne unrelated Formatierungsdiffs laeuft:

```powershell
dotnet format --verify-no-changes
```

## Erwartete Risiken und Gegenmassnahmen

| Risiko | Gegenmassnahme |
|---|---|
| Major-Updates bei `FluentAssertions`, `xunit.runner.visualstudio`, `coverlet.collector`, `QuestPDF` oder Serilog verursachen API- oder Verhaltensaenderungen. | Nach Paketgruppen-Update sofort bauen und Tests ausfuehren; Anpassungen auf betroffene Codepfade begrenzen. |
| EF Core 10 aendert Design-Time- oder Provider-Verhalten. | Restore, Build und Tests mit `Microsoft.EntityFrameworkCore.Design`, `Sqlite` und `InMemory` konsistent auf `10.0.9` halten; Migrations-Snapshot nicht ohne fachlichen Anlass veraendern. |
| Die transitive SQLitePCLRaw-Vulnerability bleibt trotz EF-Core-Upgrade bestehen. | Vulnerability-Check nach Restore wiederholen; falls noetig explizite Paketversion oder dokumentierte Ausnahme planen. |
| Bestehende Warnungen werden mit upgradebedingten Warnungen vermischt. | Baseline aus der Inventarisierung beachten; nur neue oder durch Upgrade verschärfte Befunde als Umsetzungsbedarf behandeln. |
| Publish-Verhalten aendert sich durch Framework- oder SDK-Wechsel. | Release-Publish fuer `net10.0` pruefen und Content-Regeln fuer JSON-Test-/Credential-Dateien stichprobenartig verifizieren. |

## Akzeptanzkriterien-Abdeckung

| Kriterium | Abdeckung im Plan |
|---|---|
| Alle relevanten Projekte verwenden .NET 10 oder sind begruendet ausgenommen. | Schritte 2 und 9 stellen beide `.csproj` auf `net10.0` um und suchen nach Restverweisen. |
| Build-, Test-, CI-, Docker- und sonstige Konfigurationen sind geprueft. | Schritte 1, 7, 8 und 9 pruefen Toolchain, Build/Test, Publish und Doku; CI/Docker sind laut Inventarisierung nicht vorhanden. |
| NuGet-Pakete sind aktualisiert oder begruendet dokumentiert. | Schritte 3, 4, 5 und 9 aktualisieren direkte Referenzen und pruefen Outdated-/Vulnerability-Ergebnisse. |
| Quellcode kompiliert ohne upgradebedingte Fehler. | Schritt 6 und Validierung enthalten Restore, Debug-/Release-Build und gezielte API-Anpassungen. |
| Automatisierte Tests wurden ausgefuehrt oder begruendet dokumentiert. | Schritt 7 und Validierung fuehren `dotnet test` aus. |
| Notwendige Anpassungen wegen APIs, Analyzern oder Paketverhalten sind umgesetzt. | Schritt 6 benennt die betroffenen Codebereiche und begrenzt Aenderungen auf Upgrade-Folgen. |
| Keine offensichtlichen alten .NET-Verweise bleiben zurueck. | Schritt 9 sucht nach `net9.0` und `.NET 9` in relevanten Dateien. |

## Offene Punkte

