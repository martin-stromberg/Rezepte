# Umsetzungsplan: [Review] NuGet-Paketprüfung – 2026-11-23 (Issue #227)

## Übersicht

Turnusmäßiger Wartungslauf: Alle direkten `<PackageReference>`-Einträge der
`Rezepte.sln` werden auf Updates, Schwachstellen, Deprecation und Breaking
Changes geprüft. Unbedenkliche Updates (Risiko niedrig–mittel) inklusive der
Vereinheitlichung des `Microsoft.*`-Versionsdrifts (`10.0.0`/`10.0.9`/`10.0.11`)
auf einen gemeinsamen Stand der .NET-10-Linie werden in einem PR gegen
`staging` umgesetzt; risikoreiche Major-Sprünge (`xunit.v3`, `bunit` 2.x) werden
dokumentiert und in ein Folge-Issue ausgelagert. Ergebnisartefakt ist ein
Analysebericht (`analysis-report.md`), der in den PR-Body übernommen wird.
Betroffen sind ausschließlich sechs `*.csproj`-Dateien — kein Produktivcode,
keine neuen Klassen, keine UI-/Datenmodell-Änderung.

## Scope-Klärung

| Feld | Festlegung |
|------|------------|
| Verarbeitungseinheit | Eine vollständige Paketprüfung der `Rezepte.sln` zum Stichtag der Umsetzung: alle direkten `<PackageReference>` in den sechs Projekten mit Paketreferenzen (`Rezepte.Web`, `Rezepte.Tests`, `Rezepte.Tests.Browser`, `Rezepte.Import.Plugins.AIFoto`, `Rezepte.Import.Plugins.AIUrl`, `Rezepte.Updater.TestHost`); transitive Pakete nur bei Verwundbarkeit/Deprecation. |
| Auslösende Ereignisse | Issue #227 (`[Review] NuGet-Paketprüfung – 2026-11-23`, Label `review-nuget-check`), erzeugt durch `review-nuget-packages.yml`. |
| Erwartete Wirkung | (1) Analysebericht mit Risikoklassifikation aller geprüften Pakete im PR-Body; (2) ein PR gegen `staging` mit allen Updates der Risikoklassen niedrig–mittel inkl. `Microsoft.*`-Drift-Vereinheitlichung; (3) Folge-Issue für aufgeschobene Major-Empfehlungen; (4) kein Vulnerability-Befund (`dotnet list Rezepte.sln package --vulnerable --include-transitive`). |
| Umgang mit fehlenden Objekten | Folge-Issue für risikoreiche Empfehlungen fehlt → **erzeugen** (xunit.v3/bunit-2.x-Migration). Analysebericht existiert noch nicht → **erzeugen** als `analysis-report.md` im Feature-Verzeichnis, Inhalt in den PR-Body übernehmen. Lokaler Feed `lib/nuget` für `msTools.Updater` liegt vor → **wiederverwenden**, nicht gegen nuget.org auflösen. |

### Scope-Ausschlüsse

| # | Einschränkung / Ausschluss | Begründung | Status |
|---|----------------------------|------------|--------|
| 1 | Kein Update auf die .NET-11-Welle: `Microsoft.*`/`AspNetCore`/`EFCore`/`Extensions` bleiben innerhalb der 10.0.x-Linie; `Serilog.AspNetCore` bleibt auf 10.0.0 (10.x ist laut Paket-Versionierungsrichtlinie die net10.0-Linie; 11.0.0 gehört zu .NET 11). | Expliziter Scope der Anforderung („nur innerhalb der .NET-10-SDK-Linie"). | bestätigt (Anforderungstext, 2026-11-24) |
| 2 | Keine Major-Upgrades mit Breaking Changes im selben PR: `xunit` → `xunit.v3` und `bunit` 1.40.0 → 2.x werden aufgeschoben statt mitaktualisiert. | Anforderung begrenzt den PR auf Risiko niedrig–mittel; Major/Breaking = Risiko hoch → Folge-Issue. | bestätigt (Anforderungstext, 2026-11-24) |
| 3 | Keine Prüfung von npm-Abhängigkeiten und GitHub-Actions-Versionen; keine Änderung an .NET-SDK, `TargetFramework` (`net10.0`) oder `global.json`. | Explizit aus dem Issue-Scope ausgeschlossen. | bestätigt (Anforderungstext, 2026-11-24) |
| 4 | Kein generelles Update transitiver Abhängigkeiten (nur bei Verwundbarkeit oder Deprecation — aktuell befundfrei). | Expliziter Scope der Anforderung. | bestätigt (Anforderungstext, 2026-11-24) |
| 5 | Keine Änderung am Issue-Template des Review-Workflows und kein Datenbank-/Providerwechsel. | Expliziter Scope der Anforderung bzw. `docs/dependencies.md`. | bestätigt (Anforderungstext, 2026-11-24) |
| 6 | `msTools.Updater` wird nicht aktualisiert und nicht gegen nuget.org bewertet. | Paket liegt ausschließlich im lokalen Feed `lib/nuget`; kein externer Vergleichsstand verfügbar. | bestätigt (Anforderungstext, 2026-11-24) |

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Einheitlicher `Microsoft.*`-Zielstand | Alle direkten `Microsoft.*`-Referenzen (13 Referenzen in 6 Projekten) auf **10.0.15** — höchste `10.0.x` zum Recherchezeitpunkt (November-2026-Servicing-Release, 2026-11-10). Bei Umsetzung pro Paket verifizieren, dass `10.0.15` (bzw. die dann höchste gemeinsame `10.0.x`) verfügbar ist; Ziel ist ein einheitlicher Stand, kein Mischstand. | Drift-Vereinheitlichung ist ausdrückliche Anforderung („im gleichen Zug vereinheitlichen"); Patch-Updates innerhalb der Servicing-Linie sind risikoarm und beheben den gemischten Stand `10.0.0`/`10.0.9`/`10.0.11`. |
| `xunit.runner.visualstudio` 3.1.5 → 4.0.1 | **Im PR umsetzen** (Risiko mittel trotz Major-Nummer) mit Verifikationsgate. | Geprüfter Befund: Runner 4.0.1 unterstützt laut Paketdokumentation xUnit.net ≥ 1.9.2 (deckt xunit 2.9.3 ab) und targetiert net8.0+/net472 — für den vorhandenen Teststand nicht breaking. Der Major-Sprung betrifft Plattform-/Tooling-Träger, nicht den xunit-2.x-Ausführungsvertrag. Verifikation: Test-Discovery + alle 809 Tests grün; bei Bruch Rollback des Runner-Updates und Verschiebung ins Folge-Issue. |
| `xunit` → `xunit.v3` und `bunit` 1.x → 2.x | **Aufschieben**, ein gemeinsames Folge-Issue für die gekoppelte Test-Stack-Migration. | `xunit` 2.9.3 ist `deprecated`/`Legacy`, Nachfolger `xunit.v3` ist ein anderes Paket mit Breaking Changes (Attribute, Runner-Modell, `Xunit.SkippableFact`-/Collection-Kompatibilität prüfungsbedürftig). `bunit` 2.x ist ebenfalls Major und im gleichen Migrationskontext zu bewerten → gebündeltes Folge-Issue statt verstreuter Einzelissues. |
| `Serilog.AspNetCore` 10.0.0 → 11.0.0 | **Nicht umsetzen**, im Bericht dokumentieren (kein Folge-Issue nötig). | 10.x ist die net10.0-Linie des Pakets (eigene Versionierungsrichtlinie: Paketmajor = TFM); 11.0.0 gehört zur .NET-11-Welle und ist Scope-Ausschluss Nr. 1. In der 10.x-Linie gibt es kein neueres Patch-Release — 10.0.0 ist bereits aktuell. |
| `msTools.Updater` 0.10.4 | Keine Aktion; im Bericht als **„nicht bewertbar"** kennzeichnen. | Nur im lokalen Feed `lib/nuget` verfügbar, kein nuget.org-Vergleichsstand; neue Versionen werden vermutlich manuell bereitgestellt (Default-Antwort auf offene Frage 2 der Anforderung). |
| PR-Struktur | **Ein einzelner PR** gegen `staging` für alle niedrig/mittel-Updates. | Default laut Anforderung; Umfang überschaubar (≈ 26 Version-Bumps in 6 `*.csproj`, homogener Update-Charakter, keine Codeänderung). Gruppierung produktiv/test-only nicht nötig, weil Verifikation ohnehin die gesamte Solution betrifft (Default-Antwort auf offene Frage 4). |
| Analysebericht | Artefakt `analysis-report.md` im Feature-Verzeichnis als Quelle; Inhalt in den **PR-Body** übernehmen. | PR-Body ist der in der Anforderung genannte Default bei umgesetzten Updates; die Datei im Feature-Verzeichnis bleibt als nachvollziehbarer Befund im Repo bestehen, sodass kein zusätzlicher Issue-Kommentar erforderlich ist (Default-Antwort auf offene Frage 5). |
| Deduplizierung mit `security-scan.yml` | Verweis im Bericht: wöchentlicher Montags-Scan + PR-interner Lauf `dotnet list Rezepte.sln package --vulnerable --include-transitive` als ergänzendes Gate; keine Kopie externer Befunde nötig, da aktuell befundfrei. | Scan und Review-Prüfung nutzen identischen Befundbefehl; ein Verweis reicht zur Deduplizierung (Default-Antwort auf offene Frage 3). |
| Security-Major-Fall (offene Frage 1) | Nicht eingetreten: `dotnet list --vulnerable --include-transitive` ist befundfrei (Inventory, verifiziert 2026-11-24). Sollte bei der Wiederholung zum Umsetzungsstichtag ein Befund auftreten: Schwachstellenfix priorisieren — Update im selben PR, auch bei Major, sofern kein Breaking-Change-Nachweis vorliegt; sonst Folge-Issue mit expliziter Risikoentscheidung im PR-Body. | Begründete Default-Entscheidung aus Anforderungslogik (Sicherheitsgate ist blockierend; ein offener Schwachstellenbefund würde CI ohnehin blockieren). |
| Drift-Zielversion (abgeleitete Frage 6) | Auf die höchste gemeinsam verfügbare `10.0.x` anheben, auch für Pakete ohne eigenes Update-Bedürfnis. | Anforderung verlangt Vereinheitlichung „im gleichen Zug" — Annahme aus der Anforderung wird zur Festlegung. |

## Programmabläufe

### Befund-Stichtag verifizieren und Analysebericht erstellen

1. `dotnet restore Rezepte.sln` ausführen.
2. `dotnet list Rezepte.sln package --outdated`, `--vulnerable --include-transitive`
   und `--deprecated` erneut ausführen (Befundstand kann seit der Inventory vom
   2026-11-24 fortgeschritten sein); Ergebnis-Logs unter
   `inventory/test-results/` ablegen bzw. neue Stichtags-Logs im Feature-Verzeichnis.
3. Pro `Microsoft.*`-Paket die höchste verfügbare `10.0.x` auf nuget.org ermitteln
   (CLI meldet nur die absolute neueste Version `11.0.0` — linientreue Höchstversion
   separat prüfen, Zielstand `10.0.15` bzw. neuere `10.0.x`).
4. `analysis-report.md` im Feature-Verzeichnis erstellen: Tabelle aller direkten
   Pakete (installierte Version, neueste linientreue Version, Update-Typ,
   Schwachstellen-/Deprecation-Status, Risikoklasse, Begründung, Entscheidung
   PR/aufgeschoben/nicht bewertbar).

### Paket-Updates umsetzen

1. In den sechs `*.csproj` die `Version`-Attribute der Update-Kandidaten anheben:
   - `Microsoft.*`-Block einheitlich auf die ermittelte gemeinsame `10.0.x` (13 Referenzen).
   - Third-Party-Updates gemäß Entscheidungstabelle unten (13 Referenzen).
2. `dotnet restore Rezepte.sln`.
3. Build gemäß CI-Reihenfolge: `dotnet build Rezepte.Web/Rezepte.Web.csproj -c Release`
   (Build-Reihenfolgen-Falle: `AIFoto`/`AIUrl` referenzieren `Rezepte.Web.dll` per
   `HintPath`), danach `dotnet build Rezepte.sln -c Release` bzw. direkt Solution-Build.
4. Bei einem Breaking-Change-Befund eines Kandidaten (Build-/Testfehler, neue Warnung):
   das betreffende Update aus dem PR zurücknehmen und im Bericht als aufgeschoben mit
   Begründung führen — kein Fix-Aufwand in diesem PR.

### Verifikation und PR-Erstellung

1. `dotnet test Rezepte.Tests/Rezepte.Tests.csproj` → alle Tests grün (Baseline 774).
2. `dotnet test Rezepte.Tests.Browser/Rezepte.Tests.Browser.csproj` → alle Tests grün
   (Baseline 35); bei aktualisiertem `Microsoft.Playwright` zuvor `playwright.ps1
   install chromium` ausführen, damit die Browser-Revision zum Paket passt.
3. Warnungsprüfung: Build ohne neue Warnungen (CI-Gate `TreatWarningsAsErrors=true`
   greift ohnehin; lokal `dotnet build Rezepte.sln -c Release -p:TreatWarningsAsErrors=true`).
4. `dotnet list Rezepte.sln package --vulnerable --include-transitive` → befundfrei
   (entspricht dem blockierenden CI-Gate).
5. `analysis-report.md` finalisieren (Ist-Versionen, Entscheidungen, aufgeschobene
   Empfehlungen mit Verweis auf das Folge-Issue, Verweis auf `security-scan.yml`).
6. Folge-Issue „Test-Stack-Migration: `xunit.v3` + `bunit` 2.x" anlegen.
7. PR gegen `staging` erstellen, Berichtstabelle in den PR-Body übernehmen.

Beteiligte Artefakte: `Rezepte.Web/Rezepte.Web.csproj`, `Rezepte.Tests/Rezepte.Tests.csproj`,
`Rezepte.Tests.Browser/Rezepte.Tests.Browser.csproj`,
`Rezepte.Import.Plugins.AIFoto/Rezepte.Import.Plugins.AIFoto.csproj`,
`Rezepte.Import.Plugins.AIUrl/Rezepte.Import.Plugins.AIUrl.csproj`,
`Rezepte.Updater.TestHost/Rezepte.Updater.TestHost.csproj`, `NuGet.config`
(unverändert), `analysis-report.md`, `docs/dependencies.md` (nur bei geändertem
Befundstand), PR gegen `staging`, Folge-Issue.

## Paket-Update-Entscheidungen

Risikomatrix der Anforderung: **niedrig** = Patch ohne Breaking Changes,
Dev-/Test-Pakete; **mittel** = Minor ohne Breaking Changes, Patch
produktionskritischer Pakete; **hoch** = Major, Breaking Changes,
EF-Core-Migrationsbedarf, sicherheitsrelevante Major-Sprünge.

| Paket | Projekt(e) | Installiert | Linientreues Ziel | Typ | Risiko | Entscheidung |
|-------|-----------|------------|-------------------|-----|--------|--------------|
| `Microsoft.AspNetCore.Authentication.JwtBearer` | Web | 10.0.9 | 10.0.15 | Patch | mittel (produktionskritisch) | PR |
| `Microsoft.EntityFrameworkCore.Design` | Web | 10.0.11 | 10.0.15 | Patch | niedrig (Design-/Build-Asset) | PR |
| `Microsoft.EntityFrameworkCore.Sqlite` | Web | 10.0.11 | 10.0.15 | Patch | mittel (produktionskritisch, DB-Provider) | PR |
| `QuestPDF` | Web | 2026.7.0 | 2026.9.2 | Minor-artig | mittel (produktiv; Release Notes auf Breaking Changes prüfen, ggf. aufschieben) | PR |
| `Serilog.AspNetCore` | Web | 10.0.0 | — (11.0.0 = .NET-11-Linie) | Major | hoch / Scope-Ausschluss | nicht umsetzen, Bericht |
| `Google.Cloud.Vision.V1` | Web, AIFoto | 3.8.0 | 3.8.0 | — | — | aktuell, keine Aktion |
| `Serilog.Sinks.Console` | Web | 6.1.1 | 6.1.1 | — | — | aktuell, keine Aktion |
| `Serilog.Sinks.File` | Web | 7.0.0 | 7.0.0 | — | — | aktuell, keine Aktion |
| `msTools.Updater` | Web, Tests, TestHost | 0.10.4 | nicht bewertbar (lokaler Feed) | — | nicht bewertbar | keine Aktion, Bericht |
| `Microsoft.AspNetCore.Mvc.Testing` | Tests | 10.0.9 | 10.0.15 | Patch | niedrig (Test-Paket) | PR |
| `Microsoft.EntityFrameworkCore.InMemory` | Tests | 10.0.11 | 10.0.15 | Patch | niedrig (Test-Paket) | PR |
| `Microsoft.Extensions.Caching.Memory` | Tests | 10.0.11 | 10.0.15 | Patch | niedrig (Test-Paket) | PR |
| `Microsoft.NET.Test.Sdk` | Tests, Tests.Browser | 18.7.0 | 18.12.0 | Minor | niedrig | PR |
| `xunit` | Tests, Tests.Browser | 2.9.3 | `xunit.v3` (Nachfolgepaket) | Major + deprecated | hoch | aufschieben, Folge-Issue |
| `xunit.runner.visualstudio` | Tests, Tests.Browser | 3.1.5 | 4.0.1 | Major (nicht breaking für xunit ≥ 1.9.2, s. Designentscheidung) | mittel | PR mit Verifikationsgate |
| `FluentAssertions` | Tests, Tests.Browser | 8.10.0 | 8.11.0 | Minor | niedrig | PR |
| `Moq` | Tests | 4.20.72 | 4.21.0 | Minor | niedrig | PR |
| `bunit` | Tests | 1.40.0 | 2.15.2 | Major | hoch | aufschieben, Folge-Issue |
| `AngleSharp` | Tests | 1.7.3 | 1.10.5 | Minor | niedrig (Security-Pin bleibt oberhalb der früheren vulnerable Version) | PR |
| `coverlet.collector` | Tests, Tests.Browser | 10.0.1 | 10.1.1 | Minor | niedrig | PR |
| `Microsoft.Playwright` | Tests.Browser | 1.61.0 | 1.65.1 | Minor | mittel (Browser-Revision-Kopplung: `playwright.ps1 install chromium` lokal nötig) | PR |
| `Xunit.SkippableFact` | Tests.Browser | 1.5.61 | 1.5.85 | Patch | niedrig | PR |
| `Microsoft.Extensions.Caching.Abstractions` | AIFoto | 10.0.9 | 10.0.15 | Patch | niedrig | PR |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | AIFoto, AIUrl | 10.0.9 | 10.0.15 | Patch | niedrig | PR |
| `Microsoft.Extensions.Logging.Abstractions` | AIFoto | 10.0.9 | 10.0.15 | Patch | niedrig | PR |
| `Microsoft.Extensions.Options` | AIFoto | 10.0.9 | 10.0.15 | Patch | niedrig | PR |
| `Microsoft.Extensions.Logging` | AIUrl | 10.0.9 | 10.0.15 | Patch | niedrig | PR |
| `Microsoft.Extensions.Hosting` | TestHost | 10.0.0 | 10.0.15 | Patch | niedrig (lokales Test-Tool) | PR |

Ergebnis: **26 `PackageReference`-Version-Bumps** (21 verschiedene Pakete) im PR;
**2 Major-Empfehlungen aufgeschoben** (`xunit.v3`, `bunit` 2.x → ein Folge-Issue);
`Serilog.AspNetCore` 11.x und die `Microsoft.*`-11.x-Welle als Scope-Ausschluss im
Bericht dokumentiert; `msTools.Updater` als „nicht bewertbar" geführt.

## Neue Klassen

Keine.

## Änderungen an bestehenden Klassen

Keine — die Änderungen beschränken sich auf `Version`-Attribute von
`<PackageReference>`-Einträgen in den sechs genannten `*.csproj`-Dateien gemäß
der Entscheidungstabelle oben. Es gibt keine Signatur-, Verhaltens- oder
Datenmodelländerung.

## Datenbankmigrationen

Keine. Hinweis: `Microsoft.EntityFrameworkCore.Sqlite` wird nur innerhalb der
10.0.x-Linie gepatcht; EF-Core-Patch-Releases erzeugen keinen Migrationsbedarf.

## Validierungsregeln

Keine.

## Konfigurationsänderungen

Keine. `NuGet.config` bleibt unverändert (`local-repo-packages` → `lib/nuget`,
`nuget.org`). `docs/dependencies.md` wird nur aktualisiert, falls sich der
dort dokumentierte Befundstand tatsächlich ändert (aktuell: kein
Vulnerability-Befund — voraussichtlich keine Änderung nötig).

## Seiteneffekte und Risiken

- **`QuestPDF` 2026.9.2:** Minor-artiges Update eines produktiven PDF-Pakets; bei
  Breaking Changes (Release-Notes-Prüfung) oder Build-/Testfehlern wird das Update
  zurückgenommen und als aufgeschoben dokumentiert.
- **`xunit.runner.visualstudio` 4.0.1:** Major-Sprung mit nachgewiesener
  xunit-2.x-Kompatibilität; Verifikationsgate ist die vollständige Test-Discovery
  und der grüne Lauf beider Suiten — bei Bruch Rollback und Verschiebung ins
  Folge-Issue.
- **`Microsoft.Playwright` 1.65.1:** Neue Paketversion kann eine neue
  Browser-Revision verlangen; lokal `playwright.ps1 install chromium`, CI
  installiert die Browser ohnehin frisch.
- **`Microsoft.EntityFrameworkCore.Sqlite` 10.0.15:** Patch-Update des
  produktiven DB-Providers; abgesichert durch den bestehenden Testbestand inkl.
  SQLite-basierter Integrationstests; Strategievorgaben aus
  `docs/dependencies.md` bleiben unberührt (kein `SQLitePCLRaw`-Pin nötig, solange
  der transitive Stand befundfrei bleibt).
- **Build-Reihenfolgen-Falle:** `dotnet test` auf sauberem Stand schlägt fehl,
  wenn `Rezepte.Web` nicht vorher gebaut wurde (HintPath-Referenzen der
  AI-Plugins) — CI-Reihenfolge ist verbindlich (erst `Rezepte.Web` bzw.
  Solution-Build, dann Tests).
- **`dotnet format`-Gate:** Paketversionsänderungen sind formatneutral; kein
  Risiko für Gate 1 der `pr-staging-ci.yml` erwartet.

## Umsetzungsreihenfolge

1. **Befund-Stichtag erneuern und linientreue Zielversionen verifizieren**
   - Voraussetzungen: Keine (restore-fähiger Stand; `NuGet.config` mit lokalem Feed vorhanden).
   - Beschreibung: `dotnet restore Rezepte.sln`; `dotnet list Rezepte.sln package --outdated|--vulnerable --include-transitive|--deprecated` erneut ausführen; pro `Microsoft.*`-Paket die höchste `10.0.x` ermitteln (erwartet `10.0.15`); `QuestPDF`-Release-Notes auf Breaking Changes prüfen.
2. **`analysis-report.md` anlegen**
   - Voraussetzungen: Schritt 1 (aktuelle Befundzahlen).
   - Beschreibung: Berichtstabelle aller direkten Pakete mit Versionen, Update-Typ, Security-/Deprecation-Status, Risikoklasse, Begründung, Entscheidung — als Deliverable im Feature-Verzeichnis.
3. **`Microsoft.*`-Drift vereinheitlichen**
   - Voraussetzungen: Schritt 1 (verifizierte gemeinsame `10.0.x`).
   - Beschreibung: In `Rezepte.Web`, `Rezepte.Tests`, `Rezepte.Import.Plugins.AIFoto`, `Rezepte.Import.Plugins.AIUrl`, `Rezepte.Updater.TestHost` alle direkten `Microsoft.*`-Referenzen auf den einheitlichen Stand anheben (13 Referenzen).
4. **Third-Party-Updates umsetzen**
   - Voraussetzungen: Schritt 1; `QuestPDF`-Release-Notes geprüft.
   - Beschreibung: `QuestPDF` → 2026.9.2 (Web); in beiden Testprojekten `Microsoft.NET.Test.Sdk` → 18.12.0, `FluentAssertions` → 8.11.0, `coverlet.collector` → 10.1.1, `xunit.runner.visualstudio` → 4.0.1; `Moq` → 4.21.0, `AngleSharp` → 1.10.5 (Tests); `Microsoft.Playwright` → 1.65.1, `Xunit.SkippableFact` → 1.5.85 (Tests.Browser).
5. **Restore + Build verifizieren**
   - Voraussetzungen: Schritte 3–4.
   - Beschreibung: `dotnet restore Rezepte.sln`; `dotnet build Rezepte.Web/Rezepte.Web.csproj -c Release` (Reihenfolgen-Falle), danach `dotnet build Rezepte.sln -c Release -p:TreatWarningsAsErrors=true` — fehlerfrei, keine neuen Warnungen. Bei Bruch: betroffenes Update zurücknehmen und im Bericht als aufgeschoben dokumentieren.
6. **Testbestand verifizieren**
   - Voraussetzungen: Schritt 5; bei Playwright-Update `playwright.ps1 install chromium` (Playwright-CLI aus `Microsoft.Playwright`).
   - Beschreibung: `dotnet test Rezepte.Tests/Rezepte.Tests.csproj` (774 Tests grün) und `dotnet test Rezepte.Tests.Browser/Rezepte.Tests.Browser.csproj` (35 Tests grün); explizit Test-Discovery unter `xunit.runner.visualstudio` 4.x verifizieren.
7. **Vulnerability-Abschlussbefund**
   - Voraussetzungen: Schritte 3–4.
   - Beschreibung: `dotnet list Rezepte.sln package --vulnerable --include-transitive` befundfrei; `docs/dependencies.md` nur anpassen, falls sich der Befundstand ändert.
8. **Bericht finalisieren, Folge-Issue und PR erstellen**
   - Voraussetzungen: Schritte 5–7; `analysis-report.md` aus Schritt 2.
   - Beschreibung: Bericht mit Ist-Ergebnis vervollständigen (inkl. aufgeschobener Empfehlungen und Verweis auf `security-scan.yml`); Folge-Issue „Test-Stack-Migration: `xunit.v3` + `bunit` 2.x" anlegen; PR gegen `staging` mit Berichtstabelle im PR-Body.

## Tests

### Neue Tests

Keine — Paketwartung ohne Verhaltensänderung; die Anforderung sieht ausdrücklich
den bestehenden Testbestand als Akzeptanznachweis vor.

### Betroffene bestehende Tests

Keine — geplante Updates sind Patch-/Minor-Stände ohne erwartete
API-Änderung. Es gilt die Fallback-Regel: Zeigt ein Update doch
Testbruch (Kompilier- oder Laufzeitfehler), wird das Update aus dem PR
zurückgenommen und als aufgeschobene Empfehlung dokumentiert — es werden
keine Tests an ein Breaking Change „angepasst".

### E2E-Tests (primärer Funktionsnachweis)

Keine neuen E2E-Tests erforderlich — begründete Ausnahme: Die Anforderung ändert
keinen über UI oder Nutzeraktion erreichbaren Ablauf; der gesamte „Code-Change"
sind Versionsattribute in `*.csproj`. Der Funktionsnachweis erfolgt über den
bestehenden automatisierten Bestand einschließlich der 35 Playwright-E2E-Tests
von `Rezepte.Tests.Browser` (die real publishte App gegen Chromium), ergänzt um
die CI-Gates (`TreatWarningsAsErrors`-Build, Security-Scan, Coverage-Gate). Das
reicht als Abnahmenachweis aus, weil jede relevante Regression eines Updates
(Build-, Laufzeit-, Rendering-Bruch) durch diesen Bestand sichtbar würde.

Bestehende E2E-Tests betroffen: Keine — vorausgesetzt alle Updates bleiben
verhaltensneutral; bei Playwright-Update ist die Browser-Installation zu
aktualisieren (s. Seiteneffekte).

## Akzeptanzszenarien

| # | Szenario | Auslöser | Ausgangszustand | Schritte | Sichtbare Wirkung | Nachweis |
|---|----------|----------|------------------|----------|-------------------|----------|
| 1 | Happy Path: Paketprüfung mit Update-PR | Issue #227 zur Bearbeitung geöffnet | Branch auf `staging`-Basis; Paketstände gemäß Inventory (Drift `10.0.0`/`10.0.9`/`10.0.11`, 28 direkte Pakete); kein `analysis-report.md`, kein Folge-Issue vorhanden | Befund-Stichtag neu ziehen → Bericht anlegen → 26 Version-Bumps in 6 `*.csproj` → `dotnet restore` → Build `Rezepte.Web` + Solution fehlerfrei → `dotnet test` beider Suiten grün → Vulnerability-Scan befundfrei → Folge-Issue anlegen → PR gegen `staging` | PR gegen `staging` mit Analysebericht-Tabelle im Body; `Microsoft.*` einheitlich auf `10.0.x`; Issue #227 damit fachlich abgearbeitet | `dotnet build` fehlerfrei ohne neue Warnungen; `dotnet test` 774+35 grün; `dotnet list --vulnerable --include-transitive` befundfrei; CI-Gates des PR grün |
| 2 | Update erweist sich als breaking | Build-/Testlauf nach Version-Bump | Wie Szenario 1, aber ein Update-Kandidat (z. B. `QuestPDF` oder `xunit.runner.visualstudio`) verursacht Buildfehler, neue Warnung oder Testbruch | Fehler dem Update zuordnen → Update zurücknehmen (`Version` zurücksetzen) → im `analysis-report.md` als „aufgeschoben" mit Begründung führen → ggf. Folge-Issue ergänzen → Schritte aus Szenario 1 ohne dieses Update | PR enthält nur unbedenkliche Updates; aufgeschobener Kandidat ist im Bericht transparent dokumentiert | Wiederholter Build/Test grün; Bericht nennt die Entscheidung |
| 3 | Neuer Schwachstellenbefund zum Umsetzungsstichtag | `dotnet list --vulnerable --include-transitive` im Schritt 1 | Wie Szenario 1, aber inzwischen Advisory gegen ein installiertes Paket | Befund bewerten → Fix-Update innerhalb erlaubter Linie im selben PR umsetzen (Security priorisiert, s. Designentscheidung); bei Major mit Breaking-Nachweis Folge-Issue mit Risikoentscheidung | Schwachstelle im PR behoben bzw. begründet ausgelagert; CI-Security-Gate würde sonst blockieren | Abschlussbefund `dotnet list --vulnerable` befundfrei bzw. dokumentierte Ausnahme |

## Offene Punkte

Keine — alle sechs Fragen der Anforderung wurden als begründete
Default-Entscheidungen in den Abschnitt „Designentscheidungen" überführt
(Security-Major-Fall nicht eingetreten; `msTools.Updater` = nicht bewertbar;
Deduplizierung per Verweis auf `security-scan.yml`; einzelner PR;
Analysebericht im PR-Body mit `analysis-report.md` als Repo-Artefakt;
Drift-Vereinheitlichung auf höchste gemeinsame `10.0.x`).
