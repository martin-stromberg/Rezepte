# Bestandsaufnahme: Automatisierung wiederkehrender Architektur-, Sicherheits- und Compliance-Reviews (Issue #178)

Analysiert wurden die GitHub-Actions-Infrastruktur (`.github/workflows/`,
`.github/actions/`), die Dokumentations-Oberfläche (`Docs/help/github-actions.md`,
`Docs/features.md`, `Docs/dependencies.md` u. a.) sowie der projektweite
Test-Ausgangszustand — bezogen auf die Anforderung „drei zeitgesteuerte Workflows,
die dedupliziert Review-Issues mit `@softwarewerkstatt`-Erwähnung erzeugen" aus
`requirement.md`.

## Zusammenfassung

- **Cron-Präzedenzfall existiert genau einmal:** `security-scan.yml`
  (`schedule: '0 4 * * 1'` + `workflow_dispatch`) ist der einzige zeitgesteuerte
  Workflow und zugleich das einzige File **ohne** `permissions`-Block — für die
  neuen Workflows ist `issues: write` nötig (Vorbild: `staging-to-main-promotion.yml`
  und `sync-staging-with-main.yml` deklarieren bereits `issues: write` für
  `gh label create`).
- **Dedup-Muster existiert 1:1 — aber nur für PRs:** `gh pr list --state open
  --json number | ConvertFrom-Json` → bei `Count -eq 0` erstellen, sonst
  `Write-Host` ohne Fehler (`staging-to-main-promotion.yml` Z. 63–80,
  `sync-staging-with-main.yml` Z. 55–71). `gh issue` wird im gesamten Repo
  **nirgends** verwendet (Volltextsuche: 0 Treffer); Issue-Erzeugung ist neu.
- **Label-Idempotenz vorgebildet:** `gh label create ... --force` für
  `automated-promotion`/`automated-backmerge`; im Live-Repo existieren nur die
  drei `automated-*`-Labels — kein Review-/Typ-Label bisher.
- **Composite-Action-Muster vorhanden:** `.github/actions/security-scan` und
  `build-and-package` zeigen die Repo-Konvention „Logik nur einmal, nicht in
  Workflows dupliziert" — Grundlage für die optionale gemeinsame Action der
  Anforderung.
- **Konventionen verifiziert:** `ubuntu-latest`, `timeout-minutes` pro Job,
  `concurrency`-Gruppe (außer `verify-pr-source.yml`), `shell: pwsh` explizit
  pro Schritt, `GH_TOKEN: ${{ github.token }}` für `gh`, Actions-Majors
  `checkout@v7`/`setup-dotnet@v6`/`setup-node@v7`/`upload-artifact@v7`,
  kebab-case-Dateinamen, englische Kommentare, LF-Enden (`.gitattributes`).
  `gh pr list` läuft bereits **ohne Checkout** (`-R`-Flag) in
  `pr-staging-ci.yml` Z. 340–351 — der Ansatz „kein Checkout für reine
  GitHub-API-Workflows" ist repo-erprobt.
- **Auth-Kontext:** `gh` ist lokal als `softwarewerkstatt` authentifiziert —
  der Account existiert und ist aktiv; die Wirkung seiner Erwähnung bleibt
  extern unverifiziert (Offene Frage 4 der Anforderung).
- **Doku-Lücken bestätigt:** `Docs/help/github-actions.md` hat keinen Abschnitt
  für zeitgesteuerte Workflows (der bestehende `security-scan.yml` ist dort
  ebenfalls nicht dokumentiert — bestehende Lücke); `Docs/features.md` CI/CD
  (Z. 57–63) und `Docs/dependencies.md` kennen nur das blockierende Gate.
- **Keine .NET-Artefakte betroffen:** Keine Modelle, Services, Enums oder
  Interfaces — entsprechend gibt es nur die Detaildokumente Workflows,
  Dokumentation und Tests.
- **Test-Ausgangszustand:** 809/809 Tests grün — `Rezepte.Tests` deterministisch
  759, `Rezepte.Tests` `OsInterface` 15, `Rezepte.Tests.Browser` 35;
  0 Fehlschläge, 0 Übersprünge. Kein nachgewiesener Preexisting-Fehler.
  Nachweis in [tests.md](inventory/tests.md).

## Details

- [Bestand Workflows, Composite Actions und `gh`-Konventionen](inventory/workflows.md)
- [Dokumentations-Oberfläche und Issue-Quelltexte](inventory/dokumentation.md)
- [Test-Ausgangszustand und bestehende Tests](inventory/tests.md) — Nachweisdateien unter [test-results/](inventory/test-results/)
