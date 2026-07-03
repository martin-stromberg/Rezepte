# Tests und Absicherung

## Vorhandene Tests

- `Rezepte.Tests/Services/RecipeServiceTests.cs` prueft Erstellen, Aktualisieren, Loeschen, Kochbuchabfragen und vorhandene Rezeptzuordnungen.
- `Rezepte.Tests/Services/ShoppingListServiceTests.cs` prueft Standardgruppe, manuelle Eintraege, Abhakstatus und Uebernahme ausgewaehlter Rezeptzutaten.
- Ein `CalendarServiceTests.cs` existiert derzeit nicht.
- Die Tests verwenden EF Core InMemory und FluentAssertions.

## Sinnvolle neue Tests

### Rezeptservice

- Beilagen koennen beim Erstellen eines Rezepts gespeichert werden.
- Beilagen koennen beim Aktualisieren ersetzt, hinzugefuegt und entfernt werden.
- Selbstreferenz wird abgelehnt.
- Beilagen anderer Benutzer werden abgelehnt oder ignoriert, je nach geplanter API-Semantik.
- Duplikate in der Eingabe erzeugen nur eine Beziehung.
- `GetByIdAsync` liefert Beilagen in stabiler Reihenfolge zurueck.

### Einkaufslistenservice

- Gruppierte Zutaten enthalten Hauptrezept und verlinkte Beilagen.
- Hauptrezept-Zutaten sind im Dialogmodell initial ausgewaehlt, Beilagen-Zutaten initial nicht. Falls diese Logik rein in der Komponente bleibt, sollte zumindest der Service die Gruppen mit `IsMainRecipe` liefern.
- Persistieren einer Mehrfachauswahl erzeugt getrennte Gruppen pro Rezept oder eine klar definierte Struktur.
- Zutaten nicht verlinkter Rezepte koennen nicht ueber manipulierte IDs uebernommen werden.

### Kalender

- Wenn die Logik service- oder controllerseitig erweitert wird: ausgewaehlte Beilagen erzeugen zusaetzliche Events mit Datum, Tageszeit, Portionen und Wiederholung.
- Wenn die Logik clientseitig bleibt: zumindest `CalendarService.CreateEventAsync` bestehend weiter absichern und Komponentenlogik manuell/visuell pruefen.

## Testluecken

- Es gibt aktuell keine Komponententests fuer Blazor-Dialoge.
- Kalenderlogik ist nicht durch Unit-Tests abgedeckt.
- EF-Migrationen werden nicht automatisiert gegen SQLite getestet.

## Empfohlene Verifikation nach Umsetzung

- `dotnet test`
- Optional: lokale manuelle Pruefung im Browser fuer Rezeptbearbeitung, Kalenderdialog und Einkaufslistendialog.
