# Tasks: [Review] NuGet-Paketprüfung – 2026-11-23 (Issue #227)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Analyse | Befund-Stichtag erneuern: `dotnet restore` + `dotnet list Rezepte.sln package --outdated/--vulnerable --include-transitive/--deprecated`; höchste gemeinsame `10.0.x` je `Microsoft.*`-Paket verifizieren (erwartet `10.0.15`) | Offen | — |
| 2 | Analyse | `QuestPDF`-Release-Notes 2026.7.0→2026.9.2 auf Breaking Changes prüfen (Entscheidungsvorlage für Update bzw. Aufschub) | Offen | — |
| 3 | Analyse | `analysis-report.md` im Feature-Verzeichnis anlegen und Tabelle aller direkten Pakete befüllen (Versionen, Update-Typ, Security-/Deprecation-Status, Risiko, Begründung, Entscheidung) | Offen | — |
| 4 | Paket-Update | `Rezepte.Web.csproj`: `Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.9 → einheitliche `10.0.x` | Offen | — |
| 5 | Paket-Update | `Rezepte.Web.csproj`: `Microsoft.EntityFrameworkCore.Design` + `Microsoft.EntityFrameworkCore.Sqlite` 10.0.11 → einheitliche `10.0.x` | Offen | — |
| 6 | Paket-Update | `Rezepte.Web.csproj`: `QuestPDF` 2026.7.0 → 2026.9.2 (sofern Release-Notes-Prüfung unbedenklich) | Offen | — |
| 7 | Paket-Update | `Rezepte.Tests.csproj`: `Microsoft.AspNetCore.Mvc.Testing` 10.0.9, `Microsoft.EntityFrameworkCore.InMemory` + `Microsoft.Extensions.Caching.Memory` 10.0.11 → einheitliche `10.0.x` | Offen | — |
| 8 | Paket-Update | `Rezepte.Tests.csproj`: `Microsoft.NET.Test.Sdk` 18.7.0 → 18.12.0, `FluentAssertions` 8.10.0 → 8.11.0, `coverlet.collector` 10.0.1 → 10.1.1, `xunit.runner.visualstudio` 3.1.5 → 4.0.1 | Offen | — |
| 9 | Paket-Update | `Rezepte.Tests.csproj`: `Moq` 4.20.72 → 4.21.0, `AngleSharp` 1.7.3 → 1.10.5 | Offen | — |
| 10 | Paket-Update | `Rezepte.Tests.Browser.csproj`: `Microsoft.NET.Test.Sdk` 18.7.0 → 18.12.0, `FluentAssertions` 8.10.0 → 8.11.0, `coverlet.collector` 10.0.1 → 10.1.1, `xunit.runner.visualstudio` 3.1.5 → 4.0.1 | Offen | — |
| 11 | Paket-Update | `Rezepte.Tests.Browser.csproj`: `Microsoft.Playwright` 1.61.0 → 1.65.1, `Xunit.SkippableFact` 1.5.61 → 1.5.85; lokal `playwright.ps1 install chromium` ausführen | Offen | — |
| 12 | Paket-Update | `Rezepte.Import.Plugins.AIFoto.csproj`: `Microsoft.Extensions.Caching.Abstractions`/`.DependencyInjection.Abstractions`/`.Logging.Abstractions`/`.Options` 10.0.9 → einheitliche `10.0.x` | Offen | — |
| 13 | Paket-Update | `Rezepte.Import.Plugins.AIUrl.csproj`: `Microsoft.Extensions.DependencyInjection.Abstractions` + `Microsoft.Extensions.Logging` 10.0.9 → einheitliche `10.0.x` | Offen | — |
| 14 | Paket-Update | `Rezepte.Updater.TestHost.csproj`: `Microsoft.Extensions.Hosting` 10.0.0 → einheitliche `10.0.x` | Offen | — |
| 15 | Verifikation | `dotnet restore Rezepte.sln`, dann Build in CI-Reihenfolge: `dotnet build Rezepte.Web/Rezepte.Web.csproj -c Release`, danach `dotnet build Rezepte.sln -c Release -p:TreatWarningsAsErrors=true` — fehlerfrei, keine neuen Warnungen | Offen | — |
| 16 | Verifikation | `dotnet test Rezepte.Tests/Rezepte.Tests.csproj` grün (Baseline 774 Tests, inkl. Test-Discovery unter `xunit.runner.visualstudio` 4.x) | Offen | — |
| 17 | Verifikation | `dotnet test Rezepte.Tests.Browser/Rezepte.Tests.Browser.csproj` grün (Baseline 35 Tests) | Offen | — |
| 18 | Verifikation | `dotnet list Rezepte.sln package --vulnerable --include-transitive` befundfrei (CI-Gate-Äquivalent) | Offen | — |
| 19 | Dokumentation | `analysis-report.md` finalisieren (Ist-Ergebnis, aufgeschobene Empfehlungen, Verweis auf `security-scan.yml`); `docs/dependencies.md` nur bei geändertem Befundstand aktualisieren | Offen | — |
| 20 | Extern | Folge-Issue „Test-Stack-Migration: `xunit.v3` + `bunit` 2.x" anlegen (Deprecation `xunit` 2.9.3, Major-Updates `bunit` → 2.x) | Offen | — |
| 21 | Extern | PR gegen `staging` erstellen, Analysebericht-Tabelle in den PR-Body übernehmen | Offen | — |
