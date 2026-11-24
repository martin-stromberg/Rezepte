# Anforderungsübersetzung

## Metadaten

- Aufgaben-ID: `5303623c-1534-4010-9f5c-f1bdd4dea813`
- Branch: `task/issue-237-5303623c153440109f5cf1bdd4dea813-major-version`
- Basisbranch: `staging`
- Kundenanforderung: GitHub Issue #237 – Major-Versionssprung für die .NET-11-Migration (Breaking Change an der Plugin-Schnittstelle) plus Maintenance-Branch für die .NET-10-Linie; Spiegelung desselben Schritts im Plugin-Repository `martin-stromberg/Rezepte.Plugins`.

## Fachliche Zusammenfassung

Die auf `staging` liegende Migration der Solution auf .NET 11 (Commit `e9aed2d feat: Migrate solution to .NET 11`, PR #235) macht die bisher für .NET 10 gebauten Import-Plugins inkompatibel – ein Breaking Change an der Plugin-Schnittstelle, der versionsseitig als solcher sichtbar sein muss. Das nächste Release auf `main` soll daher einen Major-Versionssprung erzeugen (erwartet `v2.0.0`), dessen Release Notes die Inkompatibilität zu .NET-10-Plugins explizit als Breaking Change ausweisen. Parallel wird die .NET-10-Linie über einen eigenen Maintenance-Branch releasefähig gehalten, und für das Plugin-Repository `martin-stromberg/Rezepte.Plugins` wird ein verlinktes Folge-Issue mit dem analogen Maintenance-/Major-Release-Schritt angelegt.

## Fachlicher Scope

- **Verarbeitungseinheit:** Die Release-/Versionslinie der Anwendung im Git-Branching- und semantic-release-Modell – konkret (a) der anstehende Promotion-Merge `staging -> main`, der den .NET-11-Stand als Major-Release veröffentlicht, und (b) die neu anzulegende .NET-10-Maintenance-Linie, die aus dieser Versionsfamilie heraus weiterhin Fixes releasen kann.
- **Auslösende Ereignisse:**
  - Zeitkritischer Fixpunkt: die nächste Staging→Main-Promotion (Draft-PR wird automatisch durch `staging-to-main-promotion.yml` erzeugt, Merge erfordert manuelle Maintainer-Freigabe). Der Maintenance-Branch muss **vor** diesem Merge angelegt oder explizit vom letzten .NET-10-Commit von `main` abgezweigt werden – `main` steht aktuell noch auf .NET 10 (Promotion #231).
  - Maintainer-Aktion beim Merge des Promotion-PRs: der resultierende Merge-Commit muss einen `BREAKING CHANGE:`-Vermerk im Body/Footer tragen, weil der Migrations-Commit `e9aed2d` selbst keinen Major-Marker (`BREAKING CHANGE:`/`!`-Suffix) enthält und semantic-release (Preset `conventionalcommits`) sonst nur ein Minor-Release erzeugen würde.
  - Jeder spätere Push auf den Maintenance-Branch: löst – sofern konfiguriert – weiterhin semantic-release-Versionierung für die .NET-10-Linie aus.
  - Einmalige, issue-getriebene Aktion: Anlage eines verlinkten Issues im Repository `martin-stromberg/Rezepte.Plugins`.
- **Erwartete Wirkung:**
  - Auf `main` entsteht ein GitHub Release mit Tag `v2.0.0` (erwartet), dessen Release Notes die Plugin-Inkompatibilität als Breaking Change benennen; die Release-Pipeline publiziert die üblichen Assets (`release-linux-x64.zip`, `release-win-x64.zip`, `update.json`, Import-Contract-ZIP, `contract-export.metadata.json`).
  - Fixes auf dem Maintenance-Branch erzeugen eigenständige Releases innerhalb der 1.x-Versionslinie, ohne die `main`-Linie zu beeinflussen.
  - Im Plugin-Repository existiert ein von hier verlinktes Issue, das .NET-11-Anhebung, eigenen .NET-10-Maintenance-Branch und Major-Release (`v0.3.7` → erwartet `v1.0.0`) abdeckt.
  - Nutzer externer Plugins werden über Release Notes/Dokumentation auf die erforderliche Plugin-Aktualisierung hingewiesen (offener Punkt, siehe unten).
- **Umgang mit fehlenden Objekten:**
  - Maintenance-Branch (z. B. `1.x` oder benannt mit `range`-Konfiguration): **erzeugen** – vom letzten .NET-10-Stand von `main` abzweigen; der konkrete SHA ist zu dokumentieren, falls die Anlage erst nach der Promotion erfolgt.
  - semantic-release-Branch-Eintrag für die Maintenance-Linie: **erzeugen** – `release.config.js` (`branches: ["main"]`) um einen Eintrag mit Versions-Range erweitern; ein frei gewählter Name wie `maintenance/net10` benötigt eine explizite `range`-Angabe.
  - `BREAKING CHANGE:`-Vermerk im Promotion-Merge-Commit: **erzeugen** – im Merge-Commit-Body eintragen (oder gleichwertig `!`-Suffix; der Merge wird manuell durch einen Maintainer ausgeführt).
  - Issue im Repository `martin-stromberg/Rezepte.Plugins`: **erzeugen** und in Issue #237 verlinken.
  - Ggf. neue `ImportContractVersion` / ApiCompat-Baseline: **zu klären** – ob die Vertragsversion (aktuell `0.3.0` in `Directory.Build.props`) mit dem Major-Release angehoben werden muss, ist offen.
- **Scope-Ausschlüsse:**
  1. Keine Code-Änderung an der Anwendung selbst: die .NET-11-Migration ist mit `e9aed2d` bereits auf `staging` abgeschlossen; diese Anforderung betrifft ausschließlich Branching, Release-Konfiguration und Commit-/Release-Kommunikation.
  2. Keine Umsetzung der Plugin-Migration in diesem Repository: die .NET-11-Anhebung der privaten Plugins erfolgt im Repository `martin-stromberg/Rezepte.Plugins`; hier ist nur das verlinkte Folge-Issue zu erstellen.
  3. Kein Rückfluss von Maintenance-Fixes nach `main` über den normalen PR-Weg: `verify-pr-source.yml` lässt PRs nach `main` ausschließlich von `staging` zu; die Maintenance-Linie releaset eigenständig (Übernahme einzelner Fixes in `staging`/`main` bleibt dem üblichen Feature-Branch-Flow vorbehalten).
  4. Kein neuer automatischer Promotion-/Merge-Mechanismus: der Merge des Promotion-PRs bleibt eine manuelle Maintainer-Entscheidung; die Anforderung verlangt nur den korrekten `BREAKING CHANGE:`-Vermerk im Merge-Commit.
  5. Keine Änderung am RC-Prerelease-Mechanismus auf `staging` (`staging-ci.yml` mit `--branches`-Override bleibt unverändert, sofern die Maintenance-Konfiguration ihn nicht kollidiert – siehe Implementierungsansatz).

## Betroffene Klassen und Komponenten

Diese Anforderung betrifft keinen Anwendungscode, sondern Git-Branches, Release-Konfiguration und CI-Workflows:

- `release.config.js` (`module.exports.branches`, aktuell `["main"]`, Zeile 74) – Erweiterung um den Maintenance-Branch inkl. `range`-Konfiguration.
- `scripts/resolve-release-version.mjs` – `AUTOMATIC_RELEASE_BRANCHES = ["main"]` (Zeile 23) und `classifyWorkflowRef` (wirft für Nicht-`main`-Branches „Unsupported release ref"): damit Pushes auf den Maintenance-Branch tatsächlich Releases erzeugen, ist hier voraussichtlich eine Erweiterung nötig. *(Ableitung aus dem Code, im Issue nicht explizit genannt – als Annahme gekennzeichnet.)*
- `.github/workflows/release.yml` – `on.push.branches: [main]` (Zeile 12): der Release-Workflow startet aktuell nicht bei Pushes auf einen Maintenance-Branch; Trigger-Erweiterung voraussichtlich erforderlich. *(Ableitung, Annahme.)*
- `.github/workflows/staging-ci.yml` – „version"-Job mit `--branches`-Override gegen `release.config.js`: prüfen, ob ein zusätzlicher Branch-Eintrag die RC-Versionsermittlung auf `staging` beeinflusst. *(Ableitung, Annahme.)*
- `.github/workflows/verify-pr-source.yml`, `staging-to-main-promotion.yml`, `sync-staging-with-main.yml` – voraussichtlich unverändert; Wechselwirkungen des Maintenance-Branches mit dem Staging-Flow sind zu prüfen (z. B. ob `sync-staging-with-main`/`staging-to-main-promotion` auf Maintenance-Pushes reagieren dürfen).
- `Directory.Build.props` – `<ImportContractVersion>0.3.0</ImportContractVersion>` (Zeile 6): nur betroffen, falls die offene Frage zum Vertrags-Bump mit „ja" beantwortet wird; dann sind zusätzlich `contract-baselines/import-contract/<semver>/`, `scripts/Export-ImportContract.ps1` und `Rezepte.Tests/ContractExport/ContractExportScriptTests.cs` betroffen (Prozedere in `Docs/help/import-plugins.md` beschrieben).
- Git-Operationen: Maintenance-Branch vom letzten .NET-10-Commit von `main`; `BREAKING CHANGE:`-Footer im Promotion-Merge-Commit.
- GitHub-Artefakte: neues Issue in `martin-stromberg/Rezepte.Plugins` mit Verlinkung in Issue #237.
- Dokumentation: `Docs/help/github-actions.md` (Branch-Modell/Versionierung um die Maintenance-Linie ergänzen), `Docs/RELEASE_NOTES.md` bzw. Release Notes (Breaking-Change-Hinweis für externe Plugin-Nutzer).
- Tests: keine Anwendungstests betroffen; Validierung erfolgt über den realen Release-Lauf (Akzeptanzkriterium) und ggf. semantic-release-Dry-Runs. Vorhandene Node-seitige Tests für `resolve-release-version.mjs` (sofern vorhanden) bei Erweiterung von `AUTOMATIC_RELEASE_BRANCHES` mitziehen.

## Implementierungsansatz

1. **Letzten .NET-10-Stand von `main` identifizieren und sichern:** Konkreten SHA ermitteln (Stand der Promotion #231; `e9aed2d` liegt nur auf `staging`). Maintenance-Branch vor der nächsten Promotion davon abzweigen – oder, falls die Promotion zuerst erfolgt, explizit vom dokumentierten SHA.
2. **Maintenance-Branch benennen und releasen:** Entweder einen Range-Namen verwenden, den semantic-release nativ als Maintenance-Branch erkennt (z. B. `1.x`), oder einen freien Namen (z. B. `maintenance/net10`) mit expliziter `range`-Konfiguration in `release.config.js`. Verbindliche Konvention ist ein offener Punkt.
3. **Release-Pfad für den Maintenance-Branch durchziehen:** `release.config.js` (`branches`), `release.yml` (`on.push.branches`) und `resolve-release-version.mjs` (`AUTOMATIC_RELEASE_BRANCHES`) konsistent erweitern, damit ein Push auf dem Maintenance-Branch denselben Release-Ablauf (Version via semantic-release, Assets via `build-and-package`, GitHub Release) durchläuft wie `main` – jedoch innerhalb der 1.x-Range. Kollisionen mit dem Tag-Namespace (`v*.*.*`) und der Asset-Reparatur-Logik gegen bestehende Releases prüfen.
4. **Major-Release sicherstellen:** Beim manuellen Merge des Promotion-PRs `staging -> main` den Merge-Commit mit einem `BREAKING CHANGE:`-Footer versehen, der die Inkompatibilität zu .NET-10-Import-Plugins benennt (z. B. „BREAKING CHANGE: Import plugins built for .NET 10 are not compatible with the .NET 11 application"). Alternativ/ergänzend kann ein Commit auf `staging` vor der Promotion den Marker tragen.
5. **Plugin-Repo-Issue anlegen und verlinken:** Issue in `martin-stromberg/Rezepte.Plugins` mit den drei Mindestinhalten (.NET-11-Anhebung ab `net10.0` in `Directory.Build.props` inkl. SDK-/Dependency-Updates; .NET-10-Maintenance-Branch analog inkl. Release-Konfiguration; Major-Release `v0.3.7` → erwartet `v1.0.0`) und Rückverlinkung in Issue #237.
6. **Dokumentation:** `Docs/help/github-actions.md` um das Maintenance-Branch-Modell ergänzen; Release Notes/Doku mit Hinweis für externe Plugin-Nutzer (abhängig vom offenen Punkt).

Abhängigkeiten: keine Anwendungsklassen; Abhängigkeiten bestehen zu semantic-release-Konfiguration, den oben genannten Workflows/Skripten und zum externen Repository `martin-stromberg/Rezepte.Plugins`.

## Konfiguration

- `release.config.js` `branches`: Erweiterung um den Maintenance-Branch. semantic-release verlangt für Maintenance-Releases eine Versions-Range – entweder implizit über den Branch-Namen (`1.x`) oder explizit über `{ name: "<branch>", range: "1.x" }`. Die verbindliche Namenskonvention ist offen (siehe Offene Fragen).
- `release.yml` `on.push.branches` und `resolve-release-version.mjs` `AUTOMATIC_RELEASE_BRANCHES`: analoge Erweiterung, falls der Maintenance-Branch automatische Releases auslösen soll (Ableitung aus dem Code, nicht explizit im Issue – Annahme).
- Keine neue Anwendungs-/Benutzerkonfiguration; keine Änderungen an `Directory.Build.props`, sofern der Vertrags-Bump nicht beschlossen wird.

## Akzeptanzkriterien

1. Es existiert ein gepushter Maintenance-Branch, der vom letzten .NET-10-Stand von `main` abzweigt und dessen letzter .NET-10-SHA dokumentiert ist.
2. `release.config.js` ist so konfiguriert, dass Pushes auf dem Maintenance-Branch semantic-release-Versionierung innerhalb der 1.x-Linie auslösen.
3. Der nächste Merge des Promotion-PRs nach `main` erzeugt ein Release mit neuem Major-Tag (erwartet `v2.0.0`); die Release Notes weisen die Inkompatibilität zu .NET-10-Plugins als Breaking Change aus.
4. Im Repository `martin-stromberg/Rezepte.Plugins` existiert ein Issue mit den drei geforderten Inhalten, verlinkt von/zu Issue #237.
5. Der Staging-Flow (RC-Prereleases, Promotion-/Sync-PRs, `verify-pr-source`) funktioniert nach der Erweiterung unverändert weiter.

## Explizite Nicht-Ziele

- Keine funktionalen Änderungen an der Anwendung oder am Plugin-Vertrag-Code (die .NET-11-Migration ist bereits auf `staging` umgesetzt).
- Keine Plugin-Migration in diesem Repository (erfolgt in `martin-stromberg/Rezepte.Plugins`).
- Keine Anhebung von `ImportContractVersion` ohne vorherige Klärung (offene Frage).
- Keine Änderung des manuellen Merge-Gate für Promotion-PRs.
- Kein Backport-Automatismus zwischen Maintenance-Branch und `main`/`staging`.

## Offene Fragen

1. Verbindliche Benennung und `range`-Konfiguration der Maintenance-Branches in beiden Repositories (`1.x` vs. freier Name wie `maintenance/net10` mit expliziter `range`).
2. Muss die Plugin-Vertragsversion (`ImportContractVersion`, aktuell `0.3.0` in beiden Repos) zusammen mit dem Major-Release angehoben werden – obwohl die API-Fläche unverändert blieb und die Inkompatibilität allein aus dem Framework-Sprung resultiert?
3. Abstimmung der Release-Reihenfolge zwischen App und Plugins: die Plugins müssen zum Major-Release der App kompatibel sein – wer released zuerst, und wie wird die kompatible Kombination kommuniziert?
4. Konkreten SHA des letzten .NET-10-Stands von `main` verbindlich dokumentieren (insbesondere falls die Branch-Anlage erst nach der Promotion erfolgt).
5. Umfang und Ort des Nutzerhinweises für externe Plugin-Betreiber (Release Notes vs. `Docs/help/import-plugins.md` bzw. `Docs/RELEASE_NOTES.md`).
6. Soll der Maintenance-Branch vollständig den `release.yml`-Pfad (alle Assets inkl. Contract-ZIP, Asset-Reparatur) durchlaufen oder ein reduziertes Release-Profil erhalten? *(Aus der Bestandsaufnahme abgeleitete Zusatzfrage.)*

## Referenzen

- GitHub Issue #237 (Repository `martin-stromberg/Rezepte`)
- Migrations-Commit `e9aed2d feat: Migrate solution to .NET 11` (PR #235, auf `staging`)
- Promotion #231 (letzter .NET-10-Stand auf `main`)
- `Docs/help/github-actions.md` (Branch-Modell, Versionierung), `Docs/help/import-plugins.md` (Breaking-Change-Workflow des Plugin-Vertrags)
