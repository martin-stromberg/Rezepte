# Offene Aufgaben

Erstellt am: 2026-11-22
Abbruchgrund: Kein Fortschritt zwischen den Iterationen erwartbar — der einzige
verbleibende Punkt ist per Plan eine externe CI-Verifikation, die erst nach
Commit/Push/PR (für `staging-ci.yml`: nach Merge auf `staging`) ausführbar ist.

## Offene Planelemente

- [ ] Task 37 — CI-Verifikation: mindestens drei grüne Komplettläufe in
  `pr-staging-ci.yml` **und** `staging-ci.yml` (Akzeptanzkriterium 10).
  - `pr-staging-ci.yml` auf PR #221: **3/3 grüne Komplettläufe nachgewiesen**
    (Run 45056050985, Attempts 1–3 — inkl. neuem `browser tests`-Job mit
    Browser-Suite + OsInterface-Subset, korrekt übersprungenem Retry-Step und
    ausgeführtem Analyze-Step; attempt-eindeutige TRX-/Trace-/Log-Artefakte).
  - `staging-ci.yml`: ausstehend — läuft ausschließlich auf Push nach
    `staging`, also erst nach dem Merge des PR. Nach dem Merge sind mindestens
    drei grüne Läufe via `gh run list --workflow staging-ci.yml` zu belegen
    (notfalls über `gh run rerun`).

## Code-Review-Befunde

Keine — `review-code.md` (Runde 2) trägt `Keine Befunde`.

## Usability-Befunde

Keine — keine UI-Änderungen (`review-usability.md`: nicht anwendbar).

## Fehlgeschlagene Tests

Keine — `test-results.md` trägt `Keine Fehler` (758/758 + 15/15 +
35/35 Browser-Suite lokal grün; Skripte per synthetischem TRX-Satz verifiziert).
