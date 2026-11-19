# Plan-Prüfung – GitHub Page für die Anwendung „Rezepte"

Geprüft: `plan.md` gegen `requirement.md`, `inventory.md`, `inventory/deployment-varianten.md` und den faktischen Repo-Stand (Branch `task/issue-186-...`, Basis `staging`; Live-Verifikationen per `gh api` und Dateisystem).

## Ergebnis

**Status:** Plan lückenhaft

Der Plan ist inhaltlich vollständig, faktisch sehr gut gegen den Repo-Stand verifiziert und deckt alle Muss-Anforderungen und Akzeptanzkriterien konzeptionell ab. Er enthält jedoch einen konkreten, nachweisbaren Fehler im geplanten Workflow-Design (Permissions für `configure-pages`), der den ersten CI-Lauf (AS-3) und das Deployment (AS-4) scheitern lassen würde, sowie mehrere kleinere Lücken.

## Abgleich Anforderung ↔ Umsetzungselemente

| Anforderung / Akzeptanzkriterium | Geplantes Umsetzungselement | Abdeckung | Anmerkung |
|---|---|---|---|
| Statische Seite über GitHub Pages (zulässig: `docs/` oder Actions-Deployment) | E1: Actions-Deployment via `pages.yml`, `upload-pages-artifact` + `deploy-pages` | Teilweise | Variantenwahl faktenbasiert korrekt (verifizierter `Docs/`↔`docs/`-Konflikt). **Aber:** `configure-pages` im build-Job hat nur `contents: read` geerbt → API-Zugriff schlägt fehl (Details unten). |
| Modernes, responsives Design; helle + dunkle Darstellung; an App-Design angelehnt | E3/E4: framework-freies CSS, Custom Properties, `prefers-color-scheme` + Toggle | Abgedeckt | Helle Token 1:1 aus `Rezepte.Web/wwwroot/app.css` verifiziert (Werte stimmen exakt). Dark-Palette ist Vorschlag — Kontrast-Prüfung ist Vorsatz ohne konkrete Methode. |
| Inhalte: Zweck, Features, Screenshots, Download-/Release-Verweise, Repo-/Doku-Verweise | Seitenkonzept Abschnitte 1–7 | Abgedeckt | Quellen verifiziert: Zwecksatz `README.md:7`, Feature-Bullets `README.md:10–19` (Plan nennt 9–20, Abweichung trivial), `demo.gif` `README.md:26`, 8 Assets in `Docs/screenshots/`, 3 Icons in `Rezepte.Web/wwwroot/icons/` — alle existieren. Repo-Datei-Links korrekt als absolute `github.com`-URLs geplant (Page liegt auf github.io). |
| Pflege aus dem Repository (Seite wächst mit) | E2 `site/` + `Build-Site.ps1` assembliert aus `Docs/screenshots/` und `wwwroot/icons/`; `paths`-Filter im Workflow | Abgedeckt | Single-Source-Prinzip sauber; Pflegeprozess für Screenshots via `Docs/screenshot-generation.md` referenziert. `artifacts/` ist bereits in `.gitignore` (Zeile 63). |
| Dokumentation des Aktualisierungswegs | `Docs/github-pages.md`, `Docs/index.md`, `Docs/help/github-actions.md` | Abgedeckt | — |
| AC1: öffentlich erreichbar unter `…/Rezepte/` | AS-5 (Maintainer-Aktivierung) + AS-6 (curl-Checks) | Abgedeckt | Korrekt als externe Abhängigkeit behandelt; nicht aus dem Feature-Branch verifizierbar — ehrlich eingeordnet, Recovery-Pfad vorhanden. |
| AC2: responsiv, modern, ansprechend | AS-2 (lokale Vorschau inkl. 375px-Check, Theme-Toggle-Persistenz) | Abgedeckt | Manuelle/halbautomatische Prüfung; vertretbar. |
| AC3: Zweck, Features, Download-Verweis vorhanden | AS-1/AS-2 + `check-site.mjs`-Referenzprüfung | Abgedeckt | — |
| AC4: Aktualisierung dokumentiert | `Docs/github-pages.md` (Schritt 7), `github-actions.md`-Abschnitt | Abgedeckt | — |

## Abhängigkeitsprüfung

| Abhängigkeit | Prüfergebnis |
|---|---|
| Pages-Aktivierung / Admin-Rechte | **Verifiziert:** `gh api repos/martin-stromberg/Rezepte/pages` → 404 (nicht konfiguriert); Token-Permissions `admin=false, maintain=false, push=true, triage=true`. Settings-Schritt „Source: GitHub Actions" liegt zwingend außerhalb der Automatisierung. Plan behandelt dies korrekt (Maintainer-Checkliste, Issue-Kommentar, `workflow_dispatch`-Recovery). Zusatzfakt: `configure-pages` kann Pages mit dem `GITHUB_TOKEN` ohnehin nicht aktivieren (`enablement` erfordert einen anderen Token) — der Maintainer-Schritt ist also unumgänglich, wie geplant. |
| Staging-Flow / Trigger-Wahl | **Verifiziert:** `release.yml` triggert `push: [main]` + `tags: v*.*.*` — Deploy-Trigger `push: main` ist deckungsgleich. `staging-to-main-promotion.yml` referenziert Display-Name „Pre-Release"; `verify-pr-source.yml` erzwingt `staging → main`. PR-Ziel `staging` korrekt. |
| Workflow-Konventionen | **Verifiziert:** `actions/checkout@v7` (12×), `setup-node@v7` mit Node `24`, `setup-dotnet@v6`, `upload-artifact@v7`, `shell: pwsh` auf `ubuntu-latest`, `concurrency`-Gruppen, `timeout-minutes` — alle im Bestand vorhanden. `scripts/` enthält bereits `.ps1`- und `.mjs`-Skripte (Konvention trifft zu). |
| Pages-Action-Versionen | **Offen, im Plan vorgesehen:** Aktuell dokumentierte Majors sind `configure-pages@v5`, `upload-pages-artifact@v4`, `deploy-pages@v4` — sie folgen einer eigenen Release-Kadenz und weichen vom Repo-Muster (`@v7`) ab. Platzhalter-Verifikation beim Implementieren ist zwingend. |
| Assets/Pfade | **Verifiziert:** `Docs/screenshots/` (8 Dateien, Groß-D exakt), `Rezepte.Web/wwwroot/icons/` (`rezepte.png`, `rezepte-180x180.png`, `rezepte_64x64.ico`), kein `site/` vorhanden (kein Konflikt), `artifacts/` in `.gitignore`. |
| Basispfad `/Rezepte/` | Relative Pfade + `check-site.mjs`-Regel (Verbot führender `/`) decken das korrekt ab; funktioniert identisch in lokaler Vorschau. |
| `workflow_dispatch`-Verfügbarkeit | Korrekt erkannt: Dispatch-Button erst, wenn der Workflow auf `main` liegt — Recovery-Reihenfolge stimmt. |

## Fehlende oder unvollständige Punkte

1. **Workflow-Permissions falsch geschichtet — konkreter Defekt (blockiert AS-3 und AS-4).** Der Plan setzt `permissions: { contents: read }` auf Workflow-Ebene und `pages: write`/`id-token: write` nur auf den deploy-Job. `actions/configure-pages` im build-Job ruft aber die Pages-REST-API auf (`GET /repos/{owner}/{repo}/pages`) und benötigt dafür die `pages`-Permission — ein dokumentiertes Problem, das der offizielle Starter durch `pages: write` + `id-token: write` auf Top-Level löst. Mit nur `contents: read` schlägt der Schritt mit 403 fehl — bereits im ersten PR-Lauf, wo gar kein Deploy passiert. Erforderliche Korrektur: Permissions wie im offiziellen Starter auf Workflow-Ebene setzen (`contents: read`, `pages: write`, `id-token: write`) **oder** dem build-Job mindestens `pages`-Zugriff geben **oder** `configure-pages` streichen (für eine handgebaute statische Seite mit relativen Pfaden optional — die Action liefert primär `base_path`-Metadaten, die der Plan nicht nutzt). Zusätzlich sollte der deploy-Job `contents: read` mitführen (Docs-Referenzmuster; job-level Permissions ersetzen das Top-Level vollständig).
2. **Closing-Keyword für Issue #186 fehlt.** Die Umsetzungsschritte erwähnen PR gegen `staging` und Issue-Kommentar, aber kein `Closes #186`/`Fixes #186` in Commit-Message oder PR-Body. Gemäß Projektregel schließt das Issue erst beim Erreichen von `main` — ohne Keyword im (Promotion-)Kontext bleibt es offen. Einzuplanen: Keyword in Commit-Body bzw. Promotion-PR-Beschreibung.
3. **Kontrast-Prüfung der Dark-Palette ohne Methode.** E4 schlägt konkrete Dark-Werte vor und nennt „WCAG AA prüfen", definiert aber kein Vorgehen (z. B. Kontrastrechner-Checkliste in `check-site.mjs`-Nähe oder dokumentierter manueller Check). Als „in der Umsetzung zu prüfen" stehen zu lassen, lädt zum Vergessen ein — in `Docs/github-pages.md` oder der Umsetzungs-Checkliste verankern.
4. **`Docs/help/github-actions.md` enthält einen weiteren, nicht eingeplanten Drift.** Zeile ~51 referenziert `pr.yml` — diese Datei existiert nicht (tatsächlich `pr-staging-ci.yml`). Der Plan korrigiert nur den Release-Trigger-Satz; die `pr.yml`-Referenz liegt im selben Dokument und sollte bei der ohnehin geplanten Änderung mitkorrigiert werden.
5. **Kleinigkeit lokale Vorschau:** `npx serve artifacts/pages` setzt Netzwerkzugang voraus. `Docs/github-pages.md` sollte eine Alternative nennen (z. B. `python -m http.server`, PowerShell-/VS-Code-Live-Server) — trivial, aber für die reproduzierbare Doku relevant.

## Hinweise

- `configure-pages`/`upload-pages-artifact` laufen im geplanten Design auch in PR-Builds. Funktional unproblematisch (validiert den Artefakt-Aufbau), erzeugt aber pro PR ein Pages-Artefakt; optional auf Non-PR-Events konditionierbar. Kein Muss.
- `register.png` wird ins Artefakt kopiert, aber nicht angezeigt — bewusste, nachvollziehbare Entscheidung.
- `paths`-Filter: Der erste PR fügt `site/**` hinzu und triggert den Workflow damit garantiert; Promotion-PRs mit Seitenänderungen werden vor dem `main`-Merge validiert. Korrekt.
- `concurrency: group pages, cancel-in-progress: false` entspricht dem offiziellen Starter-Muster; teilt die Warteschlange zwischen PR-Builds und Deploys — akzeptabel.
- Der Plan behandelt die offene Frage „Wer aktiviert Pages" korrekt: kein Anwender-Entscheid im Scope, sondern dokumentierter Maintainer-Schritt mit definierter Reihenfolge und Recovery. „Offene Punkte: keine" ist damit sachlich vertretbar.
- Die README-Zeilenreferenzen des Plans weichen minimal ab (Feature-Bullets sind Zeilen 10–19, nicht 9–20); ohne Umsetzungsrelevanz.
