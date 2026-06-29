# Detail: Benutzerbereich und Authentifizierung

## Aktueller Auth-Bereich

Die Menueleiste nutzt `AuthorizeView` in `MainLayout.razor`.

Im `Authorized`-Block werden aktuell gerendert:

| Zeile | Element | Bedeutung |
| --- | --- | --- |
| 42-44 | `Hallo, @context.User.Identity?.Name` | Sichtbare Begruessung in der Navbar. |
| 45-49 | Logout-Formular | POST nach `api/session/logout?returnUrl=/login`. |
| 50-55 | Settings-Link | Navigiert nach `/settings`, zeigt Zahnrad und Text. |

Im `NotAuthorized`-Block wird `Anmelden` nach `/login` angezeigt.

## Verfuegbare Benutzerdaten im UI-Kontext

`SessionController.Login` legt beim Login folgende Claims an:

| Quelle | Claim | Verfuegbarkeit |
| --- | --- | --- |
| Zeile 38 | `ClaimTypes.NameIdentifier` | Benutzer-ID. |
| Zeile 39 | `ClaimTypes.Name` | Benutzername. |
| Zeile 43 | `ClaimTypes.Role = Admin` | Nur bei Admin-Benutzern. |

Damit sind im `AuthorizeView` sicher verfuegbar:

- Benutzername ueber `context.User.Identity?.Name`.
- Benutzer-ID ueber `FindFirst(ClaimTypes.NameIdentifier)`.
- Adminstatus ueber `IsInRole("Admin")`.

## Datenmodell

`Rezepte.Web/Entities/User.cs` enthaelt:

- `Id`
- `Username`
- `Email`
- `PasswordHash`
- `IsAdmin`
- `CreatedAt`

Einschraenkung: `Email` und `CreatedAt` werden nicht als Claims in das Cookie geschrieben. Ohne zusaetzlichen Datenabruf oder Claim-Erweiterung stehen diese Werte im Layout nicht direkt zur Verfuegung.

Die Anforderung sagt, dass das Popup-Menue die aktuell verfuegbaren Benutzerdaten zeigen soll. Daher reicht ein Menue mit Benutzername und optional Rolle/Adminstatus aus, solange kein zusaetzlicher Datenabruf geplant wird.

## Logout

Der vorhandene Logout-Pfad ist:

```html
<form action="api/session/logout?returnUrl=/login" method="post" class="d-inline" formname="logout">
```

Der Endpunkt:

- `POST api/session/logout`
- `IgnoreAntiforgeryToken`
- ruft `HttpContext.SignOutAsync(...)` auf.
- leitet nach `/login` oder `returnUrl` weiter.

Dieses Formular kann semantisch unveraendert ins Dropdown verschoben werden. Dadurch bleibt das bestehende Logout-Verhalten erhalten.

## Benutzerlogo

Es gibt kein vorhandenes Avatarbild oder Benutzerprofilbild. Ein Benutzerlogo sollte daher als generisches Icon umgesetzt werden, z. B. mit Bootstrap Icons:

- `bi-person-circle`
- `bi-person-fill`

Bootstrap Icons sind in `App.razor` eingebunden.

Empfohlene Accessibility-Merkmale:

- Button statt Link, wenn nur das Dropdown geoeffnet wird.
- `aria-expanded` gemaess Bootstrap-Dropdown.
- `aria-label="Benutzermenue oeffnen"`.
- Sichtbarer oder screenreader-only Benutzername im Dropdown.

## Settings-Link

Die Settings-Seite:

- Route `/settings`
- `[Authorize]`
- `@rendermode InteractiveServer`

Der Link kann in der Menueleiste auf ein icon-only Element reduziert werden:

- `href="/settings"`
- `title="Einrichtung"`
- `aria-label="Einrichtung"`
- Zahnrad mit Bootstrap Icon oder bestehendem Symbol.

Der sichtbare Text `Einrichtung` muss entfernt werden.

## Umsetzungsspielraum

Minimaler Umbau im `Authorized`-Block:

- Begruessungs-`li` entfernen.
- Logout-`li` entfernen und Formular in Dropdown-Menue einsetzen.
- Settings-Link direkt neben Benutzerlogo belassen, nur icon-only.
- Benutzerlogo mit Bootstrap-Dropdown einfuehren.

Das reduziert den rechten Bereich deutlich und erfuellt die Akzeptanzkriterien ohne Backend-Aenderung.
