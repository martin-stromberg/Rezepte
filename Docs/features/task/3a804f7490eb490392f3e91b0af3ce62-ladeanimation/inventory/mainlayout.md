# MainLayout.razor (bestehend)

Datei: `Rezepte.Web/Components/Layout/MainLayout.razor`

## Beschreibung

Die `MainLayout.razor`-Komponente ist das Haupt-Layout für die gesamte Blazor-Anwendung und dient als Wrapper für alle Seiten. Sie besteht aus:

- **Navigation (Navbar)**: Bootstrap-Navigationsleiste mit Links zu Kochbüchern, Kalender und Einkaufsliste
- **Suchfunktion**: Integrierte Suchbox im NavBar für Rezeptsuche
- **Benutzermenü**: AuthorizeView mit Benutzerinformationen und Logout-Funktion
- **Inhaltsbereich**: `@Body` für den Seiteninhalt
- **Footer**: Fußzeile mit Copyright-Angabe
- **Error-UI**: Blazor-Fehlerbehandlung im Hidden-Div

## Code-Struktur

| Element | Sichtbarkeit | Beschreibung |
|---------|-------------|-------------|
| `searchQuery` | private | String zur Speicherung der Suchanfrage |
| `OnSubmitSearch()` | private | Handler für Suchformular-Submit |

## Injektion

- `NavigationManager Nav` — Für Navigation zwischen Seiten (verwendet in `OnSubmitSearch()`)

## Potentielle Integrationspunkte für LoadingBar

1. **Nach dem `<nav>`-Element**: Die LoadingBar-Komponente könnte hier positioniert werden (nach Zeile 87)
2. **Event-Binding**: Das `NavigationManager.LocationChanged`-Event könnte in der MainLayout oder einer darin eingebundenen LoadingBar-Komponente abonniert werden
3. **Service-Injection**: Ein `ILoadingBarService` könnte injiziert werden zur Kontrolle der Animation

## Abhängigkeiten

- `NavigationManager` (Blazor standard)
- `AuthorizeView` (Blazor Authorization)
- Bootstrap CSS (für Styling)

## Styling

Datei: `Rezepte.Web/Components/Layout/MainLayout.razor.css`

Die scoped CSS-Datei definiert:
- Fehlerbehandlungs-UI Styling
- NavBar-Suchformular Responsive-Design
- Benutzermenü-Dropdown Styling
- Mobile-Responsiveness für NavBar-Elemente

**Hinweis**: LoadingBar-CSS müsste entweder in diese Datei integriert werden oder als separate `LoadingBar.razor.css` erstellt werden.
