# Tasks: Major-Versionssprung (.NET 11), `1.x`-Maintenance-Branch und Folge-Issue im Plugin-Repository

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Git-Operationen | `git fetch origin`; prüfen, ob `origin/main` noch `bf108b702907d2ee715bdc0341cedece5d55bf3a` ist (Abzweigpunkt für `1.x`) | Offen | — |
| 2 | Git-Operationen | `1.x`-Branch vom dokumentierten SHA `bf108b7…` abzweigen (`git checkout -b 1.x` bzw. `git worktree add`) | Offen | — |
| 3 | Release-Konfiguration (`1.x`) | `.github/workflows/release.yml` auf `1.x`: `on.push.branches` `[main]` → `[main, 1.x]` (Z. 12) | Offen | — |
| 4 | Release-Konfiguration (`1.x`) | `release.config.js` auf `1.x`: `branches` `["main"]` → `["main", "1.x"]` (Z. 74) | Offen | — |
| 5 | Release-Konfiguration (`1.x`) | `scripts/resolve-release-version.mjs` auf `1.x`: `AUTOMATIC_RELEASE_BRANCHES` um `"1.x"` ergänzen (Z. 23, Kommentar anpassen) | Offen | — |
| 6 | Release-Konfiguration (`1.x`) | `.github/workflows/pr-staging-ci.yml` auf `1.x`: `on.pull_request.branches` um `1.x` ergänzen (Z. 13-14) | Offen | — |
| 7 | Release-Konfiguration (`1.x`) | `release.yml` auf `1.x`: letzten Step „Clear Latest flag on maintenance release" einfügen (`make_latest=false` via `gh api`, Bedingung `released == 'true' && release_action == 'create'`) | Offen | — |
| 8 | Git-Operationen | Config-Commit `chore(release): enable releases from the 1.x maintenance branch` auf `1.x` erstellen und `git push -u origin 1.x` | Offen | — |
| 9 | Validierung | Release-Lauf auf `1.x` verifizieren: `gh run list --workflow release.yml --branch 1.x` → grün, `reason=no-release`, kein Release erzeugt | Offen | — |
| 10 | Dokumentation | `Docs/RELEASE_NOTES.md` Zeile 6 (EN): Kompatibilitätsbehauptung durch Breaking-Change-Hinweis ersetzen | Offen | — |
| 11 | Dokumentation | `Docs/RELEASE_NOTES.md` Zeile 16 (DE): Kompatibilitätsbehauptung durch Breaking-Change-Hinweis ersetzen | Offen | — |
| 12 | Dokumentation | `Docs/help/github-actions.md` Abschnitt „Branch-Modell" um Maintenance-Linie `1.x` ergänzen (Abzweigpunkt, Range, Latest-Guard, kein Rückfluss, Branch-Protection-Empfehlung) | Offen | — |
| 13 | Dokumentation | `Docs/help/import-plugins.md`: Hinweis-Absatz für externe Plugin-Betreiber (.NET-10-Builds laden nicht unter .NET 11; Neubau erforderlich) | Offen | — |
| 14 | Versionsmarker | Ein Commit mit `BREAKING CHANGE:`-Footer (Plugin-Inkompatibilität) auf dem Feature-Branch erstellen und pushen; PR gegen `staging` | Offen | — |
| 15 | Validierung | Nach Merge auf `staging`: RC-Frühnachweis — `git ls-remote --tags origin 'v2.0.0-rc.*'` zeigt `v2.0.0-rc.1` | Offen | — |
| 16 | Externes Repo | `gh issue create --repo martin-stromberg/Rezepte.Plugins` mit dem Issue-Entwurf aus `plan.md` Ablauf 3 | Offen | — |
| 17 | Externes Repo | `gh issue comment 237 --repo martin-stromberg/Rezepte` mit Verweis auf das neue Plugin-Issue | Offen | — |
| 18 | Validierung | Optional: `npx semantic-release --dry-run --no-ci --branches 1.x` auf `1.x`-Checkout zur Range-Verifikation | Offen | — |
| 19 | Maintainer-Aktion | Promotion-Draft-PR `staging → main` mergen; optional `BREAKING CHANGE:`-Footer in PR-Body; Release `v2.0.0` mit BREAKING-CHANGES-Abschnitt verifizieren | Offen | — |
