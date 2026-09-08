# Benutzerkonten

## Passwort-Policy

Passwörter werden serverseitig mit PBKDF2-HMAC-SHA256 gehasht gespeichert. Das gespeicherte Format ist `iterationen.salt.hash` (Salt und Hash hexadezimal kodiert).

Für die Hash-Parameter gelten feste Grenzen (`PasswordHasher`):

- **Aktuelle Iterationszahl:** 210.000 (wird für alle neuen Hashes verwendet)
- **Minimale Iterationszahl:** 100.000 (Hashes mit weniger Iterationen werden bei der Anmeldung abgelehnt)
- **Maximale Iterationszahl:** 1.000.000 (Hashes mit mehr Iterationen werden abgelehnt, um die Verifikationskosten zu begrenzen)
- **Salt-Länge:** 16 Bytes, **Hash-Länge:** 32 Bytes

Bei der Registrierung muss das Passwort mindestens 6 Zeichen lang sein.

### Automatische Aktualisierung (Rehash beim Login)

Wird ein Passwort erfolgreich gegen einen Hash mit veralteten Parametern (weniger als 210.000 Iterationen) geprüft, erzeugt die Anwendung beim Login automatisch einen neuen Hash mit den aktuellen Parametern. Ältere Konten werden so schrittweise auf die aktuelle Policy angehoben, ohne dass eine Aktion durch den Benutzer erforderlich ist.

## Registrierung

Die Registrierung prüft Benutzernamen serverseitig. Ein Benutzername muss 3 bis 20 Zeichen lang sein und darf nur Buchstaben, Zahlen, Unterstrich und Bindestrich enthalten.

Leerzeichen, Emojis, Domains, IP-Adressen und sonstige Sonderzeichen werden abgelehnt. Reservierte oder offiziell wirkende Namen wie `admin`, `root`, `support`, `security_admin` oder appbezogene Namen wie `rezepte` können nicht verwendet werden.

Wenn ein Benutzername nicht erlaubt ist oder bereits vergeben wurde, zeigt die Anwendung eine deutschsprachige Fehlermeldung an. Wählen Sie in diesem Fall einen anderen Namen.

### Demo-Daten anlegen

Auf der Registrierungsseite kann über die Checkbox **„Demo-Daten anlegen"** gewählt werden, ob das neue Konto mit Beispielinhalten vorbefüllt wird. Der Hinweistext erklärt: „Legt automatisch fünf Beispiel-Kochbücher mit Rezepten, Terminen und Einkaufslisteneinträgen an."

Ist die Option aktiviert, stehen nach der ersten Anmeldung bereit:

- **Fünf Kochbücher** („Frühstück", „Abendessen", „Snacks", „Weihnachtszeit", „Grillsaison") mit insgesamt 43 Rezepten.
- **Fünf Kalendereinträge** aus dem Kochbuch „Abendessen" für die nächsten fünf Tage (jeweils um 18:00 Uhr).
- **Einkaufslisteneinträge** mit den Zutaten des Rezepts, das für den nächsten Tag geplant ist.

Das Anlegen erfolgt im Hintergrund und kann einen Moment dauern — die Inhalte erscheinen kurz nach der Anmeldung in den Übersichten. Die Demo-Daten sind ganz normale Einträge: Sie lassen sich wie selbst erfasste Daten bearbeiten oder einzeln löschen. Es gibt keine Funktion, um alle Demo-Daten auf einmal zu entfernen. Ist die Checkbox nicht aktiviert, startet das Konto leer.

#### Technischer Hintergrund

Das Seeding läuft als Hintergrundjob: `UserService.RegisterAsync` enqueut bei `createDemoData == true` einen Job vom Typ `seed-demo-data` (`DemoDataSeedingJobHandler.JobTypeName`) mit einem `DemoDataSeedPayload` (UserId) über `IBackgroundJobQueue`. `BackgroundJobHostedService` führt den `DemoDataSeedingJobHandler` aus, der nacheinander `ICookbookService.CreateAsync`, `IRecipeService.CreateAsync`, `ICalendarService.CreateEventAsync` (erste fünf Rezepte des Kochbuchs `DemoDataSource.CalendarCookbookName` = „Abendessen", `DateTime.Today.AddDays(1..5)`, 18:00 Uhr) sowie `IShoppingListService.EnsureDefaultGroupAsync`/`AddItemAsync` (Zutaten des ersten geplanten Rezepts) aufruft. Schlägt ein Service-Aufruf fehl oder existiert der Benutzer nicht, wirft der Handler eine Exception und der Job wird auf `Failed` gesetzt — ohne automatischen Retry. Die Datensätze stammen aus der statischen Klasse `DemoDataSource` (5 `DemoCookbook`-Einträge mit 43 `DemoRecipe`-Rezepten auf Basis von `RecipeCreateStep`/`RecipeCreateIngredient`).

Die Steuerung erfolgt ausschließlich über das Registrierungsformular bzw. das JSON-Feld `RegisterRequest.CreateDemoData` (`POST api/auth/register`); `AuthController.Register` liest den Wert per `bool.TryParse` aus `Request.Form["createDemoData"]` (unlesbare/fehlende Werte gelten als `false`). Die Benutzeranlage durch Administratoren (`AdminUsersController`) ruft `RegisterAsync` grundsätzlich mit `createDemoData: false` auf. Eine zentrale Konfiguration gibt es nicht.

## Profil

Im Profil kann der eigene Benutzername geändert werden. Für die Änderung gelten dieselben Regeln wie bei der Registrierung.

Nach einer erfolgreichen Änderung kann eine erneute Anmeldung nötig sein, damit der neue Name in der Navigation angezeigt wird.

## Benutzerverwaltung für Administratoren

Administratoren können Benutzer in den Einstellungen unter `Benutzer` anlegen und bearbeiten. Auch dort werden Benutzernamen serverseitig mit denselben Regeln geprüft.

Die Prüfung ersetzt nicht die Eindeutigkeitsprüfung. Ein technisch gültiger Benutzername wird weiterhin abgelehnt, wenn er bereits von einem anderen Benutzer verwendet wird.
