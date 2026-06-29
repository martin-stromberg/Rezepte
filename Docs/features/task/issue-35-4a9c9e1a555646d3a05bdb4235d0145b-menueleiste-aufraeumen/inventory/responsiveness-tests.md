# Detail: Responsiveness, Styles und Tests

## Bootstrap-Einbindung

`Rezepte.Web/Components/App.razor` bindet Bootstrap ein:

| Zeile | Einbindung |
| --- | --- |
| 15 | Bootstrap 5.3.3 CSS. |
| 20 | Bootstrap Icons 1.11.3. |
| 42 | Bootstrap 5.3.3 Bundle mit interaktiven Komponenten. |

Damit sind Navbar-Collapse, Dropdowns und Icons ohne neue Abhaengigkeiten nutzbar.

## Bestehende Navbar-Styles

Globale Navbar-Regeln liegen in `Rezepte.Web/wwwroot/app.css`:

| Zeile | Regel |
| --- | --- |
| 27-28 | `.app-navbar` setzt den Verlaufshintergrund. |
| 30 | `.app-navbar .navbar-brand` setzt Brand-Farbe. |
| 31 | `.app-navbar .nav-link` setzt Link-Farbe. |
| 32 | Active-/Hover-Farbe fuer Navlinks. |

`MainLayout.razor.css` enthaelt derzeit keine spezifischen Regeln fuer Navbar-Layout, Dropdowns, Avatar-Button oder mobile Suchleiste. Das ist der passende Ort fuer komponentennahe Regeln, wenn die Bootstrap-Klassen nicht ausreichen.

## Mobile Ist-Situation

Die Navbar nutzt `navbar-expand-lg`. Auf Viewports unterhalb `lg` wird die Navigation eingeklappt. Das erfuellt die Grundvoraussetzung fuer mobile Navigation.

Potenzielle Schwachstellen:

- Das Suchformular nutzt `d-flex ms-3 me-2` ohne responsive Breiten-/Margin-Anpassung.
- Linke Navigation, Suche und rechter Bereich liegen alle in derselben Collapse-Region.
- Im rechten Bereich stehen aktuell lange Texte (`Hallo, ...`, `Abmelden`, `Einrichtung`), die in der mobilen Collapse-Ansicht viel Breite und Hoehe beanspruchen.

Die geforderte Kompaktierung reduziert vor allem den rechten Bereich. Fuer mobile Stabilitaet sollte die Suche trotzdem explizit behandelt werden.

## Empfohlene responsive Anpassungen

Moegliche Bootstrap-Klassen:

- Suchformular: `w-100 w-lg-auto`, `my-2 my-lg-0`, `ms-lg-3`, `me-lg-2`.
- Rechte Navbar: `align-items-lg-center`, auf mobile vertikal mit kontrollierten Abstaenden.
- Dropdown: `dropdown-menu-lg-end` oder `dropdown-menu-end`; mobile Darstellung pruefen.
- Icon-Buttons: feste Mindestgroesse oder `btn-sm`/`nav-link` konsistent nutzen.

Moegliche isolierte CSS-Regeln:

- `.nav-user-button` fuer rundes Benutzerlogo.
- `.nav-icon-link` fuer Settings-Zahnrad.
- Media Query unterhalb `992px`, um Suchformular und rechte Navigation auf volle Breite zu bringen.

## Testlage

Das Testprojekt `Rezepte.Tests` nutzt:

- xUnit
- FluentAssertions
- Moq
- EF Core InMemory

Es gibt keine sichtbare UI-Test-Infrastruktur:

- Kein bUnit-Paket.
- Kein Playwright-Projekt.
- Keine Razor-Komponententests fuer `MainLayout`.

Deshalb sind automatisierte Tests fuer Markup-/Responsive-Verhalten im bestehenden Setup nicht direkt vorhanden.

## Sinnvolle Verifikation

Automatisiert:

- `dotnet build`
- `dotnet test`

Manuell im Browser:

- Desktopbreite:
  - `Start` nicht sichtbar.
  - Brand `Rezepte` fuehrt nach `/`.
  - Settings zeigt nur Zahnrad.
  - Benutzerlogo oeffnet Dropdown rechts.
  - Keine separate Begruessung und kein separater Logout-Link in der Navbar.
- Mobile Breite:
  - Toggler oeffnet Navigation.
  - Keine horizontalen Scrollbalken durch Navbar.
  - Suchformular bleibt bedienbar.
  - Benutzerlogo/Dropdown ist per Touch bedienbar.
  - Logout im Dropdown funktioniert.

## Rueckfalloptionen

Falls Bootstrap-Dropdown im Blazor-Kontext nicht verlaesslich arbeitet, kann das Menue alternativ mit Blazor-State umgesetzt werden:

- `bool userMenuOpen`
- Button toggelt Zustand.
- Menue wird bedingt gerendert.
- Klick ausserhalb waere dann zusaetzlich zu behandeln, falls gefordert.

Da Bootstrap Bundle bereits geladen ist, sollte zuerst die Bootstrap-native Variante versucht werden.
