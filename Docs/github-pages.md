# GitHub Page

Das Repository bringt eine statische Präsentationsseite für die Anwendung mit, die über GitHub Pages ausgeliefert wird:

- **Öffentliche URL:** `https://martin-stromberg.github.io/Rezepte/`
- **Zweck:** deutschsprachiger One-Pager mit Zweck, Features, Screenshots, Download-Verweisen und Links auf Repository/Dokumentation – responsiv, mit heller und dunkler Darstellung im Farbschema der Anwendung.

Die Seite ist bewusst getrennt von der Projektdokumentation unter `Docs/`: Es gibt **kein** `docs/`-Verzeichnis (auf case-insensitiven Dateisystemen würde es mit `Docs/` kollidieren). Die Auslieferung erfolgt ausschließlich über den Workflow `.github/workflows/pages.yml` mit einem gebauten Pages-Artefakt – nicht über eine Branch-Source und ohne Jekyll (kein `.nojekyll` nötig).

## Struktur

| Pfad | Inhalt |
|---|---|
| `site/index.html` | Gepflegter One-Pager (Header/Nav, Hero, Features, Galerie, Download, Doku-Links, Footer; `<html lang="de">`) |
| `site/assets/css/site.css` | Custom Properties für die helle Palette (aus `Rezepte.Web/wwwroot/app.css` abgeleitet) und die dunkle Palette, Layout, responsives Grid |
| `site/assets/js/theme.js` | Theme-Toggle (`data-theme`, `localStorage`, `prefers-color-scheme`-Fallback) |
| `site/assets/js/demo.js` | Start/Stopp-Steuerung für die animierte Demo-Tour (kein Autoplay; ohne JS oder bei `prefers-reduced-motion: reduce` bleibt die statische Vorschau und der Button bleibt verborgen) |
| `site/screenshots/*.webp` | Optimierte WebP-Varianten der Screenshots (aus `Docs/screenshots/*.png` erzeugt, z. B. `home.webp` ~160 KB statt ~1 MB); Hero und Galerie nutzen sie per `<picture>` mit PNG-Fallback, die statische Demo-Vorschau lädt `home.webp` direkt |
| `scripts/Build-Site.ps1` | Assembliert den Ausgabeordner aus `site/` + `Docs/screenshots/` + zwei Icons aus `Rezepte.Web/wwwroot/icons/`; das rekursive Leeren des Ausgabepfads ist auf Kindpfade unterhalb von `artifacts/` beschränkt |
| `scripts/check-site.mjs` | Statische Validierung des gebauten Outputs inkl. Referenz- (`src`/`href`/`srcset`/`data-animated-src`) und WCAG-Kontrastprüfung beider Paletten (dependency-frei, Node ≥ 20) |
| `.github/workflows/pages.yml` | Build/Validate für PRs und Deploy auf `main` |

Screenshots und Icons werden **nicht** nach `site/` dupliziert. Single Source bleibt `Docs/screenshots/` (Erzeugung siehe [screenshot-generation.md](screenshot-generation.md)) bzw. `Rezepte.Web/wwwroot/icons/`; `Build-Site.ps1` kopiert sie beim Build ins Artefakt:

- `site/*` → Ausgabe (`index.html`, `assets/css/site.css`, `assets/js/theme.js`, `assets/js/demo.js`, `screenshots/*.webp`)
- `Docs/screenshots/*` → `screenshots/`
- `rezepte-180x180.png`, `rezepte_64x64.ico` → `assets/icons/` (Header-Logo, PNG-Favicon und apple-touch-icon bzw. ICO-Favicon)

`rezepte.png` (~1,3 MB) wird bewusst **nicht** kopiert – die Datei wird auf der Seite nirgends referenziert; Header-Logo und PNG-Favicon nutzen `rezepte-180x180.png`.

## Basispfad und Links

Die Seite ist eine Projektseite unter dem Basispfad `/Rezepte/`. **Alle internen Referenzen sind relativ und ohne führenden `/`** (`assets/css/site.css`, `screenshots/home.png`, `#features`), damit die Seite unter `/Rezepte/` und in jeder lokalen Vorschau identisch funktioniert. `check-site.mjs` lässt führende `/` in lokalen Referenzen nicht zu. Externe Links (GitHub, Releases, Docs-Dateien) sind absolut mit `https://` – die Page liegt auf `github.io`, Repo-Dateien werden nicht mit ausgeliefert.

## Workflow `.github/workflows/pages.yml`

| Trigger | Verhalten |
|---|---|
| `pull_request` → `staging`/`main` | Job `build`: Assemblieren + Validierung + Artefakt-Upload. **Kein Deploy** – PRs deployen nie. |
| `push` → `main` | `build` + `deploy`: die Seite folgt dem Release-Stand (deckungsgleich mit `release.yml`). |
| `workflow_dispatch` | Manuelles Re-Deploy auf `main`, u. a. als Recovery-Pfad nach der Pages-Aktivierung. |

Der `paths`-Filter (`site/**`, `Docs/screenshots/**`, `Rezepte.Web/wwwroot/icons/**`, die beiden Skripte, die Workflow-Datei) verhindert Läufe bei seitenfremden Änderungen.

- Top-Level-Permissions: `contents: read`. Der `deploy`-Job deklariert explizit `contents: read`, `pages: write`, `id-token: write` und läuft im Environment `github-pages`.
- `actions/configure-pages` wird **nicht** verwendet: Die Action ruft die Pages-REST-API auf und bräuchte die `pages`-Permission bereits im Build-Job, wo sie in PR-Läufen mit 403 scheitern würde. Für eine handgebaute statische Seite mit relativen Pfaden liefert sie ohnehin keinen Mehrwert.
- Versionsstand der Pages-Actions (bei Umsetzung verifiziert): `actions/upload-pages-artifact@v5` (v5.0.0), `actions/deploy-pages@v5` (v5.0.1). Die ursprüngliche Planung ging von `@v4` aus; die verifiziert aktuellen Majors sind `v5`. `checkout@v7`/`setup-node@v7` folgen der Repo-Konvention.

## Lokale Vorschau

```powershell
./scripts/Build-Site.ps1 -OutputDirectory artifacts/pages
node scripts/check-site.mjs artifacts/pages
```

Den Ordner `artifacts/pages` anschließend mit einem beliebigen statischen Server ausliefern – netzwerkfreie Optionen zuerst:

```powershell
# Python (falls installiert), kein Netzwerkzugriff nötig:
cd artifacts/pages
python -m http.server 8080
# -> http://localhost:8080/
```

Alternativ die VS-Code-Erweiterung „Live Server" auf `artifacts/pages` anwenden. Mit Netzwerkzugang geht auch `npx serve artifacts/pages`.

## Validierung (`scripts/check-site.mjs`)

Der Check läuft lokal und in jedem Workflow-Lauf über dem gebauten Output:

- `index.html` vorhanden, `lang="de"`, nicht-leeres `<title>`, responsives Viewport-Meta-Tag
- jede lokale Referenz existiert als **Datei** im Output (Verzeichnisse zählen nicht); lokale Referenzen mit führendem `/` sind verboten. Erfasst werden `src`/`href`, `srcset`-Kandidaten (kommagetrennt, optionale Deskriptoren) und gezielt das `data-animated-src`-Attribut des Demo-Toggles. HTML-Kommentare und Inline-`<script>`-Inhalte werden nicht als Markup-Referenzen gewertet (externe Script-`src` schon), beliebige andere `data-*`-Attribute gelten nicht als Referenzen; ungültige Prozent-Escapes melden einen FAIL statt eines Stacktrace.
- `screenshots/` vorhanden und nicht leer
- **WCAG-Kontrastprüfung beider Paletten:** Das Skript liest die Custom-Property-Werte aus der gebauten `site.css` (hell aus `:root`, dunkel aus `[data-theme="dark"]`) und berechnet für eine feste Liste von Token-Paaren das Kontrastverhältnis über die relative Luminanz (WCAG-2.x-Formel, ohne Dependencies). Geprüfte Paare je Palette: Text↔Hintergrund, Text↔Surface, Muted↔Hintergrund, Muted↔Surface, Link↔Hintergrund, Link↔Surface, helle Schrift auf beiden Verlaufsenden (`--app-primary`, `--app-primary-600`) sowie Border↔Surface als UI-Komponenten-Check. Komponentenbindung: die Label-Farben des Hero-Buttons `.btn-primary` (`--app-primary` bzw. `:hover` `--app-primary-600`) werden gegen seine fest helle Fläche `--app-on-primary` geprüft — der Button nutzt bewusst nicht `--app-surface` als Fläche, damit die dunkle Surface-Token-Färbung den CTA nicht unleserlich macht. Schwellen: ≥ 4,5:1 für normalen Text, ≥ 3:1 für große Schrift/UI-Komponenten. Zusätzlich muss die dunkle Palette im `@media (prefers-color-scheme: dark)`-Block wertgleich zum `[data-theme="dark"]`-Block sein, damit beide Definitionen nicht auseinanderlaufen. Bei einem Fehlschlag wird der betroffene Token angepasst, nicht die Schwelle aufgeweicht.
- Exit-Code ≠ 0 mit klarer Meldung je Befund.

Zwei Tokens weichen bewusst von `app.css` ab, damit die Prüfung besteht (in `site.css` kommentiert): `--app-border` ist hell wie dunkel kräftiger als `#e6e2dc`/`#453d36` (≥ 3:1 gegen Surface), `--app-primary-600` ist leicht dunkler als `#cc4d29` (≥ 4,5:1 für weiße Schrift).

## Pflegeprozess

- **Texte/Struktur:** direkt in `site/index.html`, Stile in `site/assets/css/site.css`. Beim Ändern der Palette die beiden Dark-Blöcke synchron halten – `check-site.mjs` erzwingt das.
- **Demo-Animation:** die Tour (`screenshots/demo.gif`) startet nie automatisch. `index.html` zeigt standardmäßig die statische Vorschau (`home.webp`, bewusst ein plain `<img>` ohne `<picture>`: ein `<source>`-Element würde das per `demo.js` eingetauschte GIF weiterhin überstimmen); `site/assets/js/demo.js` blendet den Start/Stopp-Button nur ein, wenn JavaScript läuft und kein `prefers-reduced-motion: reduce` aktiv ist. Das animierte Ziel steht in `data-animated-src` und wird vom Referenz-Check geprüft.
- **WebP-Varianten:** `site/screenshots/*.webp` sind die optimierten Varianten aus `Docs/screenshots/*.png` (Hero `home`, Galerie `recipes`, `recipe-detail`, `cookbooks`, `calendar`, `shopping-list`; Erzeugung z. B. per Pillow: `Image.open('Docs/screenshots/<name>.png').save('site/screenshots/<name>.webp', 'WEBP', quality=82)`). Nach einer Screenshot-Aktualisierung müssen die WebP-Dateien neu erzeugt und committet werden.
- **Screenshots:** über den dokumentierten Prozess in [screenshot-generation.md](screenshot-generation.md) regenerieren (`scripts/screenshots/`); sie landen in `Docs/screenshots/` und werden beim nächsten Deploy automatisch ins Artefakt kopiert.
- **Auslieferung:** Änderungen auf `site/**`, Screenshots, Icons oder den Skripten lösen auf `main` automatisch Build + Deploy aus. Auf PRs läuft nur die Validierung.

## Einmalige Aktivierung (Maintainer-Schritt)

GitHub Pages muss einmalig in den Repository-Einstellungen aktiviert werden; das kann nicht aus dem Workflow heraus geschehen (erfordert Admin-Rechte):

1. GitHub → Repository `martin-stromberg/Rezepte` → **Settings → Pages → Build and deployment → Source: „GitHub Actions"**.
2. Der Schritt kann jederzeit erfolgen, am besten **vor** dem Promotion-Merge nach `main`.
3. Verifikation: `gh api repos/martin-stromberg/Rezepte/pages --jq .build_type` → `workflow`.

**Reihenfolge/Recovery:** War die Source beim ersten `push`-Lauf auf `main` noch nicht gesetzt, schlägt `deploy-pages` mit „Pages site not found" fehl. Nachholen: Settings-Schritt ausführen und den Workflow über **Actions → Pages → Run workflow** (auf `main`) erneut starten – dafür ist `workflow_dispatch` vorgesehen.

## Troubleshooting

| Symptom | Ursache / Lösung |
|---|---|
| Deploy schlägt mit „Pages site not found" fehl | Pages-Source noch nicht aktiviert → Settings-Schritt (s. o.) + Re-Run via `workflow_dispatch`. |
| Seite lädt ohne CSS/Bilder | Führende `/` in lokalen Referenzen → `check-site.mjs` fängt das bereits in CI ab; alle Pfade relativ halten. |
| Workflow-Button „Run workflow" fehlt | `workflow_dispatch` ist erst nutzbar, wenn der Workflow auf dem Default-Branch (`main`) liegt. |
| Dunkle Darstellung greift nicht | Immer beide Dark-Blöcke (Media-Query und `[data-theme="dark"]`) pflegen; der Check verlangt Wertgleichheit. |
