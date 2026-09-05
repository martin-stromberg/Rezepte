# Benutzerkonten

## Passwort-Policy

Passwörter werden serverseitig mit PBKDF2-HMAC-SHA256 gehasht gespeichert. Das gespeicherte Format ist `iterationen.salt.hash` (Salt und Hash hexadezimal kodiert).

Für die Hash-Parameter gelten feste Grenzen (`PasswordHasher`):

- **Aktuelle Iterationszahl:** 210.000 (wird für alle neuen Hashes verwendet)
- **Minimale Iterationszahl:** 100.000 (Hashes mit weniger Iterationen werden bei der Anmeldung abgelehnt)
- **Maximale Iterationszahl:** 1.000.000 (Hashes mit mehr Iterationen werden abgelehnt, um die Verifikationskosten zu begrenzen)
- **Salt-Länge:** 16 Bytes, **Hash-Länge:** 32 Bytes

Bei der Registrierung muss das Passwort mindestens 6 Zeichen lang sein.

### Automatische Aktualisierung (Rehash beim Login)

Wird ein Passwort erfolgreich gegen einen Hash mit veralteten Parametern (weniger als 210.000 Iterationen) geprüft, erzeugt die Anwendung beim Login automatisch einen neuen Hash mit den aktuellen Parametern. Ältere Konten werden so schrittweise auf die aktuelle Policy angehoben, ohne dass eine Aktion durch den Benutzer erforderlich ist.

## Registrierung

Die Registrierung prüft Benutzernamen serverseitig. Ein Benutzername muss 3 bis 20 Zeichen lang sein und darf nur Buchstaben, Zahlen, Unterstrich und Bindestrich enthalten.

Leerzeichen, Emojis, Domains, IP-Adressen und sonstige Sonderzeichen werden abgelehnt. Reservierte oder offiziell wirkende Namen wie `admin`, `root`, `support`, `security_admin` oder appbezogene Namen wie `rezepte` können nicht verwendet werden.

Wenn ein Benutzername nicht erlaubt ist oder bereits vergeben wurde, zeigt die Anwendung eine deutschsprachige Fehlermeldung an. Wählen Sie in diesem Fall einen anderen Namen.

## Profil

Im Profil kann der eigene Benutzername geändert werden. Für die Änderung gelten dieselben Regeln wie bei der Registrierung.

Nach einer erfolgreichen Änderung kann eine erneute Anmeldung nötig sein, damit der neue Name in der Navigation angezeigt wird.

## Benutzerverwaltung für Administratoren

Administratoren können Benutzer in den Einstellungen unter `Benutzer` anlegen und bearbeiten. Auch dort werden Benutzernamen serverseitig mit denselben Regeln geprüft.

Die Prüfung ersetzt nicht die Eindeutigkeitsprüfung. Ein technisch gültiger Benutzername wird weiterhin abgelehnt, wenn er bereits von einem anderen Benutzer verwendet wird.
