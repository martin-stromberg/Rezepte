# Plan-Check: Security F-07 – Passwort-Hashing-Policy und Rehash-Strategie

**Status: Plan vollständig**

Geprüft wurde `plan.md` gegen `requirement.md`, `inventory.md` und den tatsächlichen Codebestand (eigenes Plan-Review gemäß `AGENTS.md`, da kein dedizierter Unteragent verfügbar ist).

## Geprüfte Annahmen und Befunde

- **Aufrufer von `PasswordHasher`:** Repo-weite Suche bestätigt: nur `UserService` (`RegisterAsync`, `LoginAsync`, `ChangePasswordAsync`). Breaking Change der `Verify`-Signatur ist sicher.
- **Tracked Entity beim Login:** `LoginAsync` nutzt `FirstOrDefaultAsync` ohne `AsNoTracking` — Rehash kann direkt persistiert werden. Bestätigt.
- **Export-Restore:** `ExportService` stellt Benutzer mit `PasswordHash = string.Empty` wieder her. Neues `Verify` liefert dafür `Failed` (Split-Länge ≠ 3) — kein Verhaltensbruch, Login bleibt korrekt verweigert.
- **Testbarkeit schwacher Hashes:** Da `Hash` Iterationen unterhalb `MinIterations` ablehnt, müssen Tests für zu schwache/manipulierte Parameter die Hash-Strings direkt konstruieren (Format `iterations.saltHex.hashHex` bzw. `Rfc2898DeriveBytes.Pbkdf2` im Test). Dies ist im Plan implizit abgedeckt und ohne Zusatzaufwand umsetzbar.
- **`ChangePasswordAsync`:** Bei `SuccessRehashNeeded` wird ohnehin ein neuer Hash geschrieben — die `!= Failed`-Prüfung ist korrekt und hinreichend.
- **DoS-Grenze:** `MaxIterations` wird **vor** der PBKDF2-Berechnung geprüft — manipulierte Iterationszahlen erzeugen keine Verifikationskosten.
- **E2E-Begründung:** Kein Benutzerfluss betroffen (keine UI-Änderung, keine geänderten Fehlermeldungen) — der Verzicht auf E2E-Tests ist begründet; der Login-Codepfad wird über `UserServiceTests` ohne Mocking des Hashers abgedeckt.

## Akzeptanzkriterien-Abdeckung

| Kriterium | Abgedeckt durch |
|-----------|-----------------|
| Mindest- und Höchstwerte für Hashparameter | `MinIterations`/`MaxIterations` + Verify-Prüfung (Plan: Änderungen `PasswordHasher`) |
| Rehash-on-Login | Programmablauf „Rehash-on-Login" + `UserService`-Änderung |
| Tests für schwache/ungültige/zu teure Parameter | `PasswordHasherTests`-Matrix |
| Passwort-Policy dokumentiert | Schritt 5 (Dokumentation) |

## Verbleibende Hinweise (keine Blocker)

- Beim Rehash in `LoginAsync` soll ein `SaveChangesAsync`-Fehler den erfolgreichen Login nicht blockieren (opportunistisches Upgrade) — bei der Implementierung mit engem `try/catch` umsetzen.
- Sicherstellen, dass `Verify` ausschließlich `Convert.TryFromHexString`/sichere Pfade nutzt, damit keine Exception mehr entweicht.
