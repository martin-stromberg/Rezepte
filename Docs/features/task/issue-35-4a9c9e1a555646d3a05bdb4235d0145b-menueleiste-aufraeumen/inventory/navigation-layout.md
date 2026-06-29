# Detail: Navigation und Layout

## Zentrale Komponente

Die komplette Menueleiste ist in `Rezepte.Web/Components/Layout/MainLayout.razor` definiert.

Wichtige Stellen:

| Zeile | Beobachtung |
| --- | --- |
| 5 | Navbar nutzt `navbar navbar-expand-lg navbar-dark app-navbar shadow-sm`. |
| 6 | Inhalt liegt in `.container`, nicht full-width. |
| 7-10 | Markenlink `Rezepte` zeigt bereits auf `/`. |
| 11-14 | Bootstrap-Collapse-Toggler ist fuer mobile Viewports vorhanden. |
| 15-31 | Linker Navigationsbereich mit `Start`, `Kochbuecher`, `Kalender`. |
| 33-37 | Suchformular liegt innerhalb der Collapse-Region zwischen linker und rechter Navigation. |
| 39-63 | Rechte Navigation mit `AuthorizeView`, Begruessung, Logout, Settings/Login. |

## Startseiten-Navigation

Der Markenlink ist bereits funktional:

- `href="/"` auf dem Brand-Anker.
- Icon plus Text `Rezepte`.
- Damit kann der separate `Start`-Menuepunkt entfallen.

Der aktuelle `Start`-Eintrag ist:

- `NavLink href="/" Match="NavLinkMatch.All"` in Zeile 17.

Entfernen dieses Listeneintrags duerfte keine Seitenlogik beeinflussen, weil die Route `/` separat bestehen bleibt und der Markenlink bereits dorthin fuehrt.

## Primaere Navigation

Vorhandene Hauptlinks:

- `/cookbooks` mit Buch-Emoji und Text ab `sm`.
- `/calendar` mit Kalender-Emoji und Text ab `sm`.

Die Icons sind aktuell Emoji. Bootstrap Icons sind projektweit verfuegbar, daher koennte die Menueleiste konsistenter auf `bi-*`-Icons umgestellt werden. Fuer die Anforderung ist das nicht zwingend, aber beim Settings-Zahnrad sinnvoll.

## Suchformular

Das Suchformular liegt direkt in der Navbar:

- `form class="d-flex ms-3 me-2"`
- `InputText` mit `form-control-sm`
- Submit-Button mit Suchsymbol
- Handler `OnSubmitSearch` navigiert nach `/recipes` oder `/recipes/search?q=...`

Mobile Relevanz:

- `d-flex` haelt Eingabe und Button nebeneinander.
- `ms-3` kann in der eingeklappten mobilen Navigation unnoetigen linken Abstand verursachen.
- Es gibt keine explizite Regel wie `w-100`, `mt-2`, `my-2` oder responsive Margin-Klassen fuer kleine Viewports.

Fuer die mobile Anforderung sollte das Formular bei kleinen Viewports kontrolliert in die volle Breite gehen oder zumindest nicht horizontal druecken.

## Rechte Navigation

Der rechte Bereich ist aktuell in `ul.navbar-nav.ms-auto` enthalten. Im authentifizierten Zustand belegt er Platz durch:

- Textknoten `Hallo, ...`
- Buttontext `Abmelden`
- Settings-Link `⚙️ Einrichtung`

Das ist der primaere Angriffspunkt fuer die geforderte Kompaktierung.

## Erwartete Aenderungsrichtung

Naheliegende Struktur:

- Linke Navigation: `Start` entfernen, Kochbuecher/Kalender behalten.
- Suche: mobile Klassen ergaenzen.
- Rechte Navigation:
  - Settings als icon-only Link.
  - Benutzerlogo als `button.nav-link.dropdown-toggle` oder `button.btn`.
  - Dropdown-Menue als `ul.dropdown-menu.dropdown-menu-end`.
  - Logout-Formular innerhalb des Dropdowns.

Bootstrap-konformes Dropdown-Markup ist wegen `bootstrap.bundle.min.js` verfuegbar.

## Auswirkungen

Keine erwarteten Auswirkungen auf:

- Routing der Startseite.
- Kochbuch-/Kalenderseiten.
- Suchlogik.
- Settings-Seite.

Moegliche UI-Auswirkungen:

- Active-State fuer Startseite faellt weg, weil es keinen `Start`-NavLink mehr gibt.
- Brand-Link ist kein `NavLink`; er zeigt daher keinen aktiven Zustand. Das ist fuer die Anforderung akzeptabel, solange er als Startseitenlink erkennbar bleibt.
