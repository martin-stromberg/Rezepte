# Offene Aufgaben

Erstellt am: 2026-08-07
Abbruchgrund: Kein Fortschritt zwischen den letzten zwei Iterationen

Die folgenden Aufgaben konnten im automatisierten Zyklus nicht abgeschlossen werden
und müssen manuell oder in einem erneuten Lauf bearbeitet werden.

## Offene Planelemente

(Keine — Plan vollständig umgesetzt)

## Code-Review-Befunde

- [ ] SecurityTxtBrowserTests.GetSecurityTxt_ReturnsNotFound_WhenDisabled — Testzustand explizit herstellen statt auf globalen Zustand zu vertrauen
- [ ] SecurityTxtControllerTests.GetSecurityTxt_RequiresNoAuthentication — als echter Integrations-/WebApplicationFactory-Test gegen /security.txt prüfen
- [ ] SettingsService — ISecurityTxtSettingsService als verpflichtende DI-Abhängigkeit injizieren, Fallback-
ew SecurityTxtSettingsService(db) entfernen

## Fehlgeschlagene Tests

(Keine — alle Tests erfolgreich)
