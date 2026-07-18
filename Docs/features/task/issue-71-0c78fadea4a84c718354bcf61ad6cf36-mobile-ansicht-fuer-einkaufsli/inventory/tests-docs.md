# Detail: Tests und Dokumentation

## Bestehende Tests

`Rezepte.Tests/Services/ShoppingListServiceTests.cs` deckt Serviceverhalten ab:

- Standardgruppe wird bei leerer Liste erzeugt.
- Eintraege werden mit Menge, Einheit und Name angelegt.
- Abhakstatus wird persistiert.
- Rezeptzutaten und Beilagenzutaten werden in Einkaufslistengruppen uebernommen.
- Fremde Gruppen und ungueltige Rezeptauswahlen werden abgelehnt.

`Rezepte.Tests/Services/SettingsServiceTests.cs` prueft, dass der Einkaufslisten-Bearbeitungsmodus pro Nutzer gespeichert wird.

Es gibt im Testprojekt keine erkennbare Komponenten-Testinfrastruktur wie bUnit und keine bestehenden Layout-/Viewporttests fuer Blazor-Komponenten.

## Sinnvolle Testabdeckung fuer die Umsetzung

Minimal:

- Bestehende Tests ausfuehren: `dotnet test Rezepte.sln`.
- Komponente builden: implizit durch `dotnet test` oder explizit `dotnet build Rezepte.sln`.

Empfohlen fuer die konkrete Anforderung:

- Parser fuer kombiniertes Mengen-/Einheitsfeld als testbare Hilfsmethode abdecken, sofern er aus der Komponente herausloesbar oder intern sinnvoll testbar gemacht wird.
- Manuelle oder browserautomatisierte mobile Pruefung der Seite `/shopping-list` mit schmalem Viewport.
- Layoutkriterium pruefen: `document.documentElement.scrollWidth <= window.innerWidth`.
- Sichtbarkeitskriterien pruefen: Im Bearbeitungsmodus keine Checkbox pro Eintrag, Modusbuttons ohne sichtbaren Text, aber mit zugaenglichen Labels.

## Dokumentation

`Docs/help/shopping-list.md` beschreibt aktuell:

- Eingabefelder fuer Menge, Einheit und Zutat.
- Zutaten koennen abgehakt, bearbeitet oder geloescht werden.

Nach Umsetzung sollte die Hilfe geprueft und bei Bedarf angepasst werden, weil Menge und Einheit im Bearbeitungsmodus nicht mehr getrennt erscheinen. `README.md` verweist nur allgemein auf die Einkaufsliste und braucht voraussichtlich keine inhaltliche Aenderung.

## Offene Testluecke

Die zentrale Anforderung ist visuell/responsiv. Ohne Browser- oder Screenshotpruefung kann `dotnet test` nicht absichern, dass kein horizontales Scrollen entsteht oder die drei Elemente mobil wirklich nebeneinander stehen.
