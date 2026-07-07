# Testbestand und Testbedarf

## Vorhandene Tests

`Rezepte.Tests/Services/UserServiceTests.cs` deckt aktuell ab:

- erster registrierter Benutzer wird Admin
- Registrierung scheitert bei bereits vergebenem exakt gleichem Benutzernamen
- Login mit gueltigem/ungueltigem Passwort
- Profilupdate mit gueltigem Namen und E-Mail
- Passwortaenderung
- Admin-Flag per `UpdateUserAsync`
- Benutzerloeschung

Diese Tests pruefen den zentralen Service und sind daher ein guter Ort fuer Integrationsabdeckung der neuen Validierung.

## Fehlende Abdeckung bezogen auf die Anforderung

Es fehlen Tests fuer:

- gueltige Namen wie `max_mustermann`, `anna-2026`, `kochbuchFan1`
- leere, nur aus Leerraum bestehende und fehlende Namen
- Namen unter 3 und ueber 20 Zeichen
- ungueltige Zeichen, Leerzeichen, Emojis und Sonderzeichen
- reservierte Namen mit Gross-/Kleinschreibung (`admin`, `Admin`, `ADMIN`)
- App-/Domain-/Rollennahe Namen (`rezepte`, `rezepteapp`, `rezepte-admin`, `rezepte_support`)
- IP-Adressen und Domains (`127.0.0.1`, `example.com`)
- offiziell wirkende Namen (`support_team`, `security_admin`, `admin-support`, `microsoftsupport`)
- missbraeuchliche Begriffe anhand einer initialen Liste
- Umgehungsschreibweisen (`adm1n`, `r00t`, `supp0rt`)
- Beibehaltung der bestehenden Eindeutigkeitspruefung
- Anwendung derselben Regeln in `RegisterAsync`, `UpdateProfileAsync` und `UpdateUserAsync`

## Empfohlene Teststruktur

1. Neue Unit-Tests fuer den Validator, z. B. `Rezepte.Tests/Services/Validation/UsernameValidatorTests.cs`.
2. Parametrisierte Tests fuer viele Namen und erwartete Fehlermeldungen.
3. Ergaenzende `UserServiceTests`, die sicherstellen, dass alle Schreibwege den Validator nutzen:
   - `RegisterAsync_ShouldFail_WhenUsernameReserved`
   - `UpdateProfileAsync_ShouldFail_WhenUsernameInvalid`
   - `UpdateUserAsync_ShouldFail_WhenUsernameInvalid`
   - bestehender Duplikat-Test bleibt bestehen

## Controller-/UI-Tests

Im Bestand sind keine Controller-Integrationstests fuer Auth/Admin/User-API erkennbar. Fuer diese Anforderung sind sie nicht zwingend, solange die zentrale Service-/Validator-Abdeckung stark ist. Sinnvoll waere ein kleiner Controller-Test nur dann, wenn die Planung die Controller-Vorvalidierung wesentlich umbaut oder Fehlertexte fuer API-Antworten explizit absichert.

