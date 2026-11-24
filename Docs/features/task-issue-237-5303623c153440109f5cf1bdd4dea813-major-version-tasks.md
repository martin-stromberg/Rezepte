# Tasks: Major-Versionssprung (.NET 11), `1.x`-Maintenance-Branch und Folge-Issue im Plugin-Repository

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Git-Operationen | `git fetch origin`; prüfen, ob `origin/main` noch `bf108b702907d2ee715bdc0341cedece5d55bf3a` ist (Abzweigpunkt für `1.x`) | Erledigt | `origin/main` == `bf108b7` verifiziert |
| 2 | Git-Operationen | `1.x`-Branch vom dokumentierten SHA `bf108b7…` abzweigen | Erledigt | `origin/1.x` Basis = `bf108b7` (Worktree) |
| 3 | Release-Konfiguration (`1.x`) | `.github/workflows/release.yml` auf `1.x`: `on.push.branches` + `1.x` | Erledigt | Commit `5e2e214`; Run 45494549535 grün |
| 4 | Release-Konfiguration (`1.x`) | `release.config.js` auf `1.x`: `branches` + `"1.x"` | Erledigt | Commit `5e2e214` |
| 5 | Release-Konfiguration (`1.x`) | `resolve-release-version.mjs` auf `1.x`: `AUTOMATIC_RELEASE_BRANCHES` + `"1.x"` | Erledigt | Commit `5e2e214`; `branch:1.x` → `automatic` im Lauf-Log |
| 6 | Release-Konfiguration (`1.x`) | `pr-staging-ci.yml` auf `1.x`: `pull_request.branches` + `1.x`; Backmerge-Gates auf `staging` scopen | Erledigt | Commits `5e2e214`, `b7abc8d` |
| 7 | Release-Konfiguration (`1.x`) | `release.yml` auf `1.x`: „Clear Latest flag"-Step (`make_latest=false`, inkl. Repair-Pfad + Retry) | Erledigt | Commits `5e2e214`, `b7abc8d`; PATCH live in Run 45498749209 |
| 7a | Release-Konfiguration (`1.x`) | Review-Nacharbeiten: leere Maintenance-Range als `no-release` (EINVALIDNEXTVERSION bis `v2.0.0`), Repair-Scan linien-/reparaturfähig scopen, Skip-Log | Erledigt | Commits `b109f1e`, `281208c`, `5d56039`; Log: `skipping permanently unrepairable release(s): v1.4.0…v1.0.0` |
| 8 | Git-Operationen | Config-Commits auf `1.x` erstellen und pushen | Erledigt | `origin/1.x` = `5d56039` |
| 9 | Validierung | Release-Läufe auf `1.x` verifizieren: grün, kein neues Release/Tag | Erledigt | Runs 45494549535 / 45497002404 / 45498749209 / 45500121268 alle grün; kein neues Tag; Latest = `v1.22.0` |
| 10 | Dokumentation | `Docs/RELEASE_NOTES.md` (EN): Kompatibilitätsbehauptung durch Breaking-Change-Hinweis ersetzen | Erledigt | Diff im Feature-Branch |
| 11 | Dokumentation | `Docs/RELEASE_NOTES.md` (DE): analog; zusätzlich `1.x`-Maintenance- und Suchfix-Notizen | Erledigt | Diff im Feature-Branch |
| 12 | Dokumentation | `Docs/help/github-actions.md`: Maintenance-Linie `1.x` (Abzweigpunkt, Range, Latest-Guard, Empty-Range-Verhalten, kein Rückfluss, Branch-Protection-Empfehlung) | Erledigt | Diff im Feature-Branch |
| 13 | Dokumentation | `Docs/help/import-plugins.md`: Hinweis für Plugin-Betreiber (.NET-10-Builds laden nicht unter .NET 11) | Erledigt | Diff im Feature-Branch |
| 14 | Versionsmarker | Commit mit `BREAKING CHANGE:`-Footer auf dem Feature-Branch; PR gegen `staging` | Erledigt | Dry-Run-Nachweis: mit Footer → `2.0.0`, ohne → `1.23.0` (siehe `test-results.md`) |
| 15 | Validierung | Nach Merge auf `staging`: RC-Frühnachweis `v2.0.0-rc.1` | Nach Merge | `git ls-remote --tags origin 'v2.0.0-rc.*'` |
| 16 | Externes Repo | Folge-Issue in `martin-stromberg/Rezepte.Plugins` anlegen | Erledigt | `martin-stromberg/Rezepte.Plugins#23` |
| 17 | Externes Repo | Verlinkung in #237 kommentieren | Erledigt | Kommentar auf Issue #237 |
| 18 | Validierung | Optional: `semantic-release --dry-run` Range-Verifikation | Erledigt (abgewandelt) | Dry-Run auf Marker-Testbranch → `2.0.0` nachgewiesen |
| 19 | Maintainer-Aktion | Promotion-Draft-PR `staging → main` mergen → Release `v2.0.0` mit BREAKING-CHANGES-Abschnitt | Offen (Maintainer) | `gh release view v2.0.0` nach Merge |
