# Offene Aufgaben

Erstellt am: 2026-11-22
Abbruchgrund: Kein Fortschritt zwischen den Iterationen erwartbar — der einzige
verbleibende Punkt ist per Plan eine externe CI-Verifikation, die erst nach
Commit/Push/PR (für `staging-ci.yml`: nach Merge auf `staging`) ausführbar ist.

## Offene Planelemente

- [ ] Task 37 — CI-Verifikation: mindestens drei grüne Komplettläufe in
  `pr-staging-ci.yml` **und** `staging-ci.yml` (Akzeptanzkriterium 10).
  Extern verankert: `pr-staging-ci.yml` läuft auf dem PR gegen `staging`
  (Wiederholungen über `gh run rerun`/Nach-Pushes möglich), `staging-ci.yml`
  erst nach dem Merge auf `staging`. Beleg: `gh pr checks` bzw.
  `gh run list --workflow staging-ci.yml`.

## Code-Review-Befunde

Keine — `review-code.md` (Runde 2) trägt `Keine Befunde`.

## Usability-Befunde

Keine — keine UI-Änderungen (`review-usability.md`: nicht anwendbar).

## Fehlgeschlagene Tests

Keine — `test-results.md` trägt `Keine Fehler` (758/758 + 15/15 +
35/35 Browser-Suite lokal grün; Skripte per synthetischem TRX-Satz verifiziert).
