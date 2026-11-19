# Anforderungsübersetzung

## Metadaten

- Aufgaben-ID: `61917e8b-a063-457b-ae6c-ff26334ed989`
- Verknüpftes Issue: GitHub Issue #186 — „GitHub Page: Anwendung mit modernem Stil präsentieren"
- Branch: `task/issue-186-61917e8ba063457bae6cff26334ed989-github-page-anwendung-mit-mode`
- Kundenanforderung: „Bitte erstelle für dieses Repository eine GitHub Page, die die Anwendung mit modernem Stil präsentiert."

## Ziel

Das Repository erhält eine öffentlich erreichbare, statische GitHub Page unter `https://martin-stromberg.github.io/Rezepte/`, die die Anwendung „Rezepte" mit einem modernen, responsiven Erscheinungsbild präsentiert. Die Seite wird aus dem Repository heraus gepflegt und über GitHub Pages ausgeliefert.

## Kontext

- „Rezepte" ist eine deutschsprachige Webanwendung (Blazor Server, SQLite) zur Verwaltung von Kochbüchern, Rezepten, Bildern und geplanten Mahlzeiten, mit optionalen KI-Importfunktionen.
- Das Repository enthält bereits eine Dokumentation unter `Docs/`, Screenshots unter `Docs/screenshots/` (u. a. `home.png`, `recipes.png`, `recipe-detail.png`, `cookbooks.png`, `calendar.png`, `shopping-list.png`, `register.png`, `demo.gif`) sowie dokumentierte Skripte zur Screenshot-Erzeugung (`scripts/screenshots/`, beschrieben in `Docs/screenshot-generation.md`).
- Bestehende CI/CD-Workflows liegen unter `.github/workflows/` (u. a. `staging-ci.yml`, `pr-staging-ci.yml`, `release.yml`, `staging-to-main-promotion.yml`). Es gibt derzeit keinen Workflow oder Inhalt, der GitHub Pages ausliefert.
- Die Kundenanforderung nennt zwei mögliche Auslieferungswege (statischer `docs/`-Ordner oder GitHub-Actions-Deployment „passend zu den bestehenden CI/CD-Workflows"); die Wahl des Weges bleibt der Umsetzung überlassen.

## Fachliche Anforderungen

1. **Statische Seite über GitHub Pages:** Die Seite ist statisch und wird über GitHub Pages ausgeliefert — z. B. aus einem `docs/`-Ordner oder über ein GitHub-Actions-Deployment, das zu den bestehenden CI/CD-Workflows passt.
2. **Modernes, responsives Design:** Klare Typografie, konsistenter Stil, helle und dunkle Darstellung; das Erscheinungsbild lehnt sich an das Design der Anwendung an.
3. **Inhalte der Seite:**
   - prägnante Vorstellung der Anwendung (Zweck),
   - zentrale Features,
   - Screenshots,
   - Download-/Release-Verweise,
   - Verweise auf Repository und Dokumentation.
4. **Pflege aus dem Repository:** Inhalte und Screenshots der Seite werden aus dem Repository heraus gepflegt, damit die Seite mit der Anwendung mitwachsen kann.
5. **Dokumentation der Aktualisierung:** Der Aktualisierungsweg der Seite (Workflow bzw. manuelle Pflege) ist im Repository dokumentiert.

## Betroffene Bereiche

- `.github/workflows/` — bestehende CI/CD-Workflows; ggf. neuer oder angepasster Workflow für das Pages-Deployment (falls der Actions-Weg gewählt wird).
- Repository-Root bzw. `docs/` — möglicher Ablageort der statischen Seite (falls der `docs/`-Weg gewählt wird); Abgrenzung zur bestehenden Dokumentation unter `Docs/` (Groß-/Kleinschreibung beachten).
- `Docs/screenshots/` und `scripts/screenshots/` — vorhandene Screenshots und deren Erzeugung als Inhaltsquelle für die Seite.
- `Docs/` — Ablageort für die geforderte Dokumentation des Aktualisierungswegs.
- `README.md` — bestehender Einstiegspunkt mit Repository-Verweisen; ggf. Verweis auf die Page.
- GitHub-Repository-Einstellungen (Pages-Aktivierung, Source-Konfiguration) — Voraussetzung für die öffentliche Erreichbarkeit unter der Ziel-URL; liegt ggf. außerhalb der Codeänderung.

## Akzeptanzkriterien

1. Die Page ist unter `https://martin-stromberg.github.io/Rezepte/` öffentlich erreichbar.
2. Das Design ist responsiv, modern und präsentiert die Anwendung ansprechend.
3. Zweck, zentrale Features und Download-/Release-Verweis sind auf der Page vorhanden.
4. Die Aktualisierung der Seite (Workflow bzw. Pflege) ist im Repository dokumentiert.

## Explizite Nicht-Ziele und Annahmen

### Nicht-Ziele

- Keine Änderung an der Anwendung selbst (Blazor-Server-App, Features, Datenmodell).
- Kein Ersatz oder Umbau der bestehenden Projektdokumentation unter `Docs/` — die GitHub Page ist eine Präsentationsseite, kein Umzug der Doku.
- Keine eigenen Hosting- oder Domain-Lösungen außerhalb von GitHub Pages.
- Kein Ausbau der Inhalte über die geforderten Punkte hinaus (keine Blog-, Community- oder Mehrseiten-Struktur, soweit nicht für die geforderten Inhalte nötig).

### Annahmen

- Das Repository ist öffentlich bzw. GitHub Pages kann für es aktiviert werden (Voraussetzung für die öffentliche Erreichbarkeit). Die Pages-Aktivierung in den Repository-Einstellungen ist ggf. ein manueller Schritt außerhalb der Codeänderung und wird dokumentiert.
- Die vorhandenen Screenshots unter `Docs/screenshots/` können für die Seite wiederverwendet werden; neue Screenshots sind nur erforderlich, wenn die vorhandenen nicht ausreichen.
- Die Wahl zwischen `docs/`-Ordner und GitHub-Actions-Deployment ist Teil der Umsetzungsplanung; die Anforderung nennt beide Optionen als zulässig („z. B.").

## Offene Fragen

- Ist GitHub Pages für das Repository `martin-stromberg/Rezepte` bereits aktiviert, bzw. wer führt die Aktivierung/Source-Konfiguration in den Repository-Einstellungen durch, falls ein manueller Schritt nötig ist?
- Klärung im Rahmen der Planung, nicht zwingend vorab: Welcher Auslieferungsweg (`docs/`-Ordner vs. GitHub-Actions-Deployment) passt besser zum bestehenden Staging-Workflow (Feature-Branch → staging → main)?
