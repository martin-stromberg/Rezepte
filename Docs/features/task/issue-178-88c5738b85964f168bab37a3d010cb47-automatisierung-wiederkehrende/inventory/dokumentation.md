# Dokumentations-Oberfläche und Issue-Quelltexte — Bestand

Bestand der vom Akzeptanzkriterium „vollständig innerhalb des Repositories
dokumentiert" betroffenen Dateien sowie der Quelltexte für die Issue-Bodies.
Zeilenangaben beziehen sich auf Commit `94957e9`.

## `Docs/help/github-actions.md` (108 Zeilen, deutsch)

Zentrale Workflow-Dokumentation. Abschnitte: „Branch-Modell" (Z. 5–11),
„Pull-Request-Prüfung auf staging" (Z. 13–49), „Staging-Branch-CI" (Z. 51–55),
„Quellenprüfung für main" (Z. 57–59), „Synchronisation staging mit main"
(Z. 61–63), „Release-Build" (Z. 65–80), „Versionierung" (Z. 82–92),
„GitHub Pages" (Z. 94–108).

Befunde:

- Der Staging-Flow wird beschrieben: Feature-Branch → PR auf `staging` →
  Draft-PR `staging → main` → manueller Merge → Sync-PR `main → staging`
  (Z. 5–11). Für `schedule`-Workflows relevant: Cron-Trigger laufen nur auf dem
  Default-Branch (`main`), d. h. neue Workflows werden erst nach der Promotion
  wirksam — dieser Zusammenhang ist im Dokument bisher **nicht** erwähnt.
- **Bestehende Dokumentationslücke:** Der eigenständige wöchentliche Workflow
  `security-scan.yml` ist in `github-actions.md` nicht dokumentiert (kein Treffer
  für „schedule"/„Security Scan"/„cron" im File; der *blockierende* Scan als
  Gate im `static checks`-Job dagegen schon, Z. 40–43).
- Sprachlich deutsch, Dateipfade und Befehle in Backticks; Trigger-Verhalten
  teils als Tabelle (Z. 98–102).

## `Docs/features.md` — Abschnitt „CI/CD" (Z. 57–63)

- Aktuell drei Bullets: PR-Prüfungen/Promotion-/Sync-PRs/Release-Artefakte;
  Lane-Trennung der Tests; Nutzungs-/KI-Limits — plus Verweis auf
  `help/github-actions.md` und `help/os-interface-tests.md`.
- Kein Eintrag zu zeitgesteuerten Workflows oder automatischen Issues.

## `Docs/dependencies.md` (34 Zeilen)

- Dokumentiert die Dependency-/Sicherheitsstrategie: behobene transitive
  Befunde, Paketstrategie (u. a. „Der Security-Scan bleibt ein blockierender
  CI-Schritt", Z. 27), Risikoakzeptanz.
- Der wöchentliche `security-scan.yml`-Lauf ist auch hier nicht erwähnt; die
  Datei ist daher der naheliegende Ort für den Verweis auf die zusätzliche
  wöchentliche NuGet-Paketprüfung (Issue #178, Typ 2) — aktuell nichts vorhanden.

## `Docs/testing.md` — Abschnitt „NuGet-Sicherheitscheck" (Z. 35–43)

- Lokaler Befehl `dotnet list Rezepte.sln package --vulnerable --include-transitive`,
  Verweis auf blockierendes CI-Gate, `dependencies.md` und `github-actions.md`.
- Kein Bezug zu geplanten Review-Issues.

## Indizes

- `Docs/help/index.md` Z. 13: Listeneintrag „GitHub Actions — Automatisierung
  über GitHub Actions" → `github-actions.md`.
- `Docs/index.md`: verlinkt `help/index.md` („Funktionsbereiche — Übersicht")
  und `github-pages.md`; kein direkter Eintrag für Actions-Details.

## `issue.md` (Repo-Root) — Quelltexte der drei Issue-Bodies

Originaltext des GitHub-Issues #178 (deutsch). Die drei Prüfaufträge, die als
Issue-Body zu übernehmen sind (jeweils beginnend mit `@softwarewerkstatt`):

1. **Monatliches Architektur- und Sicherheitsreview** (Z. 29–45): 9 Prüfpunkte
   (Framework-/Bibliotheks-Risiken, CVEs in NuGet, ASP.NET-Core-/Blazor-/.NET-
   Empfehlungen, AuthN/AuthZ-Änderungen, Design Patterns, veraltete APIs,
   Wartbarkeit/Erweiterbarkeit/Testbarkeit, Logging/Telemetrie/Monitoring,
   Community-/MS-Doku-Erkenntnisse) + „priorisierte Liste konkreter
   Handlungsempfehlungen".
2. **Wöchentliche NuGet-Prüfung** (Z. 54–71): 6 Prüfpunkte (Updates,
   Security-Updates, bekannt gewordene Schwachstellen, ungepflegte Pakete,
   Nachfolgepakete, Breaking Changes) + Update-Empfehlungen mit Risikobewertung;
   unbedenkliche Updates (niedriges–mittleres Risiko) direkt als PR umsetzen,
   riskante/aufgeschobene im PR-Body bzw. Folge-Issue dokumentieren.
3. **Jährliches Compliance- und Copyright-Review** (Z. 80–96, jeweils 1. Januar):
   9 Prüfpunkte (Copyright in Quellcode/Webseiten/About-Dialogen, Impressum,
   Datenschutzerklärung, Drittanbieter-Lizenzen, OSS-Verpflichtungen,
   Unternehmens-/Kontaktinfo, App-Store-Metadaten) + Änderungsvorschläge und
   Risikoliste.

Akzeptanzkriterien im Original (Z. 98–105): separate Action je Prüfung,
Cron-Trigger, aussagekräftige Titel, `@softwarewerkstatt`-Erwähnung,
Duplikatprüfung offener Issues desselben Typs, vollständige Repo-Dokumentation.

## Verwandte Repo-Artefakte

- `.github/ISSUE_TEMPLATE/` (`bug_report.md`, `custom.md`, `feature_request.md`)
  — Standard-GitHub-Templates ohne Labels/Assignees; für die automatisierten
  Issues nicht vorgesehen, aber vorhandener Issue-Kontext.
- `.github/copilot-instructions.md` — allgemeine Coding-Richtlinien; berührt
  YAML-Workflows nicht direkt (englisch für Code/Kommentare, Z. 7).
- `.editorconfig` (Z. 7: `end_of_line = lf`) und `.gitattributes`
  (`* text=auto eol=lf`): neue Workflow-Dateien müssen **LF**-Zeilenenden haben.
- `.githooks/check-encoding.ps1` (Z. 12): Extension-Liste ohne `.yml`/`.yaml` —
  kein Encoding-Check für Workflow-Dateien; ebenso existiert **kein**
  yamllint/actionlint o. ä. im Repo (verifiziert per Volltextsuche; einzige
  YAML-Prüfung fände durch GitHub selbst statt).
- `ci-target-schema.md`: wird in Workflow-Kommentaren wiederholt referenziert,
  liegt aber **nicht** in diesem Repository (Vorlagen-Repo der Softwareschmiede);
  als Quellenverweis nicht lokal prüfbar.
