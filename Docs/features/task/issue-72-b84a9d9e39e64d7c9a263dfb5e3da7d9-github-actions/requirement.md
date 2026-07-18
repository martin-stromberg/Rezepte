# Anforderung: GitHub-Actions

## Metadaten

| Feld | Wert |
|------|------|
| Aufgaben-ID | b84a9d9e-39e6-4d7c-9a26-3dfb5e3da7d9 |
| Branch | task/issue-72-b84a9d9e39e64d7c9a263dfb5e3da7d9-github-actions |
| Erstellt | 2026-07-18 |
| Thema | Automatisierte Tests, Build und Release-Artefakt per GitHub Actions |

## Ziel

Für das Repository sollen GitHub-Actions eingerichtet werden, die Pull Requests automatisch testen und nach dem erfolgreichen Merge in den `main`-Branch eine kompilierte Anwendung als `release.zip` bereitstellen. Die Release-Versionierung beginnt bei `1.0.0` und wird anhand der Commit-Typen hochgezählt.

## Funktionale Anforderungen

### Pull-Request-Testlauf

- Beim Erstellen eines Pull Requests muss automatisch ein Testlauf gestartet werden.
- Wenn neue Commits in einen bestehenden Pull Request gepusht werden, muss der Testlauf erneut ausgeführt werden.
- Der Testlauf muss auf Pull Requests gegen den Zielbranch `main` reagieren.
- Der Testlauf muss den im Repository vorgesehenen Testbefehl ausführen.
- Der Workflow muss fehlschlagen, wenn die Tests fehlschlagen.

### Build nach Merge in main

- Wenn ein Pull Request abgeschlossen und in den `main`-Branch gemerged wurde, muss automatisch ein Build der Anwendung gestartet werden.
- Der Build darf nur für tatsächlich gemergte Pull Requests ausgeführt werden, nicht für geschlossene, aber nicht gemergte Pull Requests.
- Der Build muss den im Repository vorgesehenen Kompilier- oder Buildbefehl ausführen.
- Das Ergebnis des Builds muss als ZIP-Datei mit dem Namen `release.zip` abgelegt werden.
- `release.zip` muss die für Auslieferung oder Betrieb relevanten kompilierten Anwendungsdateien enthalten.

### Versionierung

- Die Versionierung muss bei `1.0.0` beginnen, wenn noch keine relevante Version vorhanden ist.
- Die Version muss anhand der Commit-Historie beziehungsweise Commit-Typen hochgezählt werden.
- Commits vom Typ `feat` müssen eine Minor-Versionserhöhung auslösen.
- Commits vom Typ `fix` müssen eine Patch-Versionserhöhung auslösen.
- Breaking Changes müssen eine Major-Versionserhöhung auslösen, sofern sie durch das verwendete Commit-Format erkennbar sind.
- Sonstige Commit-Typen wie `chore`, `docs`, `test`, `refactor` oder vergleichbare Typen dürfen keine höhere Versionserhöhung als die durch `feat`, `fix` oder Breaking Changes erforderliche Erhöhung auslösen.
- Die ermittelte Version muss eindeutig dem erzeugten Release-Artefakt zuordenbar sein.

## Akzeptanzkriterien

- Ein Pull Request gegen `main` startet beim Öffnen automatisch den Testworkflow.
- Ein Push weiterer Commits in denselben Pull Request startet den Testworkflow erneut.
- Ein fehlgeschlagener Test führt zu einem fehlgeschlagenen GitHub-Actions-Lauf.
- Das Schließen eines Pull Requests ohne Merge löst keinen Release-Build aus.
- Ein Merge eines Pull Requests in `main` löst den Buildworkflow aus.
- Nach erfolgreichem Build existiert ein Artefakt mit dem Namen `release.zip`.
- Die erste erzeugte Version ist `1.0.0`, sofern noch keine vorherige Version existiert.
- Bei nachfolgenden Releases wird die Version abhängig von den relevanten Commit-Typen semantisch erhöht.

## Nicht-funktionale Anforderungen

- Die Workflows müssen im Repository unter `.github/workflows/` abgelegt werden.
- Die Workflows müssen ohne lokale manuelle Schritte in GitHub Actions ausführbar sein.
- Secrets oder Tokens dürfen nur verwendet werden, wenn sie für die Umsetzung erforderlich sind.
- Die Lösung soll die vorhandenen Projektbefehle und Paketmanager-Konventionen des Repositorys verwenden.
- Die Workflows sollen deterministisch und wartbar aufgebaut sein.

## Annahmen

- Der Hauptbranch des Repositorys heißt `main`.
- Die Commit-Typen folgen mindestens sinngemäß dem Conventional-Commits-Format.
- Die konkrete Technologie, der Testbefehl, der Buildbefehl und das Build-Ausgabeverzeichnis werden aus dem Repository ermittelt.
- `release.zip` kann als GitHub-Actions-Artefakt oder als Bestandteil eines GitHub Releases abgelegt werden, sofern keine bestehende Projektkonvention etwas anderes vorgibt.

## Offene Punkte

- Soll `release.zip` nur als GitHub-Actions-Artefakt gespeichert werden oder zusätzlich in einem GitHub Release veröffentlicht werden?
- Soll beim Release ein Git-Tag erzeugt werden?
- Gibt es bereits eine bevorzugte Versionierungsdatei oder Release-Konvention im Repository?
