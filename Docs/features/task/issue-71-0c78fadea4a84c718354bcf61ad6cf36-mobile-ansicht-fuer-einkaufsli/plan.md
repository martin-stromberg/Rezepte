# Umsetzungsplan - Mobile Ansicht fuer Einkaufsliste

## Zielbild

Die Seite `/shopping-list` bleibt fachlich unveraendert, wird im Bearbeitungsmodus auf mobilen Viewports aber als kompakte Eintragszeile bedienbar. Pro bestehendem Einkaufslisten-Eintrag stehen dort nur noch ein kombiniertes Feld fuer Menge und Einheit, das Zutatenfeld und der Loeschen-Button nebeneinander. Die Abhaken-Checkbox erscheint ausschliesslich im Ansichtsmodus. Der Wechsel zwischen Ansicht und Bearbeitung erfolgt ueber Symbol-Buttons mit zugaenglichen Labels.

## Betroffene Dateien

| Datei | Geplante Aenderung |
|-------|--------------------|
| `Rezepte.Web/Components/Pages/ShoppingList.razor` | Markup fuer Modusbuttons und Eintragszeilen anpassen, kombiniertes Mengen-/Einheitsfeld einfuehren, Parser-/Formatter-Logik ergaenzen. |
| `Rezepte.Web/Components/Pages/ShoppingList.razor.css` | Eigene Layoutregeln fuer Bearbeitungszeilen ergaenzen und mobile Grid-Regeln gegen horizontales Scrollen absichern. |
| `Docs/help/shopping-list.md` | Hilfe auf das kombinierte Mengen-/Einheitsfeld im Bearbeitungsmodus pruefen und bei Bedarf aktualisieren. |
| `README.md` | Voraussichtlich keine inhaltliche Aenderung noetig; im Doku-Schritt pruefen. |

Nicht geplant sind Aenderungen an `ShoppingListItem`, `IShoppingListService`, `ShoppingListService`, `SettingsService` oder Datenbankmigrationen.

## Implementierungsschritte

1. Modusbuttons in `ShoppingList.razor` auf Symbol-only umstellen.
   - Fuer den Ansichtsmodus ein passendes Bootstrap-Icon verwenden, z. B. `bi-eye`.
   - Fuer den Bearbeitungsmodus ein passendes Bootstrap-Icon verwenden, z. B. `bi-pencil`.
   - Sichtbaren Text `Ansicht` und `Bearbeiten` entfernen.
   - `aria-label`, `title` und `aria-pressed` setzen bzw. erhalten, damit die Buttons trotz Symbol-only bedienbar bleiben.
   - Die bestehende `SetEditModeAsync`-Logik unveraendert weiterverwenden.

2. Eintragszeilen im Bearbeitungsmodus eindeutig markieren und Checkbox entfernen.
   - Die Checkbox nur im Ansichtsmodus rendern.
   - Die Eintragszeile je nach Modus mit einer stabilen Klasse versehen, z. B. `shopping-item-edit` fuer Bearbeitung und `shopping-item-view` fuer Ansicht.
   - Im Ansichtsmodus die bisherige Darstellung mit Checkbox und formatiertem Text erhalten.
   - Im Bearbeitungsmodus nur kombiniertes Mengenfeld, Zutatenfeld und Loeschen-Button rendern.

3. Kombiniertes Mengen-/Einheitsfeld fuer bestehende Eintraege einfuehren.
   - Die getrennten Inputs `shopping-item-amount` und `shopping-item-unit` durch ein einzelnes Textfeld ersetzen, z. B. Klasse `shopping-item-quantity`.
   - Anzeigenwert aus `FormatAmount(item.Amount)` und `item.Unit` bilden, analog zur bestehenden Anzeige ohne Zutatenname.
   - `aria-label` auf `Menge und Einheit` setzen.
   - Bei Aenderung eine neue Update-Methode aufrufen, die den kombinierten Text in `amount` und `unit` zerlegt und danach den vorhandenen `UpdateItemAsync` nutzt.

4. Parser fuer kombiniertes Mengen-/Einheitsfeld implementieren.
   - Eingabe trimmen.
   - Leere Eingabe ergibt `amount = 0` und `unit = null` oder leerer String gemaess bestehendem Serviceverhalten.
   - Der erste tokenartige numerische Anteil wird als Menge interpretiert; der Rest wird als Einheit gespeichert.
   - Dezimalwerte mit Punkt oder Komma weiter unterstuetzen, indem die bestehende `ParseAmount`-Logik fuer InvariantCulture und CurrentCulture wiederverwendet wird.
   - Beispiele: `2 kg` -> `2` / `kg`, `0.5 l` -> `0.5` / `l`, `2,5 kg` -> `2.5` / `kg`, `2` -> `2` / leer.
   - Wenn kein numerischer Anfang erkannt wird, Menge auf `0` setzen und den gesamten Text als Einheit behandeln, damit keine negativen oder ungueltigen Mengen entstehen.
   - Negative Zahlen nicht als negative Menge persistieren; bei Bedarf auf `0` begrenzen, passend zur Servicevalidierung `amount >= 0`.

5. CSS fuer Ansicht und Bearbeitung trennen.
   - Desktop-Grid fuer `shopping-item-edit` auf drei Spalten reduzieren: kombiniertes Mengenfeld, Zutatenfeld, Button.
   - View-Grid fuer `shopping-item-view` explizit auf Checkbox plus Text setzen.
   - Nicht mehr auf `:has(.shopping-item-text)` als primaeren Modus-Selektor angewiesen sein; hoechstens als Fallback bestehen lassen.
   - Inputs mit `min-width: 0` absichern, damit lange Inhalte innerhalb der Grid-Spalten schrumpfen koennen.

6. Mobile Layoutregeln gezielt fuer Bearbeitungszeilen anpassen.
   - Am Breakpoint `max-width: 575.98px` fuer `shopping-item-edit` drei Spalten definieren, z. B. `minmax(4.5rem, 0.45fr) minmax(0, 1fr) auto`.
   - Die bestehende Regel, die alle `.shopping-item .form-control` auf `grid-column: 1 / -1` setzt, entfernen oder so einschraenken, dass sie nicht fuer `shopping-item-edit` gilt.
   - Loeschen-Button in Spalte 3 halten und mit stabiler Breite behandeln.
   - Sicherstellen, dass lange Zutaten- oder Einheitentexte kein horizontales Scrollen erzeugen.

7. Hinzufuegeformular regressionsarm behandeln.
   - Das Hinzufuegeformular ist nicht Kern der Anforderung, darf aber mobil weiterhin kein horizontales Scrollen ausloesen.
   - Bestehende Felder fuer neue Eintraege koennen getrennt bleiben, sofern sie nicht gegen Akzeptanzkriterien fuer bestehende Bearbeitungszeilen verstossen.
   - Mobile CSS fuer `.shopping-add-row` bei Bedarf separat lassen, damit Aenderungen an `.shopping-item-edit` nicht unbeabsichtigt das Formular veraendern.

8. Dokumentation aktualisieren.
   - `Docs/help/shopping-list.md` auf Aussagen zu getrennten Feldern fuer Menge und Einheit pruefen.
   - Falls die Hilfe den Bearbeitungsmodus beschreibt, auf das kombinierte Mengen-/Einheitsfeld anpassen.
   - `README.md` nur aendern, wenn dort konkrete Bedienhinweise zur Einkaufsliste stehen.

## Testplan

1. Build und automatisierte Tests ausfuehren:
   - `dotnet test Rezepte.sln`

2. Parser manuell oder automatisiert gegen die geplanten Beispiele pruefen:
   - `2 kg`
   - `0.5 l`
   - `2,5 kg`
   - `2`
   - leere Eingabe
   - nicht numerischer Text

3. Mobile Layoutpruefung im Browser mit schmalem Viewport, mindestens 375 px Breite:
   - Seite `/shopping-list` oeffnen.
   - In den Bearbeitungsmodus wechseln.
   - Pro Eintrag pruefen, dass kombiniertes Mengenfeld, Zutatenfeld und Loeschen-Button in einer Zeile stehen.
   - Pruefen, dass keine Checkbox in Bearbeitungszeilen sichtbar ist.
   - Pruefen, dass die Modusbuttons keinen sichtbaren Text, aber sinnvolle `aria-label`/`title`-Werte haben.
   - In der Browserkonsole validieren: `document.documentElement.scrollWidth <= window.innerWidth`.

4. Desktop-Regression kurz pruefen:
   - Ansichtsmodus zeigt Checkbox und formatierten Eintrag.
   - Bearbeitungsmodus erlaubt Aendern und Loeschen bestehender Eintraege.
   - Hinzufuegen neuer Eintraege funktioniert weiterhin.

## Akzeptanzkriterien-Abdeckung

| Kriterium | Abdeckung im Plan |
|-----------|-------------------|
| Eintragsfelder und Loeschen-Button sind mobil klar gruppiert | Eigene `shopping-item-edit`-Zeile mit drei Elementen und gezieltem mobilen Grid. |
| Menge und Einheit nicht mehr getrennt | Ein kombiniertes Textfeld ersetzt getrennte Bearbeitungsinputs fuer bestehende Eintraege. |
| Mengenfeld, Zutatenfeld und Loeschen-Button stehen nebeneinander | Mobile Grid-Regel mit drei Spalten fuer `shopping-item-edit`. |
| Zeile passt in Bildschirmbreite | `minmax(0, ...)`, `min-width: 0` und ScrollWidth-Pruefung. |
| Checkbox im Bearbeitungsmodus nicht sichtbar | Checkbox wird nur im Ansichtsmodus gerendert. |
| Symbol-Button fuer Bearbeitungsmodus | Modusbutton `Bearbeiten` wird Icon-only mit zugaenglichem Label. |
| Symbol-Button fuer Ansichtsmodus | Modusbutton `Ansicht` wird Icon-only mit zugaenglichem Label. |
| Keine horizontale mobile Scrollbarkeit | CSS-Anpassung plus Browserpruefung `scrollWidth <= innerWidth`. |

## Risiken und Gegenmassnahmen

| Risiko | Gegenmassnahme |
|--------|----------------|
| Parser trennt ungewohnte Eingaben anders als erwartet. | Einfache, dokumentierte Regel verwenden: erste Zahl ist Menge, Rest ist Einheit; nicht numerische Eingaben werden ohne Fehler abgefangen. |
| Lange Texte erzeugen weiterhin horizontales Scrollen. | Grid-Spalten mit `minmax(0, ...)`, Inputs mit `min-width: 0` und mobile Browserpruefung. |
| Symbol-only Buttons verlieren Bedienbarkeit fuer Screenreader. | `aria-label`, `title` und `aria-pressed` explizit setzen. |
| CSS-Aenderungen beeinflussen Hinzufuegeformular oder Ansichtsmodus. | Modusspezifische Klassen statt breiter `.shopping-item .form-control`-Regeln verwenden. |

## Offene Punkte

Keine.
