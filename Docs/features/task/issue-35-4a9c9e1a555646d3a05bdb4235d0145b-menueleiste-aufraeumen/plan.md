# Umsetzungsplan: Menueleiste aufraeumen

## Zielbild

Die Menueleiste bleibt die zentrale Navigation der Anwendung, wird aber in der Desktopansicht kompakter und in schmalen Viewports stabiler. Der separate Menuepunkt `Start` entfaellt; die Startseite bleibt ueber den Markenlink `Rezepte` erreichbar. Im authentifizierten Zustand werden Begruessung und separater Abmelden-Link aus der Top-Level-Navigation entfernt und durch ein kompaktes Benutzermenue ersetzt. Die Einrichtung bleibt erreichbar, wird aber nur noch als Zahnradsymbol angezeigt.

## Technische Leitentscheidungen

- Die Umsetzung erfolgt primaer in `Rezepte.Web/Components/Layout/MainLayout.razor`.
- Komponentennahe Layout- und Responsive-Regeln werden in `Rezepte.Web/Components/Layout/MainLayout.razor.css` ergaenzt.
- Es werden keine Backend-, Datenmodell- oder Migrationsaenderungen geplant.
- Das Benutzer-Popup nutzt die vorhandenen Auth-Claims aus `AuthorizeView`:
  - Benutzername ueber `context.User.Identity?.Name`.
  - Benutzer-ID ueber `ClaimTypes.NameIdentifier`, sofern vorhanden.
  - Rolle/Adminstatus ueber `context.User.IsInRole("Admin")`.
- E-Mail und Erstellungsdatum werden nicht im Popup eingeplant, weil sie aktuell nicht als Claims im Layout verfuegbar sind.
- Das bestehende Logout-Formular bleibt semantisch erhalten und wird nur in das Dropdown verschoben.
- Bootstrap 5.3.3 und Bootstrap Icons werden genutzt; neue UI-Abhaengigkeiten sind nicht erforderlich.

## Arbeitspakete

### 1. Navbar-Struktur bereinigen

Datei: `Rezepte.Web/Components/Layout/MainLayout.razor`

- Den `NavLink` fuer `Start` vollstaendig aus der linken Navigation entfernen.
- Den vorhandenen Markenlink `Rezepte` mit `href="/"` beibehalten.
- Sicherstellen, dass der Markenlink weiterhin als klickbares, gut erkennbares Element in der Navbar steht.
- Die bestehenden Links fuer `Kochbuecher` und `Kalender` unveraendert funktionsfaehig halten.

### 2. Suchformular mobil stabilisieren

Datei: `Rezepte.Web/Components/Layout/MainLayout.razor`

- Das Suchformular mit responsiven Bootstrap-Klassen versehen, sodass es in der mobilen Collapse-Ansicht kontrolliert umbrechen kann.
- Vorgeschlagene Klassen:
  - `w-100 w-lg-auto`
  - `my-2 my-lg-0`
  - `ms-lg-3`
  - `me-lg-2`
- Eingabefeld und Suchbutton bleiben nebeneinander, duerfen aber die Navbar nicht horizontal ueberdehnen.
- Die bestehende Suchlogik `OnSubmitSearch` bleibt unveraendert.

### 3. Einrichtung als Icon-only Link umsetzen

Datei: `Rezepte.Web/Components/Layout/MainLayout.razor`

- Den Settings-Link im authentifizierten Bereich weiterhin nach `/settings` fuehren.
- Den sichtbaren Text `Einrichtung` entfernen.
- Das Zahnrad als Bootstrap Icon umsetzen, z. B. `bi bi-gear-fill`.
- `title="Einrichtung"` und `aria-label="Einrichtung"` setzen, damit die Funktion trotz icon-only Darstellung zugaenglich bleibt.
- Eine kompakte Klasse fuer Icon-Links verwenden, z. B. `nav-icon-link`.

### 4. Benutzermenue einfuehren

Datei: `Rezepte.Web/Components/Layout/MainLayout.razor`

- Die sichtbare Begruessung `Hallo, ...` aus der Top-Level-Navigation entfernen.
- Den separaten Top-Level-Abmelden-Button entfernen.
- Im `Authorized`-Block ein Bootstrap-Dropdown mit Benutzerlogo einfuehren:
  - Trigger als Button, nicht als Navigationslink.
  - Icon z. B. `bi bi-person-circle`.
  - `data-bs-toggle="dropdown"`.
  - `aria-expanded="false"`.
  - `aria-label="Benutzermenue oeffnen"`.
- Das Dropdown rechts ausrichten, z. B. mit `dropdown-menu dropdown-menu-end`.
- Im Dropdown anzeigen:
  - Benutzername.
  - Benutzer-ID, wenn der Claim vorhanden ist.
  - Rolle `Admin` oder `Benutzer`, abgeleitet aus `IsInRole("Admin")`.
- Das vorhandene Logout-Formular in das Dropdown verschieben:
  - `action="api/session/logout?returnUrl=/login"`
  - `method="post"`
  - `formname="logout"`
  - Submit-Button optisch als Dropdown-Item.
- Der `NotAuthorized`-Block mit dem Link `Anmelden` bleibt erhalten.

### 5. CSS fuer kompakte und responsive Navbar ergaenzen

Datei: `Rezepte.Web/Components/Layout/MainLayout.razor.css`

- Klassen fuer kompakte Icon-Bedienelemente ergaenzen:
  - `.nav-icon-link`
  - `.nav-user-button`
- Fuer Icon-Buttons stabile Mindestabmessungen setzen, damit Maus- und Touch-Bedienung moeglich bleibt.
- Dropdown-Inhalte so stylen, dass lange Benutzernamen nicht aus dem Menue laufen.
- Unterhalb des Bootstrap-`lg`-Breakpoints (`max-width: 991.98px`) mobile Regeln ergaenzen:
  - Suchformular auf volle Breite.
  - Rechte Navbar kontrolliert ausrichten.
  - Dropdown nicht ausserhalb des Viewports positionieren.

### 6. Namespaces fuer Claims ergaenzen

Datei: `Rezepte.Web/Components/Layout/MainLayout.razor`

- `@using System.Security.Claims` ergaenzen, falls `ClaimTypes.NameIdentifier` direkt im Markup verwendet wird.
- Optional lokale Razor-Ausdruecke schlank halten, indem wiederholte Claim-Zugriffe in kleine Variablen im `Authorized`-Block ausgelagert werden.

## Akzeptanzkriterien-Abdeckung

| Akzeptanzkriterium | Umsetzung |
| --- | --- |
| Kein sichtbarer Menuepunkt `Start` | Entfernen des `Start`-`NavLink`. |
| Klick auf Anwendungsnamen fuehrt zur Startseite | Bestehender Markenlink `href="/"` bleibt erhalten. |
| Keine separate Begruessung und kein separater Logout-Link | Beide Top-Level-Elemente werden aus dem `Authorized`-Block entfernt. |
| Benutzerlogo sichtbar | Dropdown-Trigger mit `bi-person-circle`. |
| Klick auf Benutzerlogo oeffnet Popup-Menue | Bootstrap-Dropdown mit vorhandenem Bootstrap Bundle. |
| Popup zeigt Benutzerangaben | Benutzername, Benutzer-ID falls vorhanden, Rolle/Adminstatus aus Claims. |
| Popup bietet Abmelden | Bestehendes Logout-POST-Formular im Dropdown. |
| Einrichtung nur als Zahnrad | Settings-Link mit Bootstrap-Zahnrad ohne sichtbaren Text. |
| Zahnrad oeffnet weiterhin Einrichtung | `NavLink href="/settings"` bleibt erhalten. |
| Mobile Navbar ohne Layoutfehler | Responsive Klassen und CSS fuer Suche, rechte Navigation und Dropdown. |
| Desktop rechts kompakter | Begruessung, Logout-Text und Settings-Text entfallen zugunsten zweier Icons. |

## Verifikation

### Automatisiert

- `dotnet build`
- `dotnet test`

### Manuell im Browser

- Desktopansicht:
  - `Start` ist nicht sichtbar.
  - Klick auf `Rezepte` navigiert nach `/`.
  - Rechts sind nur noch Zahnrad und Benutzerlogo sichtbar.
  - Zahnrad navigiert nach `/settings`.
  - Benutzerlogo oeffnet ein rechts ausgerichtetes Dropdown.
  - Dropdown zeigt die verfuegbaren Benutzerangaben.
  - Abmelden im Dropdown meldet ab und fuehrt nach `/login`.
- Mobile Ansicht:
  - Navbar-Toggler oeffnet und schliesst die Navigation.
  - Suchformular bleibt bedienbar.
  - Es entsteht kein horizontaler Scrollbalken durch die Navbar.
  - Benutzerlogo und Zahnrad sind per Touch bedienbar.
  - Dropdown bleibt im sichtbaren Bereich.

## Risiken und Gegenmassnahmen

| Risiko | Gegenmassnahme |
| --- | --- |
| Bootstrap-Dropdown funktioniert im Blazor-Layout nicht wie erwartet | Zuerst Bootstrap-Markup verwenden, weil das Bundle bereits eingebunden ist; bei Problemen auf Blazor-State-basiertes Dropdown ausweichen. |
| Benutzerangaben wirken unvollstaendig | Nur tatsaechlich verfuegbare Claims anzeigen und keine Backend-Erweiterung fuer E-Mail einplanen. |
| Mobile Suche erzeugt weiter horizontalen Druck | Zusaetzlich zu Bootstrap-Klassen komponentennahe Media-Query in `MainLayout.razor.css` setzen. |
| Logout-Styling im Dropdown bricht Formularsemantik | Formular unveraendert lassen und nur Button-Klassen an Dropdown-Optik anpassen. |

## Nicht Bestandteil der Umsetzung

- Keine Aenderung der Startseitenlogik.
- Keine Aenderung der Logout-Backendlogik.
- Keine Aenderung der Settings-Seite.
- Keine Erweiterung der Login-Claims um E-Mail oder weitere Profildaten.
- Keine Einfuehrung neuer UI-Testpakete.

## Offene Punkte

Keine.
