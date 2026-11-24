# Bestandsaufnahme: Vorhandene Dokumentation zu Branch-/Release-Konventionen

## `Docs/help/github-actions.md` (128 Zeilen) — zentrale Referenz

- „Branch-Modell" (Z. 5-11): Feature → `staging` → auto-Draft-PR → `main`; nur `staging` nach `main`; Sync-PR `main -> staging` nach jedem main-Push; `main` = stabile Releases. **Keine Erwähnung von Maintenance-Branches** — muss ergänzt werden (Anforderung Punkt 6).
- Workflow-Beschreibungen: PR-Prüfung (13-49), Staging-CI (51-55), `verify-pr-source` (57-59), Sync (61-63), Review-Workflows (65-83), Release-Build (85-100), **Versionierung** (102-112: `vMAJOR.MINOR.PATCH`; `feat:` minor, `fix:` patch, `type!:`/`BREAKING CHANGE:` major — dokumentiert den Major-Mechanismus bereits), Pages (114-128).
- Z. 89/98: beschreibt Publish für `net11.0` und die .NET-11-Testrunner-Kompatibilität — auf einem `1.x`-Stand wären diese Abschnitte in der dortigen Datei-Kopie veraltet (dort `net10.0`), was aber nicht zwingend korrigiert werden muss.

## `Docs/help/import-plugins.md` (215 Zeilen)

- Contract-Export-Verfahren (116-152), Breaking-Change-Workflow für die **Contract-API** (153-190, siehe contract.md), Plugin-Betrieb/Troubleshooting.
- Relevanter Ort für den Nutzerhinweis an externe Plugin-Betreiber (offene Frage 5) — bislang ohne Hinweis auf Framework-Linien (.NET 10 vs. .NET 11).

## `Docs/RELEASE_NOTES.md`

- Enthält auf `staging` bereits .NET-11-Notes — **mit der Aussage, .NET-10-Plugins blieben kompatibel** („Import plugins built for .NET 10 remain compatible…", Zeilen 6-7/17-18). Steht im Widerspruch zur Issue-#237-Prämisse und müsste für den Breaking-Change-Narrativ korrigiert werden (siehe merge-historie.md).

## Weitere Fundstellen

- `Docs/development-guide.md`, `Docs/dependencies.md`, `README.md`: keine Branch-/Release-Konventionen (README nur CI-Badge auf `pr-staging-ci.yml`).
- `Docs/testing.md`: Test-Kommandos und Lane-Modell (`Category!=OsInterface` blockierend vs. Browser-/OsInterface-Lane best-effort) — Quelle für den Test-Baseline-Befehl in `tests.md`.
- `Docs/help/application-updates.md`, `updater-requirements.md`, `updater-troubleshooting.md`: Update-Mechanik (msTools.Updater, `update.json`-Manifest).
- `Docs/features/`: etablierte Ablage `task-issue-<N>-<id>-<slug>/` mit requirement/inventory-Struktur (u. a. `task-issue-233-...-net11` für die .NET-11-Migration).
- `Docs/help/os-interface-tests.md`: Lane-Konzept, Flaky-Retry, `gh run rerun`-Regel.
- Kommentare in `release.config.js`, `release.yml`, `staging-ci.yml`, `resolve-release-version.mjs` verweisen mehrfach auf `ci-target-schema.md` (Abschnitte 3, 4.2/4.3, 4.5, 4.6, 4.8, 4.10, 6.1, 6.4, 7, 10, 11) — die Datei ist **in keinem abrufbaren Repo** vorhanden (weder hier noch in `martin-stromberg/Softwareschmiede`); die Beschreibung muss als externe Referenz gelten.
