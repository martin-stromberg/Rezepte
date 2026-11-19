# Bestandsaufnahme

Stand: Branch `task/issue-186-61917e8ba063457bae6cff26334ed989-github-page-anwendung-mit-mode`, Basis `staging`.
Anforderung: GitHub Issue #186 — öffentliche, statische GitHub Page unter `https://martin-stromberg.github.io/Rezepte/`, die die Anwendung mit modernem, responsivem Stil (hell/dunkel, an das App-Design angelehnt) präsentiert.

## Zusammenfassung

Das Repository hat alle Inhaltsquellen für eine Präsentationsseite bereits an Bord: acht aktuelle Screenshots unter `Docs/screenshots/`, einen dokumentierten, skriptgesteuerten Erzeugungsprozess (`scripts/screenshots/`, `Docs/screenshot-generation.md`), eine klar definierte Farbpalette in `Rezepte.Web/wwwroot/app.css` und eine Feature-Liste in `README.md`/`Docs/features.md`. Es existiert dagegen noch nichts für die Auslieferung: kein Pages-Workflow, kein `docs/`-Ordner, und GitHub Pages ist für `martin-stromberg/Rezepte` laut API nicht aktiviert (`GET /repos/martin-stromberg/Rezepte/pages` → 404). Das Repository ist öffentlich (`visibility: PUBLIC`), der Default-Branch ist `main`.

Die Anforderung lässt zwei Auslieferungswege offen (`docs/`-Ordner oder GitHub-Actions-Deployment). Beide sind hier gegen den tatsächlichen Repo-Stand bewertet: [Deployment-Varianten](inventory/deployment-varianten.md). Wesentliche repo-spezifische Befunde:

- **Namenskonflikt `docs/` ↔ `Docs/`:** Im Root existiert bereits `Docs/` (Dokumentation). Auf dem hier verwendeten Windows-Checkout ist das Dateisystem case-insensitiv — `ls -d docs` löst auf `Docs/` auf. Ein `docs/`-Ordner für Pages würde lokal in denselben Verzeichnisbaum fallen wie die Projektdokumentation; auf den Linux-Servern von GitHub Pages wird dagegen der wörtliche Ordnername `docs` erwartet. Das ist ein konkretes Verwechslungs- und Fehlerpotenzial dieser Variante.
- **Staging-Flow:** Der Lebenszyklus ist Feature-Branch → `staging` → (Promotion-PR) → `main`. Pages aus einem Branch-Source würde erst mit dem Erreichen von `main` live gehen; ein Actions-Deployment kann auf `push: main` triggern — deckungsgleich mit `release.yml`.
- **Kein Dark Mode in der App:** `app.css` definiert genau eine helle Palette in `:root`, kein `prefers-color-scheme`, kein `data-bs-theme`. Die geforderte „helle und dunkle Darstellung" muss die Page selbst implementieren; die App liefert nur die Farbvorlage.
- **Pages-Aktivierung liegt außerhalb des Codes:** Source-Konfiguration (Branch/`docs` oder „GitHub Actions") ist ein Repo-Settings-Schritt; unabhängig von der gewählten Variante zu dokumentieren.

## Ist-Zustand im Detail

### CI/CD-Workflows und Staging-/Release-Prozess

Sieben Workflows unter `.github/workflows/`, keine Pages-Bezüge:

| Workflow | Display-Name | Trigger | Zweck |
|---|---|---|---|
| `pr-staging-ci.yml` | PR CI for Staging | `pull_request` → `staging` | Build, Tests (Unit + Playwright-Browser), Format-Check, Security-Gate, Contract-Export; `detect-backmerge` überspringt reine main→staging-Backmerges |
| `staging-ci.yml` | Pre-Release | `push` → `staging` | gleiche Checks + `version`-Job (semantic-release Dry-Run, RC-Tags `vX.Y.Z-rc.N`) + `prerelease`-Job (`gh release create --prerelease` mit `release-linux-x64.zip`, `release-win-x64.zip`, `update.json`) |
| `staging-to-main-promotion.yml` | Staging to Main Promotion | `workflow_run` „Pre-Release" success auf `staging` | erstellt Draft-PR `staging → main`; Merge ist manuell. Achtung: Trigger referenziert den **Display-Namen** „Pre-Release", nicht den Dateinamen |
| `verify-pr-source.yml` | Verify PR Source | `pull_request` → `main` | erzwingt: nur PRs aus `staging` nach `main` |
| `sync-staging-with-main.yml` | Backmerge Main to Staging | `push` → `main` | erstellt PR `main → staging` (muss als Merge-Commit gemergt werden) |
| `release.yml` | Release | `push` → `main` und Tags `v*.*.*` | `scripts/resolve-release-version.mjs` klassifiziert automatisch/manuell/Asset-Repair; baut über `.github/actions/build-and-package`; veröffentlicht GitHub-Release via semantic-release (automatisch) bzw. `gh release create --generate-notes` (manuell) |
| `security-scan.yml` | Security Scan | Cron Mo 04:00 UTC + `workflow_dispatch` | wöchentlicher NuGet-Vulnerability-Scan über `.github/actions/security-scan` |

Composite Actions: `.github/actions/build-and-package`, `.github/actions/security-scan`.

Workflow-Konventionen: `actions/checkout@v7`, `actions/setup-node@v7` (Node 24), `actions/setup-dotnet@v6` (.NET 10.0.x), `actions/upload-artifact@v7`; `gh`-CLI wird in Workflows bereits genutzt; Runner `ubuntu-latest`; Jobs tragen `timeout-minutes`; Concurrency-Gruppen sind gesetzt. Verwendete Permissions bisher: `contents`, `pull-requests`, `checks`, `issues` — `pages: write`/`id-token: write` kommen noch nirgends vor.

Dokumentiert ist der Flow in `Docs/help/github-actions.md` (von `Docs/help/index.md:13` verlinkt). Hinweis: Der Abschnitt „Release-Build" dort beschreibt noch den alten Trigger „PR gegen main geschlossen und gemergt" — faktisch triggert `release.yml` jetzt auf `push` nach `main` bzw. Tags. Das ist ein kleiner vorhandener Doku-Drift, bei Erweiterung der Datei mit zu beachten.

### Dokumentationsstruktur und Screenshots

- `Docs/` (Groß-D): `index.md` (Doku-Übersicht), `features.md`, `configuration.md`, `deployment-guide.md`, `development-guide.md`, `install.md`, `testing.md`, `security.md`, `dependencies.md`, `screenshot-generation.md`, `Anforderungskatalog.md`, `RELEASE_NOTES.md`, dazu `help/` (Funktionsbereiche inkl. `github-actions.md`), `features/task/` (Feature-Artefakte, inkl. dieses Verzeichnisses), `projects/task/`.
- `Docs/index.md` verlinkt die Kern-Dokumente; `screenshot-generation.md` ist dort aktuell **nicht** gelistet (ebensowenig in `README.md`).
- `Docs/screenshots/` — acht Assets, alle vom 19.11.2025 und damit frisch: `home.png` (~1,0 MB), `recipe-detail.png` (~486 KB), `cookbooks.png` (~435 KB), `recipes.png` (~208 KB), `calendar.png` (~127 KB), `register.png` (~58 KB), `shopping-list.png` (~55 KB), `demo.gif` (~860 KB).
- Wiederverwendung in der Page: Die PNGs/GIF können direkt referenziert oder in den Pages-Output kopiert werden; `README.md:26` bettet `demo.gif` bereits ein.
- Wiederverwendbare Icons: `Rezepte.Web/wwwroot/icons/rezepte.png`, `rezepte-180x180.png`, `rezepte_64x64.ico` (in `App.razor:11-12` als Favicon/Apple-Touch-Icon referenziert) — als Logo/Favicon der Page nutzbar.

### Screenshot-Generierung und Pflegeprozess

- `scripts/screenshots/generate-screenshots.js` (Playwright 1.62.0, Chromium headless): publizierte App auf zufälligem Port starten, ersten Benutzer mit Demodaten-Option registrieren (wird Admin), auf Selektoren/JS-Polling warten (Blazor-WebSocket ⇒ kein `networkidle`), Viewport 1280×900, schreibt die sieben PNGs nach `Docs/screenshots/` (Zeilen 77-167).
- `scripts/screenshots/make-gif.js`: schneidet PNGs auf 1280×900 und erzeugt `demo.gif` (2 s/Frame).
- `scripts/screenshots/package.json`: `playwright`, `pngjs`, `gifenc` — eigener npm-Kontext neben dem Root-`package.json` (semantic-release).
- `Docs/screenshot-generation.md` dokumentiert Voraussetzungen, Ablauf (4 Schritte) und die Erweiterung für neue Features (neue `goto`/`screenshot`-Sequenz + `frames`-Eintrag). Dies ist der dokumentierte Pflegeprozess, auf den die Page-Dokumentation verweisen kann.

### App-Styling / Design-Anhaltspunkte

`Rezepte.Web/wwwroot/app.css` (87 Zeilen) definiert die komplette Palette in `:root` (Zeilen 2-15) — Kommentar Zeile 1: „warme Akzente (Tomate/Beere), Kräutergrün, Creme-Hintergrund":

| Token | Wert | Bedeutung |
|---|---|---|
| `--app-bg` | `#fffaf3` | helles Creme (Seitenhintergrund) |
| `--app-surface` | `#ffffff` | Karten-/Flächen |
| `--app-text` | `#2b2b2b` | Grundtext |
| `--app-muted` | `#6c757d` | sekundärer Text |
| `--app-border` | `#e6e2dc` | Rahmen |
| `--app-primary` | `#5A1F1F` | tiefes Bordeaux |
| `--app-primary-600` | `#cc4d29` | Tomaten-/Orange-Akzent |
| `--app-accent` | `#2e7d32` | Kräutergrün |
| `--app-accent-600` | `#256628` | Grün Hover |
| `--app-link` / `--app-link-hover` | `#b23a48` / `#8f2f3a` | Beere |

Weitere Stilmittel: Navbar mit Verlauf `90deg, --app-primary → --app-primary-600` (Zeile 28), Cards mit `border-radius: .75rem` + weichem Schatten (Zeilen 41-46), `.shadow-soft`, `.rounded-xl`. CSS-Framework: **Bootstrap 5.3.3** und **bootstrap-icons 1.11.3** via jsDelivr-CDN mit SRI (`App.razor:15,20,44`). Typografie: Bootstrap-Default (Systemschrift). Kein Dark Mode (s. Zusammenfassung); `navbar-dark` in `MainLayout.razor:7` ist nur Kontrastklasse auf dem Verlauf.

### README-/Docs-Verlinkung

- `README.md` trägt Badges (PR-CI, License, .NET), die Funktionsliste (Zeilen 9-20), das `demo.gif` (Zeile 26) und die Linkliste „Weitere Informationen" (Zeilen 77-89) — geeigneter Ort für einen Page-Verweis.
- Inhaltliche Quellen für die Page: Feature-Bullets `README.md:9-20` bzw. ausführlich `Docs/features.md`; Download-Verweise auf GitHub-Releases (`release-linux-x64.zip`, `release-win-x64.zip`, siehe `release.yml`-Asset-Liste); Repo-Link `https://github.com/martin-stromberg/Rezepte`.
- Für die geforderte Dokumentation des Aktualisierungswegs bieten sich an: neues `Docs/github-pages.md` (in `Docs/index.md` einordnen), Ergänzung in `Docs/help/github-actions.md` (Workflow-Doku, falls Actions-Variante) und Page-Link in `README.md`. Genauer Ort ist Planungsentscheidung.

### GitHub-Pages-Status und Staging-Einschränkungen

- `gh repo view martin-stromberg/Rezepte`: `visibility: PUBLIC`, Default-Branch `main`. Öffentliche Erreichbarkeit der Projektseite `martin-stromberg.github.io/Rezepte/` ist damit grundsätzlich möglich.
- `gh api repos/martin-stromberg/Rezepte/pages` → **404**: Pages ist nicht konfiguriert. Die Source-Einrichtung (Branch+Ordner oder „GitHub Actions") ist ein manueller Settings-/API-Schritt außerhalb der Codeänderung — in der Umsetzung zu dokumentieren und ggf. dem Maintainer zuzuweisen.
- Kein `CNAME`, kein Pages-Workflow, keine `configure-pages`/`upload-pages-artifact`/`deploy-pages`-Verwendung im Repo (grep über `.github/` und `Docs/` ohne Treffer für Pages).
- Branch-Source-Einschränkung: „Deploy from a branch" publiziert genau einen konfigurierten Branch. Wegen `verify-pr-source.yml` gelangen Inhalte nur über `staging` nach `main` — ein `docs/`-Ordner ginge also erst nach der Promotion live. Das passt zum Release-Modell (Seite erscheint mit dem nächsten Release-Stand), verzögert aber die Auslieferung bis zum Merge des Promotion-PRs.
- Pflicht bei Projektseiten: alle Asset-/Link-Pfade der Page müssen den Basispfad `/Rezepte/` berücksichtigen (relative Pfade verwenden).

## Variantenbewertung (Kurzfassung)

Vollständige Gegenüberstellung: [inventory/deployment-varianten.md](inventory/deployment-varianten.md).

- **`docs/`-Ordner (Branch-Source):** minimaler Mechanismus, kein Workflow nötig; aber harter Namenskonflikt mit `Docs/` auf case-insensitiven Entwicklungsmaschinen (hier: Windows), obligatorisches `.nojekyll`, Vermischung von Präsentations-Assets und Projektdokumentation im selben Verzeichnisbaum.
- **Actions-Deployment:** eigener Workflow auf `push: main` (deckungsgleich mit `release.yml`), kein Ordnerkonflikt, Screenshots bleiben Quelle in `Docs/screenshots/` und werden ins Artefakt kopiert, kein Jekyll nötig; erfordert neue Permissions (`pages: write`, `id-token: write`) und Source-Einstellung „GitHub Actions".

Faktenbasiert spricht der `Docs/`/`docs/`-Konflikt auf dem tatsächlichen Dev-System klar für die Actions-Variante; die endgültige Wahl ist Planungsentscheidung.

## Offene Punkte für den Plan

- Wer aktiviert GitHub Pages / setzt die Source in den Repo-Einstellungen (Voraussetzung für Akzeptanzkriterium „öffentlich erreichbar"; nicht aus Code verifizierbar)?
- Variante festlegen: `docs/`-Ordner vs. Actions-Deployment (Empfehlung s. o.).
- Wo liegen die Page-Quellen, wenn Actions gewählt wird (z. B. `site/` im Root oder neuer Ordner) — frei wählbar, sollte nicht mit `Docs/` kollidieren.
- Trigger-Wahl beim Actions-Deployment: `push: main` (mit Release-Freigabe) vs. `push: staging` (Vorschau-Charakter); Abstimmung mit dem Staging-Konzept nötig.
- Dark Mode der Page eigenständig umsetzen (`prefers-color-scheme` und/oder Toggle) — App hat keine Vorlage.
- Sprache der Page: Anwendung und Doku sind deutsch; Anforderung ebenfalls — vermutlich deutschsprachige Page, im Plan zu bestätigen.
- `Docs/help/github-actions.md` Abschnitt Release-Build ist leicht veraltet (Trigger-Beschreibung) — bei Erweiterung mit zu korrigieren oder separat als Folgepunkt zu notieren.

## Detaildokumente

- [Deployment-Varianten: `docs/`-Ordner vs. GitHub Actions](inventory/deployment-varianten.md)
