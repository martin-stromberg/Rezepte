# Umsetzungsplan: Autorisierungsproblem in Windows

## Übersicht

Die Autorisierung von API-Aufrufen aus interaktiven Blazor-Server-Komponenten wird von einem ausschließlich `IHttpContextAccessor`-basierten `DelegatingHandler` auf einen scoped `CircuitAuthHandler` umgestellt, der zusätzlich den `AuthenticationStateProvider` verwendet. `ApiClient` wird ebenfalls `Scoped`, damit der Handler im aktuellen Circuit-/Request-Scope aufgelöst wird.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Auth-Handler | `CircuitAuthHandler` als neuer scoped `DelegatingHandler` | `IHttpClientFactory` löst Message-Handler aus dem Root-Scope auf; damit steht der circuit-scoped `AuthenticationStateProvider` nicht zur Verfügung. Ein in der `ApiClient`-Factory erzeugter scoped Handler greift auf den aktuellen Scope zu. |
| `ApiClient`-Registrierung | `AddScoped<ApiClient>` mit Custom Factory | Ermöglicht, einen `HttpClient` mit einem aus dem aktuellen Scope gelösten `CircuitAuthHandler` zu bauen. |
| Token-Quelle | `IHttpContextAccessor` bevorzugt, dann `AuthenticationStateProvider` | Im Prerendering ist `IHttpContextAccessor` synchron verfügbar; im Circuit liefert `AuthenticationStateProvider` den Benutzer. |
| Duplikat `ApiAuthHandler` | Nicht genutzte `Rezepte.Web/ApiAuthHandler.cs` entfernen | Reduziert Verwirrung und Dead Code. Der aktive Handler ist `Rezepte.Web.Services.ApiAuthHandler`; er wird durch `CircuitAuthHandler` ersetzt/ergänzt. |

## Programmabläufe

### Auth-Header an einer ausgehenden API-Anfrage setzen

1. `ApiClient` wird im Circuit-/Request-Scope erzeugt.
2. Die Factory holt `IHttpMessageHandlerFactory` und baut den inneren Handler-Pipeline (nur `SocketsHttpHandler` plus optional `AntiForgeryHandler`) für den Namen `ApiClient`.
3. Ein `CircuitAuthHandler` wird mit `IHttpContextAccessor`, `AuthenticationStateProvider` und `ITokenService` aus dem aktuellen Scope erzeugt und als äußerster Handler um die Pipeline gelegt.
4. Eine ViewModel-Methode ruft z. B. `_http.GetAsync("api/users/me")` auf.
5. `CircuitAuthHandler.SendAsync` wird aufgerufen.
6. `CircuitAuthHandler` prüft `IHttpContextAccessor.HttpContext?.User`; ist keiner authentifiziert, wird `AuthenticationStateProvider.GetAuthenticationStateAsync()` abgefragt.
7. Aus dem `ClaimsPrincipal` wird die Benutzer-ID, der Benutzername und ggf. die Admin-Rolle gelesen.
8. `ITokenService.GetToken(userId)` liefert einen gecachten JWT; falls keiner vorhanden ist, wird `ITokenService.CreateToken(...)` erzeugt.
9. `Authorization: Bearer <token>` wird am `HttpRequestMessage` gesetzt.
10. Die Anfrage durchläuft den Rest der Pipeline und wird an den API-Controller weitergeleitet.

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `CircuitAuthHandler` | Klasse (DelegatingHandler) | Setzt den JWT-Bearer-Header aus `HttpContext` oder `AuthenticationStateProvider`. |

## Änderungen an bestehenden Klassen

### `Rezepte.Web.Services.ApiClient`

- **Neue Eigenschaften:** Keine.
- **Geänderte Registrierung:** Statt `AddHttpClient<ApiClient>()` wird `AddScoped<ApiClient>()` mit einer Custom Factory verwendet.
- **Optional `IDisposable`:** `ApiClient` implementiert `IDisposable`, um den erzeugten `HttpClient` und damit die Handler-Pipeline am Ende des Scopes zu disposen.

### `Rezepte.Web.Extensions.ServiceCollectionExtensions`

- **Geänderte Registrierung:**
  - `AddHttpClient<ApiClient>()` entfernt.
  - `AddHttpClient("ApiClient")` für den benamten Handler-Pool (optional mit `AntiForgeryHandler`) hinzugefügt.
  - `AddScoped<ApiClient>()` mit Custom Factory hinzugefügt.
  - `AddScoped<CircuitAuthHandler>()` hinzugefügt.
  - `AddTransient<ApiAuthHandler>()` entfernt, falls `ApiAuthHandler` vollständig ersetzt wird.

### `Rezepte.Web/ApiAuthHandler.cs`

- **Gelöscht:** Unbenutzte Datei im globalen Namespace, Duplikat zu `Rezepte.Web.Services.ApiAuthHandler`.

### `Rezepte.Web.ViewModels.UserProfileViewModel`

- **Rückgängig:** Der zuvor schnell eingefügte `EnsureAuthorizationAsync`-Helfer und die zusätzlichen Konstruktorparameter werden entfernt, weil der `CircuitAuthHandler` die Auth nun zentral übernimmt.

## Datenbankmigrationen

Keine.

## Validierungsregeln

Keine.

## Konfigurationsänderungen

Keine.

## Seiteneffekte und Risiken

- `ApiClient` ist kein typed `IHttpClientFactory`-Client mehr; die Lebensdauer ist an den Circuit/Request gebunden. Das ist für eine Blazor-Server-App mit wenigen gleichzeitigen Nutzern vertretbar, sollte aber bei massiv parallelen Szenarien geprüft werden.
- `HttpClient` und `HttpMessageHandler` werden am Ende des Scopes disposed; die `IHttpMessageHandlerFactory` verwaltet die zugrundeliegenden `SocketsHttpHandler` und Connections.
- Alle ViewModels, die `ApiClient` injizieren, erhalten weiterhin eine funktionierende Instanz ohne Codeänderungen.
- `AntiForgeryHandler` bleibt optional im inneren Handler; im Circuit fügt er keinen Token hinzu, was für API-Routen aber unproblematisch ist, da diese Anti-Forgery global ignorieren.

## Umsetzungsreihenfolge

1. **Duplikat `ApiAuthHandler` entfernen**
   - Voraussetzungen: Keine.
   - Beschreibung: `Rezepte.Web/ApiAuthHandler.cs` löschen und ggf. den Build prüfen.

2. **`CircuitAuthHandler` anlegen**
   - Voraussetzungen: `Rezepte.Web.Services.ITokenService` und `Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider` verfügbar.
   - Beschreibung: Neue Klasse `CircuitAuthHandler` in `Rezepte.Web/Services/CircuitAuthHandler.cs` anlegen. Er liest `ClaimsPrincipal` aus `IHttpContextAccessor` oder `AuthenticationStateProvider` und setzt `Authorization: Bearer <token>`.

3. **`ApiClient`-Registrierung umstellen**
   - Voraussetzungen: `CircuitAuthHandler` und `IHttpMessageHandlerFactory` verfügbar.
   - Beschreibung: In `ServiceCollectionExtensions` `AddHttpClient<ApiClient>()` durch `AddHttpClient("ApiClient")` und `AddScoped<ApiClient>()` mit Custom Factory ersetzen. `AddScoped<CircuitAuthHandler>()` registrieren.

4. **`ApiClient` optional `IDisposable` machen**
   - Voraussetzungen: `ApiClient`-Registrierung umgestellt.
   - Beschreibung: `ApiClient` implementiert `IDisposable` und disposet `Http`.

5. **Quick-Fix in `UserProfileViewModel` rückgängig machen**
   - Voraussetzungen: `ApiClient` zentral authentifiziert.
   - Beschreibung: Zusätzliche Konstruktorparameter und `EnsureAuthorizationAsync` entfernen.

6. **Build prüfen**
   - Voraussetzungen: Alle Code-Änderungen durchgeführt.
   - Beschreibung: `dotnet build Rezepte.Web` ausführen.

7. **Tests ergänzen**
   - Voraussetzungen: Kompilierbarer Code.
   - Beschreibung: Unit-Tests für `CircuitAuthHandler` mit Fake-`AuthenticationStateProvider` und Fake-`IHttpContextAccessor` anlegen; ggf. Integrationstest für `ApiClient`-Factory.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `CircuitAuthHandler_SetAuthorizationFromHttpContext` | `CircuitAuthHandlerTests` | Prüft, dass der Header aus `IHttpContextAccessor` gesetzt wird. |
| `CircuitAuthHandler_SetAuthorizationFromAuthenticationStateProvider` | `CircuitAuthHandlerTests` | Prüft, dass der Header aus `AuthenticationStateProvider` gesetzt wird, wenn `HttpContext` null ist. |
| `CircuitAuthHandler_LeavesAnonymousRequestsUnchanged` | `CircuitAuthHandlerTests` | Sicherstellen, dass keine Header an nicht authentifizierte Anfragen angehängt werden. |

### Betroffene bestehende Tests

Keine.

### E2E-Tests

| Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium |
|----------|------------------------|-------------------------------|
| Angemeldeter Benutzer ruft Profil-Seite in interaktivem Server-Modus auf | `Rezepte.Tests.Browser` (bestehend) | Keine 401-Antwort nach dem zweiten `OnInitializedAsync` mehr. |

Bestehende E2E-Tests müssen nicht geändert werden.

## Offene Punkte

Keine.
