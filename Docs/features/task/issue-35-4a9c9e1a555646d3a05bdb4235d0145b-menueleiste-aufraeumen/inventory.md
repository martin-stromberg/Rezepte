# Bestandsaufnahme: Menueleiste aufraeumen

## Zusammenfassung

Die Menueleiste ist zentral in `Rezepte.Web/Components/Layout/MainLayout.razor` umgesetzt. Sie nutzt Bootstrap 5 (`navbar`, `navbar-expand-lg`, `collapse`) und ist bereits grundsaetzlich fuer mobile Viewports einklappbar. Die konkrete Anforderung kann voraussichtlich mit eng begrenzten Aenderungen an dieser Layout-Komponente und ergaenzenden Styles umgesetzt werden.

Kein Datenmodell, keine Migration und keine neue Backend-Funktion sind zwingend erforderlich. Benutzername und Rolle liegen bereits als Claims im `AuthorizeView` vor; der vorhandene Logout-POST kann in ein Benutzer-Dropdown verschoben werden.

## Detaildokumente

- [Navigation und Layout](inventory/navigation-layout.md)
- [Benutzerbereich und Authentifizierung](inventory/auth-user-menu.md)
- [Responsiveness, Styles und Tests](inventory/responsiveness-tests.md)

## Relevante Dateien

| Datei | Relevanz |
| --- | --- |
| `Rezepte.Web/Components/Layout/MainLayout.razor` | Zentrale Menueleiste, Suchformular, sichtbare Begruessung, Logout-Link, Settings-Link. |
| `Rezepte.Web/wwwroot/app.css` | Globale Navbar-Farben und Link-Zustaende. |
| `Rezepte.Web/Components/Layout/MainLayout.razor.css` | Layout-spezifisches CSS; aktuell ohne Navbar-spezifische Regeln ausser allgemeinen Layout-/Auth-Styles. |
| `Rezepte.Web/Components/App.razor` | Bootstrap CSS, Bootstrap Icons und Bootstrap Bundle werden eingebunden. |
| `Rezepte.Web/Controllers/SessionController.cs` | Login/Logout-Endpunkte und Claims fuer den angemeldeten Benutzer. |
| `Rezepte.Web/Entities/User.cs` | Verfuegbare Benutzerdaten: `Id`, `Username`, `Email`, `IsAdmin`, `CreatedAt`. |
| `Rezepte.Web/Components/Pages/Settings.razor` | Zielseite des Einrichtungs-Menuepunkts `/settings`. |
| `Rezepte.Tests/Rezepte.Tests.csproj` | Testprojekt enthaelt Unit-Test-Stack, aber keine UI-/bUnit-/Playwright-Abhaengigkeiten. |

## Ist-Zustand zur Anforderung

### Mobile Navigation

Die Menueleiste verwendet `navbar-expand-lg`; unterhalb des `lg`-Breakpoints wird sie per Bootstrap-Toggler eingeklappt. Der Toggler verweist auf `#mainNavbar`. Die Suchleiste und der rechte Benutzerbereich liegen innerhalb des einklappbaren Bereichs.

Risiko: Das Suchformular verwendet `d-flex ms-3 me-2` ohne mobile Breitenregel. In der eingeklappten mobilen Navigation kann das bei schmalen Viewports unguenstig wirken oder horizontalen Druck erzeugen.

### Startseite

Der Markenlink `Rezepte` zeigt bereits auf `/` und ist damit als Startseitenlink vorhanden. Zusaetzlich existiert aktuell ein separater `Start`-NavLink, der entfernt werden soll.

### Benutzerbereich

Im authentifizierten Zustand werden aktuell drei sichtbare Elemente gerendert:

- Begruessung `Hallo, <Name>`
- Formularbutton `Abmelden`
- Settings-NavLink mit Zahnrad und Text `Einrichtung`

Diese Elemente sind in `AuthorizeView` gebuendelt und koennen dort in ein kompaktes Benutzermenue und einen icon-only Settings-Link umgebaut werden.

### Einrichtung

Der Settings-Link fuehrt nach `/settings` und zeigt derzeit Zahnrad plus Text. Die Zielseite ist autorisierungspflichtig und bleibt unveraendert.

## Geeigneter Umsetzungsbereich

Die Hauptaenderung sollte in `MainLayout.razor` erfolgen:

- `Start`-NavLink entfernen.
- Brand-Link als erkennbare Startseitennavigation beibehalten.
- Begruessung und Logout-Link aus der Top-Level-Navigation entfernen.
- Benutzerlogo als Dropdown-Trigger im rechten Bereich einfuehren.
- Im Dropdown Benutzerangaben und Logout-Formular anzeigen.
- Settings-Link auf icon-only Darstellung reduzieren, mit `title` und `aria-label`.
- Suchformular und rechte Navigation fuer mobile Viewports stabilisieren.

Ergaenzend sollten Styles in `MainLayout.razor.css` oder `app.css` gesetzt werden, je nachdem ob die Regeln nur das Layout oder die globale Navbar betreffen. Fuer isolierte Header-Anpassungen ist `MainLayout.razor.css` der passendere Ort.

## Vorhandene Bausteine

- Bootstrap 5.3.3 CSS und JS Bundle sind eingebunden.
- Bootstrap Icons sind eingebunden; ein Zahnrad kann als `bi bi-gear-fill` statt Emoji umgesetzt werden.
- Bootstrap-Dropdown ist verfuegbar, weil das Bundle geladen ist.
- `AuthorizeView` liefert Zugriff auf `context.User`.
- Logout per POST-Formular ist bereits vorhanden und kann weiterverwendet werden.

## Offene technische Punkte

- E-Mail ist im aktuellen Cookie-Claim nicht enthalten. Im Popup-Menue sind ohne Backend-Erweiterung sicher verfuegbar: Benutzername, Benutzer-ID und Rolle/Adminstatus aus Claims. Die `User`-Entity hat zwar `Email`, aber diese wird beim Login nicht als Claim gesetzt.
- Es gibt keine vorhandene Komponententest-Infrastruktur fuer Razor-UI. Eine automatisierte Absicherung ist ohne zusaetzliche Testpakete auf Build/Unit-Tests begrenzt.
- Bootstrap-Dropdown in Blazor Server sollte nach Markup-Aenderung manuell im Browser geprueft werden, insbesondere innerhalb der kollabierten mobilen Navbar.

## Risikoabschaetzung

| Risiko | Einschaetzung | Begruendung |
| --- | --- | --- |
| Logout-Verhalten bricht | Niedrig | Das vorhandene POST-Formular kann unveraendert in das Dropdown verschoben werden. |
| Mobile Layoutfehler | Mittel | Suchformular, Dropdown und Navbar-Collapse teilen sich engen Raum. |
| Fehlende Benutzerangaben | Mittel | E-Mail ist nicht im Auth-Claim vorhanden; Anforderung erlaubt Nutzung verfuegbarer Daten. |
| Bootstrap-Interaktion | Niedrig bis mittel | Bootstrap Bundle ist vorhanden, aber Dropdown im Blazor-Layout muss praktisch verifiziert werden. |

## Empfohlene Verifikation nach Umsetzung

- `dotnet build`
- `dotnet test`
- Manuelle Browserpruefung bei Desktopbreite und schmalem Viewport:
  - Kein sichtbarer `Start`-Menuepunkt.
  - Brand-Link navigiert nach `/`.
  - Benutzerlogo oeffnet Dropdown.
  - Dropdown zeigt Benutzerangaben.
  - Logout im Dropdown meldet ab.
  - Settings ist nur als Zahnrad sichtbar und navigiert nach `/settings`.
  - Mobile Navbar klappt ohne Ueberlauf oder Ueberlappung auf.
