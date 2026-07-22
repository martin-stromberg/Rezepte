# Anforderung

## Aufgabe

Fehler bei KI-Plugins beheben.

## Kontext

Seit der Umstellung der API-Keys fuer Gemini und Google Applications funktioniert die KI-gestuetzte Rezepterfassung nicht mehr.

Auf dem Linux-Server wurden in der Datei `rezepte.service` folgende Environment-Eintraege gesetzt:

```ini
Environment=GOOGLE_GEMINI_API_KEY={hier steht der Key, der urspruenglich aus einer Datei im Programmverzeichnis gelesen wurde}
Environment=GOOGLE_APPLICATION_CREDENTIALS=/etc/rezepte/secrets/google.application-credentials.json
```

## Problem

Die Rezepterfassung mit KI schlaegt nach der Umstellung fehl. Aus den vorhandenen Logs ist nicht ersichtlich, woran der Fehler liegt.

## Ziel

Die KI-gestuetzte Rezepterfassung soll mit den ueber die Service-Umgebung gesetzten Zugangsdaten wieder funktionieren.

## Erwartetes Verhalten

- Der Gemini-API-Key wird korrekt aus der Umgebungsvariable `GOOGLE_GEMINI_API_KEY` gelesen.
- Die Google-Application-Credentials werden korrekt ueber `GOOGLE_APPLICATION_CREDENTIALS` verwendet.
- Die KI-gestuetzte Rezepterfassung funktioniert auf dem Linux-Server wieder.
- Fehler beim Initialisieren oder Verwenden der KI-Plugins werden nachvollziehbar geloggt.
- Die Logs enthalten ausreichend Informationen, um Konfigurations-, Berechtigungs- oder Initialisierungsfehler zu erkennen.

## Akzeptanzkriterien

- Bei korrekt gesetztem `GOOGLE_GEMINI_API_KEY` kann die Anwendung Gemini fuer die Rezepterfassung verwenden.
- Bei korrekt gesetztem `GOOGLE_APPLICATION_CREDENTIALS` kann die Anwendung Google-Dienste fuer die Rezepterfassung verwenden.
- Die bisherige Key-Quelle aus einer Datei im Programmverzeichnis ist nicht mehr zwingend erforderlich, wenn die Environment-Variablen gesetzt sind.
- Fehlende, nicht lesbare oder ungueltige Zugangsdaten werden mit einer klaren Fehlermeldung protokolliert.
- Fehler in der KI-Rezepterfassung verschwinden nicht stillschweigend oder nur mit nichtssagenden Logeintraegen.
- Die Anwendung laesst sich weiterhin lokal oder in anderen Umgebungen betreiben, ohne dass bestehende Konfigurationswege unbeabsichtigt brechen.

## Randbedingungen

- Die Anwendung laeuft auf einem Linux-Server als systemd-Service.
- Die Datei mit den Google-Application-Credentials liegt unter `/etc/rezepte/secrets/google.application-credentials.json`.
- Der Gemini-API-Key wird nicht mehr aus einer Datei im Programmverzeichnis bereitgestellt, sondern ueber `GOOGLE_GEMINI_API_KEY`.
- Secrets duerfen nicht im Log ausgegeben werden.

## Offene Punkte

- Welche konkrete Fehlermeldung oder welches Verhalten tritt bei der KI-Rezepterfassung auf?
- Welche Google-Application-Funktion wird bei der Rezepterfassung verwendet?
- Soll die alte dateibasierte Gemini-Key-Konfiguration als Fallback erhalten bleiben?
