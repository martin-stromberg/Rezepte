# Bestandsaufnahme - Mobile Ansicht fuer Einkaufsliste

## Zusammenfassung

Die relevante Oberflaeche ist `Rezepte.Web/Components/Pages/ShoppingList.razor` mit isoliertem CSS in `Rezepte.Web/Components/Pages/ShoppingList.razor.css`. Der Bearbeitungsmodus rendert aktuell pro Eintrag Checkbox, Menge, Einheit, Zutat und Loeschen-Button als Grid. Auf mobilen Viewports werden alle Formularfelder ueber die volle Breite gelegt; dadurch stehen die Felder nicht nebeneinander und die Zuordnung pro Eintrag ist schwer erfassbar.

Die Anforderung kann voraussichtlich ohne Datenbankmigration und ohne Aenderung am `IShoppingListService` umgesetzt werden. Fachlich bleiben `Amount` und `Unit` im Datenmodell getrennt; nur die Bearbeitungsoberflaeche soll Menge und Einheit als gemeinsames Feld anbieten und beim Speichern wieder in die bestehenden Felder ueberfuehren.

## Detaildokumente

- [Oberflaeche und CSS](inventory/frontend.md)
- [Services und Datenmodell](inventory/services-data.md)
- [Tests und Dokumentation](inventory/tests-docs.md)

## Betroffene Dateien

| Datei | Relevanz |
|-------|----------|
| `Rezepte.Web/Components/Pages/ShoppingList.razor` | Rendert Umschaltbuttons, Eintragszeilen, Checkbox, Mengen-/Einheits-/Zutatenfelder und Loeschen-Button. |
| `Rezepte.Web/Components/Pages/ShoppingList.razor.css` | Definiert Desktop- und Mobile-Grid der Einkaufsliste; aktueller mobiler Breakpoint erzeugt die problematische Stapelung. |
| `Rezepte.Web/Entities/ShoppingListItem.cs` | Bestehendes Datenmodell mit getrennten Feldern `Amount` und `Unit`; sollte erhalten bleiben. |
| `Rezepte.Web/Services/ShoppingListService.cs` | Persistiert `Amount`, `Unit`, `Name` getrennt; voraussichtlich unveraendert nutzbar. |
| `Rezepte.Web/Services/SettingsService.cs` | Persistiert den Bearbeitungsmodus pro Nutzer; relevant fuer Umschaltbuttons, aber keine fachliche Aenderung noetig. |
| `Rezepte.Tests/Services/ShoppingListServiceTests.cs` | Bestehende Serviceabdeckung; keine Komponenten-/Layouttests vorhanden. |
| `Docs/help/shopping-list.md` | Nutzerhilfe beschreibt getrennte Eingabefelder fuer Menge, Einheit und Zutat und muss bei UI-Aenderung geprueft werden. |

## Aktuelles Verhalten

- Die Modusauswahl ist ein Bootstrap-Button-Group mit sichtbaren Texten `Ansicht` und `Bearbeiten`.
- Im Bearbeitungsmodus wird die Checkbox weiterhin vor den Eingabefeldern gerendert.
- Menge und Einheit sind separate Inputs und werden ueber eigene Handler gespeichert.
- Auf Mobile setzt die CSS-Regel `@media (max-width: 575.98px)` alle `.form-control` in `.shopping-item` auf `grid-column: 1 / -1`; damit stapeln sich Menge, Einheit und Zutat.
- Die mobile Grid-Regel nutzt fuer `.shopping-item` drei Spalten `auto minmax(0, 1fr) auto`, waehrend das Markup im Bearbeitungsmodus aktuell fuenf Elemente enthaelt.

## Planungshinweise

- Checkbox im Markup bedingt rendern: nur im Ansichtsmodus anzeigen, damit der Bearbeitungsmodus exakt die geforderten Elemente enthaelt.
- Ein kombiniertes Mengenfeld benoetigt Parser-/Formatter-Logik in der Komponente. Plausible Annahme: Eingaben wie `2 kg`, `0.5 l`, `2,5 kg` oder nur `2` sollen in `Amount` und `Unit` zurueckgefuehrt werden; unklare Texte sollten nicht zu negativem Amount fuehren.
- Der vorhandene `UpdateItemAsync` kann weiter als zentrale Persistenzmethode verwendet werden.
- Fuer Symbol-Buttons sind Bootstrap Icons bereits global eingebunden; vorhandene Icons werden im Projekt vielfach genutzt.
- CSS sollte fuer den Bearbeitungsmodus eine eigene Klasse oder einen eindeutigen Selektor erhalten, damit Ansichtsmodus und Hinzufuegeformular nicht versehentlich regressieren.
- Mobile Verifikation sollte mindestens einen schmalen Viewport pruefen, etwa 375px Breite, und `document.documentElement.scrollWidth <= window.innerWidth` validieren.

## Risiken

- Das Zusammenfuehren von Menge und Einheit ist UI-seitig einfach, aber Parsing kann bestehende Nutzereingaben beeinflussen. Der Plan sollte klare Regeln fuer den kombinierten Text festlegen.
- Sehr lange Zutaten- oder Einheitentexte koennen trotz Grid `minmax(0, 1fr)` horizontales Scrollen verursachen, wenn Inputs/Button-Mindestbreiten nicht begrenzt werden.
- Sichtbarer Text in den Modusbuttons muss entfernt werden, aber zugaengliche Namen muessen ueber `aria-label`/`title` erhalten bleiben.
- Falls keine Komponenten-Testinfrastruktur eingefuehrt werden soll, bleibt die wichtigste Absicherung eine manuelle oder Playwright-basierte Layoutpruefung.

## Annahmen

- Die Anforderung betrifft nur die bestehende Seite `/shopping-list` und nicht den Dialog zum Uebernehmen von Rezeptzutaten.
- Das Hinzufuegeformular ist nicht explizit Teil der Akzeptanzkriterien; es sollte aber nicht weiterhin horizontales Scrollen verursachen.
- Datenmodell und Datenbank bleiben unveraendert, weil Menge und Einheit nur in der Bearbeitungsansicht gemeinsam dargestellt werden sollen.
