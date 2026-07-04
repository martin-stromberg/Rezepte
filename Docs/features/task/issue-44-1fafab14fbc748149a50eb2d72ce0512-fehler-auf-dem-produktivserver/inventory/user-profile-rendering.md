# Detail: UI- und API-Fluss

## Betroffene Komponente

`Rezepte.Web/Components/Settings/UserProfile.razor` injiziert `UserProfileViewModel` und ruft in `OnInitializedAsync` `Vm.LoadAsync()` auf. Vor dem Aufruf wird `Vm.OnChange += StateHasChanged` registriert; `Dispose()` meldet den Handler wieder ab.

Die Komponente rendert zwei `EditForm`-Bloecke:

- Profilformular mit `Model="@Vm.Profile"`, `InputText` fuer `Vm.Profile.Username` und `Vm.Profile.Email` sowie `ValidationMessage`-Expressions fuer dieselben Properties.
- Passwortformular mit `Model="@Vm.Password"`, drei `InputText`-Feldern fuer `CurrentPassword`, `NewPassword` und `ConfirmPassword` sowie passenden `ValidationMessage`-Expressions.

Der gemeldete Stacktrace passt zu dieser Stelle: `InputText`/`ValidationMessage` nutzen Ausdrucksauswertung ueber Blazor Forms. Wenn dabei `ExpressionFormatter.FormatLambda` eine Runtime-Assembly nicht laden kann, entsteht der Fehler beim Rendern, nicht beim API-Endpunkt.

## ViewModel-Fluss

`Rezepte.Web/ViewModels/UserProfileViewModel.cs` verwaltet zwei Modellinstanzen:

- `Profile` als `ProfileModel`
- `Password` als `PasswordModel`

`LoadAsync` fuehrt `GET api/users/me` aus, liest `UserProfileDto` aus JSON und aktualisiert danach die bestehende `Profile`-Instanz. Die Instanz wird absichtlich nicht ersetzt, damit das Binding bestehen bleibt. Anschliessend wird ueber `Notify()` das Rendering angestossen.

Wichtig fuer die Fehleranalyse: Laut Anforderung ist `GET /api/users/me` bereits erfolgreich. Das spricht gegen einen Controller- oder DTO-Fehler und fuer ein Render-/Runtime-Problem nach dem Laden der Daten.

## API-Kontext

`Rezepte.Web/Controllers/UsersController.cs` stellt folgende JWT-geschuetzte Endpunkte bereit:

- `GET api/users/me` gibt `UserProfileDto` zurueck.
- `PUT api/users/me` aktualisiert Benutzername und E-Mail.
- `POST api/users/me/change-password` aendert das Passwort.

Die DTOs liegen in `Rezepte.Web/Contracts/UserDtos.cs`:

- `UserProfileDto`
- `UpdateProfileRequest`
- `ChangePasswordRequest`

Fuer die gemeldete Exception gibt es im API-/Contract-Code keinen direkten Hinweis auf `System.Runtime.Serialization.Primitives`. Die API ist daher nur Kontext, nicht primaerer Verdacht.

## Abgrenzung

Andere Komponenten nutzen ebenfalls `InputText`, unter anderem Login, Register und Navigation. Die konkrete Profilseite ist aber staerker betroffen, weil sie verschachtelte ViewModel-Properties in `EditForm`, `InputText` und `ValidationMessage` kombiniert und nach einem asynchronen API-Load erneut rendert.

