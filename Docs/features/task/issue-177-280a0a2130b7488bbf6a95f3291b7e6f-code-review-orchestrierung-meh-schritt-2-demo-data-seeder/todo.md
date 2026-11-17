# Aufgabenliste – Anforderungsbearbeitung

Branch: `task/issue-177-280a0a2130b7488bbf6a95f3291b7e6f-code-review-orchestrierung-meh-schritt-2-demo-data-seeder`

| Status | Schritt | Beschreibung | Artefakt |
|--------|---------|--------------|----------|
| [x] | 1 | Branch-Name ermitteln | – |
| [x] | 2 | Verzeichnisstruktur vorbereiten | `docs/features/task/issue-177-280a0a2130b7488bbf6a95f3291b7e6f-code-review-orchestrierung-meh-schritt-2-demo-data-seeder/` |
| [x] | – | Einstiegspunkt ermitteln | Schritt 3 (keine Artefakte vorhanden) |
| [x] | 3 | Anforderung übersetzen | `requirement.md` |
| [x] | 4 | Bestandsaufnahme | `inventory.md`, `inventory/` |
| [x] | 5 | Umsetzungsplanung | `plan.md` |
| [x] | 5a | Offene Punkte prüfen und ggf. Planung wiederholen | `plan.md` — keine offenen Punkte, Scope-Ausschlüsse bestätigt |
| [x] | 5b | Plan gegen Anforderung und Testbedarf prüfen | `plan-check.md` — `Plan vollständig` |
| [x] | 5c | Planungscommit | `584479c` |
| [x] | 6 | Implementierung | Codeänderungen (IDemoDataSeeder, DemoDataSeeder, Handler-Reduktion, DI, Tests) |
| [x] | 7 | Plan-Review (bedingt) | `review.md` — `Vollständig umgesetzt` |
| [x] | 8 | Usability-Review (bei UI-Änderungen) | `review-usability.md` — keine UI-Änderungen → nicht zutreffend |
| [x] | 9 | Code-Review | `review-code.md` — `Keine Befunde` |
| [x] | 10 | Tests ausführen | `test-results.md` — autom. Tests 714/714 grün; E2E fehlgeschlagen, aber identisch auf Basis-Stand (preexisting/environmental) → Eskalation an Anwender |
| [x] | – | Iteration oder Abschluss entscheiden | Keine weitere Iteration: Reviews sauber, Unit-Tests grün; E2E-Befund ist preexisting (Basis-Kontrolle), keine Regression durch diesen Schritt |
| [x] | 11 | Folgeaufgaben dokumentieren (bei Schleifenabbruch) | Kein Schleifenabbruch → `continue.md` nicht erforderlich |
| [x] | 12 | Dokumentation erstellen | `Docs/help/user-accounts.md` (Abschnitt „Technischer Hintergrund") |
| [x] | 12b | README aktualisieren | Keine Änderung nötig (kein Demo-Seeding-Technikabschnitt in README) |
| [x] | 12c | Release Notes aktualisieren | Keine Änderung nötig — reine interne Verlagerung ohne Anwender-Auswirkung (Regel: Refactorings werden weggelassen) |
| [ ] | – | Feature-Verzeichnis löschen | **Zurückgestellt** — E2E-Abnahme offen (preexisting Kalender-UI-Problem); Artefakte bleiben bis zur Anwender-Entscheidung bestehen (wie Schritt 1) |
| [ ] | – | Commit durchführen | – |

## Hinweis Ausführungsmodus

Diese Umgebung kann keine weiteren Unteragenten starten. Alle Lifecycle-Schritte wurden daher vom Orchestrierungs-Agenten selbst ausgeführt — jeweils mit frischem, unvoreingenommenem Blick pro Schritt. Die in `models.md` genannten Modell-/Effort-Zuordnungen konnten nicht angewendet werden; es gilt das Sitzungsmodell für alle Schritte. (Gleiche Vorgehensweise wie in Schritt 1.)
