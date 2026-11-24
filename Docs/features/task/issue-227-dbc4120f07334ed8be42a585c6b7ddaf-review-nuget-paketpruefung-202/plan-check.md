# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| Vollständige Paketprüfung aller direkten `<PackageReference>` der `Rezepte.sln` (Updates, Schwachstellen, Deprecation, Breaking Changes) | Erneute Befund-Stichtags-Ausführung aller drei `dotnet list`-Varianten (Programmablauf „Befund-Stichtag verifizieren", Umsetzungsreihenfolge Schritt 1); Entscheidungstabelle „Paket-Update-Entscheidungen" enthält alle 28 Pakete aus den 6 Projekten mit Paketreferenzen — Abgleich mit `inventory/packages.md` ergibt keinen Ausfall | Stichtags-Logs werden als Artefakte abgelegt; Abschlussbefund in Schritt 7 | Abgedeckt |
| Analysebericht als Tabelle aller geprüften Pakete (installierte Version, neueste Version, Update-Typ, Security-/Deprecation-Status, Risikobewertung, Begründung) | `analysis-report.md` im Feature-Verzeichnis als Deliverable (Schritt 2), Inhalt wird in den PR-Body übernommen (Designentscheidung „Analysebericht"); beschriebene Spalten decken die geforderten Felder ab | Bericht ist expliziter Schritt in der Umsetzungsreihenfolge und Nachweis in Akzeptanzszenario 1 | Abgedeckt |
| Ein PR gegen `staging` mit allen Updates der Risikoklassen niedrig–mittel | Designentscheidung „PR-Struktur" (ein PR, Umfang begründet); 26 Version-Bumps in 6 `*.csproj` gemäß Entscheidungstabelle; Ziel-Branch `staging` in Übersicht, Szenario 1 und Schritt 8 | Verifikationskette Restore → CI-Reihenfolgen-Build → `dotnet test` beider Suiten → Vulnerability-Scan (Schritte 5–7) | Abgedeckt |
| `Microsoft.*`-Versionsdrift (`10.0.0`/`10.0.9`/`10.0.11`) vereinheitlichen | Designentscheidung „Einheitlicher `Microsoft.*`-Zielstand" (alle 13 `Microsoft.*`-Referenzen auf höchste gemeinsame `10.0.x`, erwartet `10.0.15`, mit Verfügbarkeits-Prüfung zum Umsetzungsstichtag); Schritt 3 der Umsetzungsreihenfolge; abgeleitete Frage 6 als Festlegung übernommen | Build/Test-Verifikation nach dem Bump; einheitlicher Stand ist Teil der sichtbaren Wirkung in Szenario 1 | Abgedeckt |
| Risikoklassifikation niedrig/mittel/hoch gemäß Matrix der Anforderung | Risikomatrix vor der Entscheidungstabelle explizit wiederholt und pro Paket in Spalte „Risiko" angewendet (inkl. Begründungen, z. B. produktionskritisch vs. Test-Paket) | Risikoklasse ist Spalte des Analyseberichts | Abgedeckt |
| Aufgeschobene/risikoreiche Empfehlungen dokumentieren + Folge-Issue bei Major/Breaking | `xunit` → `xunit.v3` und `bunit` 1.x → 2.x als Risiko hoch aufgeschoben, ein gebündeltes Folge-Issue „Test-Stack-Migration" (Designentscheidung + Schritt 8); `Serilog.AspNetCore` 11.0.0 als Scope-Ausschluss im Bericht dokumentiert | Folge-Issue-Erzeugung ist fester Schritt 8 und Teil der sichtbaren Wirkung in Szenario 1 | Abgedeckt |
| Verifikation je Update-Runde: Restore, Build fehlerfrei, `dotnet test` grün, keine neuen Warnungen, abschließender Vulnerability-Befund befundfrei | Schritte 5–7 der Umsetzungsreihenfolge: `dotnet restore`, Build in CI-Reihenfolge (erst `Rezepte.Web`, dann Solution) mit `-p:TreatWarningsAsErrors=true`, `dotnet test` beider Suiten (Baselines 774 + 35), `dotnet list --vulnerable --include-transitive` befundfrei | Direkt als Schritte und als Nachweisspalte der Akzeptanzszenarien verankert | Abgedeckt |
| Bestehender Testbestand als Akzeptanznachweis (keine neuen Tests) | Abschnitt „Tests": keine neuen Tests, keine betroffenen bestehenden Tests, Fallback-Regel bei unerwartetem Testbruch (Update zurücknehmen statt Tests anzupassen) | Explizite Begründung im Plan | Abgedeckt |
| Lokaler Feed `lib/nuget` für `msTools.Updater` wiederverwenden, nicht gegen nuget.org auflösen | Scope-Klärung „Umgang mit fehlenden Objekten" + Scope-Ausschluss Nr. 6; Paket im Bericht als „nicht bewertbar" gekennzeichnet (Designentscheidung) | Entscheidungstabelle führt `msTools.Updater` als „keine Aktion, Bericht" | Abgedeckt |
| Deduplizierung mit `security-scan.yml` (montags-Scan) | Designentscheidung „Deduplizierung" (Verweis statt Kopie, identischer Befundbefehl); Abschlussbericht enthält Verweis (Schritt 8) | In Berichts-Finalisierung verankert | Abgedeckt |
| Scope-Ausschlüsse der Anforderung (npm, GitHub-Actions-Versionen, SDK/`TargetFramework`, .NET-11-Welle, transitive Updates, Issue-Template, DB-/Providerwechsel) | Scope-Ausschlüsse 1–6 im Plan decken sämtliche Ausschlüsse ab, jeweils mit Begründung und Status `bestätigt` | Nicht testbezogen; Scope-festgeschrieben | Abgedeckt |
| Umgang mit fehlenden Objekten (Folge-Issue erzeugen, Bericht nachliefern, lokalen Feed nutzen) | Scope-Klärung-Feld „Umgang mit fehlenden Objekten" vollständig gefüllt (Folge-Issue → erzeugen; `analysis-report.md` → erzeugen und in PR-Body übernehmen; lokaler Feed → wiederverwenden) | Ausgangszustand von Szenario 1 benennt explizit „kein `analysis-report.md`, kein Folge-Issue vorhanden" | Abgedeckt |

## Fehlende oder unvollständige Testanforderungen

Entfällt — Status: Plan vollständig.

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Kein UI-/Nutzerfluss betroffen (Wartungslauf: ausschließlich `Version`-Attribute in `*.csproj`; keine Klassen, keine UI, kein Datenmodell) | Keine neuen E2E-Tests geplant; Regressionsnachweis über den bestehenden automatisierten Bestand einschließlich der 35 Playwright-E2E-Tests von `Rezepte.Tests.Browser` (real publishte App gegen Chromium) plus CI-Gates (`TreatWarningsAsErrors`-Build, Security-Scan, Coverage-Gate) | Nicht erforderlich — mit Begründung im Plan-Abschnitt „E2E-Tests" dokumentiert; der Verzicht ist nachvollziehbar begründet, da kein über UI/Nutzeraktion erreichbarer Ablauf geändert wird |

## Fehlende oder unvollständige Planbestandteile

Entfällt — Status: Plan vollständig.

## Hinweise

- **Vollständigkeit der Entscheidungstabelle verifiziert:** Die 28 Zeilen der Tabelle „Paket-Update-Entscheidungen" decken exakt die 28 in `inventory/packages.md` inventarisierten Pakete ab (inkl. der nicht veralteten Pakete `Google.Cloud.Vision.V1`, `Serilog.Sinks.*`, `xunit`, `msTools.Updater` mit expliziter „keine Aktion"-/„nicht bewertbar"-Begründung). Die Bumps-Rechnung (13 `Microsoft.*` + 13 Third-Party = 26 `PackageReference`-Anhebungen) ist konsistent mit den 37 Referenzen des Inventars.
- **`Serilog.AspNetCore` 11.0.0 ohne Folge-Issue:** Der Plan dokumentiert den Major-Kandidaten nur im Bericht (Scope-Ausschluss Nr. 1) statt ein Folge-Issue anzulegen. Das ist schlüssig, weil es sich nicht um eine aufgeschobene Empfehlung innerhalb des Scopes handelt, sondern um einen expliziten Anforderungs-Ausschluss (.NET-11-Welle); bei der Umsetzung ist lediglich sicherzustellen, dass der Bericht die Version transparent ausweist.
- **Neue Warnungen:** Das Kriterium „keine neuen Warnungen" ist doppelt abgesichert (lokaler `TreatWarningsAsErrors`-Build in Schritt 5 und CI-Gate 3 der `pr-staging-ci.yml`).
- **Risiko `xunit.runner.visualstudio` 4.0.1:** Die Major-Einstufung wird per begründeter Kompatibilitätsanalyse (xunit ≥ 1.9.2 weiterhin unterstützt) auf „mittel" heruntergestuft — inklusive Rollback-Pfad bei Bruch. Das ist eine vertretbare, im Plan begründete Abweichung von der starren „Major = hoch"-Regel; Szenario 2 deckt den Bruchfall ab.
- **Prerelease-Hinweis:** Die Anforderung erwähnt `--outdated` „inkl. Prereleases, soweit relevant"; der Plan formuliert die Stichtags-Befehle ohne Prerelease-Flag. Da kein Paket prerelease-relevante Kandidaten hat und linientreue Zielversionen ohnehin separat ermittelt werden, ist das kein Mangel — allenfalls eine Kleinigkeit bei der Ausführung zu beachten.
- **Issue-Kommentar-Fallback:** Der Plan legt den Bericht verbindlich in den PR-Body plus `analysis-report.md` im Repo; der Anforderungs-Fallback „Bericht als Issue-Kommentar nachliefern, falls er im PR fehlt" bleibt implizit, ist aber durch das Repo-Artefakt faktisch mitabgedeckt.
