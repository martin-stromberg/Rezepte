# Umsetzungsplan: Gesperrte Benutzernamen

## Zielbild

Benutzernamen werden zentral im Server validiert, bevor Benutzer registriert oder über Profil- und Admin-Funktionen geändert werden. Die Validierung liegt in einer wiederverwendbaren Komponente, liefert deutschsprachige Fehlermeldungen und ergänzt die bestehende Eindeutigkeitsprüfung, ohne Login-Semantik oder bestehende Benutzer automatisch zu verändern.

## Technische Entscheidungen

- Die zentrale Validierung wird als neue Komponente unter `Rezepte.Web/Services/Validation/` umgesetzt.
- `UserService` injiziert den Validator und ruft ihn in `RegisterAsync`, `UpdateProfileAsync` und `UpdateUserAsync` vor der Eindeutigkeitsprüfung auf.
- Die Eindeutigkeitsprüfung bleibt in ihrer bestehenden exakten Semantik erhalten. Eine case-insensitive Eindeutigkeit wird nicht eingeführt, weil sie nicht gefordert ist und bestehende Daten betreffen könnte.
- Die fachliche Maximallänge von 20 Zeichen wird im Validator durchgesetzt. Die EF-Konfiguration `HasMaxLength(64)` bleibt zunächst unverändert, damit keine Migration und keine Prüfung bestehender Daten erforderlich wird.
- Domains und IP-Adressen werden vor der allgemeinen Zeichenprüfung erkannt, damit für Werte wie `127.0.0.1` und `example.com` eine nachvollziehbare Meldung möglich ist.
- Die Ähnlichkeitsprüfung wird konservativ umgesetzt: typische Leetspeak-Normalisierung und anschließender Vergleich gegen ausgewählte Hochrisiko-Namen. Kein breiter Levenshtein-Abgleich über alle reservierten Namen.
- Sperrlisten und Muster liegen gesammelt in einer Datei, damit sie später ohne Änderungen an mehreren Stellen erweitert werden können.

## Umsetzungsschritte

### 1. Validator-Komponente anlegen

Neue Dateien:

- `Rezepte.Web/Services/Validation/IUsernameValidator.cs`
- `Rezepte.Web/Services/Validation/UsernameValidator.cs`
- `Rezepte.Web/Services/Validation/UsernameValidationResult.cs`

Geplante API:

- `IUsernameValidator.Validate(string? username)` gibt ein Ergebnis mit `IsValid` und optionaler deutscher Fehlermeldung zurück.
- Erfolgreiche Validierung verändert den Namen nicht dauerhaft; Trimming erfolgt an den bestehenden Eingängen vor Speicherung.

Validierungsreihenfolge:

1. leer, `null` oder nur Leerraum ablehnen
2. getrimmten Namen auf Domain- und IP-Muster prüfen
3. Länge 3 bis 20 prüfen
4. erlaubte Zeichen prüfen: Buchstaben, Ziffern, `_`, `-`
5. exakt reservierte Namen case-insensitive prüfen
6. app-, domain-, rollen- und projektspezifische Namen prüfen
7. offiziell wirkende Support-/Admin-/Security-Namen prüfen
8. initiale Missbrauchssperrliste prüfen
9. konservative Umgehungs-/Leetspeak-Prüfung ausführen

Fehlermeldungen:

- `Der Benutzername muss zwischen 3 und 20 Zeichen lang sein.`
- `Der Benutzername darf nur Buchstaben, Zahlen, Unterstrich und Bindestrich enthalten.`
- `Der Benutzername ist reserviert.`
- `Dieser Benutzername kann nicht verwendet werden. Bitte wählen Sie einen anderen Namen.`
- `Der Benutzername darf keine IP-Adresse oder Domain sein.`

### 2. Sperrlisten und Muster definieren

Exakte reservierte Namen mindestens:

- `admin`, `administrator`, `root`, `system`, `support`, `guest`, `test`, `null`
- `moderator`, `superuser`, `owner`, `help`, `contact`, `info`, `about`, `login`, `signup`
- `me`, `you`, `self`, `someone`, `anyone`, `webmaster`, `security`
- `rezepte`, `rezepteapp`, `rezepte-admin`, `rezepte_support`

Support-/Security-Muster:

- gesperrte Tokenkombinationen mit Trennern: `support_team`, `security_admin`, `admin-support`
- explizite Suffix-/Sonderfälle aus der Anforderung: `microsoftsupport`
- kritisch wirkende Kombinationen aus `admin`, `support`, `security`, `team`, `helpdesk`, `moderator`

Ähnlichkeitsprüfung:

- Eingabe kleinschreiben.
- Leetspeak normalisieren, mindestens `0 -> o`, `1 -> i`, `3 -> e`, `4 -> a`, `5 -> s`, `7 -> t`.
- Normalisierte Eingabe gegen Hochrisiko-Namen `admin`, `root`, `support` prüfen.
- Zusätzlich konkrete akzeptanzrelevante Varianten `adm1n`, `r00t`, `supp0rt` durch Tests absichern.

### 3. Dependency Injection erweitern

In `Rezepte.Web/Extensions/ServiceCollectionExtensions.cs`:

- Namespace `Rezepte.Web.Services.Validation` importieren.
- `services.AddSingleton<IUsernameValidator, UsernameValidator>();` vor oder neben der Registrierung von `IUserService` ergänzen.

### 4. UserService integrieren

In `Rezepte.Web/Services/UserService.cs`:

- Konstruktor von `UserService(RezepteDbContext db)` auf `UserService(RezepteDbContext db, IUsernameValidator usernameValidator)` erweitern.
- Private Felder für DB und Validator führen.
- In `RegisterAsync` den übergebenen Namen trimmen, validieren und bei Fehler `(false, error, null)` zurückgeben.
- In `UpdateProfileAsync` und `UpdateUserAsync` die bestehende Mindestlängenprüfung durch denselben Validator ersetzen.
- Eindeutigkeitsprüfungen nach erfolgreicher Validierung unverändert beibehalten.
- Bei Speicherung den getrimmten Namen verwenden.
- Bestehende englische Fehler für Username-Duplikate möglichst auf Deutsch vereinheitlichen: `Benutzername ist bereits vergeben.`

### 5. Controller- und UI-Verhalten angleichen

In `Rezepte.Web/Controllers/AdminUsersController.cs`:

- Die eigene Username-Mindestlängenprüfung in `Create` entfernen, damit keine unvollständige Vorvalidierung die zentrale Validierung überdeckt.
- Passwort- und E-Mail-Prüfungen bleiben bestehen.
- Service-Fehler weiterhin als `{ message = error }` zurückgeben.

In `Rezepte.Web/Controllers/AuthController.cs`:

- Für JSON-Fehler deutschsprachige Fallbacks verwenden.
- Formularregistrierung kann weiterhin pauschal auf `/register?error=1` umleiten; die serverseitige Durchsetzung ist durch `UserService` erfüllt.

In `Rezepte.Web/ViewModels/UserAdminViewModel.cs` prüfen:

- Falls API-Fehler aktuell pauschal als `Anlegen fehlgeschlagen.` angezeigt werden, die `message` aus der API analog zum Profil-ViewModel übernehmen, damit Admins die zentrale deutsche Fehlermeldung sehen.

### 6. Tests ergänzen

Neue Unit-Tests:

- Datei `Rezepte.Tests/Services/Validation/UsernameValidatorTests.cs`
- Parametrisierte Tests für gültige Namen:
  - `max_mustermann`
  - `anna-2026`
  - `kochbuchFan1`
- Parametrisierte Tests für ungültige Namen:
  - leer, nur Leerraum, zu kurz, länger als 20 Zeichen
  - Leerzeichen, Emojis, `/`, `@`, `#`, `%`
  - `admin`, `Admin`, `ADMIN`, `root`, `support`, `guest`, `test`, `null`
  - `administrator`, `moderator`, `superuser`, `owner`, `help`, `contact`, `info`, `about`, `login`, `signup`, `me`, `you`, `self`, `someone`, `anyone`
  - `rezepte`, `rezepteapp`, `rezepte-admin`, `rezepte_support`, `webmaster`
  - `127.0.0.1`, `192.168.1.1`, `example.com`, `rezepte.local`
  - `support_team`, `security_admin`, `admin-support`, `microsoftsupport`
  - `adm1n`, `r00t`, `supp0rt`

Bestehende `Rezepte.Tests/Services/UserServiceTests.cs` anpassen:

- Test-Factory `CreateSut` mit `new UsernameValidator()` erweitern.
- Tests ergänzen, dass `RegisterAsync`, `UpdateProfileAsync` und `UpdateUserAsync` reservierte oder ungültige Namen ablehnen.
- Bestehenden Duplikat-Test erhalten und auf deutsche Fehlermeldung anpassen, falls die Meldung vereinheitlicht wird.
- Bestehende Positivtests mit gültigen Namen beibehalten oder bei Bedarf Namen wählen, die den neuen Regeln entsprechen.

### 7. Verifikation

Ausführen:

```powershell
dotnet test
```

Bei Fehlern in bestehenden Tests prüfen, ob sie durch die neue Konstruktorabhängigkeit, geänderte deutsche Fehlermeldungen oder durch absichtlich strengere Username-Regeln verursacht werden.

## Nicht umsetzen

- Keine Migration zur Änderung von `User.Username` von 64 auf 20 Zeichen.
- Keine automatische Umbenennung oder Deaktivierung bestehender Benutzer.
- Keine Änderung der Login-Suche auf case-insensitive Verhalten.
- Keine Admin-Oberfläche zur Pflege der Sperrliste.
- Keine vollständige redaktionelle Liste beleidigender Begriffe; nur eine initiale erweiterbare technische Grundlage.

## Risiken und Gegenmaßnahmen

| Risiko | Gegenmaßnahme |
|--------|---------------|
| Ähnlichkeitsprüfung blockiert legitime Namen | Nur konservative Normalisierung gegen wenige Hochrisiko-Namen, keine breite Distanzprüfung. |
| Controller/UI zeigen abweichende Meldungen | Username-Regeln nur im Service erzwingen und API-`message` in Admin-UI auswerten. |
| Bestehende Tests brechen durch DI-Änderung | Test-Factory zentral anpassen und Validator direkt injizieren. |
| Domains/IPs erhalten nur allgemeine Zeichenfehlermeldung | Domain-/IP-Prüfung vor der Zeichenprüfung ausführen. |
| Sperrlisten werden später an mehreren Stellen gepflegt | Alle Listen in `UsernameValidator` bzw. einer dedizierten Options-/Listenstruktur sammeln. |

## Offene Punkte

Keine.
