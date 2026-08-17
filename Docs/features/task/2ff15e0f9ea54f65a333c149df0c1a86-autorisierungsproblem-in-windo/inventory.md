# Bestandsaufnahme: Autorisierungsproblem in Windows

Analysiert wurde der Authentifizierungs- und Autorisierungsstapel der Blazor-Server-Anwendung `Rezepte.Web` im Kontext der gemeldeten 401-Fehler beim zweiten `OnInitializedAsync`.

## Zusammenfassung

- `ApiAuthHandler` liest den angemelden Benutzer ausschließlich aus `IHttpContextAccessor.HttpContext.User`.
- `IHttpContextAccessor` ist nur während der initialen HTTP-Anfrage (Prerendering) befüllt, im Blazor-Circuit `null`.
- Der JWT-Token wird in `ITokenService` (Singleton-Memory-Cache) pro Benutzer-ID gespeichert.
- `AuthenticationStateProvider` ist im Circuit verfügbar und liefert den `ClaimsPrincipal`.
- `ApiClient` ist ein typed `HttpClient`, der von `IHttpClientFactory` erzeugt wird; die Message-Handler werden aus der Root-DI-Scope aufgelöst.
- Alle interaktiven Server-Komponenten und ViewModels verwenden `ApiClient` für `api/*`-Aufrufe.
- Es gibt zwei Dateien mit dem Klassennamen `ApiAuthHandler` (root-Namespace und `Rezepte.Web.Services`), was eine Doppelbelegung darstellt.

## Details

- [Logik](inventory/logic.md)
- [Interfaces](inventory/interfaces.md)
- [Tests](inventory/tests.md)
