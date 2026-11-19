# Umsetzungsplan – GitHub Page für die Anwendung „Rezepte"

## Metadaten

- Aufgaben-ID: `61917e8b-a063-457b-ae6c-ff26334ed989`
- Verknüpftes Issue: GitHub Issue #186 — „GitHub Page: Anwendung mit modernem Stil präsentieren"
- Branch: `task/issue-186-61917e8ba063457bae6cff26334ed989-github-page-anwendung-mit-mode`
- Ziel-URL: `https://martin-stromberg.github.io/Rezepte/` (Projektseite, Basispfad `/Rezepte/`)
- Eingaben: [requirement.md](requirement.md), [inventory.md](inventory.md), [inventory/deployment-varianten.md](inventory/deployment-varianten.md), Befundbericht [plan-check.md](plan-check.md)
- PR-Zielbranch: `staging` (Sitzungsvorgabe; Live-Gang der Page erfolgt erst über den regulären Promotion-Pfad `staging → main`)

## Ziel

Eine statische, deutschsprachige Präsentationsseite für „Rezepte" wird aus dem Repository heraus gepflegt und über GitHub Pages unter `https://martin-stromberg.github.io/Rezepte/` ausgeliefert — responsiv, mit heller und dunkler Darstellung, angelehnt an das Farbschema der Anwendung.

## Entscheidungen

### E1 — Deployment-Variante: GitHub-Actions-Deployment

**Entscheidung:** Die Page wird über einen eigenen Workflow mit `actions/upload-pages-artifact` + `actions/deploy-pages` ausgeliefert (Variante B aus [inventory/deployment-varianten.md](inventory/deployment-varianten.md)). Kein `docs/`-Ordner.

**Begründung (faktenbasiert):**

- Auf dem tatsächlichen Entwicklungssystem (Windows, case-insensitives Dateisystem) fallen `docs/` und das vorhandene `Docs/` in denselben Verzeichnisbaum — ein dauerhaftes Ablage- und Diff-Fehlerrisiko und eine Vermischung von Präsentationsseite und Projektdokumentation (verifiziert in `inventory.md`: `ls -d docs` → `Docs/`).
- Der Actions-Weg kopiert die Screenshots beim Build aus `Docs/screenshots/` ins Artefakt; die Dateien bleiben single-source am dokumentierten Ort. Bei der `docs/`-Variante müssten sie physisch unter `docs/` dupliziert werden.
- Ein achter Workflow folgt den etablierten Konventionen (`actions/checkout@v7`, `concurrency`-Gruppen, `timeout-minutes`, `pwsh`-Steps).
- Artefakt-Deployment benötigt kein `.nojekyll` (kein Jekyll-Lauf).
- Beide Varianten erfordern denselben einmaligen Settings-Schritt (Pages-Aktivierung); der Actions-Weg kostet also keinen zusätzlichen manuellen Aufwand.

### E2 — Ablageort der Page-Quellen: `site/` im Repository-Root

Neuer Ordner `site/` enthält ausschließlich die gepflegten Quellen (HTML/CSS/JS). Er kollidiert nicht mit `Docs/`, ist eindeutig benannt und wird nicht von GitHub Pages direkt interpretiert — das Deployment erfolgt ausschließlich über das vom Workflow gebaute Artefakt. Screenshots und Icons werden **nicht** nach `site/` dupliziert, sondern beim Build aus den Bestandsquellen ins Ausgabeverzeichnis kopiert (siehe Asset-Handling).

### E3 — Technik: framework-freies HTML/CSS, minimales JS

Eine einzelne `index.html`, eigenes CSS mit Custom Properties, kein CSS-Framework und keine CDN-Abhängigkeiten. Begründung: Die Seite ist eine statische One-Pager-Landingpage; Bootstrap 5.3.3 (wie die App via jsDelivr) wäre Overhead und eine externe Laufzeitabhängigkeit. Das Erscheinungsbild wird über dieselben Farbtoken und Stilmittel (Karten, Verlauf, abgerundete Ecken, weiche Schatten) angelehnt. Einziges JS: ein kleiner Theme-Toggle (`site/assets/js/theme.js`).

### E4 — Dark/Light: `prefers-color-scheme` + manueller Toggle

Die App selbst hat keinen Dark Mode (`app.css` definiert nur eine helle Palette) — die Page implementiert beide Darstellungen selbst:

- Helle Palette = Farbtoken aus `Rezepte.Web/wwwroot/app.css` (`--app-bg: #fffaf3`, `--app-primary: #5A1F1F`, `--app-primary-600: #cc4d29`, `--app-accent: #2e7d32`, `--app-link: #b23a48` usw.).
- Dunkle Palette = abgeleitete Warm-Töne desselben Schemas (Startwerte; Kontrast-Verifikation mit konkreter Methode ist fester Teil von `check-site.mjs` — siehe Abschnitt „Test- und Prüfbedarf"): Hintergrund `#211d1a`, Surface `#2b2622`, Text `#f2ece4`, Muted `#b8aea4`, Border `#453d36`, Primärverlauf unverändert Bordeaux→Tomate, Link `#e08a97`.
- Mechanik: `@media (prefers-color-scheme: dark)` als Default + Toggle-Button, der `data-theme="light"|"dark"` auf `<html>` setzt und die Wahl in `localStorage` persistiert. Ein Inline-Snippet im `<head>` wendet die gespeicherte Wahl vor dem ersten Paint an (kein Flackern).

### E5 — Sprache: Deutsch

`<html lang="de">`, deutsche UI-Texte — konsistent mit Anwendung, Dokumentation und Issue-Sprache.

### E6 — Workflow-Trigger

- `pull_request` → `staging` (und `main`, für den Promotion-PR): nur **Build + Validierung**, kein Deploy. PRs deployen nie.
- `push` → `main`: Build + Validierung + Deploy. Deckungsgleich mit `release.yml` — die Seite folgt dem Release-Stand, was zum dokumentierten Staging-Modell passt.
- `workflow_dispatch`: manuelles Re-Deploy (u. a. als Recovery-Pfad, falls die Pages-Aktivierung erst nach dem ersten Merge erfolgt).
- `paths`-Filter auf `site/**`, `scripts/Build-Site.ps1`, `scripts/check-site.mjs`, `Docs/screenshots/**`, `Rezepte.Web/wwwroot/icons/**`, `.github/workflows/pages.yml` — vermeidet Deploys bei seitenfremden Änderungen.

## Zielbild: Dateien und Ordner

| Pfad | Status | Zweck |
|---|---|---|
| `site/index.html` | neu | One-Pager: Header/Nav, Hero, Features, Screenshots-Galerie, Download, Doku-Links, Footer |
| `site/assets/css/site.css` | neu | Custom Properties (hell + dunkel), Layout, responsive Grid, Karten-/Navbar-Optik angelehnt an `app.css` |
| `site/assets/js/theme.js` | neu | Theme-Toggle (`data-theme`, `localStorage`, `prefers-color-scheme`-Fallback) |
| `scripts/Build-Site.ps1` | neu | Assembliert `site/` + `Docs/screenshots/` + Icons aus `Rezepte.Web/wwwroot/icons/` in ein Ausgabeverzeichnis (Default `artifacts/pages`) — identisch für lokale Vorschau und CI |
| `scripts/check-site.mjs` | neu | Statische Validierung des gebauten Outputs inkl. WCAG-Kontrastprüfung beider Paletten (siehe Testbedarf) — dependency-frei, Node ≥ 20 |
| `.github/workflows/pages.yml` | neu | Build/Validate-Job für PRs und Pushes + Deploy-Job nur auf `main` |
| `Docs/github-pages.md` | neu | Dokumentation: Struktur, Workflow, lokale Vorschau, Pflegeprozess, einmalige Pages-Aktivierung |
| `Docs/index.md` | geändert | Eintrag für `github-pages.md` in der Übersicht |
| `Docs/help/github-actions.md` | geändert | Neuer Abschnitt zum Pages-Workflow; nebenbei zwei verifizierte Doku-Drifte korrigieren: veralteter Release-Trigger-Satz (Zeile 59: tatsächlich `push` auf `main`/Tags, nicht „PR geschlossen und gemergt") und nicht existierende `pr.yml`-Referenz (Zeile 51: tatsächlich `pr-staging-ci.yml`) |
| `README.md` | geändert | Verweis auf die Page in der Liste „Weitere Informationen" (erster Eintrag) sowie kurzer Link im Intro-Bereich |
| `Docs/RELEASE_NOTES.md` | geändert | Ein Eintrag jeweils in „What's New" und „Neuerungen" |

Explizit **nicht** angelegt: `docs/` (Konflikt mit `Docs/`), `CNAME`, `.nojekyll`, Mehrseiten-Struktur.

## Seitenkonzept

### Inhalte (alle geforderten Punkte, deutsch)

1. **Header:** Logo (`rezepte.png` Icon), Titel „Rezepte", Nav-Links (Features, Screenshots, Download, Dokumentation, GitHub), Theme-Toggle.
2. **Hero:** App-Name, prägnanter Zwecksatz (aus `README.md:7`), CTAs „Zum Download" (→ `releases/latest`) und „GitHub-Repository", daneben `home.png`.
3. **Features:** Card-Grid mit ~6 Kacheln, aus `README.md:10-19` verdichtet (Kochbücher & Rezepte, Öffentliche Links zum Teilen, Einkaufsliste & Kalender, Importe aus Backup/Datei/URL/Web, Plugin-Framework & KI-Import, Export/Sicherung/Updates).
4. **Screenshots:** Galerie mit `demo.gif`, `recipes.png`, `recipe-detail.png`, `cookbooks.png`, `calendar.png`, `shopping-list.png` — `loading="lazy"`, aussagekräftige `alt`-Texte. `register.png` wird bewusst nicht gezeigt (nur Registrierungsformular, kein Informationsgewinn); bleibt aber im Artefakt.
5. **Download:** Link auf `releases/latest` mit Benennung der Assets `release-win-x64.zip` / `release-linux-x64.zip` (Asset-Namen aus `release.yml`/`staging-ci.yml`), Verweis auf die Installationsanleitung.
6. **Dokumentation:** Verweise auf `Docs/features.md`, `Docs/install.md`, `Docs/index.md` — als absolute `github.com/martin-stromberg/Rezepte/...`-URLs (die Page liegt auf github.io, Repo-Dateien werden nicht mit ausgeliefert).
7. **Footer:** Lizenz-Verweis, GitHub-Link.

### Responsive Design

Mobile-first: fließendes Grid (`auto-fit`/`minmax`), `clamp()`-Typografie, `meta viewport`, Bilder `max-width: 100%`, Nav ohne JS-Breakpoint-Brüche (Links umbrechen/zentrieren unter ~640px). Ziel: saubere Darstellung von ~360px bis Desktop.

### Basispfad `/Rezepte/` und Asset-Handling

- **Alle** internen Referenzen relativ (`assets/css/site.css`, `screenshots/home.png`, `#features` usw.) — niemals führender `/`. Dadurch funktioniert die Seite unter `/Rezepte/` und identisch in jeder lokalen Vorschau.
- Externe Links (GitHub, Releases, Docs) absolut mit `https://`.
- `scripts/Build-Site.ps1` kopiert: `site/*` → Output; `Docs/screenshots/*` → `output/screenshots/`; `rezepte.png`, `rezepte-180x180.png`, `rezepte_64x64.ico` aus `Rezepte.Web/wwwroot/icons/` → `output/assets/icons/` (favicon, apple-touch-icon, Header-Logo). Script bricht mit Fehler ab, wenn eine Quelle fehlt.

## Workflow-Design: `.github/workflows/pages.yml`

Name `Pages`, orientiert an den Bestandskonventionen:

```yaml
on:
  push:
    branches: [main]
    paths: [site/**, Docs/screenshots/**, Rezepte.Web/wwwroot/icons/**,
            scripts/Build-Site.ps1, scripts/check-site.mjs, .github/workflows/pages.yml]
  pull_request:
    branches: [staging, main]
    paths: [<gleiche Liste>]
  workflow_dispatch:

permissions: { contents: read }
concurrency: { group: pages, cancel-in-progress: false }

jobs:
  build:
    runs-on: ubuntu-latest
    timeout-minutes: 10
    steps:
      - actions/checkout@v7
      - actions/setup-node@v7 (node-version 24, analog Bestand)
      - pwsh: ./scripts/Build-Site.ps1 -OutputDirectory artifacts/pages
      - run: node scripts/check-site.mjs artifacts/pages
      - actions/upload-pages-artifact@v4 mit path: artifacts/pages

  deploy:
    if: github.event_name != 'pull_request' && github.ref_name == 'main'
    needs: build
    runs-on: ubuntu-latest
    timeout-minutes: 10
    permissions: { contents: read, pages: write, id-token: write }
    environment: { name: github-pages, url: ${{ steps.deployment.outputs.page_url }} }
    steps:
      - actions/deploy-pages@v4 (id: deployment)
```

### `configure-pages`-Verzicht und Permissions-Schichtung (Befund aus plan-check.md)

**Entscheidung:** `configure-pages` wird **nicht** verwendet; Top-Level bleibt `permissions: { contents: read }`, der deploy-Job deklariert explizit `{ contents: read, pages: write, id-token: write }`. Damit entfällt die Alternative „offizielles Starter-Muster" (`pages: write` + `id-token: write` auf Top-Level). Begründung:

- **Konkreter Defekt vermieden:** `configure-pages` ruft die Pages-REST-API auf (`GET /repos/{owner}/{repo}/pages`) und benötigt dafür die `pages`-Permission. Im ursprünglichen Design hätte der Schritt im build-Job mit geerbtem `contents: read` bereits im ersten PR-Lauf (AS-3) mit 403 versagt — ohne dass ein Deploy stattfindet.
- **Der Schritt würde vor dem Maintainer-Schritt überhaupt fehlschlagen:** Pages ist für das Repo nicht aktiviert (verifiziert: API → 404), und `configure-pages` kann die Source mit dem `GITHUB_TOKEN` nicht selbst aktivieren (`enablement` erfordert Admin-Rechte). Mit `configure-pages` im build-Job wäre jeder PR-Check rot, bis der Maintainer die Settings umstellt — die Aktivierung würde vom dokumentierten „jederzeit, am besten vor Promotion" zur PR-Blockade.
- **Least Privilege:** Ohne `configure-pages` benötigt der build-Job keine `pages`-/`id-token`-Permission; `pages: write` + `id-token: write` bleiben auf den deploy-Job beschränkt, der ohnehin nur auf `main` läuft.
- **Kein Funktionsverlust:** Die Action liefert primär `base_path`-Metadaten und SSG-Bindungen — für eine handgebaute statische Seite mit konsequent relativen Pfaden ungenutzt.
- **Permissions-Schichtung beachten:** Job-Level-`permissions` ersetzen die Top-Level-Permissions vollständig — der deploy-Job führt `contents: read` deshalb explizit mit (entspricht dem Referenzmuster der offiziellen Starter-Dokumentation).

### Versionspinning

- Repo-Konvention bleibt: `actions/checkout@v7`, `actions/setup-node@v7` (Node 24).
- Pages-Actions folgen einer **eigenen Release-Kadenz** und weichen vom `@v7`-Muster ab. Stand aus dem Befundbericht: `actions/upload-pages-artifact@v4`, `actions/deploy-pages@v4`. Diese Majors sind verbindlich geplant, bei der Implementierung aber kurz gegen die aktuellen Releases der `actions`-Repositorien zu verifizieren (bei Abweichung die dann aktuelle Major-Version verwenden).

### Weitere Workflow-Details

- `cancel-in-progress: false` entsprechend dem GitHub-Starter-Muster, damit kein Deploy halbfertig abgebrochen wird.
- Der Deploy-Job läuft niemals in PRs; `workflow_dispatch` ist auf `main` beschränkt (`ref_name`-Bedingung).
- `upload-pages-artifact` läuft bewusst auch in PR-Builds (kein Event-Filter): validiert den Artefakt-Aufbau bereits im PR und bleibt funktional unproblematisch — ein Pages-Artefakt pro PR-Lauf ist akzeptabel.

## GitHub-Pages-Aktivierung (einmaliger Maintainer-Schritt)

**Verbindlicher Fakt:** Das SCM-Token hat kein `admin`-Recht (`admin=false`, `maintain=false`; `push=true`, `triage=true`). Die Source-Einstellung kann daher **nicht** aus der Automatisierung heraus gesetzt werden — einmaliger manueller Schritt durch jemanden mit Admin-Rechten:

- GitHub → Repository `martin-stromberg/Rezepte` → **Settings → Pages → Build and deployment → Source: „GitHub Actions"**.
- Reihenfolge: Der Schritt kann **jederzeit** ausgeführt werden, am besten vor dem Promotion-Merge nach `main`. Erfolgt er später, schlägt `deploy-pages` mit „Pages site not found" fehl; Recovery = Settings-Schritt nachholen + Workflow über `workflow_dispatch` erneut starten (dafür ist der Trigger vorgesehen).
- Verifikation: `gh api repos/martin-stromberg/Rezepte/pages --jq .build_type` → `workflow`.

**Kommunikation (Umsetzungspflicht):**

- PR-Beschreibung (PR → `staging`) enthält eine Maintainer-Checkliste mit genau diesem Schritt und dem Hinweis, dass die öffentliche Erreichbarkeit erst nach Merge nach `main` + Aktivierung verifizierbar ist.
- Kommentar auf Issue #186 nach PR-Erstellung mit derselben Anleitung (einmaliger Settings-Schritt, Reihenfolge, Recovery via Re-Run).
- `Docs/github-pages.md` dokumentiert den Schritt dauerhaft.

## Umsetzungsschritte

1. `site/index.html` erstellen: deutsche Texte, Abschnitte wie im Seitenkonzept, `<html lang="de">`, Meta-Viewport, favicon/apple-touch-icon über `assets/icons/`, relative Pfade, Inline-Theme-Init im `<head>`.
2. `site/assets/css/site.css` erstellen: Token-Definition hell (`:root`, Werte aus `app.css`), dunkel via `@media (prefers-color-scheme: dark)` + `[data-theme="dark"]`, `[data-theme="light"]`-Override; Layout, Karten, Verlauf-Header (`90deg, --app-primary → --app-primary-600`), responsive Grid, Fokus-Stile.
3. `site/assets/js/theme.js` erstellen: Toggle-Logik, `localStorage`-Persistenz, aria-Label-Wechsel.
4. `scripts/Build-Site.ps1` erstellen: Parameter `-OutputDirectory` (Default `artifacts/pages`); kopiert `site/`, `Docs/screenshots/` → `screenshots/`, die drei Icon-Dateien → `assets/icons/`; wirft Fehler bei fehlenden Quellen.
5. `scripts/check-site.mjs` erstellen: prüft u. a. `index.html` vorhanden, `lang="de"`, nicht-leerer `<title>`, Meta-Viewport vorhanden, jede lokale `src`/`href`-Referenz (kein `http(s):`, `mailto:`, `#`) existiert im Output, keine lokalen Referenzen mit führendem `/`, Screenshots-Verzeichnis nicht leer; zusätzlich die WCAG-Kontrastprüfung beider Paletten aus `site.css` und die Konsistenz der dunklen Token zwischen Media-Query und `[data-theme="dark"]` (Methodik s. Testbedarf); Exit-Code ≠ 0 mit klarer Meldung je Befund.
6. `.github/workflows/pages.yml` erstellen (Design s. o. — ohne `configure-pages`, Permissions wie beschrieben).
7. `Docs/github-pages.md` erstellen: Zweck/URL, `site/`-Struktur, Assemblierung, Workflow-Trigger, lokale Vorschau (`./scripts/Build-Site.ps1` + beliebiger Static-Server — **netzwerkfreie Alternativen** zuerst nennen: `python -m http.server` bzw. VS-Code-Erweiterung „Live Server"; `npx serve artifacts/pages` nur als Option mit Netzwerkzugang), Kontrast-Methodik (Kurzbeschreibung der `check-site.mjs`-Prüfung), Pflegeprozess (Texte in `site/index.html`; Screenshots über den dokumentierten Prozess `Docs/screenshot-generation.md` regenerieren → Deploy läuft automatisch), einmalige Aktivierung, Troubleshooting.
8. `Docs/index.md`: Eintrag „GitHub Page" in die Übersichtsliste.
9. `Docs/help/github-actions.md`: Abschnitt „GitHub Pages" (Trigger, Jobs, Permissions, Environment, Aktivierung) + zwei Drift-Korrekturen: Release-Trigger-Satz (Zeile 59: `push` auf `main`/Tags statt „PR geschlossen und gemergt") und `pr.yml`-Referenz (Zeile 51: → `pr-staging-ci.yml`).
10. `README.md`: Page-Link ergänzen (Intro + „Weitere Informationen").
11. `Docs/RELEASE_NOTES.md`: Eintrag in „What's New" und „Neuerungen" (neue GitHub Page unter der Ziel-URL, gepflegt aus `site/`).
12. Lokal bauen + validieren + Vorschau prüfen (s. Akzeptanzszenarien), dann Commit/PR gegen `staging` inkl. Maintainer-Checkliste; Issue-Kommentar #186.
13. **Issue-Verknüpfung (Closing-Keyword):** Commit-Message-Body enthält `Closes #186` (gemäß Projektregel; ein `(Issue #186)` allein schließt nichts). Da der Task-Branch über `staging` in `main` gelangt, schließt GitHub das Issue automatisch, sobald der Commit über den Promotion-Pfad `main` erreicht — das ist gewünscht (Auslieferung gilt erst dann). Zur Sichtbarkeit/Verlinkung wird `Closes #186` zusätzlich in die PR-Beschreibung (PR → `staging`) aufgenommen.

## Akzeptanzszenarien

- **AS-1 (lokaler Build, Happy Path):** `./scripts/Build-Site.ps1` erzeugt `artifacts/pages/` mit `index.html`, `assets/css/site.css`, `assets/js/theme.js`, `assets/icons/` (3 Dateien) und `screenshots/` (8 Dateien). `node scripts/check-site.mjs artifacts/pages` endet mit Exit-Code 0.
- **AS-2 (lokale Vorschau):** `artifacts/pages` wird mit einem Static-Server ausgeliefert; die Seite rendert mit Header, Hero, Features, Galerie (alle Bilder laden), Download- und Doku-Links; Theme-Toggle wechselt hell/dunkel und überlebt Reload; bei 375px Viewport-Breite keine horizontale Scrollbar.
- **AS-3 (PR-Validierung):** PR gegen `staging` triggert `pages.yml`; Job `build` läuft grün (Build + Checks), Job `deploy` wird übersprungen — kein Deployment aus PRs.
- **AS-4 (Deployment nach Merge):** Nach Promotion-Merge auf `main` laufen `build` + `deploy`; `deploy-pages` meldet Erfolg und `page_url` = `https://martin-stromberg.github.io/Rezepte/` (setzt AS-5 voraus, sonst dokumentierter Fehlerpfad mit `workflow_dispatch`-Recovery).
- **AS-5 (Maintainer-Aktivierung):** In den Repo-Settings ist Pages-Source = „GitHub Actions" gesetzt; `gh api repos/martin-stromberg/Rezepte/pages --jq .build_type` → `workflow`.
- **AS-6 (öffentliche Erreichbarkeit):** `curl -s -o /dev/null -w "%{http_code}" https://martin-stromberg.github.io/Rezepte/` → `200`; `https://martin-stromberg.github.io/Rezepte/screenshots/home.png` → `200`; HTML enthält `lang="de"`. **Einschränkung:** erst nach Merge auf `main` + abgeschlossener Aktivierung verifizierbar — nicht aus dem Feature-Branch heraus prüfbar.

## Test- und Prüfbedarf

- **Statische Checks (in CI + lokal, `check-site.mjs`):** Referenz-Auflösung aller lokalen `src`/`href`, Verbot führender `/` bei lokalen Pfaden (Basispfad-Sicherheit), `lang="de"`, `<title>`, Meta-Viewport. Optional ergänzend `npx html-validate` auf `artifacts/pages` (nicht zwingend, nur wenn ohne Setup verfügbar).
- **WCAG-Kontrastprüfung der Paletten (konkrete Methode, Befund aus plan-check.md):** `check-site.mjs` extrahiert die Custom-Property-Werte aus `site/assets/css/site.css` (hell aus dem `:root`-Block, dunkel aus dem `[data-theme="dark"]`-Block) und berechnet für eine fest im Skript hinterlegte Liste relevanter Token-Paare das Kontrastverhältnis nach WCAG 2.x über die relative Luminanz — dependency-frei, dieselbe Formel wie gängige Kontrastrechner. Mindestpaare je Palette: Text↔Hintergrund, Text↔Surface, Muted↔Hintergrund, Muted↔Surface, Link↔Hintergrund, Link↔Surface, helle Schrift auf Verlaufsenden (`--app-primary`, `--app-primary-600`) für Navbar/Hero/Buttons; Border↔Surface als UI-Komponenten-Check. Schwellen: ≥ 4,5:1 für normalen Text, ≥ 3:1 für große Schrift und UI-Komponenten. Das Skript verlangt außerdem, dass die dunklen Token im `@media (prefers-color-scheme: dark)`-Block und im `[data-theme="dark"]`-Block wertgleich sind (kein Auseinanderlaufen der Palette). Bei einem Fehlschlag wird der betroffene Dark-Token angepasst, nicht die Schwelle aufgeweicht. Ergebnisse (Paar, Ratio, bestanden) werden in `test-results.md` dokumentiert; die Methodik wird kurz in `Docs/github-pages.md` beschrieben.
- **Workflow-Validierung:** YAML-Syntax-Review; erster Beweis ist der grüne PR-Lauf (AS-3). Deploy-Pfad ist erst auf `main` ausführbar — vorher trocken reviewen (`if`-Bedingung, Environment, Job-Permissions `pages: write`/`id-token: write`/`contents: read`; der Permissions-Befund aus plan-check.md ist im Design bereits korrigiert).
- **Browser-Prüfung:** lokale Vorschau (AS-2) manuell oder per vorhandenem Playwright-Setup (`scripts/screenshots/`, Chromium) — Screenshot zur Sichtprüfung reicht.
- **Grenze:** Echte öffentliche Erreichbarkeit (AS-6) ist erst nach Merge + Maintainer-Schritt verifizierbar; der Plan liefert mit `workflow_dispatch` den Recovery-Pfad für die Nachholung.

## Risiken und Einschränkungen

- **Berechtigungslücke (verbindlich):** Pages-Aktivierung erfordert Admin — dokumentierter Maintainer-Schritt + PR-/Issue-Kommunikation (s. o.). Kein Scope-Problem, aber Abhängigkeit außerhalb des Codes.
- **Deploy ohne aktivierte Source schlägt fehl** — durch Reihenfolge-Hinweis + `workflow_dispatch` abgefedert.
- **Asset-Größen:** `home.png` ~1 MB, `demo.gif` ~860 KB — akzeptabel für eine Landingpage; alle Galeriebilder mit `loading="lazy"`.
- **Case-Sensitivity:** `site/` muss konsequent klein geschrieben werden; unter keinen Umständen `docs/` anlegen.
- **`workflow_dispatch` erst nach Merge nutzbar** (GitHub zeigt den Dispatch-Button erst, wenn der Workflow auf dem Default-Branch liegt).

## Explizite Nicht-Ziele

- Keine Änderungen an der Anwendung (`Rezepte.Web`, Datenmodell, Features).
- Kein Umbau/Ersatz von `Docs/`; die Page ist Präsentationsseite, kein Doku-Umzug.
- Keine Custom Domain, kein Hosting außerhalb GitHub Pages.
- Keine Mehrseiten-/Blog-Struktur.
- Keine neuen Dependencies für die Page selbst (kein Framework, kein Bundler).

## Offene Punkte

Keine. Alle Entscheidungen sind aus den Inventory-Fakten ableitbar und oben getroffen. Die Pages-Aktivierung ist kein offener Scope-Punkt, sondern ein dokumentierter einmaliger Maintainer-Schritt mit definierter Reihenfolge und Recovery-Pfad (Abschnitt „GitHub-Pages-Aktivierung").
