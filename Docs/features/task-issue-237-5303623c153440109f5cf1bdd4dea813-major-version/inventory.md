# Bestandsaufnahme: Major-Versionssprung (.NET-11-Migration) + .NET-10-Maintenance-Branch + Plugin-Repo-Spiegelung

Bestandsaufnahme der Release-/Versionsinfrastruktur (Git-Branching, semantic-release, GitHub-Actions, Plugin-Vertrag) bezogen auf GitHub Issue #237 — kein Anwendungscode betroffen; analysiert wurden Workflows, Release-Skripte, Merge-Historie, Vertragsmechanik und das externe Plugin-Repository.

## Zusammenfassung

- **Merge-Modell anders als angenommen:** Alle Merges sind **echte Merge-Commits** (`allow_squash_merge: false`; `merge_commit_title/message = PR_TITLE/PR_BODY`). Einzelne Commits samt Footer überleben bis `main` → ein `BREAKING CHANGE:`-Footer kann in einem beliebigen staging-Commit **oder** im Promotion-Merge-Commit-Body (= Promotion-PR-Body) stehen. Aktuell existiert kein einziger `BREAKING CHANGE:`/`type!:`-Commit → die nächste Promotion würde **v1.23.0** (minor) erzeugen, nicht v2.0.0.
- **Letzter .NET-10-Stand von `main`:** `bf108b702907d2ee715bdc0341cedece5d55bf3a` (Promotion #231; `v1.22.0` ist erreichbarer In-Range-Tag → semantic-release-Range-Voraussetzung für `1.x` erfüllt).
- **Maintenance-Branch-Releasung erfordert drei konsistente Änderungen auf dem `1.x`-Branch selbst:** `release.yml` `on.push.branches` (Z. 12), `release.config.js` `branches` (Z. 74; `"1.x"` → impliziter Range, freier Name → `{name, range:"1.x"}` nötig), `resolve-release-version.mjs` `AUTOMATIC_RELEASE_BRANCHES` (Z. 23 — sonst `Unsupported release ref 'branch:1.x'`). Staging-RC-Ermittlung bleibt unberührt (`--branches staging`-Override ersetzt die Config).
- **Nebenbedingungen dokumentiert:** PRs gegen `1.x` hätten aktuell **keine PR-CI** (`verify-pr-source`/`pr-staging-ci` sind main-/staging-fixiert); Asset-Reparatur (`incompleteReleases`) und Tag-Namespace `v*.*.*` wirken linienübergreifend; Maintenance-Releases erzeugen `update.json` und ggf. „Latest"-Markierung — Seiteneffekt auf App-Updater offen.
- **Plugin-Repo `martin-stromberg/Rezepte.Plugins` nutzt keine semantic-release-Machinerie:** eigener `release.yml` auf `pull_request: closed` (ohne Branch-Filter) + `Get-NextReleaseVersion.ps1` (bump ab global höchstem Tag, keine Range-Logik). Stand: `net10.0`, `ImportContractVersion 0.3.0`, latest `v0.3.7`; Maintenance-Branch dort hat kein Muster — Custom-Versionierung würde bei Nicht-main-Merges falsche Versionen erzeugen.
- **`ImportContractVersion` ≠ Ladekompatibilität:** versioniert nur den Contract-Export/ApiCompat-Baselines; Plugin-Loading (`PluginLoadContext`) prüft keine Vertragsversion. `Docs/RELEASE_NOTES.md` behauptet auf staging bereits, .NET-10-Plugins blieben kompatibel — **Widerspruch zur Issue-Prämisse**, für Planung/Release-Notes relevant.

**Test-Ausgangszustand:** deterministische Lane grün — `Rezepte.Tests` 763/763 bestanden (Filter `Category!=OsInterface`), Builds warnfrei. OsInterface-Subset und `Rezepte.Tests.Browser` nicht ausgeführt (CI-Best-Effort-Lane, Playwright nötig). Details und Nachweis: [inventory/tests.md](inventory/tests.md).

## Details

- [Release-/CI-Workflows (Trigger, Jobs, Gates)](inventory/release-workflows.md)
- [Versionsermittlung: release.config.js, resolve-release-version.mjs, semantic-release-Maintenance-Semantik](inventory/version-resolution.md)
- [Merge-Modell und Commit-Message-Überleben (Korrektur der Squash-Annahme)](inventory/merge-historie.md)
- [Voraussetzungen auf dem `1.x`-Maintenance-Branch](inventory/maintenance-branch.md)
- [Plugin-Repository `martin-stromberg/Rezepte.Plugins`](inventory/plugin-repo.md)
- [Import-Contract, contract-baselines/, Plugin-Ladepfad](inventory/contract.md)
- [Vorhandene Dokumentation zu Branch-/Release-Konventionen](inventory/docs.md)
- [Tests: Ausgangszustand und Abdeckung](inventory/tests.md)
