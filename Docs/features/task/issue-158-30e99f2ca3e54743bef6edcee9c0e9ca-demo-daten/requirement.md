### Fachliche Zusammenfassung

Bei der Registrierung kann ein neuer Anwender über eine Checkbox im `Register.razor`-Formular wählen, ob Demo-Daten für sein Konto angelegt werden. Nach erfolgreicher Registrierung wird ein Hintergrundjob über `IBackgroundJobQueue` gestartet, der im Kontext des neuen Benutzers fünf vorbefüllte Kochbücher mit insgesamt 43 Rezepten, fünf Kalendereinträge aus dem Kochbuch „Abendessen" für die nächsten fünf Tage sowie die Zutaten des Rezepts für den nächsten Tag in der Einkaufsliste anlegt. Die Daten sollen über die bestehenden Service-Methoden erzeugt werden, damit sie sich nicht von manuell erfassten Daten unterscheiden.

### Betroffene Klassen und Komponenten

- `Register.razor` – UI-Seite mit neuer Checkbox für Demo-Daten.
- `AuthController` – liest den Demo-Daten-Hinweis aus dem Formular bzw. JSON-Body und reicht ihn an `IUserService.RegisterAsync` weiter.
- `RegisterRequest` / `RegisterRequestForm` – DTOs mit optionalem `CreateDemoData`-Flag.
- `IUserService` / `UserService` – `RegisterAsync` muss das `createDemoData`-Argument entgegennehmen und bei Erfolg einen Hintergrundjob enqueuen.
- `IBackgroundJobQueue` / `BackgroundJob` / `BackgroundJobHostedService` – für das asynchrone Ausführen des Seedings nach der Registrierung.
- `ICookbookService` / `CookbookService` – Anlegen der fünf Kochbücher.
- `IRecipeService` / `RecipeService` – Anlegen der 43 Demo-Rezepte und Zuordnung zu den Kochbüchern.
- `ICalendarService` / `CalendarService` – Anlegen eines Kalendereintrags pro Tag für die nächsten fünf Tage.
- `IShoppingListService` / `ShoppingListService` – Hinzufügen der Zutaten des Rezepts für den nächsten Tag.
- `RezepteDbContext` – Persistenz aller erzeugten Entitäten.
- `Entities`: `User`, `Cookbook`, `Recipe`, `RecipeCookbook`, `CalendarEvent`, `ShoppingListGroup`, `ShoppingListItem`, `RecipeIngredient`, `RecipeStep`.
- Feste Demo-Datenquelle (neue Datei, z. B. statische C#-Klasse oder JSON-Ressource) mit 43 Rezepten, Schritten und Zutaten.
- Neuer `DemoDataSeedingJobHandler` (oder vergleichbare Klasse), der `IBackgroundJobHandler` implementiert und die bestehenden Services aufruft.
- Tests: `UserServiceTests`, ggf. neuer `DemoDataSeedingJobHandlerTests` und E2E-Test für den UI-Registrierungsfluss.

### Implementierungsansatz

1. `Register.razor` erhält eine Checkbox `Demo-Daten anlegen`, deren Wert über das `name`-Attribut an den Form-POST übergeben wird.
2. `AuthController.Register` erweitert das Auslesen um `createDemoData` aus dem Formular oder dem JSON-Body und übergibt es an `IUserService.RegisterAsync`.
3. `RegisterRequest` und `RegisterRequestForm` werden um ein `bool CreateDemoData`-Feld ergänzt.
4. `IUserService.RegisterAsync` nimmt einen zusätzlichen Parameter `bool createDemoData` entgegen. Bei `true` und erfolgreicher Registrierung enqueut es über `IBackgroundJobQueue.EnqueueAsync` einen neuen Job-Typ (z. B. `seed-demo-data`) mit der neuen `UserId` als Payload.
5. `BackgroundJobHostedService` führt den neuen `DemoDataSeedingJobHandler` aus, der mittels `IServiceScopeFactory` ein Scoped-Service-Set erzeugt und die folgenden Aufrufe sequenziell tätigt:
   - `ICookbookService.CreateAsync` für „Frühstück", „Abendessen", „Snacks", „Weihnachtszeit" und „Grillsaison".
   - `IRecipeService.CreateAsync` für alle Demo-Rezepte unter Angabe des jeweiligen `cookbookId`; Rezepte werden aus der statischen Demo-Datenquelle gelesen.
   - `ICalendarService.CreateEventAsync` für jeweils ein Rezept aus dem Kochbuch „Abendessen" an den nächsten fünf Tagen.
   - `IShoppingListService.EnsureDefaultGroupAsync` bzw. `AddItemAsync` für die Zutaten des Rezepts, das am nächsten Tag geplant ist.
6. Der Handler verwendet ausschließlich die bestehenden Service-Methoden, um Owner-`UserId`, Zeitstempel und Validierungen konsistent mit manueller Dateneingabe zu halten.
7. Die 43 Rezepte werden einmalig definiert und als eingebettete Ressource oder statische Klasse in die Assembly kompiliert, sodass sie im Release verfügbar sind.

### Konfiguration

- Verhalten ist benutzerspezifisch über das Registrierungsformular steuerbar (`CreateDemoData`-Flag).
- Keine zentrale Anwendungskonfiguration erforderlich. Optional kann in `appsettings.json` ein Feature-Schalter `DemoData:Enabled` oder `DemoData:SeedOnRegistration` eingeführt werden, um den Hintergrundprozess global abschalten zu können, sofern die Anwendung das betreibbar machen muss.

### Offene Fragen

1. Auswahl der fünf Abendessen-Rezepte für den Kalender: Sollen die ersten fünf Rezepte des Kochbuchs „Abendessen" verwendet werden, oder ist eine zufällige oder feste Zuordnung gewünscht?
2. Auswahl des „Rezepts für den nächsten Tag" für die Einkaufsliste: Soll das Rezept des ersten Kalendereintrags (Tag +1) verwendet werden, und was passiert, wenn an einem Tag mehrere Rezepte geplant sind?
3. Soll die Demo-Daten-Option auch bei der JSON-API-Registrierung (`RegisterRequest`) verfügbar sein, oder ausschließlich im Web-Formular?
4. Was soll bei einem Fehler des Hintergrundjobs passieren (Wiederholung, Benachrichtigung, stilles Loggen)?
5. Sind für die Demo-Rezepte Bilder oder Platzhalter vorgesehen, oder werden ausschließlich Textdaten gespeichert?
6. Sollen die erzeugten Demo-Daten vom Anwender nachträglich als Gruppe löschbar sein, oder einzeln wie reguläre Datensätze?
7. Soll der Hintergrundjob sofort nach der Registrierung laufen oder lediglich in die Warteschlange eingereiht werden, bis der nächste `BackgroundJobHostedService`-Poll ihn abholt?
