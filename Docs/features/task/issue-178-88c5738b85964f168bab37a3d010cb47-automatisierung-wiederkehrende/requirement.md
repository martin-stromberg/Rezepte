# Anforderungsübersetzung

## Metadaten

- Aufgaben-ID: `88c5738b-8596-4f16-8bab-37a3d010cb47`
- Branch: `task/issue-178-88c5738b85964f168bab37a3d010cb47-automatisierung-wiederkehrende`
- Kundenanforderung: „Automatisierung wiederkehrender Architektur-, Sicherheits- und Compliance-Reviews" (GitHub Issue #178, `martin-stromberg/Rezepte`)

## Fachliche Zusammenfassung

Die CI/CD-Infrastruktur des Repositories wird um drei eigenständige, zeitgesteuerte GitHub-Actions-Workflows erweitert, die zu definierten Intervallen automatisch Review-Issues im Repository anlegen: ein monatliches Architektur- und Sicherheitsreview (jeweils am ersten Tag des Monats), eine wöchentliche NuGet-Paketprüfung und ein jährliches Compliance- und Copyright-Review (jeweils am 1. Januar). Jedes erzeugte Issue enthält im Body eine Erwähnung des Accounts `@softwarewerkstatt`, die die externe automatische Analyse und Bearbeitung anstößt. Vor dem Anlegen prüft jeder Workflow, ob bereits ein offenes Issue desselben Review-Typs existiert, und erzeugt in diesem Fall kein Duplikat.

## Fachlicher Scope

- **Verarbeitungseinheit:** Ein Review-Zyklus je Prüfungsart — also der geplante Anlauf, genau ein Issue des Typs „Architektur- und Sicherheitsreview", „NuGet-Paketprüfung" bzw. „Compliance- und Copyright-Review" zu erzeugen oder wegen eines offenen Vorgänger-Issues zu verwerfen.
- **Auslösende Ereignisse:** Cron-Trigger (`on: schedule`) je Workflow — monatlich am 1. Tag, wöchentlich, jährlich am 1. Januar (jeweils UTC). Nach Repo-Konvention (`security-scan.yml`) ist zusätzlich `workflow_dispatch` für manuelle Anläufe vorgesehen.
- **Erwartete Wirkung:** Pro fälligem Zyklus ohne offenen Vorgänger entsteht ein neues offenes GitHub-Issue mit aussagekräftigem Titel, `@softwarewerkstatt`-Erwähnung und dem im Issue #178 definierten Prüfauftrag als Body. Die Erwähnung ist der vereinbarte Auslöser für die automatische Analyse- und Bearbeitungsautomatik des Accounts. Bei bereits offenem Issue desselben Typs ist die sichtbare Wirkung eine protokollierte Nicht-Erstellung (kein Duplikat).
- **Umgang mit fehlenden Objekten:**
  - Offenes Issue desselben Review-Typs bereits vorhanden: verwerfen — der Lauf endet erfolgreich ohne Issue-Erstellung.
  - Dedup-/Typ-Label noch nicht im Repository vorhanden: erzeugen — idempotent über `gh label create ... --force`, analog zum bestehenden `automated-promotion`-Label in `staging-to-main-promotion.yml`.
  - Vorangegangenes Issue wurde geschlossen: neuer Zyklus — ein neues Issue wird regulär angelegt.
- **Scope-Ausschlüsse:**
  1. Die Workflows führen die Reviews nicht selbst durch. Analyse, Handlungsempfehlungen und — bei der NuGet-Prüfung — die Umsetzung unbedenklicher Updates in Pull Requests sind Aufgabe der durch die `@softwarewerkstatt`-Erwähnung angestoßenen Automatik, nicht des Workflows.
  2. Kein automatisches Schließen, Eskalieren oder Verfallen überfällig offener Review-Issues; die Anforderung verlangt nur, kein Duplikat zu erzeugen.
  3. Keine Paketaktualisierung durch die Workflows selbst — im Gegensatz zum bestehenden blockierenden Security-Gate und zum wöchentlichen `security-scan.yml` bleibt der neue Workflow reine Issue-Erzeugung.
  4. Keine zusätzlichen Benachrichtigungskanäle (E-Mail, Chat); die `@softwarewerkstatt`-Erwähnung im Issue-Body ist der einzige geforderte Auslöser.
  5. Wann der wöchentliche Lauf in der Woche erfolgt, ist fachlich nicht festgelegt; ein konkreter Wochentag/Uhrzeit ist als Umsetzungsannahme zu dokumentieren (siehe Offene Fragen).

## Betroffene Klassen und Komponenten

Es sind keine .NET-Klassen, Datenmodelle, Services oder UI-Komponenten betroffen. Die Änderung liegt ausschließlich in der GitHub-Actions-Infrastruktur und deren Dokumentation:

- Neu: `.github/workflows/` — drei separate Workflow-Dateien (Benennung nach Repo-Konvention im Plan festzulegen, Vorschlag: `review-architecture-security.yml`, `review-nuget-packages.yml`, `review-compliance.yml`).
- Optional neu: `.github/actions/` — eine gemeinsame Composite Action für die Sequenz „Label sicherstellen → offene Issues prüfen → Issue erzeugen", damit die Logik wie bei `.github/actions/security-scan` nur einmal existiert statt in drei Workflows dupliziert zu werden (Entscheidung im Plan).
- Doku: `Docs/help/github-actions.md` — neuer Abschnitt für die drei Review-Workflows (Akzeptanzkriterium „vollständig innerhalb des Repositories dokumentiert").
- Doku: `Docs/features.md` — Ergänzung des Abschnitts „CI/CD".
- Ggf. `Docs/dependencies.md` — Verweis auf die neue wöchentliche NuGet-Prüfung neben dem bestehenden Vulnerability-Scan.
- Tests: nicht anwendbar — reine Workflow-Änderung ohne Testoberfläche; Verifikation über YAML-Lint/Review und `workflow_dispatch`-Probelauf.

## Implementierungsansatz

- Pro Prüfung ein eigener Workflow mit `on: schedule: - cron: '...'` (UTC) plus `workflow_dispatch`; kein Checkout erforderlich, da nur die GitHub-API über `gh` genutzt wird.
- Minimale Berechtigungen nach Repo-Konvention: Top-Level-`permissions` mit `issues: write` (für `gh issue create`/`gh label create`); `GH_TOKEN: ${{ github.token }}` wie in `staging-to-main-promotion.yml` und `sync-staging-with-main.yml`.
- Steps in `pwsh` (Repo-Konvention): (1) Typ-Label idempotent anlegen, (2) `gh issue list --state open --label <typ-label>` auswerten und bei Treffer ohne Erstellung beenden, (3) `gh issue create --title ... --body ... --label <typ-label>` — der Body beginnt mit `@softwarewerkstatt` und enthält den jeweiligen Prüfauftrag aus Issue #178.
- `concurrency`-Gruppe je Workflow wie in den bestehenden Workflows.
- Issue-Titel: aussagekräftig und eindeutig pro Typ (Vorschlag: mit Typ-Bezeichner und Datum, z. B. „[Review] Monatliches Architektur- und Sicherheitsreview – YYYY-MM"), damit die Duplikaterkennung und die Zuordnung zum Zyklus nachvollziehbar bleiben.
- Hinweis zur Wirksamkeit: `schedule`-Trigger laufen nur auf dem Default-Branch; die Workflows werden erst nach der Promotion `staging → main` aktiv (Repo-Staging-Flow gemäß `Docs/help/github-actions.md`).

## Konfiguration

Keine Anwendungs- oder Laufzeitkonfiguration erforderlich. Intervalle, Issue-Titel, Body-Texte und Label-Namen werden in den Workflow-Dateien fest gepflegt. Es werden keine zusätzlichen Secrets benötigt — `GITHUB_TOKEN` mit `issues: write` genügt (Annahme; abhängig davon, ob für die `@softwarewerkstatt`-Erwähnung bzw. optionale Zuweisung weitere Rechte nötig sind, siehe Offene Fragen).

## Akzeptanzkriterien

1. Für jede der drei Prüfungen existiert ein separater Workflow unter `.github/workflows/`.
2. Jeder Workflow wird zeitgesteuert über einen `schedule`-Cron-Trigger ausgeführt (monatlich am 1. Tag; wöchentlich; jährlich am 1. Januar, jeweils UTC).
3. Die erzeugten Issues tragen aussagekräftige, dem Review-Typ zugeordnete Titel.
4. Der Issue-Body enthält die Erwähnung `@softwarewerkstatt` sowie den im Issue #178 definierten Prüfauftrag des jeweiligen Typs.
5. Vor jeder Issue-Erstellung wird auf bereits offene Issues desselben Typs geprüft; bei Treffer wird kein Duplikat angelegt und der Lauf endet erfolgreich.
6. Workflows, Cron-Zeitpläne, Duplikatlogik und der Auslösemechanismus über `@softwarewerkstatt` sind vollständig im Repository dokumentiert (`Docs/help/github-actions.md`, `Docs/features.md`).

## Offene Fragen

1. **Wochentag/Uhrzeit der wöchentlichen NuGet-Prüfung:** Nicht vorgegeben. Annahme: ein fester Wochentag, zeitlich gestaffelt zum bestehenden `security-scan.yml` (Montag 04:00 UTC), z. B. Dienstag 05:00 UTC. Zu bestätigen.
2. **Duplikatkriterium:** Die Anforderung nennt „offene Issues des vorherigen Zyklus" ohne Erkennungsmerkmal. Annahme: Dedup über einen typspezifischen Label, den der Workflow selbst vergibt. Alternativ wäre eine Titel- oder Autoren-basierte Suche — fragiler, da Titel variieren können.
3. **Erwähnung vs. Zuweisung:** Reicht die `@softwarewerkstatt`-Erwähnung im Body (wie gefordert), oder soll das Issue zusätzlich per `--assignee` zugewiesen werden? Zuweisung setzt Collaborator-Zugriff des Accounts voraus — aktuell ungeprüfte Annahme.
4. **Wirksamkeit der Erwähnung:** Die Annahme, dass die Body-Erwähnung die externe Automatik von `@softwarewerkstatt` anstößt, liegt außerhalb des Repositories und kann hier nicht verifiziert werden.
5. **Manueller Trigger:** `workflow_dispatch` ist nicht gefordert, aber Repo-Konvention (`security-scan.yml`). Annahme: wird ergänzt, um Probeläufe und die Verifikation zu ermöglichen.
