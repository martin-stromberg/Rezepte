# Bestandsaufnahme: Merge-Modell und Commit-Message-Überleben

## Korrigierter Befund (wichtig)

Die Voraussetzung „alle Merges sind Squash-Merges" trifft **nicht** zu. Nachweis:

- Repo-Einstellungen (`gh api repos/martin-stromberg/Rezepte`): `allow_merge_commit: true`, `allow_squash_merge: false`, `allow_rebase_merge: false`, `merge_commit_title: "PR_TITLE"`, `merge_commit_message: "PR_BODY"`.
- `bf108b7` („Automated promotion from staging to main (#231)") hat **zwei** Eltern: `79fb92d` (bisheriges main) + `d90e018` (staging-Spitze) → echter Merge-Commit.
- `79fb92d` (#222) und `31894e5` (#219): ebenfalls jeweils 2 Eltern — Promotion-Merges sind durchgängig echte Merge-Commits.
- `e9aed2d` („feat: Migrate solution to .NET 11 (#235)", PR-Merge auf `staging`): 2 Eltern `aa9263e` + `b7f6834` — auch Feature→staging-Merges sind echte Merge-Commits.
- Der „(#NNN)"-Suffix im Betreff stammt aus `merge_commit_title: PR_TITLE` und ist **kein** Squash-Indikator.

## Message-Komposition

- Betreff = PR-Titel + ` (#NNN)`; Body = **vollständiger PR-Body** (`merge_commit_message: PR_BODY`).
- Belege:
  - `bf108b7` Body = wörtlich der Promotion-PR-Body („Automated promotion from staging to main. Contains all changes …").
  - `e9aed2d` Body = PR-#235-Body (`## Summary …`, `Closes #228/#229/#233`, „Generated with Devin") — **nicht** die Commit-Liste des Feature-Branches.
- Commit-Footer des Feature-Branches gehen **nicht** in den Merge-Commit-Body ein, überleben aber als eigene Commits in der Historie: `0b3b96f` (Commit auf dem #235-Branch) trägt `Co-Authored-By: Devin …` im Body und ist über `b7f6834 → e9aed2d` Vorfahre; `e9aed2d` selbst enthält keinen Co-Authored-Footer.

## Konsequenz für `BREAKING CHANGE:`

Da main's Historie sämtliche staging-Commits als Vorfahren enthält, analysiert `@semantic-release/commit-analyzer` auf `main` alle Commits seit dem letzten Tag (`v1.22.0`), inklusive Merge-Commits und Feature-Branch-Commits. Ein Major-Marker zählt, wenn er in **irgendeinem** erreichbaren Commit steht:

1. In einem Commit auf `staging` vor der Promotion — z. B. als `BREAKING CHANGE:`-Footer in einem Commit dieses Feature-Branches (Footer muss auf Zeilenanfang stehen, Format `BREAKING CHANGE: <text>`).
2. Im **Promotion-Merge-Commit** selbst: Da `merge_commit_message: PR_BODY` gilt, landet der PR-Body des Promotion-PRs im Merge-Commit. Der Maintainer kann beim Merge den Commit-Body editieren bzw. den PR-Body vorher ergänzen — ein `BREAKING CHANGE:`-Footer dort wird vom Analyzer gelesen.
3. Alternativ ein `type!:`-Betreff in einem Commit auf `staging` (z. B. `feat!:`) — der Promotion-Merge-Titel „Automated promotion from staging to main" trägt keinen Conventional-Commit-Typ und liefert ohne Footer keinen Bump.

## Ist-Zustand der anstehenden Version

- Range `v1.22.0..staging-HEAD` enthält u. a. `feat:` (e9aed2d-Merge) und `fix:` (b7f6834) → nächstes Release wäre **minor** `v1.23.0`; der auf `staging` bereits erzeugte RC-Tag `v1.23.0-rc.1` bestätigt diese Erwartung.
- In `git log`-Recherche (200 Commits auf `main`, komplette relevante Range auf `staging`) existiert **kein** `BREAKING CHANGE:`-Footer und **kein** `type!:`-Betreff — ohne Eingriff entsteht kein `v2.0.0`.
- `Docs/RELEASE_NOTES.md` (auf `staging`, ab Zeile 5) behauptet bereits das Gegenteil der Issue-Prämisse: „Import plugins built for .NET 10 remain compatible and continue to load under .NET 11; rebuilding plugins is not required." — Widerspruch zur Anforderung („macht .NET-10-Plugins inkompatibel"), für die Planung relevant (Release-Notes-Korrektur nötig, wenn der Breaking-Change-Narrativ bestätigt wird).
