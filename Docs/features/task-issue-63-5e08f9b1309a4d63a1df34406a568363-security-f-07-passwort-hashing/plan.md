# Umsetzungsplan: Security F-07 – Passwort-Hashing-Policy und Rehash-Strategie

## Übersicht

`PasswordHasher` wird um eine explizite Hashing-Policy (Min-/Max-/Ziel-Iterationen, feste Salt-/Hash-Längen) erweitert. `Verify` liefert ein `PasswordVerificationResult` statt `bool` und lehnt ungültige oder policy-widrige Parameter ab. `UserService.LoginAsync` führt bei erfolgreicher Verifikation mit veralteten Parametern einen Rehash mit den aktuellen Parametern durch (Rehash-on-Login). Die Policy wird in `Docs/help/` dokumentiert.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Policy-Werte | `public const`-Konstanten in `PasswordHasher` (`CurrentIterations = 210_000`, `MinIterations = 100_000`, `MaxIterations = 1_000_000`, `SaltLengthBytes = 16`, `HashLengthBytes = 32`) | Bisheriger Default war ebenfalls fest kodiert; keine externe Konfigurierbarkeit gefordert. 210.000 folgt der OWASP-Empfehlung für PBKDF2-HMAC-SHA256; 100.000 als Untergrenze akzeptiert alle existierenden Hashes; 1.000.000 als DoS-Obergrenze. |
| Verifikationsergebnis | Neuer Enum `PasswordVerificationResult` (`Failed`, `Success`, `SuccessRehashNeeded`) im selben File/Namespace | Etabliertes Muster (ASP.NET `PasswordHasher<T>`), vermeidet `out`-Parameter, macht Rehash-Bedarf explizit. |
| `Verify`-Rückgabetyp | Breaking Change von `bool` auf `PasswordVerificationResult` | Nur zwei Aufrufstellen (`LoginAsync`, `ChangePasswordAsync`), keine externen Konsumenten; explizites Ergebnis verhindert, dass `SuccessRehashNeeded` übersehen wird. |
| Fehlverhalten bei malformed Hashes | `Verify` liefert `Failed` statt Exception | Sicherer als `FormatException`/`OverflowException` im Request-Pfad; kein 500er bei manipulierten DB-Werten. |
| Rehash-Ort | `UserService.LoginAsync` direkt nach erfolgreicher Verifikation | Rehash-on-Login ist die vom Issue genannte Strategie; Entität ist bereits tracked, `SaveChangesAsync` genügt. Rehash-Fehler dürfen den erfolgreichen Login nicht verhindern. |

## Programmabläufe

### Verifikation mit Policy-Prüfung

1. `PasswordHasher.Verify(password, hashString)` prüft Null-Argumente.
2. Split auf `.` — bei nicht exakt 3 Teilen → `Failed`.
3. `int.TryParse` der Iterationszahl — bei Fehlschlag oder Wert außerhalb `[MinIterations, MaxIterations]` → `Failed`.
4. Hex-Dekodierung von Salt und Hash in `try/catch` bzw. `Convert.TryFromHexString` — bei ungültigem Hex oder abweichender Länge (Salt ≠ `SaltLengthBytes`, Hash ≠ `HashLengthBytes`) → `Failed`.
5. PBKDF2 mit gespeicherter Iterationszahl; `FixedTimeEquals`-Vergleich — bei Ungleichheit → `Failed`.
6. `iterations < CurrentIterations` → `SuccessRehashNeeded`, sonst `Success`.

Beteiligte Klassen: `PasswordHasher`, `PasswordVerificationResult`

### Rehash-on-Login

1. `UserService.LoginAsync` lädt die `Entities.User`-Entität (tracked).
2. `PasswordHasher.Verify` liefert `Failed` → `null` zurück.
3. Bei `SuccessRehashNeeded`: `entity.PasswordHash = PasswordHasher.Hash(password)` (mit `CurrentIterations`) und `await _db.SaveChangesAsync(ct)`; anschließend `MatchUser(entity)` zurückgeben. Scheitert das Persistieren, wird der Login nicht blockiert (Ergebnis wird trotzdem zurückgegeben — Rehash ist opportunistisch).
4. Bei `Success` → `MatchUser(entity)` zurückgeben.

Beteiligte Klassen: `UserService`, `PasswordHasher`, `RezepteDbContext`, `Entities.User`

### Passwortwechsel und Registrierung

- `RegisterAsync` und `ChangePasswordAsync` erzeugen Hashes mit `CurrentIterations` (Default-Parameter von `Hash`).
- `ChangePasswordAsync` prüft `Verify(...) != Failed` (statt bisher `== true`).

Beteiligte Klassen: `UserService`, `PasswordHasher`

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `PasswordVerificationResult` | Enum (in `Rezepte.Web.Security`) | Ergebnis der Passwort-Verifikation: `Failed`, `Success`, `SuccessRehashNeeded` |

## Änderungen an bestehenden Klassen

### `PasswordHasher` (statische Klasse)

- **Neue Konstanten:** `CurrentIterations`, `MinIterations`, `MaxIterations`, `SaltLengthBytes`, `HashLengthBytes` — Policy-Grenzen.
- **Geänderte Methode `Hash`:** Default `iterations = CurrentIterations`; explizite Iterationen außerhalb `[MinIterations, MaxIterations]` → `ArgumentOutOfRangeException`.
- **Geänderte Methode `Verify`:** Rückgabetyp `PasswordVerificationResult` statt `bool`; strikte Format- und Policy-Prüfung ohne Exceptions (siehe Programmablauf).

### `UserService` (Klasse)

- **Geänderte Methode `LoginAsync`:** Auswertung des neuen Ergebnis-Enums; Rehash + `SaveChangesAsync` bei `SuccessRehashNeeded`.
- **Geänderte Methode `ChangePasswordAsync`:** `Verify`-Ergebnis gegen `Failed` prüfen statt boolscher Auswertung.

## Datenbankmigrationen

Keine — `PasswordHash` bleibt ein String-Feld; das Format `iterations.salt.hash` ist unverändert.

## Validierungsregeln

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| `iterations` (gespeicherter Hash) | Ganzzahl, `MinIterations ≤ n ≤ MaxIterations` | `Failed` — schwache oder manipulierte Parameter werden abgelehnt |
| `salt` (gespeicherter Hash) | gültiges Hex, exakt `SaltLengthBytes` | `Failed` |
| `hash` (gespeicherter Hash) | gültiges Hex, exakt `HashLengthBytes` | `Failed` |
| `iterations` (Parameter von `Hash`) | `MinIterations ≤ n ≤ MaxIterations` | `ArgumentOutOfRangeException` |

## Konfigurationsänderungen

Keine.

## Seiteneffekte und Risiken

- **`Verify`-Signatur ändert sich** (`bool` → Enum): Alle Aufrufer (`LoginAsync`, `ChangePasswordAsync`, Tests) müssen angepasst werden. Suche nach weiteren Aufrufern im Repo erforderlich.
- **Bestehende Hashes mit `iterations = 100_000`:** bleiben verifizierbar (`MinIterations = 100_000`), werden aber beim nächsten Login auf `CurrentIterations` angehoben — gewünschtes Verhalten.
- **Login-Latenz:** steigt von ~100k auf ~210k PBKDF2-Iterationen — akzeptabel und intendiert.

## Umsetzungsreihenfolge

1. **`PasswordVerificationResult`-Enum und `PasswordHasher`-Erweiterung**
   - Voraussetzungen: Keine
   - Enum anlegen; Policy-Konstanten; `Hash`-Default und Parameter-Guard; `Verify` mit neuer Signatur und Policy-Prüfung.

2. **`UserService` anpassen**
   - Voraussetzungen: Schritt 1
   - `LoginAsync`: Enum auswerten, Rehash-on-Login; `ChangePasswordAsync`: `Failed`-Prüfung.

3. **Neue Tests: `PasswordHasherTests`**
   - Voraussetzungen: Schritt 1
   - Neue Testklasse unter `Rezepte.Tests/Security/` (xUnit + FluentAssertions, Konvention aus `UserServiceTests`).

4. **`UserServiceTests` erweitern / anpassen**
   - Voraussetzungen: Schritt 2
   - Rehash-on-Login-Test (Hash mit niedrigerer Iterationszahl einpflanzen, Login, gespeicherten Hash auf `CurrentIterations` prüfen); ggf. bestehende Tests an neue Signatur anpassen.

5. **Dokumentation**
   - Voraussetzungen: Schritte 1–4
   - Passwort-Hashing-Policy in `Docs/help/` dokumentieren (Abschnitt in `user-accounts.md` oder eigenes Security-Dokument), ggf. `docs/RELEASE_NOTES.md`.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `Hash_ShouldUseCurrentIterations_ByDefault` | `PasswordHasherTests` | Erzeugter Hash kodiert `CurrentIterations`, Salt/Hash-Längen korrekt |
| `Verify_ShouldReturnSuccess_WhenHashCurrent` | `PasswordHasherTests` | Happy Path |
| `Verify_ShouldReturnFailed_WhenPasswordWrong` | `PasswordHasherTests` | Negativfall |
| `Verify_ShouldReturnSuccessRehashNeeded_WhenIterationsBelowCurrent` | `PasswordHasherTests` | Hash mit `MinIterations` → `SuccessRehashNeeded` |
| `Verify_ShouldReturnFailed_WhenIterationsBelowMin` | `PasswordHasherTests` | Schwacher Parameter (z. B. 1.000) → `Failed` |
| `Verify_ShouldReturnFailed_WhenIterationsAboveMax` | `PasswordHasherTests` | Zu teurer/manipulierter Parameter → `Failed` (DoS-Schutz) |
| `Verify_ShouldReturnFailed_WhenHashStringMalformed` | `PasswordHasherTests` | Falscher Aufbau, nicht parsebare Iteration, ungültiges Hex, falsche Salt-/Hash-Länge — jeweils `Failed`, keine Exception |
| `Verify_ShouldReturnFailed_WhenArgumentsNull` | `PasswordHasherTests` | Null-Handling konsistent |
| `LoginAsync_ShouldRehashPassword_WhenStoredHashOutdated` | `UserServiceTests` | Nach Login steht der gespeicherte Hash auf `CurrentIterations` |
| `LoginAsync_ShouldNotRehash_WhenStoredHashCurrent` | `UserServiceTests` | Gespeicherter Hash bleibt unverändert |
| `ChangePasswordAsync_ShouldReturnError_WhenStoredHashInvalid` | `UserServiceTests` | Manipulierter gespeicherter Hash → Fehler statt Exception |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `UserServiceTests` (alle `LoginAsync`-Tests) | `Verify`-Signatur intern geändert — Tests selbst bleiben fachlich gleich, müssen aber weiter kompilieren/laufen |
| Sonstige Aufrufer von `PasswordHasher.Verify`/`Hash` | Repo-weite Suche nach direkten Aufrufen außerhalb `UserService` |

### E2E-Tests (primärer Funktionsnachweis)

Keine erforderlich. Die Anforderung betrifft ausschließlich interne Hashing-Parameter und den serverseitigen Login-Codepfad; es gibt keinen neuen oder geänderten Benutzerfluss — Login-Maske, Fehlermeldungen und Navigation bleiben unverändert. Der Funktionsnachweis erfolgt über Service- und Unit-Tests auf `UserService`-Ebene, die den realen Login-Codepfad durchlaufen (kein Mocking des Hashers).

## Offene Punkte

Keine.
