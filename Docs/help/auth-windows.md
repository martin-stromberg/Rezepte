# Autorisierung im interaktiven Server-Modus

## Hintergrund

Die Blazor-Server-Anwendung läuft im interaktiven Server-Modus. Jede Seite wird beim ersten Laden zweimal initialisiert:

1. Während des Server-Prerenderings existiert ein `HttpContext`, und der `IHttpContextAccessor` enthält den authentifizierten Benutzer.
2. Nach Abschluss des Prerenderings wird ein Blazor-Circuit aufgebaut. Ab diesem Zeitpunkt steht kein `HttpContext` mehr zur Verfügung; der Benutzer ist aber über `AuthenticationStateProvider` erreichbar.

## Lösung

Das Setzen des `Authorization`-Headers für interne API-Aufrufe erfolgt zentral im `CircuitAuthHandler`. Der Handler setzt den JWT-Token aus dem `HttpContext` (Prerendering) oder, falls dieser nicht vorhanden ist, aus dem `AuthenticationStateProvider` (Circuit).

## Relevante Dateien

- `Rezepte.Web/Services/CircuitAuthHandler.cs` — `DelegatingHandler`, der `Authorization: Bearer <token>` an jede API-Anfrage anhängt
- `Rezepte.Web/Extensions/ServiceCollectionExtensions.cs` — Registrierung des scoped `ApiClient` mit `CircuitAuthHandler`
- `Rezepte.Web/Services/ApiClient.cs` — typed `HttpClient`, der den Handler nutzt

## Hinweise für Betrieb

- Der `ApiClient` ist ein scoped Service und folgt dem Circuit-/Request-Lebenszyklus.
- `Rezepte.Web/Services/ApiAuthHandler.cs` ist nicht mehr in Verwendung; stattdessen wird `CircuitAuthHandler` eingesetzt.
