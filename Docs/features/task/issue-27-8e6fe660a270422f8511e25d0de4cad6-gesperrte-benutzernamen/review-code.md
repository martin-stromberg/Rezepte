# Code-Review

Status: Keine Befunde

## Gepruefte Punkte

- Formular-Registrierung gibt serverseitige Fehler nicht mehr nur als generisches `error=1` weiter, sondern redirectet mit der konkreten englischen Fehlermeldung.
- Registrierungsseite rendert die per Querystring gelieferte Fehlermeldung sichtbar als Bootstrap-Alert mit `role="alert"`.
- Nicht lokalisierte Fehlertexte bleiben gemaess `CLAUDE.md` Englisch.
- Der neue Controller-Test deckt den Formularpfad fuer einen serverseitig abgelehnten Benutzernamen ab.

## Befunde

Keine.
