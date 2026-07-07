# Bestandsaufnahme: Gesperrte Benutzernamen

## Zusammenfassung

Die Anwendung ist eine ASP.NET Core/Blazor-Anwendung mit zentralem `IUserService` fuer Benutzeranlage, Login, Profilbearbeitung und Admin-Benutzerverwaltung. Benutzernamen werden derzeit nur minimal validiert: Pflichtfeld/Mindestlaenge 3 in DTOs, ViewModels, Controllern und Service. Eine zentrale Sperrlisten-, Zeichen-, Laengen-, Domain/IP- oder Aehnlichkeitsvalidierung existiert nicht.

Die beste technische Andockstelle fuer die neue Anforderung ist eine zentrale Username-Validierung im Service-Layer, die von `UserService.RegisterAsync`, `UserService.UpdateProfileAsync` und `UserService.UpdateUserAsync` aufgerufen wird. Admin-Erstellung nutzt bereits `RegisterAsync`; Admin-Bearbeitung und Profilbearbeitung laufen ebenfalls ueber `UserService`. Controller- und UI-Vorpruefungen sollten entweder auf grobe Pflichtfeldpruefung reduziert oder an dieselben Fehlermeldungen angepasst werden, damit keine abweichenden Regeln entstehen.

## Detaildokumente

- [Benutzerfluesse und Einstiegspunkte](inventory/user-flows.md)
- [Aktueller Validierungsstand](inventory/current-validation.md)
- [Datenmodell und Dependency Injection](inventory/data-model-and-di.md)
- [Testbestand und Testbedarf](inventory/tests.md)
- [Risiken und offene technische Entscheidungen](inventory/risks.md)

## Betroffene Kernbereiche

| Bereich | Dateien | Relevanz |
|--------|---------|----------|
| Service-Layer | `Rezepte.Web/Services/UserService.cs` | Zentrale Stelle fuer Registrierung, Profil-Update, Admin-Update und Eindeutigkeitspruefung. |
| API Registrierung | `Rezepte.Web/Controllers/AuthController.cs`, `Rezepte.Web/Contracts/AuthDtos.cs` | Oeffentliche Registrierung via Formular und JSON. |
| API Admin | `Rezepte.Web/Controllers/AdminUsersController.cs` | Admin-Erstellung und Admin-Bearbeitung von Benutzernamen. |
| API Profil | `Rezepte.Web/Controllers/UsersController.cs`, `Rezepte.Web/Contracts/UserDtos.cs` | Authentifizierte Aenderung des eigenen Benutzernamens. |
| Blazor UI/ViewModels | `Rezepte.Web/Components/Pages/Register.razor`, `Rezepte.Web/ViewModels/UserAdminViewModel.cs`, `Rezepte.Web/ViewModels/UserProfileViewModel.cs` | Anzeige/Weitergabe von Fehlern, teils eigene Vorvalidierung. |
| Datenmodell | `Rezepte.Web/Data/RezepteDbContext.cs`, `Rezepte.Web/Entities/User.cs` | Aktuell DB-MaxLength 64 und eindeutiger Index auf `Username`; fachlich gefordert sind 3 bis 20 Zeichen. |
| Tests | `Rezepte.Tests/Services/UserServiceTests.cs` | Vorhandene UserService-Tests sind naheliegender Ort fuer zentrale Validierungsregeln. |

## Ist-Zustand gegen Anforderung

| Anforderung | Ist-Zustand | Konsequenz |
|------------|-------------|------------|
| Zentrale serverseitige Validierung | Teilweise zentral ueber `UserService`, aber Regeln sind dupliziert und unvollstaendig. | Neue Validierung sollte in eigener zentraler Komponente liegen und vom `UserService` genutzt werden. |
| Laenge 3 bis 20 | Mindestlaenge 3 teilweise vorhanden; DB erlaubt 64; keine Maximalregel 20. | Service-Validierung und ggf. DB-Konfiguration/Migration angleichen. |
| Erlaubte Zeichen | Keine zentrale Zeichenregel. | Regex/Parser fuer `A-Z`, `a-z`, `0-9`, `_`, `-` noetig. |
| Reservierte Namen | Nicht vorhanden. | Wartbare Sperrliste benoetigt. |
| App-/Domainnamen | Nicht vorhanden. | Sperrliste bzw. Muster benoetigt. |
| IP-/Domainmuster | Durch geforderte Zeichenregel waeren Punkte ohnehin ungueltig; explizite Fehlermeldung/Muster kann trotzdem sinnvoll sein. | Reihenfolge der Validierung planen. |
| Support-/Security-Muster | Nicht vorhanden. | Muster-/Substring-Pruefung noetig, ohne normale Namen zu stark zu blockieren. |
| Missbrauchssperrliste | Nicht vorhanden. | Erweiterbare Liste mit initial begrenztem Satz. |
| Aehnlichkeitspruefung | Nicht vorhanden. | Kleine Normalisierung fuer Leetspeak plus konservative Distanz-/Exact-Normalized-Pruefung empfehlenswert. |
| Eindeutigkeit | Vorhanden, aber case-sensitive im Service und DB-Index je nach SQLite-Kollation. | Bestehende Pruefung erhalten; Case-Sensitivity fachlich pruefen, aber nicht ungefragt ausweiten. |
| Deutsche Fehlermeldungen | Gemischt deutsch/englisch; Encoding wirkt an mehreren Stellen fehlerhaft dargestellt. | Einheitliche deutsche Service-Fehler sind erforderlich. |

## Empfehlung fuer die Planung

1. Neue Komponente im Web-Projekt einfuehren, z. B. `Services/Validation/UsernameValidator.cs` mit Ergebnisobjekt und wartbaren Listen.
2. `IUsernameValidator` in DI registrieren und in `UserService` injizieren.
3. `RegisterAsync`, `UpdateProfileAsync` und `UpdateUserAsync` vor der Eindeutigkeitspruefung validieren.
4. Admin-Controller-Vorvalidierung entfernen oder so minimieren, dass Service-Fehler durchgereicht werden.
5. Tests zuerst auf Validator-Ebene breit abdecken; anschliessend UserService-Tests fuer Integration und bestehende Eindeutigkeit ergaenzen.

