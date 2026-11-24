# Umsetzungsplan: Major-Versionssprung (.NET 11), `1.x`-Maintenance-Branch und Folge-Issue im Plugin-Repository

## Übersicht

GitHub Issue #237 verlangt drei Dinge: (a) das nächste Release auf `main` muss einen Major-Sprung erzeugen (erwartet `v2.0.0`), weil die auf `staging` liegende .NET-11-Migration (`e9aed2d`) für .NET 10 gebaute Import-Plugins inkompatibel macht; (b) die .NET-10-Linie bleibt über einen neuen Maintenance-Branch `1.x` releasefähig; (c) das Plugin-Repository `martin-stromberg/Rezepte.Plugins` erhält ein verlinktes Folge-Issue mit dem analogen Schritt. Betroffen sind ausschließlich Git-Branches, Release-/CI-Konfiguration und Dokumentation — kein Anwendungscode.

## Scope-Klärung

| Feld | Festlegung |
|------|------------|
| Verarbeitungseinheit | Die Release-/Versionslinie der Anwendung im Git-Branching- und semantic-release-Modell: der Promotion-Merge `staging → main` (Major-Release) und die neue .NET-10-Maintenance-Linie `1.x`. |
| Auslösende Ereignisse | (1) Implementierungs-Commit mit `BREAKING CHANGE:`-Footer auf diesem Feature-Branch (landet über PR → `staging` → Promotion in `main`s Historie). (2) Einmaliger direkter Push des neuen `1.x`-Branches inkl. Config-Commit. (3) Manueller Maintainer-Merge des Promotion-Draft-PRs `staging → main`. (4) Jeder spätere Push auf `1.x`. (5) Einmalige Remote-Aktion: Issue-Anlage in `martin-stromberg/Rezepte.Plugins` plus Verlinkung in Issue #237. |
| Erwartete Wirkung | GitHub Release `v2.0.0` auf `main` mit „BREAKING CHANGES"-Abschnitt in den generierten Notes und allen Standard-Assets; ein gepushter `1.x`-Branch, auf dem Pushes semantic-release-Releases innerhalb der `1.x`-Range auslösen (Fix → `v1.22.1` usw.), ohne jemals das „Latest"-Flag zu übernehmen; ein beidseitig verlinktes Issue im Plugin-Repo. |
| Umgang mit fehlenden Objekten | Maintenance-Branch `1.x`: **erzeugen** — Abzweigpunkt `bf108b702907d2ee715bdc0341cedece5d55bf3a` (letzter .NET-10-Stand von `main`, Promotion #231; `v1.22.0` ist als In-Range-Tag erreichbar). Branch-Config (`release.yml`-Trigger, `release.config.js`-Eintrag, `AUTOMATIC_RELEASE_BRANCHES`, `pr-staging-ci`-Trigger, Latest-Guard): **erzeugen** als erster Commit auf `1.x` — Push-Trigger lesen die Workflow-Datei des gepushten Commits, daher muss die Konfiguration auf dem Branch selbst liegen. `BREAKING CHANGE:`-Marker: **erzeugen** als Footer im Implementierungs-Commit dieses Feature-Branches. Issue im Plugin-Repo: **erzeugen** und in #237 verlinken. `ImportContractVersion`: **unverändert lassen** (Entscheidung, siehe unten). |

### Scope-Ausschlüsse

| # | Einschränkung / Ausschluss | Begründung | Status |
|---|----------------------------|------------|--------|
| 1 | Keine Anwendungscode-Änderung: die .NET-11-Migration ist mit `e9aed2d` auf `staging` abgeschlossen; diese Anforderung betrifft nur Branching, Release-Konfiguration und Kommunikation. | Aus der Anforderung übernommen. | bestätigt |
| 2 | Keine Plugin-Migration in diesem Repository: die .NET-11-Anhebung der privaten Plugins erfolgt in `martin-stromberg/Rezepte.Plugins`; hier wird nur das verlinkte Folge-Issue erstellt. | Aus der Anforderung übernommen. | bestätigt |
| 3 | Kein Rückfluss von Maintenance-Fixes nach `main`/`staging` und kein Backport-Automatismus; `verify-pr-source.yml` lässt ohnehin nur `staging → main` zu. | Aus der Anforderung übernommen. | bestätigt |
| 4 | Kein automatisierter Promotion-Merge: der Merge des Promotion-PRs bleibt manuelle Maintainer-Entscheidung. | Aus der Anforderung übernommen. | bestätigt |
| 5 | Keine Änderung am RC-Prerelease-Mechanismus (`staging-ci.yml` `--branches`-Override bleibt unverändert; der zusätzliche `branches`-Eintrag liegt ohnehin nur auf `1.x`). | Aus der Anforderung übernommen. | bestätigt |
| 6 | Kein automatischer Update-Kanal für die `1.x`-Linie: Nutzer, die auf der .NET-10-Linie bleiben, werden über den App-Updater (`update.json`/`releases/latest`) **nicht** auf Maintenance-Releases hingewiesen — sie müssen diese Releases manuell installieren. Der Latest-Guard sorgt dafür, dass umgekehrt auch kein `v1.x`-Release den Bestand an `v2.x`-Nutzern zum „Downgrade-Angebot" macht. | Der Update-Mechanismus (`msTools.Updater`, `ApplicationUpdates` in `Rezepte.Web/appsettings.json`) kennt keine Versionslinien-Kanäle; ein echter Zwei-Kanal-Updater wäre ein Produkt-Feature außerhalb dieser Anforderung. Das Risiko eines falschen „Latest" ist ohne Guard größer als der Nutzen eines auto-erkannten Maintenance-Updates. | bestätigt (2026-11-24 — via Issue-Kommentar in #237 zur Kenntnis vorgelegt; synchrone Rückfrage im SCM-Kanal nicht möglich, Entscheidung dokumentiert und korrigierbar) |
| 7 | Keine Branch-Protection-Regel für `1.x` im Planumfang: GitHub-Branch-Protection ist ein Remote-Repo-Setting, nicht per Datei versionierbar. Empfehlung an den Maintainer, `1.x` analog `main`/`staging` zu schützen, wird in `Docs/help/github-actions.md` vermerkt. | Nicht als Code-Artefakt umsetzbar; dokumentarisch abgedeckt. | bestätigt (2026-11-24 — via Issue-Kommentar in #237 zur Kenntnis vorgelegt; synchrone Rückfrage im SCM-Kanal nicht möglich, Entscheidung dokumentiert und korrigierbar) |

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Name des Maintenance-Branches | **`1.x`** (Range-Name) | semantic-release behandelt Branch-Namen im Range-Muster (`1.x`) automatisch als Maintenance-Branch mit implizitem `range`; ein freier Name (`maintenance/net10`) erforderte `{ name, range: "1.x" }`-Objektsyntax ohne Mehrwert. Die Konvention skaliert für künftige Linien (`2.x`). `v1.22.0` ist vom Abzweigpunkt erreichbar → Range-Voraussetzung erfüllt. |
| Träger des `BREAKING CHANGE:`-Markers | **Footer im Implementierungs-Commit dieses Feature-Branches** (derselbe Commit korrigiert `Docs/RELEASE_NOTES.md` und ergänzt die Doku — der Marker dokumentiert sich selbst) | Alle Merges sind echte Merge-Commits (`allow_squash_merge: false`); der Commit überlebt als Vorfahre in `main`s Historie und wird vom `conventionalcommits`-Analyzer gelesen — unabhängig davon, ob der Maintainer den Promotion-Merge-Body editiert. Der Marker wirkt außerdem sofort auf `staging`: der `version`-Job erzeugt danach RCs als `v2.0.0-rc.N` — Frühnachweis vor der Promotion. Fallback bleibt dokumentiert: Maintainer kann den Footer zusätzlich in den Promotion-PR-Body schreiben (`merge_commit_message: PR_BODY`). |
| Mechanismus für Branch-Anlage + Config-Commit | **Direkter Push** (`git checkout -b 1.x <SHA>` → Änderungen → `chore:`-Commit → `git push -u origin 1.x`) | Ein PR-Weg ist strukturell unmöglich: PRs gegen `staging`/`main` würden die 1.x-spezifische Konfiguration auf die falsche Linie bringen, und ein PR gegen `1.x` setzt den Branch bereits voraus (Henne-Ei). Der `chore:`-Typ des Config-Commits erzeugt absichtlich keinen Bump — der erste Push validiert den Release-Pfad, ohne ein Release zu erzeugen (`released=false`, `reason=no-release`). |
| Ort der Release-Konfigurationsänderungen | **Nur auf `1.x`** — `staging`/`main` bleiben unverändert | Bei `push`-Events wertet GitHub Actions die Workflow-Datei des gepushten Commits aus; die `staging`/`main`-Kopien wirken nie auf `1.x`-Pushes. Einsparen der Änderung auf `staging` vermeidet unnötige Diff-Masse und hält `AUTOMATIC_RELEASE_BRANCHES` auf `main` bewusst restriktiv. |
| „Latest"-Flag / `update.json` bei Maintenance-Releases | **Guard einbauen**: neuer Konditional-Step am Ende von `release.yml` **auf `1.x`** setzt `make_latest=false` auf dem soeben erzeugten Release | GitHub markiert die zuletzt erzeugte nicht-prerelease Release als „Latest" — ein `v1.22.1`, das zeitlich nach `v2.0.0` entsteht, würde sonst `releases/latest` übernehmen und dem Updater/ allen Konsumenten ein Downgrade anbieten. Der Guard existiert nur in der `1.x`-Kopie der Datei und trifft damit genau die Maintenance-Linie; `v2.x`-Releases bleiben „Latest". |
| `ImportContractVersion` (`0.3.0`) | **Kein Bump** | Die Property versioniert ausschließlich den Contract-Export (Asset-Name, Assembly-Versionen, ApiCompat-Baseline-Zuordnung); der Plugin-Ladepfad (`PluginLoadContext` in `PluginManager.cs`) prüft keine Vertragsversion, und ApiCompat vergleicht API-Fläche, nicht TFM — die API ist unverändert, die Inkompatibilität entsteht allein durch den Framework-Sprung. Ein Bump würde das dokumentierte Baseline-Prozedere (neue Baseline + `ContractExportScriptTests`-Anpassungen + `contract-lock`-Update im Plugin-Repo) ohne funktionalen Gewinn erzwingen und den Vertrag fälschlich als geändert ausweisen. |
| Release-Profil auf `1.x` (Issue-Frage 6) | **Voller `release.yml`-Pfad** (keine reduzierte Variante) | Maintenance-Fixes sollen dieselbe Asset-Qualität liefern (beide ZIPs, `update.json`, Contract-ZIP); ein reduziertes Profil wäre neue Komplexität ohne Anforderung. Das blockierende Release-Gate (beide Testsuiten) bleibt der Qualitätsanker. |
| PR-CI-Lücke für `1.x` | **`pr-staging-ci.yml`-Trigger um `1.x` erweitern** (nur auf dem `1.x`-Branch) | Sonst hätten Fix-PRs gegen `1.x` keinerlei CI. Kosten: eine Zeile im `pull_request.branches`-Block; die Basis-Ref-Workflow-Datei auf `1.x` ist .NET-10-konsistent (`dotnet-version: 10.0.x`). |
| Release-Reihenfolge App ↔ Plugins (Issue-Frage 3) | **App (`v2.0.0`) zuerst, Plugins folgen über das Folge-Issue** | Die Plugins können nicht vor der App sinnvoll „fertig" sein (Contract-ZIP/SDK-Abgleich referenziert das App-Release). Die kompatible Kombination wird über die Release Notes (BREAKING-CHANGES-Text) und das Plugin-Issue kommuniziert; kein synchronisierter Release-Mechanismus nötig. |

## Programmabläufe

### Ablauf 1: Maintenance-Branch `1.x` anlegen und Release-Pfad aktivieren (direkter Push)

1. `git fetch origin` — prüfen, ob `origin/main` noch auf `bf108b702907d2ee715bdc0341cedece5d55bf3a` steht. Falls `main` inzwischen weiter ist (Promotion schon gemergt): trotzdem explizit von diesem dokumentierten SHA abzweigen — der SHA ist der verbindlich dokumentierte letzte .NET-10-Stand.
2. `git checkout -b 1.x bf108b702907d2ee715bdc0341cedece5d55bf3a` (alternativ `git worktree add`, um den Feature-Branch-Checkout nicht zu verlassen).
3. Auf `1.x` fünf Änderungen vornehmen:
   - `.github/workflows/release.yml` Z. 12: `branches: [main]` → `[main, 1.x]` (`dotnet-version: '10.0.x'` Z. 46 bleibt — auf diesem Stand bereits korrekt für .NET 10).
   - `release.config.js` Z. 74: `branches: ["main"]` → `["main", "1.x"]`.
   - `scripts/resolve-release-version.mjs` Z. 23: `AUTOMATIC_RELEASE_BRANCHES = ["main"]` → `["main", "1.x"]` (sonst „Unsupported release ref 'branch:1.x'").
   - `.github/workflows/pr-staging-ci.yml` Z. 13-14: `- staging` → `- staging` + `- 1.x`.
   - `.github/workflows/release.yml`: neuen letzten Step „Clear Latest flag on maintenance release" einfügen (Details unter „Änderungen an bestehenden Dateien").
4. Commit: `chore(release): enable releases from the 1.x maintenance branch` — der `chore:`-Typ erzeugt absichtlich keinen Version-Bump.
5. `git push -u origin 1.x`. Der Push löst `release.yml` bereits aus (die Workflow-Datei des Push-Commits enthält `1.x` im Trigger): `resolve-release-version.mjs` klassifiziert `branch:1.x` nun als `automatic`, der semantic-release-Dry-Run findet seit `v1.22.0` nur `chore:`/`docs:`-Commits → `released=false`, `reason=no-release` → Lauf endet grün, **ohne** Release. Damit ist der Pfad aktiviert und zugleich validiert.

Beteiligte Komponenten: `git`, `.github/workflows/release.yml`, `release.config.js`, `scripts/resolve-release-version.mjs`, `.github/workflows/pr-staging-ci.yml`, `gh run` (Verifikation).

### Ablauf 2: Major-Marker, Release-Notes-Korrektur und Doku auf `staging` (dieser Feature-Branch)

1. `Docs/RELEASE_NOTES.md` korrigieren: Zeile 6 (EN) und Zeile 16 (DE) behaupten fälschlich, .NET-10-Plugins blieben kompatibel — ersetzen durch den Breaking-Change-Hinweis („Plugins built for .NET 10 are not compatible … must be rebuilt and republished for .NET 11", analog deutsch).
2. `Docs/help/github-actions.md` Abschnitt „Branch-Modell" (Z. 5-11) um die Maintenance-Linie ergänzen: Abzweigpunkt `bf108b7`, Range-Semantik (`1.x` → impliziter Range), voller Release-Pfad, Latest-Guard, kein Rückfluss nach `main`, Empfehlung Branch-Protection für `1.x`.
3. `Docs/help/import-plugins.md`: Hinweis-Absatz für externe Plugin-Betreiber — für .NET 10 gebaute Plugins laden unter .NET 11 nicht; Neubau und Neuveröffentlichung für .NET 11 erforderlich.
4. Ein Commit mit `BREAKING CHANGE:`-Footer (Footer-Text = die nutzerseitige Breaking-Change-Notiz; er erscheint wörtlich im „BREAKING CHANGES"-Abschnitt der generierten Release Notes):

   ```
   docs(release): document .NET 11 plugin incompatibility and 1.x maintenance line

   BREAKING CHANGE: Import plugins built for .NET 10 are not compatible with the
   .NET 11 application and must be rebuilt and republished for .NET 11.
   ```

5. Push + PR gegen `staging` → nach dem Merge (echter Merge-Commit; der Commit bleibt als Vorfahre erhalten) ermittelt `staging-ci.yml`s `version`-Job per `--branches staging`-Dry-Run die nächste Version `2.0.0` und erzeugt RC `v2.0.0-rc.1` — Frühnachweis des Markers vor der Promotion.
6. Beim späteren manuellen Merge des Promotion-Draft-PRs `staging → main` ist **keine** weitere Aktion nötig: der Marker liegt bereits in der Historie. Optional kann der Maintainer denselben Footer in den PR-Body kopieren (harmlose Redundanz, landet über `merge_commit_message: PR_BODY` zusätzlich im Merge-Commit).

Beteiligte Komponenten: `Docs/RELEASE_NOTES.md`, `Docs/help/github-actions.md`, `Docs/help/import-plugins.md`, `staging-ci.yml` (`version`-Job), `staging-to-main-promotion.yml`.

### Ablauf 3: Folge-Issue in `martin-stromberg/Rezepte.Plugins` anlegen und verlinken (Remote-Aktion)

1. `gh issue create --repo martin-stromberg/Rezepte.Plugins` mit Titel „Major-Release v1.0.0 für die .NET-11-Migration + .NET-10-Maintenance-Branch" und dem untenstehenden Issue-Entwurf (deutsch, passend zur Konvention von Issue #237).
2. `gh issue comment 237 --repo martin-stromberg/Rezepte` mit Verweis auf das neue Issue; der Issue-Entwurf verweist umgekehrt auf `martin-stromberg/Rezepte#237`.

**Issue-Entwurf (Body, wörtlich zu übernehmen):**

````markdown
## Hintergrund

Die Hauptanwendung (`martin-stromberg/Rezepte`) migriert auf .NET 11 und veröffentlicht
dies als Major-Release `v2.0.0` (siehe martin-stromberg/Rezepte#237). Für .NET 10
gebaute Import-Plugins sind mit der .NET-11-Anwendung **nicht mehr kompatibel** —
dieses Repository benötigt denselben Major- bzw. Maintenance-Schritt.

## Aufgaben

- [ ] **Plugins auf .NET 11 heben** (derzeit `net10.0` in `Directory.Build.props`):
  `net10.0` → `net11.0`; `dotnet-version: 10.0.x` → `11.0.x` in `ci.yml` und
  `release.yml`; NuGet-Abhängigkeiten aktualisieren. `contract-lock.json` via
  `scripts/Update-ContractExport.ps1` auf das Contract-ZIP des App-Releases `v2.0.0`
  umhängen (die App behält `ImportContractVersion 0.3.0`, der Asset-Name bleibt
  `rezepte-import-contract-0.3.0.zip`).
- [ ] **Major-Release `v1.0.0` erzeugen** (aktuell `v0.3.7`):
  `Get-NextReleaseVersion.ps1` wertet `BREAKING CHANGE:`/`type!:` in den Commits des
  gemergten PRs aus — der Migrations-PR muss einen Footer tragen, z. B.:
  `BREAKING CHANGE: Plugins built for .NET 10 are not compatible with the .NET 11
  application and must be rebuilt for .NET 11.`
- [ ] **.NET-10-Maintenance-Branch anlegen**: vom letzten .NET-10-Stand von `main`
  abzweigen (vor dem Merge der .NET-11-Migration; SHA dokumentieren).
  **Achtung — die hiesige Release-Machinerie ist nicht branch-sensitiv:**
  `release.yml` triggert auf jeden gemergten PR (`pull_request: closed` ohne
  Branch-Filter) und `Get-NextReleaseVersion.ps1` zählt ab dem global höchsten
  `vX.Y.Z`-Tag hoch. Ohne Anpassung würde ein Fix-PR auf den Maintenance-Branch
  eine falsche Version erzeugen (z. B. `v1.0.1` statt `v0.3.8`). Minimalansatz:
  `release.yml` auf `pull_request.branches: [main]` einschränken und Maintenance-
  Releases manuell taggen — oder `Get-NextReleaseVersion.ps1` branch-/range-fähig
  machen. Zu prüfen ist außerdem, dass Maintenance-Releases nicht das GitHub-
  „Latest"-Flag übernehmen.
- [ ] **Doku**: `docs/help/ci-release-package.md` (Versionsregeln),
  `docs/help/contract-export.md`, README-.NET-Badge.

## Referenzen

- martin-stromberg/Rezepte#237 (Anforderung App-Seite)
- App-Maintenance-Branch: `1.x`, abgezweigt von `bf108b7` (letzter .NET-10-Stand,
  `v1.22.0` erreichbar)
````

Beteiligte Komponenten: `gh issue create`, `gh issue comment` (Remote-Aktionen, keine Repo-Dateien).

### Ablauf 4: Maintenance-Release auf `1.x` (Zielbild — tritt bei jedem späteren Fix ein)

1. Fix-Branch von `1.x` abzweigen → PR gegen `1.x` → `pr-staging-ci.yml` läuft (durch die Trigger-Erweiterung) → Merge als echter Merge-Commit.
2. Push auf `1.x` → `release.yml` → `resolve-release-version.mjs` → `kind=automatic` → semantic-release-Dry-Run findet letzten In-Range-Tag (`v1.22.0`) → `fix:` → `v1.22.1` (`feat:` → `v1.23.0`; Major verlässt die Range und wird dort nicht erzeugt).
3. Voller Pfad: Restore/Build, **beide** Testsuiten blockierend, `build-and-package` (ZIPs + `update.json`), Contract-Export, `semantic-release`-Publish → GitHub Release `v1.22.1`.
4. Guard-Step (nur in der `1.x`-Kopie von `release.yml` vorhanden): setzt `make_latest=false` auf dem neuen Release → `v2.x` bleibt „Latest", kein Downgrade-Angebot.

Beteiligte Komponenten: `pr-staging-ci.yml`, `release.yml`, `release.config.js`, `scripts/resolve-release-version.mjs`, `semantic-release`.

### Ablauf 5: Promotion-Merge `staging → main` → Major-Release `v2.0.0` (Happy Path)

1. Maintainer mergt den automatisch erzeugten Draft-PR `staging → main` manuell (echter Merge-Commit, `merge_commit_message: PR_BODY`).
2. Push auf `main` → `release.yml` → `automatic` → semantic-release analysiert alle erreichbaren Commits seit `v1.22.0` — darunter der `docs(release):`-Commit mit `BREAKING CHANGE:`-Footer → nächste Version `2.0.0`.
3. Voller Release-Pfad → GitHub Release `v2.0.0` mit „BREAKING CHANGES"-Abschnitt (Footer-Text) und allen Assets (`release-linux-x64.zip`, `release-win-x64.zip`, `update.json`, `rezepte-import-contract-0.3.0.zip`, `contract-export.metadata.json`).
4. `sync-staging-with-main.yml` erstellt wie bisher den Backmerge-PR `main → staging` (unverändert).

Beteiligte Komponenten: `staging-to-main-promotion.yml`, `release.yml`, `scripts/resolve-release-version.mjs`, `release.config.js`, `sync-staging-with-main.yml`.

## Neue Klassen

Keine.

## Änderungen an bestehenden Dateien

### `.github/workflows/release.yml` (Workflow — nur auf `1.x`)

- **Geänderter Trigger:** `on.push.branches` (Z. 12) `[main]` → `[main, 1.x]` — Push auf `1.x` löst den Release-Pfad aus.
- **Neuer Step „Clear Latest flag on maintenance release"** als letzter Step des `release`-Jobs, Bedingung `steps.version.outputs.released == 'true' && steps.version.outputs.release_action == 'create'` — da der Step nur in der `1.x`-Kopie existiert, trifft er genau Maintenance-Releases (automatische wie manuell getaggte). Inhalt: Release-ID via `gh api repos/{owner}/{repo}/releases/tags/<tag> --jq .id` ermitteln, dann `gh api -X PATCH repos/{owner}/{repo}/releases/<id> -f make_latest=false` (äquivalent zulässig: `gh release edit <tag> --latest=false`, sofern vom gh-CLI im Runner unterstützt). `GH_TOKEN: ${{ github.token }}` (`contents: write` reicht).
- **Unverändert:** `dotnet-version: '10.0.x'` (korrekt für die .NET-10-Linie), Tag-Trigger `v*.*.*`, `concurrency.group: release`, Release-Gate, alle Publish-Pfade.

### `release.config.js` (nur auf `1.x`)

- **Geänderte Konfiguration:** `module.exports.branches` (Z. 74) `["main"]` → `["main", "1.x"]` — `1.x` ist ein Range-Muster-Name, semantic-release setzt den Range implizit; kein `{ name, range }`-Objekt nötig.

### `scripts/resolve-release-version.mjs` (nur auf `1.x`)

- **Geänderte Konstante:** `AUTOMATIC_RELEASE_BRANCHES` (Z. 23) `["main"]` → `["main", "1.x"]` — sonst wirft `classifyWorkflowRef` (Z. 44-54) „Unsupported release ref 'branch:1.x'". Kommentar Z. 21-22 sinngemäß ergänzen (Maintenance-Linie).

### `.github/workflows/pr-staging-ci.yml` (nur auf `1.x`)

- **Geänderter Trigger:** `on.pull_request.branches` (Z. 13-14) `- staging` → zusätzlich `- 1.x` — PR-CI für Fix-PRs gegen den Maintenance-Branch.

### `Docs/RELEASE_NOTES.md` (auf diesem Feature-Branch → `staging`)

- **Zeile 6 (EN):** ersetzen durch „Import plugins built for .NET 10 are **not** compatible with the .NET 11 application and will not load; plugins must be rebuilt and republished for .NET 11."
- **Zeile 16 (DE):** analog auf Deutsch.

### `Docs/help/github-actions.md` (auf diesem Feature-Branch → `staging`)

- **Abschnitt „Branch-Modell" (Z. 5-11):** Maintenance-Linie `1.x` ergänzen — Abzweigpunkt `bf108b7` (letzter .NET-10-Stand), Range-Semantik, voller `release.yml`-Pfad, Latest-Guard, kein Rückfluss, Empfehlung Branch-Protection.

### `Docs/help/import-plugins.md` (auf diesem Feature-Branch → `staging`)

- **Neuer Hinweis-Absatz** (nahe Plugin-Betrieb/Troubleshooting): Framework-Linien — für .NET 10 gebaute Plugins laden nicht unter .NET 11; Neubau + Neuveröffentlichung für .NET 11 erforderlich; Verweis auf die Plugin-Repo-Linie.

## Datenbankmigrationen

Keine.

## Validierungsregeln

Keine (keine Anwendungseingaben). Die wirksame „Validierung" der Release-Konfiguration erfolgt über den semantic-release-Dry-Run und den realen Workflow-Lauf — siehe Abschnitt „Tests".

## Konfigurationsänderungen

| Eintrag | Typ | Standardwert | Zweck |
|---------|-----|--------------|-------|
| `release.config.js` → `branches` (Z. 74) | Liste | `["main", "1.x"]` (nur auf `1.x`) | semantic-release-Versionierung auf der Maintenance-Range |
| `release.yml` → `on.push.branches` (Z. 12) | Liste | `[main, 1.x]` (nur auf `1.x`) | Push-Trigger für den Maintenance-Branch |
| `resolve-release-version.mjs` → `AUTOMATIC_RELEASE_BRANCHES` (Z. 23) | Liste | `["main", "1.x"]` (nur auf `1.x`) | `classifyWorkflowRef` akzeptiert `branch:1.x` |
| `pr-staging-ci.yml` → `on.pull_request.branches` (Z. 13-14) | Liste | `[staging, 1.x]` (nur auf `1.x`) | PR-CI für Maintenance-Fix-PRs |
| `release.yml` → neuer Guard-Step | Step | `make_latest=false` (nur auf `1.x`) | Verhindert „Latest"-Übernahme durch Maintenance-Releases |

Keine Änderungen an `appsettings`, `Directory.Build.props` oder auf `staging`/`main`.

## Seiteneffekte und Risiken

- **Erster `1.x`-Push:** Erzeugt absichtlich kein Release (`chore:`-Commit → `no-release`); der Lauf validiert den Pfad. Restrisiko: Existierte zufällig ein stabiler Release mit fehlenden Assets, griffe `incompleteReleases` — aktuell ist kein unvollständiger stabiler Release bekannt.
- **Linienübergreifende Asset-Reparatur:** `incompleteReleases` scannt alle stabilen Releases; ein `1.x`-Push könnte theoretisch einen `main`-Release „reparieren" (und umgekehrt) — der Reparaturpfad checkt den jeweiligen Tag aus und baut den korrekten Commit; akzeptables, dokumentiertes Verhalten.
- **Gemeinsamer Tag-Namespace `v*.*.*`:** Manuell gepuschte `v1.y.z`-Tags auf `1.x`-Commits laufen über den `manual`-Pfad der dortigen Workflow-Datei — konsistent; keine Kollision mit `v2.x`-Tags möglich, solange Tags nicht doppelt vergeben werden (harte Fehlerbedingung existiert bereits).
- **`concurrency.group: release`:** Serialisiert Release-Läufe über beide Linien — gewollt.
- **PRs gegen `1.x` ohne Quellbeschränkung:** `verify-pr-source.yml` greift nur bei Basis `main`; jeder Branch kann in `1.x` gemergt werden — entspricht Scope-Ausschluss 3.
- **`pr-staging-ci.yml` auf `1.x`:** `detect-backmerge` vergleicht fest gegen `origin/main` — auf `1.x`-PRs stets `false`, Jobs laufen normal. `no-pending-backmerge` kann Maintenance-PRs blockieren, solange ein Backmerge-PR `main → staging` offen ist — seltene, harmlose Nebenbedingung.
- **`update.json` in Maintenance-Releases:** Wird weiterhin als Asset erzeugt; der Latest-Guard verhindert, dass Konsumenten des „Latest"-Release es sehen. Konsumenten, die alle Releases semantisch sortieren, wählen ohnehin `v2.x` — kein Downgrade-Risiko.
- **`staging-ci.yml`/RC-Ermittlung:** Unberührt — der `--branches staging`-Override ersetzt die `branches`-Config vollständig; die `1.x`-Einträge liegen zudem nur auf `1.x`. Nach dem Marker-Commit wechselt die RC-Linie von `v1.23.0-rc.*` auf `v2.0.0-rc.*` — gewollt und Frühnachweis.
- **Doku-Stand auf `1.x`:** Die dortige `Docs/`-Kopie beschreibt `net11.0` nicht (Stand .NET 10) — fachlich korrekt für die Linie; keine Korrektur nötig.
- **`RELEASE_NOTES.md`-Widerspruch:** Die falsche Kompatibilitätsbehauptung wird durch diesen Plan behoben (Ablauf 2).

## Umsetzungsreihenfolge

1. **Abzweigpunkt verifizieren**
   - Voraussetzungen: Keine (`git`/`gh`-Zugriff auf `origin`).
   - Beschreibung: `git fetch origin`; prüfen, ob `origin/main` noch `bf108b702907d2ee715bdc0341cedece5d55bf3a` ist; den SHA als Abzweigpunkt festhalten (dokumentiert in diesem Plan und später in `Docs/help/github-actions.md`).

2. **`1.x`-Branch anlegen, konfigurieren, pushen**
   - Voraussetzungen: Schritt 1; Schreibrecht auf `martin-stromberg/Rezepte`.
   - Beschreibung: `git checkout -b 1.x <SHA>`; die fünf Änderungen aus „Änderungen an bestehenden Dateien" (`release.yml` Trigger + Guard-Step, `release.config.js`, `resolve-release-version.mjs`, `pr-staging-ci.yml`) anwenden; `chore:`-Commit; `git push -u origin 1.x`; anschließend `gh run list --workflow release.yml --branch 1.x` — erster Lauf muss grün ohne Release enden (`reason=no-release`).

3. **Marker-Commit + Doku-Korrektur auf diesem Feature-Branch**
   - Voraussetzungen: Keine (unabhängig von Schritt 2).
   - Beschreibung: `Docs/RELEASE_NOTES.md` (Z. 6 + 16), `Docs/help/github-actions.md` (Branch-Modell), `Docs/help/import-plugins.md` (Plugin-Hinweis) ändern; ein Commit mit `BREAKING CHANGE:`-Footer; pushen; PR gegen `staging` einstellen.

4. **Folge-Issue im Plugin-Repo anlegen und verlinken**
   - Voraussetzungen: `gh`-Zugriff auf `martin-stromberg/Rezepte.Plugins`.
   - Beschreibung: `gh issue create` mit dem Issue-Entwurf aus Ablauf 3; `gh issue comment 237` im Hauptrepo mit dem Verweis.

5. **Nach dem Merge des Feature-Branch-PRs: RC-Frühnachweis prüfen**
   - Voraussetzungen: Schritt 3 gemergt auf `staging`.
   - Beschreibung: `staging-ci`-Lauf abwarten; im `version`-Job bzw. via `git ls-remote --tags origin 'v2.0.0-rc.*'` prüfen, dass der nächste RC `v2.0.0-rc.1` heißt — beweist, dass der Analyzer den Footer sieht, bevor die Promotion läuft.

6. **Promotion-Merge `staging → main` (Maintainer-Aktion)**
   - Voraussetzungen: Schritt 5 (idealerweise auch Schritt 2, damit die Maintenance-Linie vor dem .NET-11-Stand auf `main` existiert — zwingend ist nur der dokumentierte SHA).
   - Beschreibung: Maintainer mergt den Draft-PR; optional `BREAKING CHANGE:`-Footer zusätzlich in den PR-Body. Erwartet: Release-Lauf erzeugt `v2.0.0` mit BREAKING-CHANGES-Abschnitt.

7. **Optionaler Dry-Run-Nachweis auf `1.x`**
   - Voraussetzungen: Schritt 2; `npm ci` im `1.x`-Checkout.
   - Beschreibung: Auf `1.x` lokal `npx semantic-release --dry-run --no-ci --branches 1.x` ausführen (ggf. mit einem Test-`fix:`-Commit auf einem Wegwerf-Branch), um die Range-Versionierung (`v1.22.x`/`v1.23.0`) vor dem ersten echten Fix zu demonstrieren.

## Tests

### Neue Tests

Keine automatisierten Tests — die Anforderung betrifft ausschließlich Git-/CI-/Release-Konfiguration; eine Node-Testsuite für `resolve-release-version.mjs` existiert nicht (`package.json` ohne `test`-Skript). Validierung erfolgt über die folgenden Ausführungsnachweise:

| Prüfung | Wo | Was wird nachgewiesen? |
|---------|----|------------------------|
| `gh run list --workflow release.yml --branch 1.x` → grüner Lauf, `reason=no-release` | Remote, nach Schritt 2 | `1.x`-Trigger + `AUTOMATIC_RELEASE_BRANCHES` + `branches`-Eintrag greifen konsistent; kein Spurious-Release beim Config-Commit |
| `git ls-remote --tags origin 'v2.0.0-rc.*'` → `v2.0.0-rc.1` vorhanden | Remote, nach Schritt 5 | `BREAKING CHANGE:`-Footer wird vom Analyzer gelesen — vor der Promotion |
| `gh release view v2.0.0 --json tagName,isLatest` + Notes enthalten „BREAKING CHANGES" | Remote, nach Schritt 6 | Major-Release mit Breaking-Change-Hinweis (Akzeptanzkriterium 3) |
| `npx semantic-release --dry-run --no-ci --branches 1.x` auf `1.x`-Checkout | Lokal, Schritt 7 (optional) | Range-Versionierung auf `1.x` (`v1.22.x`) |

### Betroffene bestehende Tests

Keine — kein Anwendungscode geändert; `Rezepte.Tests` (763/763 grün im Ausgangszustand) und `Rezepte.Tests.Browser` bleiben unberührt und laufen unverändert in den CI-/Release-Gates weiter.

### E2E-Tests (primärer Funktionsnachweis)

Keine E2E-Tests erforderlich: Die Anforderung berührt keinen über UI oder Nutzeraktion erreichbaren Ablauf — es gibt keine Anwendungsoberflächen-Änderung. Der „Benutzerfluss" ist die Release-Pipeline selbst; der Funktionsnachweis ist definitionsgemäß der reale Workflow-Lauf (so auch das Akzeptanzkriterium der Anforderung formuliert) plus die oben geplanten Dry-Run-/CLI-Verifikationen.

## Akzeptanzszenarien

| # | Szenario | Auslöser | Ausgangszustand | Schritte | Sichtbare Wirkung | Nachweis |
|---|----------|----------|------------------|----------|-------------------|----------|
| 1 | **Happy Path: Major-Release bei Promotion** | Maintainer mergt Draft-PR `staging → main` | `staging` enthält den Marker-Commit (Schritt 3); `main` auf .NET 10 (`bf108b7`); `1.x` gepusht oder zumindest SHA dokumentiert | Merge → `release.yml` auf `main` → `resolve` (`automatic`) → Dry-Run findet `BREAKING CHANGE:` → `2.0.0` → voller Release-Pfad | GitHub Release `v2.0.0` mit „BREAKING CHANGES"-Abschnitt (Plugin-Inkompatibilität) und allen 5 Assets; `isLatest=true` | `gh release view v2.0.0`; realer Release-Lauf |
| 2 | Maintenance-Branch-Aktivierung ohne Release | `git push -u origin 1.x` des Config-Commits | `1.x` existiert nicht; `origin/main` ≙ `bf108b7` | Branch von `bf108b7` → 5 Config-Änderungen → `chore:`-Commit → Push | Release-Lauf auf `1.x` endet grün, `released=false`, `reason=no-release`; kein Tag/Release entsteht | `gh run list --workflow release.yml --branch 1.x`; `gh release list` unverändert |
| 3 | Maintenance-Fix erzeugt `v1.22.x`, nicht „Latest" | Merge eines `fix:`-PRs nach `1.x` (zukünftig) | `1.x` aktiv; `v2.0.0` bereits „Latest" | PR (CI via `pr-staging-ci`) → Merge → Push → `release.yml` → `v1.22.1` → Guard | Release `v1.22.1` existiert mit allen Assets; `v2.0.0` bleibt „Latest" | `gh release view v1.22.1 --json isLatest` → `false`; optional Dry-Run vorab (Schritt 7) |
| 4 | Staging-Flow unverändert + Marker-Frühnachweis | Merge des Feature-Branch-PRs nach `staging` | `staging` ohne Marker; letzter RC `v1.23.0-rc.1` | Merge → `staging-ci` → `version`-Job (`--branches staging`) → `prerelease`-Job | Neuer RC heißt `v2.0.0-rc.1` (statt `v1.23.0-rc.2`); Promotion-Draft-PR wird wie gehabt aktualisiert | RC-Tag via `git ls-remote`; `staging-ci`-Run grün |
| 5 | Folge-Issue im Plugin-Repo verlinkt | `gh issue create` + `gh issue comment` | Kein Issue in `Rezepte.Plugins` für .NET 11 | Issue mit Entwurf aus Ablauf 3 erstellen; Kommentar in #237 | Issue existiert mit den 3 Pflichtinhalten (.NET-11-Anhebung, Maintenance-Branch inkl. Branch-Awareness-Warnung, `v1.0.0`); Verlinkung beidseitig | `gh issue view <N> --repo martin-stromberg/Rezepte.Plugins`; Kommentar in #237 |
| 6 | Fehlerfall abgewehrt: Push auf `1.x` ohne Config-Commit | Hypothetischer `1.x`-Push ohne die drei Einträge | — | Push würde `release.yml` nicht auslösen bzw. `classifyWorkflowRef` würde `Unsupported release ref 'branch:1.x'` werfen | Nicht zutreffend — verhindert durch die verbindliche Checkliste in Ablauf 1 (alle fünf Änderungen gehören in denselben Commit vor dem ersten Push) | Commit-Diff des Config-Commits (Review) |

## Offene Punkte

Keine — alle offenen Fragen der Anforderung wurden entschieden (Branch-Name `1.x`; kein `ImportContractVersion`-Bump; App zuerst, Plugins über Folge-Issue; SHA `bf108b702907d2ee715bdc0341cedece5d55bf3a` dokumentiert; Nutzerhinweis in `RELEASE_NOTES.md` + `import-plugins.md` + generierten Release Notes; voller Release-Pfad auf `1.x`; Latest-Guard eingebaut). Die Scope-Ausschlüsse 6 und 7 sind Entscheidungen mit Begründung und warten lediglich auf Anwenderbestätigung — sie blockieren die Umsetzung nicht.
