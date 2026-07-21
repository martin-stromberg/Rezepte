# Umsetzungsrelevante Dateien

## Direkt betroffen

### `Rezepte.Web/Services/Import/ImportOrchestrator.cs`

Erwartete Aenderungen:

- Besitzerfeld in `ImportSession` einfuehren, z. B. `InitiatorUserId`.
- `StartImportAsync` speichert die uebergebene `userId` im Sessionobjekt.
- UserId-aware Zugriffsmethoden fuer Status, Confirm, Selection und Cancel bereitstellen.
- Fremde und unbekannte Sessions ohne Detailpreisgabe behandeln.

### `Rezepte.Web/Controllers/CookbooksController.cs`

Erwartete Aenderungen:

- In allen Status-, Confirm- und Selection-Cancel-Endpunkten `GetUserId()` auswerten.
- Bei fehlender UserId `Unauthorized()` liefern.
- Fuer alle Sessionzugriffe userId-aware Orchestrator-Methoden verwenden.
- Fremde Sessions nicht an `ToSessionStatus` uebergeben.
- Fehlerantworten fuer fremde Sessions generisch halten.

### `Rezepte.Tests/Services/Import/ImportOrchestratorTests.cs`

Erwartete Aenderungen:

- Bestehende Tests an neue Orchestrator-Signaturen anpassen.
- Negativtests fuer fremde UserId bei Status, Confirm, SubmitSelection und CancelSelection ergaenzen.
- Sicherstellen, dass fremde Mutationsversuche Sessionzustand und TaskCompletionSources nicht veraendern.

## Wahrscheinlich nicht direkt betroffen

### `Rezepte.Web/Components/Shared/CreateRecipeDialog.razor`

Der Client ruft die bestehenden Endpunkte auf und transportiert keine Besitzerinformation. Das ist korrekt: die Besitzerinformation muss serverseitig aus der Authentifizierung stammen. Aenderungen sind voraussichtlich nur notwendig, wenn sich Statuscodes oder Fehlertexte so aendern, dass die UI spezifisch darauf reagiert.

### `Rezepte.Web/Extensions/ServiceCollectionExtensions.cs`

Die Singleton-Registrierung des Orchestrators ist fuer die aktuelle In-Memory-Sessionverwaltung relevant, aber nicht zwingend zu aendern. Die Besitzbindung kann innerhalb des bestehenden Singleton-Orchestrators umgesetzt werden.

### Persistenz und Migrationen

Import-Sessions sind aktuell in-memory und nicht EF-persistent. Es ist voraussichtlich keine Datenbankmigration erforderlich.

## Dokumentation

Nach Umsetzung sollte geprueft werden, ob `Docs/help/import-plugins.md` oder README-Abschnitte zum Importablauf angepasst werden muessen. Fuer die reine Sicherheitskorrektur ist fachliche Benutzerdokumentation wahrscheinlich nur minimal betroffen.
