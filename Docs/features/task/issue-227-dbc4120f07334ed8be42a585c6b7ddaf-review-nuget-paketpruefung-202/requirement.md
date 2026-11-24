# Anforderungsübersetzung

## Metadaten

- Aufgaben-ID: `dbc4120f-0733-4ed8-be42-a585c6b7ddaf`
- Issue: #227 – `[Review] NuGet-Paketprüfung – 2026-11-23` (Label `review-nuget-check`)
- Branch: `task/issue-227-dbc4120f07334ed8be42a585c6b7ddaf-review-nuget-paketpruefung-202`
- Thema: Turnusmäßige Prüfung aller NuGet-Pakete der Solution mit direktem Update-PR gegen `staging`

## Fachliche Zusammenfassung

Alle in der `Rezepte.sln` direkt referenzierten NuGet-Pakete (`<PackageReference>`) sind turnusmäßig auf verfügbare Updates, bekannte Schwachstellen, Deprecation/EOL und Breaking Changes zu prüfen. Das Ergebnis ist ein Analysebericht mit Risikoklassifikation; Empfehlungen mit niedrigem bis mittlerem Risiko werden direkt in einem Pull Request gegen `staging` umgesetzt, risikoreiche oder aufgeschobene Punkte werden dokumentiert bzw. als Folge-Issue ausgelagert. Zusätzlich ist der bestehende Versionsdrift der `Microsoft.*`-Pakete zwischen den Projekten zu vereinheitlichen.

## Fachlicher Scope

- **Verarbeitungseinheit:** Eine vollständige Paketprüfung der Solution `Rezepte.sln` zum Stichtag der Bearbeitung – umfasst alle direkten `<PackageReference>`-Einträge aller `*.csproj` (aktuell 10 Projekte, davon 6 mit Paketreferenzen: `Rezepte.Web`, `Rezepte.Tests`, `Rezepte.Tests.Browser`, `Rezepte.Import.Plugins.AIFoto`, `Rezepte.Import.Plugins.AIUrl`, `Rezepte.Updater.TestHost`; ohne Paketreferenzen: `Rezepte.Import.Abstractions`, `Rezepte.Import.PluginSdk`, `Rezepte.Import.Plugins.Backup`, `Rezepte.Tests.PluginFixture`) sowie transitive Pakete nur bei Verwundbarkeit oder Deprecation.
- **Auslösende Ereignisse:** Wöchentlich dienstags erzeugtes Review-Issue durch `.github/workflows/review-nuget-packages.yml` (Label `review-nuget-check`, Duplikatsschutz über `./.github/actions/create-review-issue`); konkret diese Instanz Issue #227.
- **Erwartete Wirkung:**
  1. Analysebericht als Tabelle aller geprüften Pakete (installierte Version, neueste Version, Update-Typ, Schwachstellen-/Deprecation-Status, Risikobewertung, Begründung) im PR-Body oder als Issue-Kommentar.
  2. Ein Pull Request gegen `staging` mit allen unbedenklichen Updates (Risiko niedrig bis mittel) inklusive Vereinheitlichung des `Microsoft.*`-Versionsdrifts.
  3. Dokumentation risikoreicher/aufgeschobener Empfehlungen im PR-Body; bei Major-Updates oder Breaking Changes ein separates Folge-Issue.
- **Umgang mit fehlenden Objekten:**
  - Fehlt ein Folge-Issue für risikoreiche Empfehlungen: erzeugen (Major-Updates, Breaking Changes).
  - Fehlt der Analysebericht im PR: als Issue-Kommentar auf Issue #227 nachliefern.
  - Lokaler Feed `lib/nuget` für `msTools.Updater` liegt vor (`msTools.Updater.0.10.4.nupkg`, Quelle `local-repo-packages` in `NuGet.config`): wiederverwenden, nicht gegen nuget.org auflösen.
- **Scope-Ausschlüsse:**
  - Keine Prüfung von npm-Abhängigkeiten (`package.json`).
  - Keine Prüfung/Aktualisierung von GitHub-Actions-Versionen.
  - Keine Änderung am .NET-SDK oder `TargetFramework` (`net10.0`).
  - `Microsoft.*`-/`System.*`-Pakete nur innerhalb der .NET-10-SDK-Linie aktualisieren, kein Major-Sprung über die Framework-Welle hinaus.
  - Kein generelles Update transitiver Abhängigkeiten (nur bei Verwundbarkeit oder Deprecation).
  - Keine Änderung am Issue-Text-Template des Workflows in dieser Instanz (Verbesserungsvorschläge nur als Analysekommentar oder separates Issue).
  - Kein Datenbank-/Providerwechsel als Folge von Paketupdates (vgl. `docs/dependencies.md`).

## Betroffene Klassen und Komponenten

Keine Änderung an Datenmodell, Services, Interfaces oder UI – es handelt sich um einen Wartungslauf. Betroffene bzw. zu erstellende Artefakte:

- Projektdateien mit Paketreferenzen (Update-Kandidaten bzw. Drift-Ausgleich):
  - `Rezepte.Web/Rezepte.Web.csproj` (`Google.Cloud.Vision.V1` 3.8.0, `Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.9, `Microsoft.EntityFrameworkCore.Design`/`.Sqlite` 10.0.11, `QuestPDF` 2026.7.0, `Serilog.AspNetCore` 10.0.0, `Serilog.Sinks.Console` 6.1.1, `Serilog.Sinks.File` 7.0.0, `msTools.Updater` 0.10.4)
  - `Rezepte.Tests/Rezepte.Tests.csproj` (`Microsoft.NET.Test.Sdk` 18.7.0, `xunit` 2.9.3, `xunit.runner.visualstudio` 3.1.5, `FluentAssertions` 8.10.0, `Microsoft.AspNetCore.Mvc.Testing` 10.0.9, `Microsoft.EntityFrameworkCore.InMemory` 10.0.11, `Microsoft.Extensions.Caching.Memory` 10.0.11, `Moq` 4.20.72, `msTools.Updater` 0.10.4, `bunit` 1.40.0, `AngleSharp` 1.7.3, `coverlet.collector` 10.0.1)
  - `Rezepte.Tests.Browser/Rezepte.Tests.Browser.csproj` (`Microsoft.NET.Test.Sdk` 18.7.0, `xunit` 2.9.3, `xunit.runner.visualstudio` 3.1.5, `FluentAssertions` 8.10.0, `Microsoft.Playwright` 1.61.0, `Xunit.SkippableFact` 1.5.61, `coverlet.collector` 10.0.1)
  - `Rezepte.Import.Plugins.AIFoto/Rezepte.Import.Plugins.AIFoto.csproj` (`Google.Cloud.Vision.V1` 3.8.0, `Microsoft.Extensions.*` 10.0.9)
  - `Rezepte.Import.Plugins.AIUrl/Rezepte.Import.Plugins.AIUrl.csproj` (`Microsoft.Extensions.*` 10.0.9)
  - `Rezepte.Updater.TestHost/Rezepte.Updater.TestHost.csproj` (`Microsoft.Extensions.Hosting` 10.0.0, `msTools.Updater` 0.10.4)
- `NuGet.config` (Paketquellen `nuget.org` + `local-repo-packages` → `lib/nuget`; voraussichtlich unverändert)
- `docs/dependencies.md` (Paketstrategie/Befundstand; ggf. aktualisieren, falls sich der Sicherheitsbefund ändert)
- Tests: keine neuen Tests; bestehender Testbestand als Akzeptanznachweis (`dotnet test` grün)
- Externe Artefakte: PR gegen `staging`, ggf. Folge-Issue(s), Analysebericht (PR-Body oder Issue-Kommentar)

## Implementierungsansatz

1. Bestandsaufnahme: `dotnet list Rezepte.sln package --outdated` (inkl. Prereleases, soweit relevant), `dotnet list Rezepte.sln package --vulnerable --include-transitive`, `dotnet list Rezepte.sln package --deprecated` je Projekt/aus dem Solution-Kontext; Paketquellen gemäß `NuGet.config` (lokaler Feed `lib/nuget` für `msTools.Updater`).
2. Pro direktem Paket Update-Typ (Patch/Minor/Major), Security-/Deprecation-Status, Pflegezustand (letztes Release, Upstream-Repository) und Breaking-Change-Risiko aus Release Notes/Advisories bewerten; Pakete ohne auswertbare Quellen als „nicht bewertbar" kennzeichnen statt zu raten.
3. Risikoklassifikation anwenden: niedrig (Patch ohne Breaking Changes, Dev-/Test-Pakete), mittel (Minor ohne Breaking Changes, Patch produktionskritischer Pakete), hoch (Major, Breaking Changes, EF-Core-Migrationsbedarf, sicherheitsrelevante Major-Sprünge).
4. Feature-Branch vom aktuellen `staging` (bzw. `main`-Stand gemäß Repo-Workflow Feature → staging → main); unbedenkliche Updates in einem PR umsetzen, dabei `Microsoft.*`-Versionsdrift vereinheitlichen (aktuell gemischt `10.0.0`/`10.0.9`/`10.0.11` über die Projekte hinweg).
5. Verifikation je Update-Runde: Restore, `dotnet build` fehlerfrei, `dotnet test` grün (bestehender Bestand), keine neuen Warnungen, abschließend `dotnet list Rezepte.sln package --vulnerable --include-transitive` ohne Befund (entspricht dem blockierenden Security-Gate in `pr-staging-ci.yml`/`staging-ci.yml` und der Strategie in `docs/dependencies.md`, `NU1903` wird nicht unterdrückt).
6. Analysebericht im PR-Body (Tabelle aller Pakete) oder als Kommentar auf Issue #227; risikoreiche/aufgeschobene Empfehlungen dokumentieren, bei Major-Updates/Breaking Changes Folge-Issue anlegen.
7. Befunde mit dem wöchentlichen `security-scan.yml`-Lauf (montags) deduplizieren bzw. auf vorhandene Scan-Ergebnisse verweisen.

## Konfiguration

Keine neue Konfiguration. Die Paketquellen sind bereits in `NuGet.config` hinterlegt (`local-repo-packages` → `lib/nuget`, `nuget.org`). Der Turnus des Review-Issues wird ausschließlich über `.github/workflows/review-nuget-packages.yml` gesteuert und ist nicht Teil dieser Umsetzung.

## Offene Fragen

1. **[offen]** Schwachstelle, deren Fix einen Major-Sprung erfordert (hohes Risiko): trotzdem sofort ein PR (Sicherheit priorisiert) oder Folge-Issue (Stabilität priorisiert)?
2. **[offen]** `msTools.Updater` (lokaler Feed `lib/nuget`, aktuell `0.10.4`): neue Versionen werden vermutlich manuell bereitgestellt – wie soll der Update-Status dieses Pakets im Bericht bewertet werden (z. B. „nicht bewertbar/lokal gepflegt")?
3. **[offen]** Überschneidung mit `security-scan.yml` (wöchentlicher Vulnerability-Scan): Befunde deduplizieren bzw. auf vorhandene Scan-Ergebnisse verweisen – in welcher Form (Verweis im Bericht, Kopie der Befunde)?
4. **[offen]** Ein einzelner PR für alle unbedenklichen Updates vs. Gruppierung (z. B. produktiv vs. Test-only)? Default laut Anforderung: ein PR, sofern der Umfang überschaubar bleibt – Schwellenwert für „überschaubar" ist zu konkretisieren.
5. **[offen]** Wo soll der Analysebericht verbindlich abgelegt werden – PR-Body (Default bei umgesetzten Updates) oder zusätzlich/primär als Kommentar auf Issue #227, damit der Bericht auch ohne PR nachvollziehbar bleibt?
6. **[abgeleitet]** Soll der `Microsoft.*`-Versionsdrift zwingend auf die jeweils höchste im SDK-Rahmen verfügbare gemeinsame Version angehoben werden, auch wenn dadurch Pakete ohne eigenes Update-Bedürfnis angefasst werden? (Annahme: ja, gemäß Randbedingung „Vereinheitlichung im gleichen Zug" – als Annahme gekennzeichnet.)
