# Umsetzungsplan: Einfuehrung staging-Branch

## Zusammenfassung

Das Repository erhaelt einen zweistufigen CI-Fluss: Feature-Branches -> `staging` -> `main`. Dazu werden bestehende Workflows angepasst und neue Workflows ergaenzt. Der `main`-Branch bleibt fuer stabile Releases; `staging` ist Integrations- und Qualitaetsstufe.

## Aenderungen

### 1. `.github/workflows/pr.yml` anpassen
- Trigger aendern von `branches: [main]` auf `branches: [staging]`.
- Back-Merge-Erkennung ergaenzen: Wenn `github.head_ref == 'main'`, ueberspringen (reiner `main -> staging` Sync).
- Alle bestehenden Jobs (Build, Test, Format, Contract-Export) bleiben erhalten.

### 2. `.github/workflows/staging-ci.yml` neu
- Trigger: `push: branches: [staging]`.
- Wiederverwendung der etablierten Build-/Test-Schritte aus `pr.yml` fuer vollstaendige Integrationstests.
- Back-Merge-Erkennung auf Push-Ebene: Wenn Tree identisch zu `main`, Pipeline ueberspringen.
- Keine Prerelease-Erstellung (laut Empfehlung nicht benoetigt).

### 3. `.github/workflows/staging-to-main-promotion.yml` neu
- Trigger: `workflow_run` auf erfolgreichen Abschluss von `Staging Branch CI` auf `staging`.
- Prueft, ob `main` hinter `staging` zurueckliegt.
- Erstellt Draft-PR `staging -> main`, falls noch nicht vorhanden.
- Verwendet `gh pr create` und `automated-promotion`-Label.
- Kein Auto-Merge (manuelle Freigabe).

### 4. `.github/workflows/sync-staging-with-main.yml` neu
- Trigger: `push: branches: [main]`.
- Prueft, ob `staging` hinter `main` zurueckliegt.
- Erstellt PR `main -> staging` (kein Direct-Push), um Tags/History konsistent zu halten.
- Hinweis im Body: Merge mit "Create a merge commit".

### 5. `.github/workflows/verify-pr-source.yml` neu
- Trigger: `pull_request` auf `main`.
- Verbietet PRs nach `main`, die nicht aus `staging` kommen.
- Fuer Ruecksync-PRs (main -> staging) wird `pr.yml` auf staging-Seite entsprechend entlastet.

### 6. `.github/workflows/release.yml` beibehalten
- Trigger bleibt `pull_request_target` auf `main` bei merge.
- Release-Erzeugung, Versionierung, release.zip, Git-Tag und GitHub Release bleiben unveraendert.

## Offene Punkte

Keine. Alle technischen Entscheidungen koennen aus den Anforderungen und der Vergleichsanalyse abgeleitet werden.

## Risiken und Hinweise

- `pull_request_target` in `release.yml` erfordert Branch-Protection-Konfiguration in GitHub, um sicherzustellen, dass nur verifizierte Promotion-PRs gemergt werden.
- Branch-Protection Rules fuer `staging` und `main` muessen manuell im Repository aktiviert werden (ausserhalb dieser Workflow-Dateien).
- Die neuen Workflows setzen den `gh`-CLI-Befehl und `GITHUB_TOKEN` mit passenden Permissions voraus.
