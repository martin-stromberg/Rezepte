# Kalenderintegration

## Ist-Zustand

- `Rezepte.Web/Components/Pages/RecipePage.razor:24` bietet die Aktion "Termin planen".
- `Rezepte.Web/Components/Pages/RecipePage.razor:246` bereitet fuer diese Aktion ein `CalendarEventDto` mit dem aktuellen Rezept vor.
- `Rezepte.Web/Components/Shared/CalendarEventDialog.razor:230` nimmt ein optionales `InitialEvent` entgegen.
- `Rezepte.Web/Components/Shared/CalendarEventDialog.razor:265` bis `Rezepte.Web/Components/Shared/CalendarEventDialog.razor:273` uebernimmt Initialwerte in das Formular.
- `Rezepte.Web/Components/Shared/CalendarEventDialog.razor:322` speichert das Formular.
- `Rezepte.Web/Components/Shared/CalendarEventDialog.razor:322` bis `Rezepte.Web/Components/Shared/CalendarEventDialog.razor:339` sendet bei neuen Terminen genau einen `POST /api/calendar`.
- `Rezepte.Web/Controllers/CalendarController.cs:43` nimmt neue Kalendereintraege entgegen.
- `Rezepte.Web/Services/CalendarService.cs:37` erstellt einen einzelnen `CalendarEvent` und validiert das Rezept ueber `IRecipeService.GetByIdAsync`.
- `Rezepte.Web/Components/Pages/Calendar.razor:178` laedt Monatstermine; Rezept-Previews werden separat ueber `GET /api/recipes/{id}` geladen.

## Luecke zur Anforderung

Beim Einfuegen eines Rezepts in den Kalender werden keine verlinkten Beilagen vorgeschlagen. Der Dialog hat keinen Zustand fuer mehrere vorgeschlagene Rezepte und keine Logik, ausgewaehlte Beilagen als zusaetzliche Termine anzulegen.

## Naheliegende Anpassungen

- Beim Setzen oder Aendern von `form.RecipeId` Beilagen fuer dieses Rezept laden.
- Im Dialog eine Liste vorgeschlagener Beilagen anzeigen, jede einzeln abwaehlbar/auswaehlbar.
- Beim Speichern neuer Termine zusaetzlich fuer jede ausgewaehlte Beilage einen Kalendertermin mit demselben Datum, derselben Tageszeit, Wiederholung und passenden Portionen anlegen.
- Die vorhandene API kann dafuer mehrfach clientseitig aufgerufen werden. Alternativ kann ein Batch-Endpunkt eingefuehrt werden, wenn atomare Erstellung aller Termine gefordert ist.
- Bei Bearbeitung bestehender Termine sollte die Beilagenvorschlagslogik vorsichtig behandelt werden. Die Anforderung spricht vom Einfuegen eines Rezepts, nicht zwingend vom Bearbeiten bestehender Termine.

## Risiken

- Mehrere clientseitige `POST`-Aufrufe sind nicht atomar. Ein Teilfehler kann Hauptrezept ohne einige Beilagen speichern.
- Wiederholungen (`Recurrence`, `RecurrenceDays`) werden schon im Event gespeichert und sollten fuer Beilagen konsistent uebernommen werden.
- Der Dialog kann aus `Calendar.razor` ohne vorgewaehltes Rezept und aus `RecipePage.razor` mit vorgewaehltem Rezept geoeffnet werden; beide Einstiegspunkte muessen funktionieren.
