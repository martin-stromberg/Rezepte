# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| 1. Separater Workflow je Prüfung unter `.github/workflows/` | Umsetzungsreihenfolge Schritt 2: `review-architecture-security.yml`, `review-nuget-packages.yml`, `review-compliance.yml` nach gemeinsamem Skelett; gemeinsame Logik in Composite Action `create-review-issue` (Schritt 1) | YAML-Syntaxprüfung der vier neuen YAML-Dateien + Konventions-Review im PR | Abgedeckt |
| 2. `schedule`-Cron-Trigger (monatlich am 1. Tag; wöchentlich; jährlich am 1. Januar, UTC) | `'0 6 1 * *'` / `'0 5 * * 2'` / `'0 7 1 1 *'` in Scope-Klärung und Workflow-Beschreibung; Wochentag Dienstag 05:00 als dokumentierte Annahme (gestaffelt zu `security-scan.yml` Montag 04:00); `workflow_dispatch` ergänzt | Konventions-Review (Cron-Ausdrücke gegen Anforderung); Funktionswirksamkeit ehrlich als „erst auf `main` wirksam" deklariert | Abgedeckt |
| 3. Aussagekräftige, typzugeordnete Issue-Titel | `[Review] <Bezeichnung> – <Zyklus>` mit typspezifischem Datumsformat (`yyyy-MM`/`yyyy-MM-dd`/`yyyy`) als Action-Input | Konventions-Review + `workflow_dispatch`-Probelauf (Titel im erzeugten Issue sichtbar, Szenario 1) | Abgedeckt |
| 4. Issue-Body mit `@softwarewerkstatt` + Prüfauftrag aus #178 | `issue-body`-Input je Workflow mit vollständigem Prüfauftrag aus `issue.md` (Z. 29–45 / 54–71 / 80–96), erste Zeile `@softwarewerkstatt`; Übergabe über `env:`-Variable gegen Quoting-Probleme | `workflow_dispatch`-Probelauf Happy Path (Body im Issue prüfbar, Szenario 1) | Abgedeckt |
| 5. Dedup: kein Duplikat bei offenem Issue desselben Typs, Lauf erfolgreich | Label-basierte Dedup: `gh issue list --state open --label <typ-label> --json number` → `Count -gt 0` → `Write-Host` ohne `gh issue create`; Label idempotent via `gh label create --force` (bewährtes Muster aus `staging-to-main-promotion.yml`) | Zweiter `workflow_dispatch`-Lauf protokolliert Nicht-Erstellung (Szenario 2); Szenario 3 deckt Neuzyklus nach Schließen | Abgedeckt |
| 6. Vollständige Dokumentation im Repository | Schritt 3: neuer Abschnitt in `Docs/help/github-actions.md` (Workflows, Cron-Pläne, Labels, Dedup-Logik, `@softwarewerkstatt`-Mechanismus, Composite Action, Default-Branch-Einschränkung, `security-scan.yml`-Lückenschluss); Bullet in `Docs/features.md` CI/CD; Satz in `Docs/dependencies.md` Paketstrategie | Review der Dokumentationsänderungen im PR | Abgedeckt |

## Fehlende oder unvollständige Testanforderungen

Keine — der Funktionsnachweis ist für eine Workflow-only-Änderung ehrlich und vollständig aufgebaut: YAML-Syntaxprüfung und Konventions-Review lokal vor dem Commit, echter Funktionsnachweis (Happy Path + Dedup-Pfad) über `workflow_dispatch`-Probelauf nach der Promotion auf `main`, da `schedule`/`workflow_dispatch` nur auf dem Default-Branch wirken. Die Anforderung selbst definiert die Verifikation genau so („YAML-Lint/Review und `workflow_dispatch`-Probelauf").

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Zeitgesteuerter Review-Lauf erzeugt Issue (AK 1–4) | Kein Browser-E2E möglich; Funktionsnachweis über `workflow_dispatch`-Probelauf nach Merge (Umsetzungsschritt 5, Akzeptanzszenario 1) | Nicht erforderlich mit Begründung: keine Anwendungs-UI betroffen; Auslöser und sichtbare Wirkung liegen auf der GitHub-Actions-Plattform, nicht in `Rezepte.Tests.Browser`-Reichweite. Plan begründet dies nachvollziehbar (Abschnitt „E2E-Tests"). |
| Dedup-Pfad: Nicht-Erstellung bei offenem Vorgänger (AK 5) | Zweiter `workflow_dispatch`-Lauf, protokollierte Nicht-Erstellung (Akzeptanzszenario 2) | Nicht erforderlich mit Begründung — gleiche Begründung; Abdeckung über dokumentierten Probelauf. |

## Fehlende oder unvollständige Planbestandteile

Keine. Scope-Klärung vollständig (Verarbeitungseinheit, Ereignisse, Wirkung, Umgang mit fehlenden Objekten inkl. Label-Nichtexistenz und geschlossenem Vorgänger); die vier ursprünglichen „Scope-Ausschlüsse" wurden bei der Orchestrierungsprüfung als Anforderungsbestandteile adjudiert und sind im Plan entsprechend eingeordnet — keine unbestätigten Ausschlüsse offen. Umsetzungsreihenfolge mit Voraussetzungen je Schritt; Risiken (Default-Branch-Inertheit, nicht repo-seitig verifizierbare `@softwarewerkstatt`-Wirksamkeit inkl. Gegenprobe, neue Labels, 1.-Januar-Doppellauf, manuell entferntes Dedup-Label) benannt. Der Widerspruch „kein Checkout" (Anforderung) vs. Checkout-Pflicht für lokale Composite Actions ist aufgelöst und begründet.

## Hinweise

- **Möglicher lokaler Skript-Dry-Run (optionale Ergänzung zu Schritt 4):** `gh` ist lokal als `softwarewerkstatt` authentifiziert; die Dedup-Abfrage (`gh issue list -R martin-stromberg/Rezepte --state open --label <typ-label> --json number`) samt `ConvertFrom-Json`/`Count`-Auswertung ließe sich read-only vor dem Merge lokal proben, ohne Labels oder Issues zu erzeugen. Das würde Skriptfehler (JSON-Parsing, `@()`-Array-Normalisierung, `Get-Date`-Format-Input) vor dem Post-Merge-Probelauf abfangen. Nicht als Lücke gewertet, da die Anforderung die Verifikation explizit auf YAML-Lint/Review und `workflow_dispatch`-Probelauf festlegt — empfohlene Verstärkung.
- Der `workflow_dispatch`-Probelauf erzeugt reale Issues und Labels im Live-Repo; der Plan sieht das Schließen des Test-Issues vor. Ergänzend könnten die dabei angelegten `review-*`-Labels als dauerhafte Nebenwirkung akzeptiert oder bereinigt werden — bewusste Entscheidung im Risikoabschnitt bereits vorweggenommen.
- Hinweis zur Nachplanung: Schritt 5 der Umsetzungsreihenfolge liegt außerhalb des Task-Branches (durch Maintainer nach Promotion). Das ist korrekt und ehrlich deklariert; beim Abschluss des Tasks sollte der Probelauf als offene Maintainer-Aufgabe kommuniziert werden.
