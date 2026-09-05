# Tasks: Security F-07 – Passwort-Hashing-Policy und Rehash-Strategie

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Security | Enum `PasswordVerificationResult` anlegen | Offen | — |
| 2 | Security | Policy-Konstanten in `PasswordHasher` (`CurrentIterations`, `MinIterations`, `MaxIterations`, `SaltLengthBytes`, `HashLengthBytes`) | Offen | — |
| 3 | Security | `PasswordHasher.Hash` auf `CurrentIterations`-Default und Parameter-Guard umstellen | Offen | — |
| 4 | Security | `PasswordHasher.Verify` auf `PasswordVerificationResult` mit Format-/Policy-Prüfung umstellen | Offen | — |
| 5 | Logik | `UserService.LoginAsync` Rehash-on-Login implementieren | Offen | — |
| 6 | Logik | `UserService.ChangePasswordAsync` auf neues `Verify`-Ergebnis umstellen | Offen | — |
| 7 | Tests | Testklasse `PasswordHasherTests` anlegen (schwache, ungültige, zu teure Parameter + Happy Path) | Offen | — |
| 8 | Tests | `UserServiceTests` um Rehash-on-Login-Tests erweitern | Offen | — |
| 9 | Dokumentation | Passwort-Hashing-Policy in `Docs/help/` dokumentieren | Offen | — |
| 10 | Dokumentation | `docs/RELEASE_NOTES.md` aktualisieren | Offen | — |
