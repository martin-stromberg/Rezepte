# Umsetzungsplan: Automatisierung wiederkehrender Architektur-, Sicherheits- und Compliance-Reviews (Issue #178)

## Übersicht

Drei neue, zeitgesteuerte GitHub-Actions-Workflows unter `.github/workflows/` erzeugen zu definierten Intervallen (monatlich am 1., wöchentlich, jährlich am 1. Januar, jeweils UTC) Review-Issues mit `@softwarewerkstatt`-Erwähnung im Body — jeweils nur, wenn kein offenes Issue desselben Review-Typs existiert. Die gemeinsame Logik „Label sicherstellen → offene Issues prüfen → Issue erzeugen" wird einmalig als Composite Action `.github/actions/create-review-issue` implementiert. Betroffen sind ausschließlich die GitHub-Actions-Infrastruktur und die Dokumentationsdateien `Docs/help/github-actions.md`, `Docs/features.md` und `Docs/dependencies.md` — keine .NET-Artefakte.

## Scope-Klärung

| Feld | Festlegung |
|------|------------|
| Verarbeitungseinheit | Ein Review-Zyklus je Prüfungsart: ein geplanter Workflow-Anlauf, der genau ein Issue des Typs „Architektur- und Sicherheitsreview", „NuGet-Paketprüfung" bzw. „Compliance- und Copyright-Review" erzeugt oder wegen eines offenen Vorgänger-Issues verwirft. |
| Auslösende Ereignisse | `on: schedule:`-Cron-Trigger je Workflow (UTC): `'0 6 1 * *'` (monatlich, 1. Tag, 06:00), `'0 5 * * 2'` (wöchentlich, Dienstag 05:00), `'0 7 1 1 *'` (jährlich, 1. Januar, 07:00) — Stunden gestaffelt, da am 1. Januar Monats- und Jahreslauf zusammenfallen. Zusätzlich `workflow_dispatch` (Repo-Konvention, `security-scan.yml`; ermöglicht Probelauf/Verifikation). Wirksam erst auf dem Default-Branch `main`. |
| Erwartete Wirkung | Pro fälligem Zyklus ohne offenen Vorgänger: ein neues offenes GitHub-Issue mit typspezifischem Titel (inkl. Zyklus-Datum), typspezifischem Label, `@softwarewerkstatt` als erster Body-Zeile und dem jeweiligen Prüfauftrag aus Issue #178 als Body. Bei offenem Vorgänger: protokollierte Nicht-Erstellung, Lauf endet erfolgreich. |
| Umgang mit fehlenden Objekten | Offenes Issue mit demselben Typ-Label: verwerfen (kein Issue, Erfolgsmeldung im Log). Typ-Label nicht vorhanden: erzeugen (idempotent via `gh label create ... --force`, Muster `automated-promotion`/`automated-backmerge`). Vorgänger-Issue geschlossen: neues Issue wird regulär angelegt. Repository-/Token-Kontext: `GITHUB_TOKEN` mit `issues: write`; kein zusätzliches Secret. |

### Scope-Ausschlüsse

Keine. Die ursprünglich hier gelisteten vier Punkte wurden bei der Orchestrierungsprüfung (Schritt 5a) als **Anforderungsbestandteile, nicht als Ausschlüsse** eingeordnet — sie schränken keine naheliegende Nutzererwartung ein, sondern wiederholen, was Issue #178 ausdrücklich festlegt:

1. Die Workflows führen die Reviews nicht selbst durch — Issue #178 definiert sie ausschließlich als Issue-Erzeuger; die Analyse ist explizit Aufgabe der durch die `@softwarewerkstatt`-Erwähnung angestoßenen Automatik (steht so im vorgegebenen Issue-Body).
2. Kein automatisches Schließen/Eskalieren überfälliger Review-Issues — Issue #178 verlangt nur die Duplikatvermeidung; ein offener Vorgänger unterdrückt bewusst Folge-Issues.
3. Keine Paketaktualisierung/`dotnet list package` im NuGet-Workflow — Issue #178 legt den Issue-Body fest, der die Paketanalyse dem `@softwarewerkstatt`-Auftrag zuweist.
4. Keine zusätzlichen Benachrichtigungskanäle — Issue #178 legt die `@softwarewerkstatt`-Erwähnung im Issue-Body als einzigen Auslöser fest.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Gemeinsame Logik | Eine Composite Action `.github/actions/create-review-issue/action.yml` für die Sequenz „Label sicherstellen → offene Issues prüfen → Issue erzeugen"; die drei Workflows parametrisieren sie über Inputs (Label-Name/-Farbe/-Beschreibung, Issue-Titel, Datumsformat für den Titel-Suffix, Issue-Body). | Repo-Konvention: Gemeinsame Logik existiert nur einmal (`security-scan`-, `build-and-package`-Action mit explizitem Begründungskommentar). Drei identische Step-Sequenzen wären Wartungsduplikation. |
| Checkout-Bedarf | Jeder Workflow enthält einen `actions/checkout@v7`-Schritt vor dem Action-Aufruf. | Lokale Composite Actions (`uses: ./.github/actions/...`) erfordern zwingend den ausgecheckten Workflow-Stand auf dem Runner. Der Ansatz „kein Checkout" ist daher mit der Composite-Action-Entscheidung nicht kombinierbar; `contents: read` wird in `permissions` ergänzt. |
| Duplikaterkennung | Typspezifisches Label als Dedup-Schlüssel: `review-architecture-security`, `review-nuget-check`, `review-compliance`. Prüfung via `gh issue list --state open --label <label> --json number`; Erstellung nur bei `Count -eq 0`. | Übernimmt 1:1 das bewährte PR-Dedup-Muster (`gh pr list ... --json number` → `ConvertFrom-Json` → Count-Check). Label- statt Titelsuche, weil Titel das Zyklus-Datum enthalten und damit pro Lauf variieren. Prefix `review-` statt `automated-*`: Der Label bezeichnet den Review-Typ (Dedup-Schlüssel); die automatische Erzeugung ergibt sich aus dem Workflow-Kontext. |
| Wochentag des wöchentlichen Laufs | Dienstag 05:00 UTC (`'0 5 * * 2'`). | Annahme laut Anforderung dokumentiert: gestaffelt zum bestehenden `security-scan.yml` (Montag 04:00 UTC), damit die beiden wöchentlichen Läufe nicht zusammenfallen. |
| Issue-Titel | `[Review] <Bezeichnung> – <Zyklus>`: „[Review] Architektur- und Sicherheitsreview – yyyy-MM" (monatlich), „[Review] NuGet-Paketprüfung – yyyy-MM-dd" (wöchentlich, Laufdatum), „[Review] Compliance- und Copyright-Review – yyyy" (jährlich). Der Datums-Suffix wird von der Action per `Get-Date -Format` (UTC) aus einem Format-Input gebildet. | Deutsch wie die Issue-Texte aus #178; der Zyklus im Titel macht Zuordnung und Duplikatlogik nachvollziehbar (Vorschlag der Anforderung). |
| Issue-Body-Übergabe | Der mehrzeilige Body wird als Action-Input `issue-body` deklariert und im Step über eine Umgebungsvariable (`env: ISSUE_BODY: ${{ inputs.issue-body }}`) an `gh issue create --body $env:ISSUE_BODY` weitergereicht — nicht per String-Interpolation ins Skript. | Verhindert Quoting-/Escaping-Probleme mit den mehrzeiligen deutschen Markdown-Bodies (Backticks, Gedankenstriche, `@`-Erwähnung). |
| Erwähnung vs. Zuweisung | Nur die `@softwarewerkstatt`-Erwähnung im Body, kein `--assignee`. | Die Anforderung fordert ausdrücklich die Erwähnung als Auslöser; eine Zuweisung setzt Collaborator-Status des Accounts voraus und ist nicht verlangt. |
| Trigger-Ergänzung | `workflow_dispatch` ohne Inputs in allen drei Workflows. | Repo-Konvention (`security-scan.yml`, `pages.yml`); einziger praktikabler Verifikationsweg vor dem ersten echten Zyklus (Cron wirkt erst nach Promotion auf `main`). |
| `concurrency` / Timeout | Statische `concurrency.group` je Workflow (`review-architecture-security`, `review-nuget-check`, `review-compliance`) mit `cancel-in-progress: true`; `timeout-minutes: 5`. | Entspricht `security-scan.yml` (einziges Schedule-Präzedenz) bzw. der unteren Grenze der üblichen Job-Timeouts (5–45). |

## Programmabläufe

### Geplanter Review-Lauf (identisch in allen drei Workflows)

1. Der `schedule`-Cron-Trigger (oder `workflow_dispatch`) startet den einzigen Job `create-review-issue` auf `ubuntu-latest`.
2. Step `Checkout` (`actions/checkout@v7`) stellt die Composite Action bereit — ohne weitere Repo-Nutzung.
3. Step `Create review issue` ruft `uses: ./.github/actions/create-review-issue` mit den typspezifischen Inputs und `env: GH_TOKEN: ${{ github.token }}` auf.
4. Innerhalb der Action, Step 1 (`pwsh`): `gh label create <label-name> --color <color> --description <desc> --force -R ${{ github.repository }}` stellt das Typ-Label idempotent sicher (Muster: `automated-promotion` in `staging-to-main-promotion.yml` Z. 56–60).
5. Step 2 (`pwsh`): `gh issue list -R ${{ github.repository }} --state open --label <label-name> --json number | ConvertFrom-Json` liefert offene Issues des Typs.
   - Bei `@($existing).Count -eq 0`: Titel-Suffix via `Get-Date` (UTC, Format-Input) bilden, `gh issue create -R ${{ github.repository }} --title "<Titel>" --body $env:ISSUE_BODY --label "<label-name>"` erzeugt das Issue; Log-Ausgabe mit Issue-URL/-Nummer.
   - Bei `Count -gt 0`: `Write-Host`-Meldung mit der Nummer des offenen Vorgänger-Issues; der Step endet erfolgreich ohne Erstellung (exakt das bewährte Muster aus `staging-to-main-promotion.yml` Z. 68–79).
6. Sichtbare Wirkung: neues offenes Issue mit `@softwarewerkstatt`-Erwähnung als erster Body-Zeile — oder protokollierte Nicht-Erstellung.

Beteiligte Komponenten: `.github/workflows/review-architecture-security.yml`, `.github/workflows/review-nuget-packages.yml`, `.github/workflows/review-compliance.yml`, `.github/actions/create-review-issue/action.yml`, `gh`-CLI mit `GH_TOKEN`.

## Neue Klassen

Keine .NET-Klassen betroffen — die Änderung liegt ausschließlich in der CI-Infrastruktur. Neue Dateien:

| Datei | Typ | Zweck |
|-------|-----|-------|
| `.github/actions/create-review-issue/action.yml` | Composite Action | Parametrisierte Sequenz „Label sicherstellen → Dedup-Prüfung → Issue erzeugen". Inputs: `label-name`, `label-color`, `label-description`, `issue-title` (Präfix), `issue-title-date-format` (pwsh-`Get-Date`-Format für den Zyklus-Suffix), `issue-body` (mehrzeilig). Zwei `pwsh`-Steps analog den bestehenden Actions; Header-Kommentar mit Begründung (Konvention). |
| `.github/workflows/review-architecture-security.yml` | Workflow | `schedule: '0 6 1 * *'` + `workflow_dispatch`; Job: Checkout → `create-review-issue`-Aufruf mit Label `review-architecture-security`, Titel `[Review] Architektur- und Sicherheitsreview`, Format `yyyy-MM`, Body = Prüfauftrag 1 aus `issue.md` Z. 29–45. |
| `.github/workflows/review-nuget-packages.yml` | Workflow | `schedule: '0 5 * * 2'` + `workflow_dispatch`; Label `review-nuget-check`, Titel `[Review] NuGet-Paketprüfung`, Format `yyyy-MM-dd`, Body = Prüfauftrag 2 aus `issue.md` Z. 54–71. |
| `.github/workflows/review-compliance.yml` | Workflow | `schedule: '0 7 1 1 *'` + `workflow_dispatch`; Label `review-compliance`, Titel `[Review] Compliance- und Copyright-Review`, Format `yyyy`, Body = Prüfauftrag 3 aus `issue.md` Z. 80–96. |

Gemeinsames Workflow-Skelett (Konventionen): `name:` (englisch), englischer Header-Kommentarblock mit Zweck und Cron-Begründung, `permissions: contents: read` + `issues: write`, statische `concurrency.group` + `cancel-in-progress: true`, `runs-on: ubuntu-latest`, `timeout-minutes: 5`, Steps `shell`-frei bzw. in der Action `shell: pwsh`, `GH_TOKEN: ${{ github.token }}`, LF-Zeilenenden (`.gitattributes`).

## Änderungen an bestehenden Klassen

Keine .NET-Klassen betroffen. Angepasste Dokumentationsdateien:

### `Docs/help/github-actions.md`

- Neuer Abschnitt „Zeitgesteuerte Review-Workflows" (z. B. nach „Synchronisation staging mit main"): Tabelle der drei Workflows mit Datei, Cron-Zeitplan (UTC), Typ-Label und Zweck; Erläuterung der Dedup-Logik (offenes Issue mit Typ-Label verhindert Neuanlage), der Label-Idempotenz (`--force`), des Auslösemechanismus (`@softwarewerkstatt`-Erwähnung als erster Body-Zeile stößt die externe Bearbeitungsautomatik an) und des Composite-Action-Bausteins `create-review-issue`.
- Hinweis im selben Abschnitt: `schedule`- und `workflow_dispatch`-Trigger wirken nur auf dem Default-Branch `main` — die Workflows werden erst nach der Promotion `staging → main` aktiv; Verifikation erfolgt danach per `workflow_dispatch`.
- Im selben Abschnitt eine kurze Zeile zum bestehenden `security-scan.yml` (Montag 04:00 UTC), der als einziger bisheriger Schedule-Workflow dort bislang fehlt — schließt die in der Bestandsaufnahme festgestellte Dokumentationslücke am passenden Ort.

### `Docs/features.md` (Abschnitt „CI/CD", Z. 57–63)

- Ein zusätzliches Bullet: zeitgesteuerte Review-Workflows, die monatliche/wöchentliche/jährliche Review-Issues mit `@softwarewerkstatt`-Erwähnung und Duplikatvermeidung erzeugen.

### `Docs/dependencies.md` (Abschnitt „Paketstrategie", Z. 23–28)

- Ein ergänzender Satz: Neben dem blockierenden Security-Gate und dem wöchentlichen `security-scan.yml`-Lauf erzeugt `review-nuget-packages.yml` wöchentlich ein NuGet-Prüf-Issue (Updates, Schwachstellen, Pflegezustand, Breaking Changes) mit `@softwarewerkstatt`-Auftrag.

## Datenbankmigrationen

Keine.

## Validierungsregeln

Keine — es gibt keine neuen Anwendungseingaben. Die Workflow-Parameter sind fest verdrahtete Konstanten; die Dedup-Regel („nur anlegen, wenn kein offenes Issue mit dem Typ-Label existiert") ist selbst die einzige fachliche Bedingung und oben beschrieben.

## Konfigurationsänderungen

Keine. Intervalle, Titel, Bodies und Label-Namen werden als Konstanten in den Workflow-Dateien gepflegt; es werden keine Secrets oder `appsettings`-Einträge benötigt (`GITHUB_TOKEN` mit `issues: write` genügt — `issues: write` deckt `gh issue create`/`gh issue list`/`gh label create` ab, wie `staging-to-main-promotion.yml`/`sync-staging-with-main.yml` bereits für `gh label create` belegen).

## Seiteneffekte und Risiken

- **Aktivierung erst nach Promotion:** `schedule`- und `workflow_dispatch`-Trigger laufen nur auf dem Default-Branch. Auf `staging` bzw. dem Task-Branch sind die Workflows inert; ein falscher „Funktioniert nicht"-Befund vor dem Merge ist ausgeschlossen, wenn dies dokumentiert ist (erfolgt in `github-actions.md`).
- **Wirksamkeit der `@softwarewerkstatt`-Erwähnung:** Ob die Erwähnung die externe Analyse-Automatik tatsächlich anstößt, liegt außerhalb des Repositories und ist nicht repo-seitig verifizierbar (Bestandsaufnahme: `gh`-Account `softwarewerkstatt` existiert und ist aktiv). Risiko: erzeugte Issues blieben unbearbeitet, ohne dass der Workflow fehlschlägt. Gegenprobe: nach dem ersten `workflow_dispatch`-Probelauf bzw. Zyklus die Reaktion des Accounts beobachten.
- **Neue Labels im Live-Repo:** Die drei `review-*`-Labels werden beim ersten Lauf automatisch angelegt — bewusste, idempotente Nebenwirkung nach dem `automated-*`-Vorbild.
- **Doppelter Lauf am 1. Januar:** Monats- und Jahres-Workflow fallen zeitlich zusammen; durch die gestaffelten Stunden (06:00/07:00 UTC) und getrennte `concurrency`-Gruppen unkritisch.
- **Issue-Spam bei fehlerhafter Dedup:** Läge das Label vor, das Issue aber ohne Label (z. B. manuell entfernt), entstünde ein Duplikat — akzeptiert, da das Label ausschließlich durch diese Workflows verwaltet wird.

## Umsetzungsreihenfolge

1. **Composite Action `.github/actions/create-review-issue/action.yml` anlegen**
   - Voraussetzungen: Keine (nur Repo-Konventionen: `pwsh`, `GH_TOKEN`, `gh`-CLI auf `ubuntu-latest` vorinstalliert).
   - Beschreibung: `name`, `description` (englisch, mit Begründung „exists once instead of being duplicated"), die sechs Inputs, `runs.using: composite` mit zwei `pwsh`-Steps (Label sicherstellen; Dedup-Prüfung + Issue-Erzeugung mit `env: ISSUE_BODY`). Logik exakt nach dem Muster `staging-to-main-promotion.yml` Z. 56–79, für `gh issue` statt `gh pr` und mit `-R ${{ github.repository }}`.

2. **Drei Workflow-Dateien anlegen**
   - Voraussetzungen: Schritt 1 (Action existiert, da `uses: ./.github/actions/create-review-issue` sie referenziert).
   - Beschreibung: `review-architecture-security.yml`, `review-nuget-packages.yml`, `review-compliance.yml` nach dem gemeinsamen Skelett; je Workflow Cron-Zeitplan, Typ-Label/-Farbe/-Beschreibung, Titel-Präfix, Datumsformat und der vollständige deutsche Prüfauftrag aus `issue.md` als `issue-body` (erste Zeile `@softwarewerkstatt`). Mojibake-korrigierte Originaltexte übernehmen (die `issue.md` ist UTF-8; die Umlaute sind im Quellissue korrekt deutsch).

3. **Dokumentation ergänzen**
   - Voraussetzungen: Schritte 1–2 (Dokumentation beschreibt den fertigen Stand).
   - Beschreibung: Neuer Abschnitt in `Docs/help/github-actions.md` (Workflows, Zeitpläne, Labels, Dedup, `@softwarewerkstatt`-Mechanismus, Default-Branch-Einschränkung, kurze Erwähnung von `security-scan.yml`); Bullet in `Docs/features.md` CI/CD; Satz in `Docs/dependencies.md` Paketstrategie.

4. **Lokale Verifikation**
   - Voraussetzungen: Schritte 1–3.
   - Beschreibung: YAML-Syntaxprüfung der vier neuen YAML-Dateien (z. B. `python -c "import yaml; ..."` oder gleichwertig — im Repo existiert kein yamllint/actionlint); Review gegen die Konventionsliste (`permissions`, `concurrency`, `timeout-minutes`, LF-Enden, englische Kommentare, gepinnte Action-Versionen); `git`-Diff gegen die Eingaben. Kein Build/Test-Lauf nötig (keine .NET-Änderung); die Baseline 809/809 grün bleibt unberührt.

5. **Funktionale Verifikation nach Merge nach `main`** (durch Anwender/Maintainer, nicht im Task-Branch möglich)
   - Voraussetzungen: Promotion des Task-Branches über `staging` nach `main`.
   - Beschreibung: `workflow_dispatch`-Probelauf je Workflow: beim ersten Lauf wird Label angelegt und Issue erzeugt; beim sofortigen zweiten Lauf wird die Nicht-Erstellung protokolliert (Dedup-Nachweis). Erzeugtes Test-Issue anschließend manuell schließen.

## Tests

### Neue Tests

Keine .NET-Tests — es existiert keine Testoberfläche für GitHub-Actions-Workflows (Bestandsaufnahme: kein yamllint/actionlint im Repo, `check-encoding.ps1` ohne `.yml`-Extension). Der Funktionsnachweis erfolgt stattdessen über:

| Nachweis | Wo | Was wird geprüft |
|----------|-----|-------------------|
| YAML-Syntaxprüfung | lokal vor Commit | Vier neue YAML-Dateien sind parsebar. |
| Konventions-Review | Pull Request | `permissions` (`contents: read` + `issues: write`), `concurrency`, `timeout-minutes`, `workflow_dispatch`, Cron-Zeitpläne, LF-Enden, englische Kommentare, Inputs der Action korrekt verdrahtet. |
| `workflow_dispatch`-Probelauf | nach Merge auf `main` | Happy Path (Label + Issue erzeugt, `@softwarewerkstatt` im Body, Titel mit Zyklus-Datum) und Dedup-Pfad (Zweitlauf ohne neue Issue-Erstellung). |

### Betroffene bestehende Tests

Keine.

### E2E-Tests (primärer Funktionsnachweis)

Nicht erforderlich — die Anforderung berührt keinen über UI oder Nutzeraktion in der Anwendung erreichbaren Ablauf. Die einzige „Benutzerfläche" ist die GitHub-Actions-Plattform selbst; der dortige Auslöser (`schedule`/`workflow_dispatch`) und die sichtbare Wirkung (Issue im GitHub-Repo) sind durch E2E-Tests im `Rezepte.Tests.Browser`-Sinn nicht abdeckbar und werden stattdessen über den dokumentierten `workflow_dispatch`-Probelauf nach Promotion verifiziert (siehe Umsetzungsreihenfolge Schritt 5).

## Akzeptanzszenarien

| # | Szenario | Auslöser | Ausgangszustand | Schritte | Sichtbare Wirkung | Nachweis |
|---|----------|----------|------------------|----------|-------------------|----------|
| 1 | Happy Path: monatliches Review-Issue wird erzeugt | `schedule`-Cron `'0 6 1 * *'` (oder `workflow_dispatch` zum Nachstellen) auf `main` | Workflows auf `main` merged; kein offenes Issue mit Label `review-architecture-security`; Label existiert noch nicht | Job läuft: Checkout → Action: `gh label create ... --force` → `gh issue list` (leer) → `gh issue create` | Neues offenes Issue „[Review] Architektur- und Sicherheitsreview – YYYY-MM" mit Label und `@softwarewerkstatt`-Body; Label neu im Repo | `workflow_dispatch`-Probelauf; Issue + Label auf GitHub sichtbar |
| 2 | Dedup: kein Duplikat bei offenem Vorgänger | Zweiter Lauf desselben Workflows (`workflow_dispatch` oder nächster Zyklus) | Offenes Issue mit dem Typ-Label existiert (aus Szenario 1) | Job läuft: Label-Check (idempotent) → `gh issue list` liefert Treffer → `Write-Host`, kein `gh issue create` | Kein neues Issue; Workflow-Lauf grün; Log nennt die Nummer des offenen Issues | Zweiter Probelauf + Issue-Liste |
| 3 | Neuer Zyklus nach geschlossenem Vorgänger | Cron-Fälligkeit (z. B. Folgemonat) | Vorgänger-Issue wurde geschlossen | Wie Szenario 1 | Neues Issue des Typs mit neuem Zyklus-Datum im Titel | Beobachtung im Folgezyklus |
| 4 | Inaktiv vor Promotion | Task-Branch auf `staging` gemerged, noch nicht auf `main` | Workflow-Dateien nur auf `staging` vorhanden | — | Keine Workflow-Runs (Schedule/Dispatch wirken nur auf Default-Branch); kein Fehlschlag | Dokumentierter Zusammenhang; Abwesenheit von Runs |
| 5 | Jahreswechsel-Doppellauf | 1. Januar: Monats- und Jahres-Cron fällig | Keine offenen Issues beider Typen | Beide Workflows laufen gestaffelt (06:00/07:00 UTC) | Zwei verschiedene Review-Issues, keine Interferenz | Beobachtung/Dokumentation der Konstellation |

## Offene Punkte

Keine.

Alle in `requirement.md` gelisteten offenen Fragen wurden im Plan entschieden: Wochentag (Dienstag 05:00 UTC, Designentscheidung), Duplikatkriterium (typbezogenes Label, Designentscheidung), Erwähnung statt Zuweisung (wie gefordert nur Erwähnung, Designentscheidung), `workflow_dispatch` (ergänzt, Designentscheidung), Token-Rechte (`issues: write` genügt, Konfiguration). Die nicht repo-seitig verifizierbare Wirksamkeit der `@softwarewerkstatt`-Erwähnung ist als Risiko (nicht als offener Punkt) geführt, inklusive Gegenprobe per Probelauf.
