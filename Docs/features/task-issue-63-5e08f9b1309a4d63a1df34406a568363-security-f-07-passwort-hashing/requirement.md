# Übersetzte Anforderung: Security F-07 – Passwort-Hashing-Policy und Rehash-Strategie

## Fachliche Zusammenfassung

Das eigene PBKDF2-HMAC-SHA256-Passwort-Hashing (`PasswordHasher`) nutzt feste Defaultparameter und akzeptiert beim Verifizieren die im gespeicherten Hash-String kodierte Iterationszahl ohne Policy-Grenzen. Es soll eine Passwort-Hashing-Policy mit Mindest- und Höchstwerten für die Hash-Parameter eingeführt sowie eine Rehash-on-Login-Strategie implementiert werden, damit Hashes mit veralteten (zu schwachen) Parametern beim nächsten erfolgreichen Login automatisch auf die aktuellen Parameter angehoben werden.

## Betroffene Klassen und Komponenten

- `Rezepte.Web/Security/PasswordHasher.cs` — zentrale Erweiterung: Policy-Konstanten, strukturierte Verifikationsergebnisse, Parameter-Validierung
- `Rezepte.Web/Services/UserService.cs` — `LoginAsync`: Rehash-on-Login bei veralteten Parametern
- Neue Enum `PasswordVerificationResult` (o. ä.) für `Failed` / `Success` / `SuccessRehashNeeded`
- Tests: neue Testklasse für `PasswordHasher` sowie Erweiterung von `UserServiceTests` (Rehash-Verhalten)
- Dokumentation: `Docs/help/` (Passwort-Policy)

## Implementierungsansatz

- `PasswordHasher` erhält öffentliche Policy-Konstanten: `CurrentIterations` (Zielwert für neue Hashes), `MinIterations` (untere Akzeptanzgrenze beim Verifizieren), `MaxIterations` (obere Grenze als DoS-Schutz gegen manipulierte Iterationszahlen) sowie feste Längen für Salt und Hash.
- `Verify` liefert statt `bool` ein `PasswordVerificationResult` und prüft Format und Parameter strikt: ungültige Struktur, nicht parsebare Iterationszahl, Iterationen außerhalb `[MinIterations, MaxIterations]`, falsche Salt-/Hash-Längen → `Failed` (kein Exception-Absturz mehr bei malformed Strings).
- `Success` mit `iterations < CurrentIterations` → `SuccessRehashNeeded`.
- `UserService.LoginAsync` schreibt bei `SuccessRehashNeeded` einen neuen Hash mit `CurrentIterations` in die Entität und persistiert ihn.
- `Hash` verwendet `CurrentIterations` als Default; explizit übergebene Iterationen werden gegen die Policy geprüft.

## Konfiguration

Keine externe Konfiguration — die Policy ist als feste Konstanten im Code verankert (Kompilationszeit-Policy), analog zum bisherigen festen Default von 100.000.

## Offene Fragen

- Konkrete Werte für Min/Max: Vorschlag `MinIterations = 100_000` (bisheriger Default als Untergrenze), `CurrentIterations = 210_000` (OWASP-Empfehlung für PBKDF2-HMAC-SHA256), `MaxIterations = 1_000_000` (Schutz gegen DoS durch manipulierte Hashes).
