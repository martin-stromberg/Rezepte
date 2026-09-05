# Bestandsaufnahme: Security F-07 – Passwort-Hashing-Policy und Rehash-Strategie

Analysiert wurden das Passwort-Hashing (`PasswordHasher`), der Authentifizierungsfluss (`UserService`) und die zugehörigen Tests, bezogen auf die Anforderung aus `requirement.md`.

## Zusammenfassung

- `PasswordHasher` ist eine statische Klasse mit `Hash` (PBKDF2-HMAC-SHA256, fester Default `iterations = 100_000`, Salt 16 Bytes, Hash 32 Bytes, Format `iterations.saltHex.hashHex`) und `Verify` (liefert `bool`).
- `Verify` parst die gespeicherte Iterationszahl mit `int.Parse` ohne Grenzen und wirft bei malformed Eingaben (`FormatException`, `OverflowException`) — keine Policy-Grenzen, kein DoS-Schutz.
- Es gibt keine Rehash-Strategie: `UserService.LoginAsync` verifiziert nur; alte Parameter bleiben ewig bestehen.
- `UserService.ChangePasswordAsync` erzeugt bei Passwortwechsel bereits einen neuen Hash mit Default-Parametern — die einzige implizite Upgrade-Stelle.
- Es existieren keine Tests für `PasswordHasher`; `UserServiceTests` deckt Login/Registrierung/Passwortwechsel über InMemory-EF-Core ab.
- Es gibt keinen Enum für Verifikationsergebnisse und keine dokumentierte Passwort-Hashing-Policy (`Docs/help/user-accounts.md` beschreibt nur Benutzernamen-Regeln).

## Details

- [Logik](inventory/logic.md)
- [Tests](inventory/tests.md)
