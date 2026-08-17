# Tasks: Autorisierungsproblem in Windows

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Logik | Duplikat `Rezepte.Web/ApiAuthHandler.cs` löschen | Erledigt | Build erfolgreich |
| 2 | Logik | `CircuitAuthHandler` in `Rezepte.Web/Services` anlegen | Erledigt | `CircuitAuthHandlerTests` |
| 3 | Konfiguration | `ApiClient`-Registrierung in `ServiceCollectionExtensions` auf scoped Custom Factory umstellen | Erledigt | `CircuitAuthHandlerTests` / Build |
| 4 | Logik | `ApiClient` optional `IDisposable` machen | Erledigt | Build erfolgreich |
| 5 | Logik | Quick-Fix in `UserProfileViewModel` rückgängig machen | Erledigt | Build erfolgreich |
| 6 | Tests | `CircuitAuthHandler`-Unit-Tests mit Fake-Provider anlegen | Erledigt | `CircuitAuthHandlerTests` (4 bestanden) |
| 7 | Tests | Build und Testlauf prüfen | Erledigt | 360/360 Tests bestanden |
| 8 | Dokumentation | `Docs/help/`-Eintrag zum Auth-Problem anlegen | Offen | — |
| 9 | Dokumentation | `README.md` aktualisieren, falls notwendig | Offen | — |
