# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| 1. Gepushter Maintenance-Branch vom letzten .NET-10-Stand von `main`; letzter .NET-10-SHA dokumentiert | Ablauf 1 / Umsetzungsreihenfolge Schritt 1–2: `git checkout -b 1.x bf108b702907d2ee715bdc0341cedece5d55bf3a`, Push; SHA dokumentiert in `plan.md`, `Docs/help/github-actions.md` (persistent auf `staging`/`main`) und im Plugin-Repo-Issue. Verifiziert: `origin/main` steht tatsächlich auf `bf108b7` (Promotion #231, `net10.0` in `Directory.Build.props` nachgewiesen); Fallback auf dokumentierten SHA bei vorheriger Promotion ist vorgesehen. | Szenario 2: `gh run list --workflow release.yml --branch 1.x` + `gh release list` unverändert | Abgedeckt |
| 2. `release.config.js` so konfiguriert, dass Pushes auf dem Maintenance-Branch semantic-release-Versionierung in der 1.x-Linie auslösen | Konsistente Fünfer-Änderung auf `1.x` selbst (korrekt: Push-Trigger lesen die Workflow-Datei des gepushten Commits): `release.config.js` Z. 74 `["main"]` → `["main", "1.x"]`, `release.yml` Z. 12 `[main]` → `[main, 1.x]`, `resolve-release-version.mjs` Z. 23 `AUTOMATIC_RELEASE_BRANCHES`, `pr-staging-ci.yml` Z. 13-14 um `1.x`, Latest-Guard-Step. Verifiziert: Zeilennummern stimmen exakt; `"1.x"` als Range-Muster-Name erzeugt implizit `range: "1.x"` (semantic-release-Maintenance-Semantik); `v1.22.0` (`79fb92d`, direkter Merge-Parent von `bf108b7`) ist als In-Range-Tag erreichbar. | Erster `1.x`-Lauf muss grün ohne Release enden; optionaler lokaler Dry-Run (Schritt 7); Szenario 3 für den späteren Fix-Fall | Abgedeckt |
| 3. Nächster Promotion-Merge erzeugt Major-Tag (erwartet `v2.0.0`) mit Breaking-Change-Ausweis in den Release Notes | `BREAKING CHANGE:`-Footer im Implementierungs-Commit des Feature-Branches (Ablauf 2, Schritt 4) → landet über PR → `staging` → Promotion als Vorfahre in `main`s Historie. Verifiziert: `allow_squash_merge: false` (gh api), alle Merges echte Merge-Commits → der Analyzer liest den Footer; kein bestehender `BREAKING CHANGE:`/`type!:`-Marker in der Historie → sonst `v1.23.0`; `v1.23.0-rc.1` auf `staging` bestätigt die Minor-Erwartung. PR-Body-Fallback für den Maintainer dokumentiert. | Szenario 1 (Happy Path): `gh release view v2.0.0 --json tagName,isLatest` + „BREAKING CHANGES"-Abschnitt; Frühnachweis `v2.0.0-rc.1` via `git ls-remote` nach Staging-Merge (Schritt 5) | Abgedeckt |
| 4. Issue in `martin-stromberg/Rezepte.Plugins` mit den drei Pflichtinhalten, beidseitig verlinkt | Ablauf 3: `gh issue create` mit wörtlichem Entwurf — enthält (a) .NET-11-Anhebung inkl. `dotnet-version: 10.0.x → 11.0.x` in `ci.yml`/`release.yml` und `contract-lock.json`-Umhängung, (b) .NET-10-Maintenance-Branch **inkl. korrekter Warnung** vor der nicht branch-sensitiven Machinerie (`pull_request: closed` ohne Filter; `Get-NextReleaseVersion.ps1` bump ab globalem Höchst-Tag → `v1.0.1` statt `v0.3.8`), (c) Major-Release `v0.3.7` → `v1.0.0`; Verlinkung via `gh issue comment 237`. Alle Fakten des Entwurfs decken sich mit `inventory/plugin-repo.md`. | Szenario 5: `gh issue view <N> --repo martin-stromberg/Rezepte.Plugins` + Kommentar in #237 | Abgedeckt |
| 5. Staging-Flow (RC-Prereleases, Promotion-/Sync-PRs, `verify-pr-source`) funktioniert unverändert | Keine Änderungen auf `staging`/`main` (bewusste Designentscheidung: `push`-Events lesen die Workflow-Datei des gepushten Commits); `staging-ci.yml`-`--branches staging`-Override ersetzt die `branches`-Config vollständig — der `1.x`-Eintrag liegt zudem nur auf `1.x`. Verifiziert: `verify-pr-source.yml` triggert nur bei Basis `main`; `staging-to-main-promotion`/`sync-staging-with-main` nur auf `staging`/`main`. | Szenario 4: nächster RC heißt `v2.0.0-rc.1` (Marker-Wirkung, kein Flow-Bruch), `staging-ci`-Lauf grün, Promotion-Draft-PR wird aktualisiert | Abgedeckt |

## Fehlende oder unvollständige Testanforderungen

Keine.

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Kein UI-/Nutzerfluss betroffen — die Anforderung betrifft ausschließlich Git-Branches, Release-/CI-Konfiguration und Dokumentation | Keine E2E-Tests; Funktionsnachweis = realer Workflow-Lauf + Dry-Runs/CLI-Verifikationen (vom Akzeptanzkriterium so gefordert; im Plan mit Begründung dokumentiert) | Nicht erforderlich mit Begründung |

## Fehlende oder unvollständige Planbestandteile

Keine. `Scope-Klärung` ist vollständig (Verarbeitungseinheit, auslösende Ereignisse, erwartete Wirkung, Umgang mit fehlenden Objekten); alle 7 Scope-Ausschlüsse tragen Begründung und Status; `Akzeptanzszenarien` enthält einen vollständigen Happy Path vom Maintainer-Merge bis zum sichtbaren `v2.0.0`-Release; alle offenen Fragen der Anforderung sind entschieden.

## Hinweise

Optionale Hinweise zur Nachplanung (keine Lücken):

- **Faktische Ungenauigkeit in Scope-Ausschluss 6/7:** Der Vermerk „via Issue-Kommentar in #237 zur Kenntnis vorgelegt" ist nicht verifizierbar — Issue #237 enthält aktuell nur 2 Kommentare (Wartebestätigung, Refinement-Analyse), keiner legt die Ausschluss-Entscheidungen vor. Status `bestätigt` + Begründung sind formal vorhanden; die Herkunftsangabe sollte korrigiert oder der Kommentar tatsächlich gepostet werden.
- **Schritt 7 (optionaler Dry-Run):** `npx semantic-release --dry-run --no-ci --branches 1.x` nutzt `releasePlugins` inkl. `@semantic-release/github` — dessen `verifyConditions` benötigt `GITHUB_TOKEN`/`GH_TOKEN` im Environment; ohne Token schlägt der Schritt fehl. Alternativ `RESOLVE_DRY_RUN=true` setzen (dann greifen nur `dryRunPlugins`/commit-analyzer) oder `GH_TOKEN` aus `gh auth token` belegen.
- **`reason=no-release`-Nachweis:** `gh run list` zeigt nur Status/Conclusion; die Step-Outputs `released`/`reason` stehen im Log des Steps „Resolve release version" (`gh run view --job <id> --log`) — Verifikationsbefehl in Schritt 2 entsprechend präzisieren.
- **Guard-Step-Robustheit:** Ein Fehlschlag des `gh api`-PATCH ließe den Release-Job rot enden, obwohl das Release bereits veröffentlicht ist — optional Retry oder `continue-on-error: true` erwägen. Zudem existiert ein kurzes Zeitfenster zwischen Release-Erstellung und Guard, in dem `releases/latest` auf das Maintenance-Release zeigt (kosmetisch).
- **Marker-Ort als bewusste Abweichung transparent halten:** Der Issue-Wortlaut verlangt den `BREAKING CHANGE:`-Vermerk im Promotion-Merge-Commit; der Plan setzt ihn stattdessen in einen `docs:`-Commit des Feature-Branches — funktional gleichwertig (Analyzer liest alle erreichbaren Commits) und robuster, aber dem Maintainer gegenüber explizit kommunizieren (PR-Body-Fallback ist dokumentiert).
- **`release.config.js`-Kommentar (Z. 3-11)** beschreibt auf `1.x` weiterhin nur die `main`-/`staging`-Logik; beim Config-Commit optional um den Maintenance-Eintrag ergänzen.
- **Verifizierte Kernannahmen (de-riskiert):** `v1.22.0..bf108b7` enthält ausschließlich `chore:`/`docs:`/`plan:`-/Merge-Commits → der erste `1.x`-Push erzeugt nachweislich kein Spurious-Release; `update.json`/Asset-Reparatur/`concurrency.group` sind im Risikoabschnitt korrekt behandelt; `permissions: contents: write` reicht für den `make_latest`-PATCH (Update-Release-Endpoint verlangt Contents-Write).
