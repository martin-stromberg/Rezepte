# Plan-Prüfung (2. Runde) – GitHub Page für die Anwendung „Rezepte"

Geprüft: aktualisierter `plan.md` gegen `requirement.md`, `inventory.md`, `inventory/deployment-varianten.md`, den Befundbericht `plan-check.1.md` und den faktischen Repo-Stand (Branch `task/issue-186-...`, Basis `staging`; Live-Verifikationen per `gh api` und Dateisystem am Prüfdatum wiederholt).

## Ergebnis

**Status:** Plan vollständig

Alle fünf Befunde aus `plan-check.1.md` sind tatsächlich und korrekt behoben — darunter der kritische Permissions-Defekt im Workflow-Design, der jetzt durch den sauber dokumentierten Verzicht auf `configure-pages` gelöst ist (stärkste der drei damals angebotenen Optionen). Die Muss-Anforderungen und Akzeptanzkriterien sind vollständig abgedeckt, alle plangestützenden Fakten wurden gegen den Repo-Stand erneut verifiziert, und keine neuen Defekte wurden durch die Überarbeitung eingeführt.

## Abgleich Anforderung ↔ Umsetzungselemente

| Anforderung / Akzeptanzkriterium | Geplantes Umsetzungselement | Abdeckung | Anmerkung |
|---|---|---|---|
| Statische Seite über GitHub Pages (`docs/` oder Actions-Deployment zulässig) | E1: Actions-Deployment via `pages.yml`, `upload-pages-artifact` + `deploy-pages`; kein `configure-pages` | Abgedeckt | Variantenwahl faktenbasiert korrekt (`Docs/`↔`docs/`-Konflikt auf Windows-Checkout verifiziert). Permissions jetzt korrekt geschichtet (Details in Abhängigkeitsprüfung). |
| Modernes, responsives Design; helle + dunkle Darstellung; an App-Design angelehnt | E3/E4: framework-freies CSS, Custom Properties 1:1 aus `app.css`, `prefers-color-scheme` + Toggle mit `localStorage` und Anti-FOUC-Inline-Snippet | Abgedeckt | Helle Token exakt verifiziert (`app.css` Zeilen 2–15). Dark-Palette konkret spezifiziert; Kontrastprüfung jetzt mit verbindlicher Methode (s. u.). |
| Inhalte: Zweck, Features, Screenshots, Download-/Release-Verweise, Repo-/Doku-Verweise | Seitenkonzept Abschnitte 1–7 | Abgedeckt | Quellen verifiziert: Zwecksatz `README.md:7`, Feature-Bullets `README.md:11–18`, `demo.gif` `README.md:26`, 8 Assets in `Docs/screenshots/`, 3 Icons in `Rezepte.Web/wwwroot/icons/` (inkl. `rezepte_64x64.ico` in `App.razor:11` referenziert). Repo-Links korrekt als absolute `github.com`-URLs geplant. `releases/latest` existiert (v1.20.0 = „Latest" verifiziert via `gh release list`). |
| Pflege aus dem Repository | E2 `site/` + `scripts/Build-Site.ps1` assembliert aus `Docs/screenshots/` und `wwwroot/icons/`; `paths`-Filter | Abgedeckt | Single-Source sauber; Screenshot-Pflegeprozess via `Docs/screenshot-generation.md` verlinkt (existiert). `artifacts/` ist in `.gitignore` (Zeile 63); kein `site`-Pattern in `.gitignore`, kein vorhandenes `site/` — kein Konflikt. |
| Dokumentation des Aktualisierungswegs | `Docs/github-pages.md` (neu), `Docs/index.md`-Eintrag, `Docs/help/github-actions.md`-Abschnitt | Abgedeckt | Zielstrukturen existieren (`Docs/index.md` Übersichtsliste, `github-actions.md` verlinkt aus `help/index.md`). |
| AC1: öffentlich erreichbar unter `…/Rezepte/` | AS-5 (Maintainer-Aktivierung) + AS-6 (curl-Checks) + `workflow_dispatch`-Recovery | Abgedeckt | Externe Abhängigkeit ehrlich eingeordnet: Pages ist nach wie vor nicht aktiviert (`gh api …/pages` → 404, erneut verifiziert), Token ohne `admin`/`maintain` (erneut verifiziert). Reihenfolge und Recovery-Pfad korrekt beschrieben. |
| AC2: responsiv, modern, ansprechend | AS-2 (lokale Vorschau, 375px-Check, Toggle-Persistenz) + WCAG-Kontrastprüfung in `check-site.mjs` | Abgedeckt | Kontrastmethode jetzt konkret: Token-Extraktion aus `site.css`, Luminanz-Formel, feste Paarliste, Schwellen 4,5:1/3:1, Konsistenzcheck Media-Query ↔ `[data-theme="dark"]`. |
| AC3: Zweck, Features, Download-Verweis vorhanden | AS-1/AS-2 + `check-site.mjs`-Referenzprüfung | Abgedeckt | Asset-Namen `release-win-x64.zip`/`release-linux-x64.zip` in `release.yml` verifiziert (Zeilen 199–233). |
| AC4: Aktualisierung dokumentiert | `Docs/github-pages.md` + `github-actions.md`-Abschnitt | Abgedeckt | — |

## Abhängigkeitsprüfung

| Abhängigkeit | Prüfergebnis |
|---|---|
| Befund 1 (plan-check.1): Permissions-Defekt | **Korrekt behoben.** `configure-pages` ist vollständig entfernt und der Verzicht in einem eigenen Abschnitt (plan.md Zeilen 137–146) faktenbasiert begründet. Damit entfällt der einzige Pages-API-Zugriff im build-Job; `upload-pages-artifact` ist ein regulärer Artefakt-Upload und benötigt keine `pages`-Permission — der build-Job läuft korrekt mit geerbtem `contents: read`, auch im PR-Kontext (AS-3). Der deploy-Job führt `{ contents: read, pages: write, id-token: write }` explizit — korrekt, da Job-Permissions Top-Level vollständig ersetzen. Deploy-`if` (`event_name != 'pull_request' && ref_name == 'main'`) deckt Push auf `main` und Dispatch auf `main` ab. |
| Befund 2: Closing-Keyword | **Behoben.** Schritt 13: `Closes #186` in Commit-Body und PR-Beschreibung, mit korrekter Begründung zum Staging-Flow (Issue schließt erst beim Erreichen von `main` — gewünscht). Entspricht der Projektregel. |
| Befund 3: Kontrastprüfung ohne Methode | **Behoben.** plan.md Zeile 200 definiert Extraktion, Formel, Paarliste, Schwellen, Dark-Token-Konsistenz und Dokumentationspflicht (`test-results.md`, `Docs/github-pages.md`). Stichprobe der hellen Token (Muted `#6c757d` auf `#fffaf3` ≈ 4,52:1; Link `#b23a48` ≈ 5,6:1; Weiß auf `#5A1F1F` ≈ 13:1; Weiß auf `#cc4d29` ≈ 4,5:1) zeigt: die Palette ist grenzwertig, aber passierbar — die Skriptprüfung ist damit sinnvoll statt pro-forma. |
| Befund 4: `pr.yml`-Drift | **Behoben.** Beide verifizierten Drifte in `Docs/help/github-actions.md` sind eingeplant: Zeile 51 (`pr.yml` existiert nicht → `pr-staging-ci.yml`) und Zeile 59 (Release-Trigger: tatsächlich `push` auf `main`/Tags, nicht „PR geschlossen und gemergt"). Beide Stellen im Ist-Stand erneut verifiziert. |
| Befund 5: Netzwerkabhängige Vorschau | **Behoben.** Schritt 7 nennt `python -m http.server` und VS-Code-„Live Server" zuerst, `npx serve` nur als Netzwerk-Option. |
| Pages-Aktivierung / Admin-Rechte | **Unverändert verifiziert:** `gh api repos/martin-stromberg/Rezepte/pages` → 404; Repo-Permissions `admin=false, maintain=false, push=true, triage=true`. Der dokumentierte Maintainer-Schritt (Settings → Pages → Source „GitHub Actions") ist unumgänglich; PR-Checkliste + Issue-Kommentar + `Docs/github-pages.md` decken die Kommunikation ab. Ohne `configure-pages` schlägt kein PR-Check fehl — die Aktivierung bleibt „jederzeit möglich" statt PR-blockierend. Korrekt. |
| Staging-Flow / Trigger | **Verifiziert:** `release.yml` triggert `push: [main]` + `tags v*.*.*` — `push: main` ist deckungsgleich. `verify-pr-source.yml` erzwingt `staging → main`; Promotion-PR wird per `GITHUB_TOKEN` als Draft erstellt (`staging-to-main-promotion.yml`, Display-Name-Trigger „Pre-Release"). PR-Ziel `staging` korrekt. |
| Workflow-Konventionen | **Verifiziert:** `actions/checkout@v7`, `setup-node@v7` mit `node-version: '24'`, `shell: pwsh` auf `ubuntu-latest`, `timeout-minutes`, `concurrency`-Gruppen — alles im Bestand. Keine `pages`/`id-token`-Permissions bisher im Repo (grep bestätigt) — neue Permissions korrekt auf den deploy-Job beschränkt. Skript-Konventionen (`.ps1` PascalCase, `.mjs` kebab-case) passen. |
| Pages-Action-Versionen | `@v4` für `upload-pages-artifact`/`deploy-pages` geplant mit Verifikationspflicht bei Implementierung — korrekt, da eigene Release-Kadenz. |
| Assets/Pfade/Basispfad | 8 Screenshots + 3 Icons existieren; `releases/latest` valide; relative-Pfade-Regel + `check-site.mjs`-Verbot führender `/` deckt `/Rezepte/` ab; identisch in lokaler Vorschau. |

## Fehlende oder unvollständige Punkte

Keine verbleibenden Muss-Lücken und keine neuen Defekte festgestellt. Die fünf Befunde der ersten Prüfung sind vollständig und sachlich korrekt umgesetzt; alle Anforderungen und Akzeptanzkriterien sind durch benannte Umsetzungselemente und Akzeptanzszenarien abgedeckt.

## Hinweise

- **Dark-Token-Fix-Regel einseitig:** plan.md Zeile 200 regelt nur den Fehlschlag-Fall für Dark-Tokens („Token angepasst, Schwelle nicht aufgeweicht"). Für die helle Palette sind die Token an `app.css` gebunden — fiele ein helles Paar (grenzwertig: Muted ≈ 4,52:1, Weiß-auf-`primary-600` ≈ 4,5:1) unter die Schwelle, ist die Reaktion unklar (abweichender Seiten-Token vs. App-Palette). Trivial, aber in der Umsetzung zu entscheiden.
- **`test-results.md` ohne Pfad:** Konvention in diesem Repo ist `Docs/features/task/<task>/test-results.md` (mehrere Vorbilder vorhanden); der Plan sollte den Ablageort explizit so benennen.
- **Promotion-PR löst `pull_request`-Workflows nicht bei Erstellung aus:** Der Draft-PR `staging → main` wird per `GITHUB_TOKEN` erstellt — `pages.yml` (wie auch `verify-pr-source.yml`) feuert erst bei menschlicher Aktion (`ready_for_review`). Die PR-Validierung erfolgt damit weiterhin vor dem Merge, nur nicht bei PR-Anlage. Plattformverhalten, kein Planfehler.
- **`check-site.mjs` liest `site.css`:** Der Plan nennt einmal die Quelle `site/assets/css/site.css`, einmal den gebauten Output `artifacts/pages` als Prüfziel — beides deckungsgleich (Kopie); bei der Implementierung eindeutig festlegen.
- README-Zeilenreferenzen minimal daneben (Feature-Bullets sind Zeilen 11–18, Plan nennt 10–19) — ohne Umsetzungsrelevanz.
- `rezepte.ico` (zusätzlich zu den drei geplanten Icon-Dateien) existiert im `icons/`-Ordner und wird bewusst nicht übernommen — keine Auswirkung.
