# Deployment-Varianten für die GitHub Page

Bewertung der zwei von der Anforderung zugelassenen Auslieferungswege gegen den faktischen Repo-Stand. Keine Umsetzungsentscheidung — Grundlage für den Plan.

## Rahmenbedingungen (verifiziert)

- Repo `martin-stromberg/Rezepte`: öffentlich, Default-Branch `main`, Pages derzeit nicht aktiviert (API → 404).
- Ziel-URL `https://martin-stromberg.github.io/Rezepte/` ⇒ Projektseite, Basispfad `/Rezepte/`.
- Branch-Modell: Feature → `staging` → manuell freigegebener Promotion-PR → `main`; `verify-pr-source.yml` blockiert direkte PRs nach `main`; `sync-staging-with-main.yml` hält `staging` nach Releases synchron.
- Root enthält `Docs/` (Dokumentation). Windows-Checkout mit case-insensitivem Dateisystem: `docs` und `Docs` sind lokal dasselbe Verzeichnis (verifiziert: `ls -d docs` → `Docs/`).
- Page-Inhalte (HTML/CSS) sind bisher nirgends vorhanden; Screenshots-Quelle ist `Docs/screenshots/`.

## Variante A: `docs/`-Ordner (Branch-Source „Deploy from a branch")

Page-Quellen liegen als fertiges HTML/CSS/Assets im Ordner `docs/` des publizierten Branchs (typischerweise `main`).

**Vorteile**

- Kein zusätzlicher Workflow, keine neuen Actions-Permissions, kein Environment nötig; kleinster Mechanismus.
- Quellen liegen versioniert direkt im Repo; keine Build-Schritte — was committed ist, wird ausgeliefert.
- GitHub-üblichster, in den Settings direkt wählbarer Weg.

**Nachteile / Risiken (repo-spezifisch)**

- **Namenskonflikt mit `Docs/`:** GitHub Pages erwartet wörtlich `docs/` (Linux-Server, case-sensitiv), während lokale Windows-/macOS-Checkouts `docs` und `Docs` nicht unterscheiden. Dateien landen lokal im `Docs/`-Baum; die Trennung von Präsentationsseite und Projektdokumentation ist im Working Tree praktisch nicht sichtbar und fehleranfällig (falsche Ablage, verwirrende Diffs).
- Pages-Branch-Deployment läuft durch Jekyll, sofern keine `.nojekyll`-Datei vorhanden ist — für reine statische Seiten muss `.nojekyll` mit committed werden, sonst Verarbeitungsnebenwirkungen.
- Live-Gang hängt am konfigurierten Branch: Inhalte auf `staging` werden nicht ausgeliefert; erst der merge nach `main` publiziert. Konsistent mit dem Release-Modell, aber kein Vorab-Check der fertigen Seite vor Promotion möglich (außer lokaler Vorschau).
- Pages-Settings: Source „Branch `main`, Ordner `/docs`" manuell zu setzen.

## Variante B: GitHub-Actions-Deployment

Eigener Workflow (z. B. `pages.yml`) baut ein Artefakt und deployed es mit `actions/upload-pages-artifact` + `actions/deploy-pages`; Trigger naheliegend `push: main` (parallel zu `release.yml`), optional zusätzlich `workflow_dispatch`.

**Vorteile**

- **Kein Ordnerkonflikt:** Quellen können in einem beliebigen Ordner liegen (z. B. `site/`), `Docs/` bleibt unverändert reine Dokumentation.
- Passt zur bestehenden CI/CD-Struktur: sieben Workflows mit klaren Konventionen (pinned Actions-Versionen wie `checkout@v7`, `concurrency`-Gruppen, `timeout-minutes`, `gh`-CLI); ein Pages-Workflow wäre ein achter Baustein desselben Musters.
- Screenshots können im Job aus `Docs/screenshots/` ins Artefakt kopiert werden — Single Source bleibt der dokumentierte Screenshot-Prozess (`scripts/screenshots/`, `Docs/screenshot-generation.md`); keine Duplikate im Repo.
- `upload-pages-artifact` deployed das Artefakt ohne Jekyll — kein `.nojekyll` nötig.
- Trigger frei wählbar: `push: main` (Seite folgt Release-Stand) oder `push: staging` (früher live) — Abstimmung mit Staging-Konzept möglich.
- Keine Vermischung von Page-Assets und Doku-Verzeichnisbaum.

**Nachteile / Risiken**

- Neuer Permissions-Bedarf (`pages: write`, `id-token: write`), der in keinem bestehenden Workflow vorkommt; optional `github-pages`-Environment für Deployment-Schutzregeln.
- Pages-Source muss in den Repo-Settings auf „GitHub Actions" gestellt werden (Settings-Schritt, alternativ via API `gh api repos/martin-stromberg/Rezepte/pages -f build_type=workflow` — liegt außerhalb der Codeänderung, ist zu dokumentieren).
- Ein Workflow mehr zu pflegen; Deployments hängen von Action-Läufen ab (Fehlerquelle zusätzlich zum Inhalt).

## Repo-faktische Abwägung

| Kriterium | `docs/`-Ordner | Actions-Deployment |
|---|---|---|
| Mechanismus | minimal, kein CI nötig | neuer Workflow, folgt bestehenden Konventionen |
| Konflikt mit `Docs/` | ja (case-insensitive Dev-Systeme) | nein |
| Jekyll-Bypass nötig | ja (`.nojekyll`) | nein (Artefakt-Deployment) |
| Screenshots | müssen physisch unter `docs/` liegen oder verlinkt werden | aus `Docs/screenshots/` ins Artefakt kopierbar |
| Live-Gang | mit Merge des Page-Ordners auf `main` | mit Workflow-Lauf auf gewähltem Trigger |
| Settings-Aufwand | Source Branch+`/docs` setzen | Source „GitHub Actions" setzen |
| Neue Permissions | keine | `pages: write`, `id-token: write` |

**Tendenz aus den Fakten:** Der `Docs/`↔`docs/`-Konflikt ist auf dem tatsächlichen Entwicklungssystem (Windows) ein konkretes, dauerhaftes Fehlerrisiko und vermischt die Page faktisch mit der Projektdokumentation — beides spricht gegen den von der Anforderung vorgeschlagenen `docs/`-Pfad und für das Actions-Deployment, das zudem besser zum dokumentierten Staging-/Release-Prozess und zur Screenshot-Quelle passt. Beide Varianten erfordern denselben einmaligen Settings-Schritt (Pages-Aktivierung), sodass der Actions-Weg keinen zusätzlichen manuellen Aufwand gegenüber `docs/` bedeutet.

## Offene Punkte an den Plan

- Finale Variantenwahl und Ablageort der Page-Quellen.
- Trigger und Zuschnitt des Workflows (bei Variante B).
- Konkrete Beschreibung des Settings-Schritts (für Maintainer-Doku), unabhängig von der Variante.
- Behandlung des Basispfads `/Rezepte/` in Links/Asset-Pfaden.
