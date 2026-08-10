# Uebersetzte Anforderung: Einfuehrung eines staging-Branches im CI-Prozess

## Zielsetzung

Der bestehende CI-Prozess soll erweitert werden, sodass Entwicklungs-PRs nicht mehr direkt in `main` gemergt werden. Ein neuer `staging`-Branch dient als Integrations- und Qualitaetssicherungsstufe. Alle automatisierten Tests, Sicherheitspruefungen und Qualitaets-Gates sollen sowohl auf PRs gegen `staging` als auch auf Aenderungen innerhalb dieses Branches ausgefuehrt werden. `main` bleibt ausschliesslich fuer stabile, freigegebene Versionen zustaendig und erzeugt weiterhin finale Release-Artefakte (release.zip).

## Funktionale Anforderungen

### Branch- und Workflow-Struktur
- Neuer Branch `staging` wird eingefuehrt.
- Feature-Branches werden ausschliesslich als Pull Requests gegen `staging` erstellt.
- Merge von `staging` nach `main` erst nach erfolgreicher Validierung aller Qualitaetskriterien.
- `main` bleibt stabil und dient ausschliesslich der Release-Erzeugung.

### CI fuer PRs gegen `staging`
- Unit-Tests
- Integrationstests
- Sicherheitspruefungen (Dependency-Scans, Static Code Analysis)
- Linting und Formatierungspruefungen
- Build-Validierung

### CI fuer Aenderungen im `staging`-Branch
- Vollstaendige Integrationstests
- Optionale End-to-End-/Performance-Tests oder Staging-Deployment
- Qualitaets-Gates (Mindest-Coverage, Sicherheitslevel)
- Automatische Erstellung eines PRs nach `main`, sofern alle Checks erfolgreich sind

### CI fuer `main`-Branch
- Automatische Versionserhoehung (Semantic Release)
- Erzeugung finaler Release-Artefakte
- Veroeffentlichung eines Releases inkl. release.zip
- Optionales Produktions-Deployment

### Team- und Prozessregeln
- Keine direkten Commits in `main`.
- PRs nach `main` ausschliesslich aus dem stabilen `staging`-Branch.
- Merge in `staging` oder `main` nur bei erfolgreichen Checks.

## Empfohlene Vorgehensweise (aus workflow-staging-vergleich.md)

1. `main` als Zielbranch fuer Promotion (`staging -> main` Draft-PR).
2. `staging`-CI mit Build, Tests, Security-Check und Format-/Analyse-Gate.
3. Back-Merge-Erkennung, um reine Rueckmerges effizient zu behandeln.
4. Rücksync-Workflow (`main -> staging` als PR), damit Tags/History konsistent bleiben.
5. Manuelle Merge-Freigabe per Branch-Protection (keine Auto-Merge-Aktion auf `main`).

## Nicht-Ziele

- Keine Einfuehrung von RC-/Prerelease-Artefakten, sofern nicht explizit benoetigt.
- Keine automatischen Merges in `main`; nur Draft-PR-Erstellung.
