# Offene Aufgaben

Erstellt am: 2026-08-07
Abbruchgrund: Kein Fortschritt zwischen den letzten zwei Iterationen

Die folgenden Aufgaben konnten im automatisierten Zyklus nicht abgeschlossen werden
und müssen manuell oder in einem erneuten Lauf bearbeitet werden.

## Offene Planelemente

- [x] Canonical-URL nicht mehr durch Admin konfigurierbar machen; sie muss serverseitig automatisch ermittelt werden
- [x] Für jedes Ausgabeformat eine eigene Canonical-URL ausgeben (`/security.txt`, `/.well-known/security.md`, `/.well-known/security.html`)

## Code-Review-Befunde

- [x] SecurityTxtBrowserTests.GetSecurityTxt_ReturnsNotFound_WhenDisabled — Testzustand explizit herstellen statt auf globalen Zustand zu vertrauen
- [x] SecurityTxtControllerTests.GetSecurityTxt_RequiresNoAuthentication — als echter Integrations-/WebApplicationFactory-Test gegen /security.txt prüfen
- [x] SettingsService — ISecurityTxtSettingsService als verpflichtende DI-Abhängigkeit injizieren, Fallback-
ew SecurityTxtSettingsService(db) entfernen

## Fehlgeschlagene Tests

(Keine — alle Tests erfolgreich)

