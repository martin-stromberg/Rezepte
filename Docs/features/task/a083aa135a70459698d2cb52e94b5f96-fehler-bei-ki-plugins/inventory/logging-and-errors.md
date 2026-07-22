# Detail: Logging und Fehlerbehandlung

## Vorhandenes Logging

`GeminiClient` loggt vor Requests:

- Modellname als Information
- Request-Body-Laenge als Debug

`BaseAIImportHandler.HandleAsync()` loggt Exceptions waehrend der Rezeptverarbeitung als Error mit Dateiname.

`ImportService` loggt:

- Importanforderung als Information
- `CanHandleAsync()`-Exceptions als Warning
- Handler-Fehler beim Import als Error

`ImportOrchestrator` loggt:

- `CanHandleAsync()`-Exceptions als Warning
- Handler-Fehler beim Import als Warning
- Session-Fehler als Error
- Confirmation-Prompts als Information

## Bestehende Fehleraufbereitung

`ImportExceptionHelper.BeautifyExceptionMessage()` extrahiert technische Details aus Exception-Meldungen und mappt einige Faelle wie Billing oder Permission auf lesbarere deutsche Meldungen.

Es gibt zusaetzlich eine private, aehnliche Methode `BeautifyExceptionMessage()` in `BaseAIImportHandler`, die aber dort nicht verwendet wird. Die aktive Fehleraufbereitung nutzt den zentralen `ImportExceptionHelper`.

## Diagnose-Luecke

Wenn ein Handler in `CanHandleAsync()` einfach `false` zurueckgibt, wird keine Ursache protokolliert. Genau das passiert bei deaktivierten KI-Handlern, zum Beispiel wenn:

- kein Service-Account-Pfad vorhanden ist
- der Pfad leer ist
- die Datei nicht existiert
- globale KI-Settings deaktiviert sind
- Benutzer-Settings deaktiviert sind
- Gemini- oder Vision-Settings deaktiviert sind

Danach lautet das Ergebnis oft nur "No suitable import plugin found for this file or URL." Das erfuellt die Anforderung nach nachvollziehbaren Initialisierungs- und Konfigurationslogs noch nicht.

## Credential-sichere Logging-Anforderungen

Secrets duerfen nicht geloggt werden. Sinnvoll und sicher waeren:

- Name der erwarteten Umgebungsvariable
- ob ein Wert vorhanden ist
- Quelle des Werts: Environment oder Konfiguration
- Service-Account-Pfad nur als Pfad, nicht Dateiinhalt
- Existenz und Lesbarkeit der Datei
- Exception-Typ und Message bei Google-Initialisierung
- Plugin-ID und Handlername

Der Gemini-API-Key selbst darf weder komplett noch teilweise geloggt werden.

## Umsetzungsschwerpunkte

- Diagnose-Logging in Credential-Provider oder in eine separate Credential-Diagnoseklasse aufnehmen.
- `CanHandleAsync()`/`IsActiveAsync()` fuer KI-Handler mit begruendeten Debug- oder Warning-Logs ausstatten.
- `GeminiClient.InitHttpClientAsync()` bei fehlender oder fehlerhafter Authentifizierung mit konkreter, aber secret-freier Fehlermeldung versehen.
- Fehler beim Bilddownload in `GeminiClient.ParseRecipe()` werden aktuell geschluckt; fuer die vorliegende Anforderung ist das zweitrangig, aber ebenfalls eine stille Fehlerstelle.
